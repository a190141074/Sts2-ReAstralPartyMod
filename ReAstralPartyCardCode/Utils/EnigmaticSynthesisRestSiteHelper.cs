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
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumScythe>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.EtheriumIngot, 2),
                new(EnigmaticUniqueMaterialKind.EnderRod, 2)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumSword>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.EtheriumIngot, 4),
                new(EnigmaticUniqueMaterialKind.EnderRod, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumAxe>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.EtheriumIngot, 4),
                new(EnigmaticUniqueMaterialKind.EnderRod, 2)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumHelmet>()),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 5)]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumCuirass>()),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 8)]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumGreaves>()),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 7)]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEtheriumBoots>()),
            false,
            [new(EnigmaticUniqueMaterialKind.EtheriumIngot, 4)]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisTwistedHeart>()),
            true,
            [
                new(EnigmaticUniqueMaterialKind.EarthHeart, 1),
                new(EnigmaticUniqueMaterialKind.BlazePowder, 2),
                new(EnigmaticUniqueMaterialKind.RedstoneDust, 2),
                new(EnigmaticUniqueMaterialKind.GhastTear, 1),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisTheTwist>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.NetheriteIngot, 2),
                new(EnigmaticUniqueMaterialKind.NefariousEssence, 4),
                new(EnigmaticUniqueMaterialKind.RedstoneDust, 1),
                new(EnigmaticUniqueMaterialKind.TwistedHeart, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisTheInfinitum>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.CosmicHeart, 2),
                new(EnigmaticUniqueMaterialKind.EnchanterPearl, 1),
                new(EnigmaticUniqueMaterialKind.NefariousEssence, 2),
                new(EnigmaticUniqueMaterialKind.TheTwist, 1),
                new(EnigmaticUniqueMaterialKind.NetheriteIngot, 2),
                new(EnigmaticUniqueMaterialKind.AbyssalHeart, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEnchanterPearl>()),
            true,
            [
                new(EnigmaticUniqueMaterialKind.Emerald, 1),
                new(EnigmaticUniqueMaterialKind.NefariousEssence, 2),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1),
                new(EnigmaticUniqueMaterialKind.BlazePowder, 1),
                new(EnigmaticUniqueMaterialKind.CryingObsidian, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisHeavenScroll>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.GoldIngot, 2),
                new(EnigmaticUniqueMaterialKind.NetherStar, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.TriccScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.LapisLazuli, 2),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisCursedScroll>()),
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
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisAvariceScroll>()),
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
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisXpScroll>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.ExperienceBottle, 4),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.TriccScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.Emerald, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisEscapeScroll>()),
            false,
            [
                new(EnigmaticUniqueMaterialKind.PhantomMembrane, 4),
                new(EnigmaticUniqueMaterialKind.RecallPotion, 1),
                new(EnigmaticUniqueMaterialKind.Dye, 1),
                new(EnigmaticUniqueMaterialKind.TriccScroll, 1),
                new(EnigmaticUniqueMaterialKind.EnchantedFeather, 1),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForMaterial(EnigmaticUniqueMaterialKind.BlazePowder, 2),
            true,
            [new(EnigmaticUniqueMaterialKind.BlazeRod, 1)]),
        new(
            EnigmaticSynthesisRecipeResult.ForMaterial(EnigmaticUniqueMaterialKind.EnderEye, 1),
            true,
            [
                new(EnigmaticUniqueMaterialKind.BlazePowder, 1),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForMaterial(EnigmaticUniqueMaterialKind.EnderRod, 2),
            true,
            [
                new(EnigmaticUniqueMaterialKind.BlazeRod, 2),
                new(EnigmaticUniqueMaterialKind.AstralDust, 2),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisCosmicHeart>()),
            true,
            [
                new(EnigmaticUniqueMaterialKind.AstralDust, 4),
                new(EnigmaticUniqueMaterialKind.NetherStar, 1),
                new(EnigmaticUniqueMaterialKind.BlazePowder, 2),
                new(EnigmaticUniqueMaterialKind.HeartOfTheSea, 1),
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForMaterial(EnigmaticUniqueMaterialKind.EvilIngot, 1),
            true,
            [
                new(EnigmaticUniqueMaterialKind.GhastTear, 4),
                new(EnigmaticUniqueMaterialKind.NefariousEssence, 4),
                new(EnigmaticUniqueMaterialKind.NetheriteIngot, 1)
            ]),
        new(
            EnigmaticSynthesisRecipeResult.ForRelic(ModelDb.Relic<EnigmaticSynthesisRecallPotion>()),
            true,
            [
                new(EnigmaticUniqueMaterialKind.EnderPearl, 1),
                new(EnigmaticUniqueMaterialKind.AwkwardPotion, 1),
                new(EnigmaticUniqueMaterialKind.BlazePowder, 1)
            ])
];

    public static IReadOnlyList<EnigmaticSynthesisRecipeView> AllRecipes { get; } = Recipes
        .Select(static (recipe, index) => new EnigmaticSynthesisRecipeView(
            index,
            recipe.Result,
            recipe.AllowDuplicateResult,
            recipe.Costs.Select(static cost => new EnigmaticMaterialCostView(cost.Kind, cost.Amount)).ToList()))
        .ToList();

    public static bool CanUse(Player? owner)
    {
        return owner != null && GetEligibleRecipes(owner).Count > 0;
    }

    public static IReadOnlyList<EnigmaticSynthesisRecipeView> GetEligibleRecipesForSelection(Player? owner)
    {
        return GetEligibleRecipes(owner)
            .Select(ToRecipeView)
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
        if (recipe.Result.ResultRelic != null
            && !recipe.AllowDuplicateResult
            && owner.Relics.Any(owned =>
                !owned.IsMelted && GetCanonicalId(owned) == GetCanonicalId(recipe.Result.ResultRelic)))
            return false;
        if (recipe.Result.ResultRelic != null
            && PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.Result.ResultRelic))
            return false;

        var consumptionPlan = BuildConsumptionPlan(owner, recipe.Costs);
        if (consumptionPlan == null)
            return false;

        await ConsumeMaterialsAsync(consumptionPlan);
        await GrantCraftResultAsync(owner, recipe.Result);
        return true;
    }

    public static async Task<bool> TryCraftAsync(Player? owner, EnigmaticSynthesisRecipeView? selectedRecipe)
    {
        if (owner == null || selectedRecipe == null)
            return false;
        if (!TryGetRecipe(selectedRecipe, out var recipe))
            return false;
        if (!HasRequiredMaterials(owner, recipe))
            return false;
        if (recipe.Result.ResultRelic != null
            && !recipe.AllowDuplicateResult
            && owner.Relics.Any(owned =>
                !owned.IsMelted && GetCanonicalId(owned) == GetCanonicalId(recipe.Result.ResultRelic)))
            return false;
        if (recipe.Result.ResultRelic != null
            && PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.Result.ResultRelic))
            return false;

        var consumptionPlan = BuildConsumptionPlan(owner, recipe.Costs);
        if (consumptionPlan == null)
            return false;

        await ConsumeMaterialsAsync(consumptionPlan);
        await GrantCraftResultAsync(owner, recipe.Result);
        return true;
    }

    public static int GetOwnedMaterialStacks(Player? owner, EnigmaticUniqueMaterialKind kind)
    {
        return owner == null ? 0 : GetOwnedSynthesisAmount(owner, kind);
    }

    public static bool CanCraft(Player? owner, EnigmaticSynthesisRecipeView recipe)
    {
        if (owner == null)
            return false;

        if (!TryGetRecipe(recipe, out var internalRecipe))
            return false;

        return HasRequiredMaterials(owner, internalRecipe)
               && (internalRecipe.Result.ResultRelic == null
                   || internalRecipe.AllowDuplicateResult
                   || owner.Relics.All(owned =>
                       owned.IsMelted || GetCanonicalId(owned) != GetCanonicalId(internalRecipe.Result.ResultRelic)))
               && (internalRecipe.Result.ResultRelic == null
                   || !PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, internalRecipe.Result.ResultRelic));
    }

    public static bool TryGetSourceRecipeIndex(EnigmaticUniqueMaterialKind kind, out int recipeIndex)
    {
        var targetRelicId = EnigmaticRewardRegistry.GetConfig(kind).Relic.Id;
        for (var i = 0; i < Recipes.Count; i++)
        {
            var result = Recipes[i].Result;
            if (result.ResultKind == kind)
            {
                recipeIndex = i;
                return true;
            }

            if (result.ResultRelic != null && GetCanonicalId(result.ResultRelic) == targetRelicId)
            {
                recipeIndex = i;
                return true;
            }
        }

        recipeIndex = -1;
        return false;
    }

    private static IReadOnlyList<EnigmaticSynthesisRecipe> GetEligibleRecipes(Player? owner)
    {
        if (owner == null)
            return [];

        return Recipes
            .Where(recipe => HasRequiredMaterials(owner, recipe))
            .Where(recipe => recipe.Result.ResultRelic == null
                             || recipe.AllowDuplicateResult
                             || owner.Relics.All(owned =>
                                 owned.IsMelted || GetCanonicalId(owned) != GetCanonicalId(recipe.Result.ResultRelic)))
            .Where(recipe => recipe.Result.ResultRelic == null
                             || !PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, recipe.Result.ResultRelic))
            .ToList();
    }

    private static bool TryGetRecipe(RelicModel relic, out EnigmaticSynthesisRecipe recipe)
    {
        var canonicalId = GetCanonicalId(relic);
        recipe = Recipes.FirstOrDefault(candidate =>
            candidate.Result.ResultRelic != null && GetCanonicalId(candidate.Result.ResultRelic) == canonicalId);
        return recipe.Result.ResultRelic != null;
    }

    private static bool TryGetRecipe(EnigmaticSynthesisRecipeView recipeView, out EnigmaticSynthesisRecipe recipe)
    {
        if (recipeView.Index < 0 || recipeView.Index >= Recipes.Count)
        {
            recipe = default;
            return false;
        }

        recipe = Recipes[recipeView.Index];
        return true;
    }

    private static bool HasRequiredMaterials(Player owner, EnigmaticSynthesisRecipe recipe)
    {
        return recipe.Costs.All(cost => GetOwnedSynthesisAmount(owner, cost.Kind) >= cost.Amount);
    }

    private static int GetOwnedSynthesisAmount(Player owner, EnigmaticUniqueMaterialKind kind)
    {
        var total = GetOwnedMaterials(owner, kind).Sum(static material => Math.Max(0, material.SynthesisAmount));
        if (EnigmaticTheOneBoxHelper.IsBoxedKind(kind))
            total += EnigmaticTheOneBoxHelper.GetStoredAmount(owner, kind);

        return total;
    }

    private static List<EnigmaticUniqueMaterialRelicBase> GetOwnedMaterials(Player owner,
        EnigmaticUniqueMaterialKind kind)
    {
        var targetId = EnigmaticRewardRegistry.GetConfig(kind).Relic.Id;
        return owner.Relics
            .OfType<EnigmaticUniqueMaterialRelicBase>()
            .Where(material =>
                !material.IsMelted && material.SynthesisAmount > 0 && GetCanonicalId(material) == targetId)
            .OrderByDescending(material => material.SynthesisAmount)
            .ToList();
    }

    private static List<EnigmaticMaterialConsumptionStep>? BuildConsumptionPlan(
        Player owner,
        IEnumerable<EnigmaticMaterialCost> costs)
    {
        var plan = new List<EnigmaticMaterialConsumptionStep>();
        foreach (var cost in costs)
        {
            var remaining = Math.Max(0, cost.Amount);
            var materials = GetOwnedMaterials(owner, cost.Kind);
            foreach (var material in materials)
            {
                if (remaining <= 0)
                    break;

                var toConsume = Math.Min(remaining, material.SynthesisAmount);
                if (toConsume <= 0)
                    continue;

                plan.Add(EnigmaticMaterialConsumptionStep.ForRelic(material, toConsume));
                remaining -= toConsume;
            }

            if (remaining > 0 && EnigmaticTheOneBoxHelper.IsBoxedKind(cost.Kind))
            {
                var boxedAmount = EnigmaticTheOneBoxHelper.GetStoredAmount(owner, cost.Kind);
                var toConsume = Math.Min(remaining, boxedAmount);
                if (toConsume > 0)
                {
                    plan.Add(EnigmaticMaterialConsumptionStep.ForBox(owner, cost.Kind, toConsume));
                    remaining -= toConsume;
                }
            }

            if (remaining > 0)
                return null;
        }

        return plan;
    }

    private static async Task ConsumeMaterialsAsync(
        IEnumerable<EnigmaticMaterialConsumptionStep> plan)
    {
        foreach (var step in plan)
        {
            if (step.Material != null)
            {
                await step.Material.ConsumeForSynthesisAsync(step.Amount);
                continue;
            }

            if (step.BoxKind != null)
            {
                var box = EnigmaticTheOneBoxHelper.GetBox(step.Owner);
                if (box != null)
                    await box.ConsumeStoredMaterialAsync(step.BoxKind.Value, step.Amount);
            }
        }
    }

    private static async Task GrantCraftResultAsync(Player owner, EnigmaticSynthesisRecipeResult result)
    {
        if (result.ResultKind != null)
        {
            await EnigmaticRewardRegistry.GetConfig(result.ResultKind.Value)
                .GrantRewardAsync(owner, result.ResultAmount);
            return;
        }

        var resultRelic = result.ResultRelic!;
        if (GetCanonicalId(resultRelic) == ModelDb.Relic<EnigmaticSynthesisTwistedHeart>().Id)
            await EnigmaticSynthesisTwistedHeart.GrantStacks(owner, 1);
        else
            await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, resultRelic);
    }

    private static ModelId GetCanonicalId(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic).Id;
    }

    private static EnigmaticSynthesisRecipeView ToRecipeView(EnigmaticSynthesisRecipe recipe)
    {
        var index = -1;
        for (var i = 0; i < Recipes.Count; i++)
            if (Recipes[i].Equals(recipe))
            {
                index = i;
                break;
            }

        if (index < 0)
            throw new InvalidOperationException("Failed to resolve enigmatic synthesis recipe index.");

        return AllRecipes[index];
    }

    private readonly record struct EnigmaticMaterialCost(EnigmaticUniqueMaterialKind Kind, int Amount);

    private readonly record struct EnigmaticMaterialConsumptionStep(
        Player Owner,
        EnigmaticUniqueMaterialRelicBase? Material,
        EnigmaticUniqueMaterialKind? BoxKind,
        int Amount)
    {
        public static EnigmaticMaterialConsumptionStep ForRelic(EnigmaticUniqueMaterialRelicBase material, int amount)
        {
            return new EnigmaticMaterialConsumptionStep(material.Owner!, material, null, amount);
        }

        public static EnigmaticMaterialConsumptionStep ForBox(Player owner, EnigmaticUniqueMaterialKind kind, int amount)
        {
            return new EnigmaticMaterialConsumptionStep(owner, null, kind, amount);
        }
    }

    private readonly record struct EnigmaticSynthesisRecipe(
        EnigmaticSynthesisRecipeResult Result,
        bool AllowDuplicateResult,
        IReadOnlyList<EnigmaticMaterialCost> Costs);
}

internal sealed record EnigmaticMaterialCostView(
    EnigmaticUniqueMaterialKind Kind,
    int Amount);

internal sealed record EnigmaticSynthesisRecipeView(
    int Index,
    EnigmaticSynthesisRecipeResult Result,
    bool AllowDuplicateResult,
    IReadOnlyList<EnigmaticMaterialCostView> Costs);

internal sealed record EnigmaticSynthesisRecipeResult(
    RelicModel? ResultRelic,
    EnigmaticUniqueMaterialKind? ResultKind,
    int ResultAmount)
{
    public static EnigmaticSynthesisRecipeResult ForRelic(RelicModel relic)
    {
        return new EnigmaticSynthesisRecipeResult(relic, null, 1);
    }

    public static EnigmaticSynthesisRecipeResult ForMaterial(EnigmaticUniqueMaterialKind kind, int amount)
    {
        return new EnigmaticSynthesisRecipeResult(null, kind, Math.Max(1, amount));
    }

    public RelicModel DisplayRelic => ResultRelic ?? EnigmaticRewardRegistry.GetConfig(ResultKind!.Value).Relic;
}
