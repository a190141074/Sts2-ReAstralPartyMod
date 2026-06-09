using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class MoonPropShopExtraRelicsHelper
{
    private const string ContextId = "moon_prop_shop_extra_relics";
    private const int ExtraRelicCount = 3;
    private const int ColumnsPerRow = 3;
    private const float DefaultRowSpacing = 144f;
    private static readonly ConditionalWeakTable<MerchantInventory, object> PatchedInventories = new();

    private static readonly Func<RelicModel>[] MoonPropRelicFactories =
    [
        static () => ModelDb.Relic<MoonPropFragileCrown>(),
        static () => ModelDb.Relic<MoonPropHellfireTincture>(),
        static () => ModelDb.Relic<MoonPropShapedGlass>(),
        static () => ModelDb.Relic<MoonPropCorpsebloom>(),
        static () => ModelDb.Relic<MoonPropFocusedConvergence>(),
        static () => ModelDb.Relic<MoonPropMercurialRachis>(),
        static () => ModelDb.Relic<MoonPropLightFluxPauldron>(),
        static () => ModelDb.Relic<MoonPropStoneFluxPauldron>()
    ];

    public static void EnsureMoonPropEntries(MerchantInventory? inventory, Player? player)
    {
        if (inventory == null || player == null)
            return;
        if (PatchedInventories.TryGetValue(inventory, out _))
            return;

        for (var slotIndex = 0; slotIndex < ExtraRelicCount; slotIndex++)
        {
            var relic = CreateMoonPropRelicForSlot(player, slotIndex);
            inventory.AddRelicEntry(new MerchantRelicEntry(relic, player));
        }

        PatchedInventories.Add(inventory, new object());
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] Added {ExtraRelicCount} MoonProp merchant relic entries for player {player.NetId} on floor {player.RunState?.TotalFloor ?? -1}.");
    }

    public static void EnsureMoonPropRelicSlots(NMerchantInventory merchantInventory, MerchantInventory inventory)
    {
        if (IsFakeMerchantInventory(merchantInventory))
            return;
        if (merchantInventory.GetNodeOrNull<Control>("%Relics") is not Control relicContainer)
            return;

        var relicSlots = relicContainer.GetChildren().OfType<NMerchantRelic>().ToList();
        if (relicSlots.Count == 0)
            return;

        var rowSpacing = ResolveRowSpacing(merchantInventory, relicContainer);
        while (relicSlots.Count < inventory.RelicEntries.Count)
        {
            var slotIndex = relicSlots.Count;
            var column = slotIndex % ColumnsPerRow;
            var templateIndex = Math.Min(column, Math.Min(ColumnsPerRow, relicSlots.Count) - 1);
            var template = relicSlots[templateIndex];
            var duplicatedNode = template.Duplicate();
            if (duplicatedNode is not NMerchantRelic extraSlot)
            {
                duplicatedNode.QueueFree();
                return;
            }

            extraSlot.Name = $"{template.Name}_MoonPropExtra{slotIndex}";
            extraSlot.Position = GetExtraSlotPosition(relicSlots, templateIndex, slotIndex, rowSpacing);
            relicContainer.AddChild(extraSlot);
            relicSlots.Add(extraSlot);
        }
    }

    private static RelicModel CreateMoonPropRelicForSlot(Player player, int slotIndex)
    {
        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            MoonPropRelicFactories.Length,
            MainFile.ModId,
            ContextId,
            player.RunState?.Rng.StringSeed ?? "<null_seed>",
            player.RunState?.CurrentActIndex ?? -1,
            player.RunState?.TotalFloor ?? -1,
            player.NetId,
            slotIndex);
        return MoonPropRelicFactories[selectedIndex]().ToMutable();
    }

    private static Vector2 GetExtraSlotPosition(
        IReadOnlyList<NMerchantRelic> existingSlots,
        int templateIndex,
        int slotIndex,
        float rowSpacing)
    {
        var template = existingSlots[templateIndex];
        var row = slotIndex / ColumnsPerRow;
        if (row <= 0)
            return template.Position;

        return template.Position + new Vector2(0f, rowSpacing * row * 2f);
    }

    private static float ResolveRowSpacing(NMerchantInventory merchantInventory, Control relicContainer)
    {
        if (merchantInventory.GetNodeOrNull<Control>("%Potions") is Control potionContainer)
        {
            var spacing = Math.Abs(potionContainer.Position.Y - relicContainer.Position.Y);
            if (spacing > 1f)
                return spacing;
        }

        return DefaultRowSpacing;
    }

    private static bool IsFakeMerchantInventory(NMerchantInventory merchantInventory)
    {
        return string.Equals(merchantInventory.GetType().Name, "NFakeMerchantInventory", StringComparison.Ordinal);
    }
}

public sealed class MoonPropShopCreateInventoryPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_create_inventory";

    public static string Description => "Append three extra MoonProp relic entries to normal merchant inventories";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant), [typeof(Player)])];
    }

    public static void Postfix(Player player, MerchantInventory __result)
    {
        MoonPropShopExtraRelicsHelper.EnsureMoonPropEntries(__result, player);
    }
}

public sealed class MoonPropShopInitializeInventoryPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_initialize_inventory";

    public static string Description => "Clone merchant relic slots so the extra MoonProp relics render below the stock merchant rows";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize), [typeof(MerchantInventory), typeof(MerchantDialogueSet)])];
    }

    public static void Prefix(NMerchantInventory __instance, MerchantInventory inventory)
    {
        MoonPropShopExtraRelicsHelper.EnsureMoonPropEntries(inventory, inventory.Player);
        MoonPropShopExtraRelicsHelper.EnsureMoonPropRelicSlots(__instance, inventory);
    }
}

public sealed class MoonPropShopStackPurchasePatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_stack_purchase";

    public static string Description => "Convert duplicate MoonProp shop purchases into stack gains";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantRelicEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])];
    }

    public static bool Prefix(
        MerchantRelicEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost,
        ref Task<(bool, int)> __result)
    {
        if (inventory?.Player == null || __instance.Model is not MoonPropStackableRelicBase model)
            return true;

        var owner = inventory.Player;
        var existing = owner.Relics
            .OfType<MoonPropStackableRelicBase>()
            .FirstOrDefault(relic =>
                !relic.IsMelted
                && (relic.CanonicalInstance?.Id ?? relic.Id) == (model.CanonicalInstance?.Id ?? model.Id));
        if (existing == null)
            return true;

        __result = HandleStackPurchaseAsync(__instance, owner, existing, ignoreCost);
        return false;
    }

    private static async Task<(bool, int)> HandleStackPurchaseAsync(
        MerchantRelicEntry entry,
        Player owner,
        MoonPropStackableRelicBase existing,
        bool ignoreCost)
    {
        var spentGold = ignoreCost ? 0 : entry.Cost;
        if (!ignoreCost)
            await PlayerCmd.LoseGold(spentGold, owner, GoldLossType.Spent);

        owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(owner.NetId).BoughtRelics.Add(entry.Model!.Id);
        existing.AddStacks(1);

        RunManager.Instance?.RewardSynchronizer?.SyncLocalGoldLost(spentGold);
        RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedRelic(entry.Model!);
        return (true, spentGold);
    }
}
