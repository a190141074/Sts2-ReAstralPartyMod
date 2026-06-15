using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamMap;

public sealed class DreamModeMerchantRoomEnterPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_merchant_room_enter";

    public static string Description => "Restore saved merchant inventory for revisited dream-mode shops";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantRoom), "EnterInternal", [typeof(IRunState), typeof(bool)])];
    }

    public static void Prefix(MerchantRoom __instance, out MerchantInventory? __state)
    {
        __state = __instance.Inventory;
    }

    public static void Postfix(MerchantRoom __instance, IRunState? runState, MerchantInventory? __state)
    {
        if (runState is not RunState concreteRunState)
            return;
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEnabled(concreteRunState))
            return;

        LucidDreamMaliceRuntimeHelper.ResetDreamModeShopCacheIfRunChanged(concreteRunState);
        if (__state != null)
        {
            SetMerchantInventory(__instance, __state);
            RefreshMerchantRoomNode(__instance, concreteRunState);
            return;
        }

        if (!LucidDreamMaliceRuntimeHelper.TryGetDreamModeShopInventory(concreteRunState, out var savedInventory))
            return;

        var localPlayer = LocalContext.GetMe(concreteRunState.Players) ?? concreteRunState.Players.FirstOrDefault();
        if (localPlayer == null)
            return;

        try
        {
            var reconstructed = ReconstructInventory(savedInventory, localPlayer);
            MoonPropShopExtraRelicsHelper.EnsureMoonPropEntries(reconstructed, localPlayer);
            SetMerchantInventory(__instance, reconstructed);
            RefreshMerchantRoomNode(__instance, concreteRunState);
            MainFile.Logger.Info(
                $"[DreamMode] Restored merchant inventory from snapshot at coord=({concreteRunState.CurrentMapCoord?.col},{concreteRunState.CurrentMapCoord?.row}) | entries={savedInventory.Entries.Count}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[DreamMode] Failed to reconstruct saved merchant inventory. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void SetMerchantInventory(MerchantRoom room, MerchantInventory inventory)
    {
        AccessTools.Property(typeof(MerchantRoom), nameof(MerchantRoom.Inventory))?.SetValue(room, inventory);
    }

    private static void RefreshMerchantRoomNode(MerchantRoom room, RunState runState)
    {
        if (NRun.Instance == null)
            return;

        NRun.Instance.SetCurrentRoom(NMerchantRoom.Create(room, runState.Players));
    }

    private static MerchantInventory ReconstructInventory(
        LucidDreamMaliceRuntimeHelper.DreamModeSavedShopInventory saved,
        Player player)
    {
        var inventory = new MerchantInventory(player);
        var characterEntries = AccessTools.Field(typeof(MerchantInventory), "_characterCardEntries")?.GetValue(inventory) as List<MerchantCardEntry>
                               ?? throw new InvalidOperationException("Could not access merchant character entries.");
        var colorlessEntries = AccessTools.Field(typeof(MerchantInventory), "_colorlessCardEntries")?.GetValue(inventory) as List<MerchantCardEntry>
                               ?? throw new InvalidOperationException("Could not access merchant colorless entries.");
        var relicEntries = AccessTools.Field(typeof(MerchantInventory), "_relicEntries")?.GetValue(inventory) as List<MerchantRelicEntry>
                           ?? throw new InvalidOperationException("Could not access merchant relic entries.");
        var potionEntries = AccessTools.Field(typeof(MerchantInventory), "_potionEntries")?.GetValue(inventory) as List<MerchantPotionEntry>
                            ?? throw new InvalidOperationException("Could not access merchant potion entries.");

        foreach (var entry in saved.Entries)
        {
            switch (entry.Type)
            {
                case "CharacterCard":
                    characterEntries.Add(CreateMerchantCardEntry(player, inventory, entry, isColorless: false));
                    break;
                case "ColorlessCard":
                    colorlessEntries.Add(CreateMerchantCardEntry(player, inventory, entry, isColorless: true));
                    break;
                case "Relic":
                    relicEntries.Add(CreateMerchantRelicEntry(player, entry));
                    break;
                case "Potion":
                    potionEntries.Add(CreateMerchantPotionEntry(player, entry));
                    break;
                case "CardRemoval":
                    SetCardRemovalEntry(inventory, player, entry);
                    break;
            }
        }

        var updateEntriesMethod = AccessTools.Method(typeof(MerchantInventory), "UpdateEntries", [typeof(PurchaseStatus), typeof(MerchantEntry)]);
        if (updateEntriesMethod != null)
        {
            var updateEntriesDelegate =
                (Action<PurchaseStatus, MerchantEntry>)Delegate.CreateDelegate(
                    typeof(Action<PurchaseStatus, MerchantEntry>),
                    inventory,
                    updateEntriesMethod);
            foreach (var merchantEntry in inventory.AllEntries)
                merchantEntry.PurchaseCompleted += updateEntriesDelegate;
        }

        return inventory;
    }

    private static MerchantCardEntry CreateMerchantCardEntry(
        Player player,
        MerchantInventory inventory,
        LucidDreamMaliceRuntimeHelper.DreamModeSavedShopEntry savedEntry,
        bool isColorless)
    {
        var merchantEntry = isColorless
            ? new MerchantCardEntry(
                player,
                inventory,
                Array.Empty<CardModel>(),
                Enum.IsDefined(typeof(CardRarity), savedEntry.CardRarityRaw)
                    ? (CardRarity)savedEntry.CardRarityRaw
                    : CardRarity.Uncommon)
            : new MerchantCardEntry(
                player,
                inventory,
                Array.Empty<CardModel>(),
                Enum.IsDefined(typeof(CardType), savedEntry.CardTypeRaw)
                    ? (CardType)savedEntry.CardTypeRaw
                    : CardType.Skill);

        if (!string.IsNullOrWhiteSpace(savedEntry.ModelId))
        {
            var cardModel = ModelDb.GetById<CardModel>(ModelId.Deserialize(savedEntry.ModelId));
            var creationResult = new CardCreationResult(((ICardScope)player.RunState).CreateCard(cardModel, player));
            AccessTools.Property(typeof(MerchantCardEntry), nameof(MerchantCardEntry.CreationResult))?.SetValue(merchantEntry, creationResult);
        }

        AccessTools.Field(typeof(MerchantEntry), "_cost")?.SetValue(merchantEntry, savedEntry.Cost);
        if (savedEntry.IsOnSale)
            AccessTools.Property(typeof(MerchantCardEntry), nameof(MerchantCardEntry.IsOnSale))?.SetValue(merchantEntry, true);
        if (!savedEntry.IsStocked)
            AccessTools.Method(typeof(MerchantCardEntry), "ClearAfterPurchase")?.Invoke(merchantEntry, null);

        return merchantEntry;
    }

    private static MerchantRelicEntry CreateMerchantRelicEntry(
        Player player,
        LucidDreamMaliceRuntimeHelper.DreamModeSavedShopEntry savedEntry)
    {
        var relic = ModelDb.GetById<RelicModel>(ModelId.Deserialize(savedEntry.ModelId!)).ToMutable();
        var merchantEntry = new MerchantRelicEntry(relic, player);
        AccessTools.Field(typeof(MerchantEntry), "_cost")?.SetValue(merchantEntry, savedEntry.Cost);
        if (!savedEntry.IsStocked)
            AccessTools.Method(typeof(MerchantRelicEntry), "ClearAfterPurchase")?.Invoke(merchantEntry, null);

        return merchantEntry;
    }

    private static MerchantPotionEntry CreateMerchantPotionEntry(
        Player player,
        LucidDreamMaliceRuntimeHelper.DreamModeSavedShopEntry savedEntry)
    {
        var potion = ModelDb.GetById<PotionModel>(ModelId.Deserialize(savedEntry.ModelId!)).ToMutable();
        var merchantEntry = new MerchantPotionEntry(potion, player);
        AccessTools.Field(typeof(MerchantEntry), "_cost")?.SetValue(merchantEntry, savedEntry.Cost);
        if (!savedEntry.IsStocked)
            AccessTools.Method(typeof(MerchantPotionEntry), "ClearAfterPurchase")?.Invoke(merchantEntry, null);

        return merchantEntry;
    }

    private static void SetCardRemovalEntry(
        MerchantInventory inventory,
        Player player,
        LucidDreamMaliceRuntimeHelper.DreamModeSavedShopEntry savedEntry)
    {
        var entry = new MerchantCardRemovalEntry(player);
        AccessTools.Field(typeof(MerchantEntry), "_cost")?.SetValue(entry, savedEntry.Cost);
        if (!savedEntry.IsStocked)
            entry.SetUsed();

        AccessTools.Property(typeof(MerchantInventory), nameof(MerchantInventory.CardRemovalEntry))?.SetValue(inventory, entry);
    }
}

public sealed class DreamModeMerchantRoomExitPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_merchant_room_exit";

    public static string Description => "Snapshot merchant inventory on exit so revisited dream-mode shops keep stock";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantRoom), nameof(MerchantRoom.Exit), [typeof(IRunState)])];
    }

    public static void Prefix(MerchantRoom __instance, IRunState? runState)
    {
        if (runState is not RunState concreteRunState)
            return;
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEnabled(concreteRunState))
            return;

        LucidDreamMaliceRuntimeHelper.SaveDreamModeShopInventory(concreteRunState, __instance.Inventory);
    }
}
