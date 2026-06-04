using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EnigmaticSynthesisRestSiteHelper
{
    private static readonly IReadOnlyList<EnigmaticSynthesisRecipe> Recipes =
    [
        new(
            ModelDb.Relic<EnigmaticSynthesisEtheriumHelmet>(),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 5)]),
        new(
            ModelDb.Relic<EnigmaticSynthesisEtheriumCuirass>(),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 8)]),
        new(
            ModelDb.Relic<EnigmaticSynthesisEtheriumGreaves>(),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 7)]),
        new(
            ModelDb.Relic<EnigmaticSynthesisEtheriumBoots>(),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 4)]),
        new(
            ModelDb.Relic<EnigmaticSynthesisTwistedHeart>(),
            true,
            [
                new(EnigmaticUniqueMaterialKind.EarthHeart, 1),
                new(EnigmaticUniqueMaterialKind.BlazePowder, 2),
                new(EnigmaticUniqueMaterialKind.RedstoneDust, 2),
                new(EnigmaticUniqueMaterialKind.GhastTear, 1),
                new(EnigmaticUniqueMaterialKind.EnderEye, 1)
            ]),
        new(
            ModelDb.Relic<EnigmaticSynthesisTheTwist>(),
            false,
            [
                new(EnigmaticUniqueMaterialKind.NetheriteIngot, 2),
                new(EnigmaticUniqueMaterialKind.NefariousEssence, 4),
                new(EnigmaticUniqueMaterialKind.RedstoneDust, 1),
                new(EnigmaticUniqueMaterialKind.TwistedHeart, 1)
            ]),
        new(
            ModelDb.Relic<EnigmaticSynthesisCursedScroll>(),
            false,
            [
                new(EnigmaticUniqueMaterialKind.PhantomMembrane, 2),
                new(EnigmaticUniqueMaterialKind.TwistedHeart, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.DarkestScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.RedstoneDust, 2),
                new(EnigmaticUniqueMaterialKind.EnchantedBook, 1)
            ]),
        new(
            ModelDb.Relic<EnigmaticSynthesisAvariceScroll>(),
            false,
            [
                new(EnigmaticUniqueMaterialKind.GoldIngot, 4),
                new(EnigmaticUniqueMaterialKind.GemRing, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.DarkestScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.TwistedHeart, 1)
            ]),
        new(
            ModelDb.Relic<EnigmaticSynthesisXpScroll>(),
            false,
            [
                new(EnigmaticUniqueMaterialKind.ExperienceBottle, 4),
                new(EnigmaticUniqueMaterialKind.EnderEye, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.TriccScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.Emerald, 1)
            ])
    ];

    public static IReadOnlyList<EnigmaticSynthesisRecipeView> AllRecipes { get; } = Recipes
        .Select(static recipe => new EnigmaticSynthesisRecipeView(
            recipe.Relic,
            recipe.AllowDuplicateResult,
            recipe.Costs.Select(static cost => new EnigmaticMaterialCostView(cost.Kind, cost.Amount)).ToList()))
        .ToList();

    public static bool CanUse(Player? owner)
    {
        return owner != null && GetEligibleRecipes(owner).Count > 0;
    }

    public static IReadOnlyList<RelicModel> GetEligibleRelics(Player? owner)
    {
        return GetEligibleRecipes(owner)
            .Select(static recipe => recipe.Relic)
            .ToList();
    }

    public static async Task<bool> TryCraftAsync(Player? owner, RelicModel? selectedRelic)
    {
        if (owner == null || selectedRelic == null)
            return false;
        if (!TryGetRecipe(selectedRelic, out var recipe))
            return false;
        if (!HasRequiredMaterials(owner, recipe))
            return false;
        if (!recipe.AllowDuplicateResult &&
            owner.Relics.Any(owned => !owned.IsMelted && GetCanonicalId(owned) == GetCanonicalId(recipe.Relic)))
            return false;
        if (PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.Relic))
            return false;

        var consumptionPlan = BuildConsumptionPlan(owner, recipe.Costs);
        if (consumptionPlan == null)
            return false;

        await ConsumeMaterialsAsync(consumptionPlan);
        await GrantCraftResultAsync(owner, recipe.Relic);
        return true;
    }

    public static int GetOwnedMaterialStacks(Player? owner, EnigmaticUniqueMaterialKind kind)
    {
        return owner == null ? 0 : GetTotalStacks(owner, kind);
    }

    public static bool CanCraft(Player? owner, EnigmaticSynthesisRecipeView recipe)
    {
        if (owner == null)
            return false;

        return recipe.Costs.All(cost => GetOwnedMaterialStacks(owner, cost.Kind) >= cost.Amount)
               && (recipe.AllowDuplicateResult ||
                   owner.Relics.All(owned =>
                       owned.IsMelted || GetCanonicalId(owned) != GetCanonicalId(recipe.ResultRelic)))
               && !PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.ResultRelic);
    }

    private static IReadOnlyList<EnigmaticSynthesisRecipe> GetEligibleRecipes(Player? owner)
    {
        if (owner == null)
            return [];

        return Recipes
            .Where(recipe => HasRequiredMaterials(owner, recipe))
            .Where(recipe => recipe.AllowDuplicateResult ||
                             owner.Relics.All(owned => owned.IsMelted || GetCanonicalId(owned) != GetCanonicalId(recipe.Relic)))
            .Where(recipe => !PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.Relic))
            .ToList();
    }

    private static bool TryGetRecipe(RelicModel relic, out EnigmaticSynthesisRecipe recipe)
    {
        var canonicalId = GetCanonicalId(relic);
        recipe = Recipes.FirstOrDefault(candidate => GetCanonicalId(candidate.Relic) == canonicalId);
        return recipe.Relic != null;
    }

    private static bool HasRequiredMaterials(Player owner, EnigmaticSynthesisRecipe recipe)
    {
        return recipe.Costs.All(cost => GetTotalStacks(owner, cost.Kind) >= cost.Amount);
    }

    private static int GetTotalStacks(Player owner, EnigmaticUniqueMaterialKind kind)
    {
        return GetOwnedMaterials(owner, kind).Sum(static material => Math.Max(0, material.Stacks));
    }

    private static List<EnigmaticUniqueMaterialRelicBase> GetOwnedMaterials(Player owner, EnigmaticUniqueMaterialKind kind)
    {
        var targetId = EnigmaticRewardRegistry.GetConfig(kind).Relic.Id;
        return owner.Relics
            .OfType<EnigmaticUniqueMaterialRelicBase>()
            .Where(material => !material.IsMelted && material.Stacks > 0 && GetCanonicalId(material) == targetId)
            .OrderByDescending(material => material.Stacks)
            .ToList();
    }

    private static List<(EnigmaticUniqueMaterialRelicBase Material, int Amount)>? BuildConsumptionPlan(
        Player owner,
        IEnumerable<EnigmaticMaterialCost> costs)
    {
        var plan = new List<(EnigmaticUniqueMaterialRelicBase Material, int Amount)>();
        foreach (var cost in costs)
        {
            var remaining = Math.Max(0, cost.Amount);
            var materials = GetOwnedMaterials(owner, cost.Kind);
            foreach (var material in materials)
            {
                if (remaining <= 0)
                    break;

                var toConsume = Math.Min(remaining, material.Stacks);
                if (toConsume <= 0)
                    continue;

                plan.Add((material, toConsume));
                remaining -= toConsume;
            }

            if (remaining > 0)
                return null;
        }

        return plan;
    }

    private static async Task ConsumeMaterialsAsync(IEnumerable<(EnigmaticUniqueMaterialRelicBase Material, int Amount)> plan)
    {
        foreach (var (material, amount) in plan)
            await material.ConsumeStacksAsync(amount);
    }

    private static async Task GrantCraftResultAsync(Player owner, RelicModel resultRelic)
    {
        if (GetCanonicalId(resultRelic) == ModelDb.Relic<EnigmaticSynthesisTwistedHeart>().Id)
            await EnigmaticSynthesisTwistedHeart.GrantStacks(owner, 1);
        else
            await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, resultRelic);
    }

    private static ModelId GetCanonicalId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
    }

    private readonly record struct EnigmaticMaterialCost(EnigmaticUniqueMaterialKind Kind, int Amount);

    private readonly record struct EnigmaticSynthesisRecipe(
        RelicModel Relic,
        bool AllowDuplicateResult,
        IReadOnlyList<EnigmaticMaterialCost> Costs);
}

internal sealed record EnigmaticMaterialCostView(
    EnigmaticUniqueMaterialKind Kind,
    int Amount);

internal sealed record EnigmaticSynthesisRecipeView(
    RelicModel ResultRelic,
    bool AllowDuplicateResult,
    IReadOnlyList<EnigmaticMaterialCostView> Costs);
