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
        static () => ModelDb.Relic<MoonPropLongstandingSolitude>(),
        static () => ModelDb.Relic<MoonPropCorpsebloom>(),
        static () => ModelDb.Relic<MoonPropFocusedConvergence>(),
        static () => ModelDb.Relic<MoonPropTranscendence>(),
        static () => ModelDb.Relic<MoonPropEgocentrism>(),
        static () => ModelDb.Relic<MoonPropEulogyZero>(),
        static () => ModelDb.Relic<MoonPropMercurialRachis>(),
        static () => ModelDb.Relic<MoonPropLightFluxPauldron>(),
        static () => ModelDb.Relic<MoonPropStoneFluxPauldron>(),
        static () => ModelDb.Relic<MoonPropBeadsOfFealty>()
    ];

    public static bool IsMoonPropRelic(RelicModel? relic)
    {
        return relic is MoonPropStackableRelicBase or MoonPropBeadsOfFealty;
    }

    public static RelicModel CreateDeterministicMoonPropRelic(Player player, string contextId, params object?[] extraContext)
    {
        return CreateDeterministicMoonPropRelicExcluding(player, contextId, null, extraContext);
    }

    public static RelicModel CreateDeterministicMoonPropRelicExcluding(
        Player player,
        string contextId,
        IReadOnlyCollection<ModelId>? excludedRelicIds,
        params object?[] extraContext)
    {
        var candidateFactories = GetAvailableMoonPropRelicFactories(player, excludedRelicIds);
        if (candidateFactories.Count == 0)
            throw new InvalidOperationException("No Moon Prop relic candidates are available for deterministic selection.");

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidateFactories.Count,
            MainFile.ModId,
            contextId,
            player.RunState?.Rng.StringSeed ?? "<null_seed>",
            player.RunState?.CurrentActIndex ?? -1,
            player.RunState?.TotalFloor ?? -1,
            player.NetId,
            extraContext.Length == 0 ? "<none>" : string.Join("|", extraContext.Select(static part => part?.ToString() ?? "<null>")));
        return candidateFactories[selectedIndex]().ToMutable();
    }

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
        return CreateDeterministicMoonPropRelic(player, ContextId, slotIndex);
    }

    private static List<Func<RelicModel>> GetAvailableMoonPropRelicFactories(
        Player player,
        IReadOnlyCollection<ModelId>? excludedRelicIds)
    {
        var excluded = excludedRelicIds ?? Array.Empty<ModelId>();
        var hasOwnedBeadsOfFealty = HasEverOwnedRelic(player, ModelDb.Relic<MoonPropBeadsOfFealty>().Id);
        return MoonPropRelicFactories
            .Where(factory =>
            {
                var relic = factory();
                var relicId = GetCanonicalRelicId(relic);
                if (excluded.Contains(relicId))
                    return false;
                if (relic is MoonPropBeadsOfFealty && hasOwnedBeadsOfFealty)
                    return false;
                return true;
            })
            .ToList();
    }

    private static bool HasEverOwnedRelic(Player? player, ModelId relicId)
    {
        if (player == null)
            return false;

        if (player.Relics.Any(relic => GetCanonicalRelicId(relic) == relicId))
            return true;

        return player.RunState.MapPointHistory
            .SelectMany(static actEntries => actEntries)
            .Select(entry => entry.GetEntry(player.NetId))
            .Any(playerEntry => playerEntry.BoughtRelics.Contains(relicId));
    }

    private static ModelId GetCanonicalRelicId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
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
        var hasDiscountCount = !ignoreCost && MoonPropLongstandingSolitudeShopHelper.HasActiveDiscountCount(owner);
        var spentGold = ignoreCost
            ? 0
            : hasDiscountCount
                ? entry.Cost
                : entry.Cost;
        if (spentGold > 0)
            await PlayerCmd.LoseGold(spentGold, owner, GoldLossType.Spent);

        owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(owner.NetId).BoughtRelics.Add(entry.Model!.Id);
        existing.AddStacks(1);

        RunManager.Instance?.RewardSynchronizer?.SyncLocalGoldLost(spentGold);
        RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedRelic(entry.Model!);
        return (true, spentGold);
    }
}

internal static class MoonPropShopFreePurchasePatchHelper
{
    public static bool TryForceIgnoreCost(Player? owner, MerchantEntry? entry, bool ignoreCost, ref bool __result)
    {
        if (ignoreCost)
            return false;
        if (!MoonPropLongstandingSolitudeShopHelper.ShouldTreatEntryAsFree(owner, entry))
            return false;

        __result = true;
        return true;
    }
}

public sealed class MoonPropShopFreePurchaseCardPrefixPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_card_prefix";

    public static string Description => "Treat card purchases as free while Moon Prop free purchases remain";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantCardEntry), "CanAfford", [typeof(Player), typeof(bool)])];
    }

    public static bool Prefix(MerchantCardEntry __instance, Player player, bool ignoreCost, ref bool __result)
    {
        return !MoonPropShopFreePurchasePatchHelper.TryForceIgnoreCost(player, __instance, ignoreCost, ref __result);
    }
}

public sealed class MoonPropShopFreePurchasePotionPrefixPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_potion_prefix";

    public static string Description => "Treat potion purchases as free while Moon Prop free purchases remain";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantPotionEntry), "CanAfford", [typeof(Player), typeof(bool)])];
    }

    public static bool Prefix(MerchantPotionEntry __instance, Player player, bool ignoreCost, ref bool __result)
    {
        return !MoonPropShopFreePurchasePatchHelper.TryForceIgnoreCost(player, __instance, ignoreCost, ref __result);
    }
}

public sealed class MoonPropShopFreePurchaseRemovalPrefixPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_removal_prefix";

    public static string Description => "Treat card removal as free while Moon Prop free purchases remain";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantCardRemovalEntry), "CanAfford", [typeof(Player), typeof(bool)])];
    }

    public static bool Prefix(MerchantCardRemovalEntry __instance, Player player, bool ignoreCost, ref bool __result)
    {
        return !MoonPropShopFreePurchasePatchHelper.TryForceIgnoreCost(player, __instance, ignoreCost, ref __result);
    }
}

public sealed class MoonPropShopFreePurchaseRelicPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_relic";

    public static string Description => "Consume Moon Prop free purchases after successful normal relic purchases";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantRelicEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])];
    }

    public static async Task<(bool, int)> Postfix(
        Task<(bool, int)> __result,
        MerchantRelicEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost)
    {
        var result = await __result;
        if (!result.Item1 || ignoreCost || inventory?.Player == null)
            return result;

        MoonPropLongstandingSolitudeShopHelper.TryConsumeFreePurchase(inventory.Player, __instance);
        return result;
    }
}

public sealed class MoonPropShopFreePurchaseCardPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_card";

    public static string Description => "Consume Moon Prop free purchases after successful card purchases";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantCardEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])];
    }

    public static async Task<(bool, int)> Postfix(
        Task<(bool, int)> __result,
        MerchantCardEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost)
    {
        var result = await __result;
        if (!result.Item1 || ignoreCost || inventory?.Player == null)
            return result;

        MoonPropLongstandingSolitudeShopHelper.TryConsumeFreePurchase(inventory.Player, __instance);
        return result;
    }
}

public sealed class MoonPropShopFreePurchasePotionPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_potion";

    public static string Description => "Consume Moon Prop free purchases after successful potion purchases";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantPotionEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])];
    }

    public static async Task<(bool, int)> Postfix(
        Task<(bool, int)> __result,
        MerchantPotionEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost)
    {
        var result = await __result;
        if (!result.Item1 || ignoreCost || inventory?.Player == null)
            return result;

        MoonPropLongstandingSolitudeShopHelper.TryConsumeFreePurchase(inventory.Player, __instance);
        return result;
    }
}

public sealed class MoonPropShopFreePurchaseRemovalPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_shop_free_purchase_removal";

    public static string Description => "Consume Moon Prop free purchases after successful card removal purchases";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantCardRemovalEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])];
    }

    public static async Task<(bool, int)> Postfix(
        Task<(bool, int)> __result,
        MerchantCardRemovalEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost)
    {
        var result = await __result;
        if (!result.Item1 || ignoreCost || inventory?.Player == null)
            return result;

        MoonPropLongstandingSolitudeShopHelper.TryConsumeFreePurchase(inventory.Player, __instance);
        return result;
    }
}
