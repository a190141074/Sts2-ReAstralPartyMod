using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public readonly record struct TokenBreakthroughCandidate(RelicModel Source, RelicModel Target, decimal QualityFactor);

public static class TokenBreakthroughHelper
{
    private static readonly IReadOnlyList<(Type Source, Type Target)> Promotions =
    [
        (typeof(TokenBlueBoxingGloveGeneral), typeof(TokenPurpleBoxingGloveIntermediate)),
        (typeof(TokenPurpleBoxingGloveIntermediate), typeof(TokenGoldBoxingGlovePremium)),
        (typeof(TokenBlueMotorcycleHelmetGeneral), typeof(TokenPurpleMotorcycleHelmetIntermediate)),
        (typeof(TokenPurpleMotorcycleHelmetIntermediate), typeof(TokenGoldMotorcycleHelmetPremium)),
        (typeof(TokenBlueSpeedRollerGeneral), typeof(TokenPurpleSpeedRollerIntermediate)),
        (typeof(TokenPurpleSpeedRollerIntermediate), typeof(TokenGoldSpeedRollerPremium)),
        (typeof(TokenBlueBankCardGeneral), typeof(TokenPurpleBankCardIntermediate)),
        (typeof(TokenPurpleBankCardIntermediate), typeof(TokenGoldBankCardPremium)),
        (typeof(TokenBlueSandwichBiscuitGeneral), typeof(TokenPurpleSandwichBiscuitIntermediate)),
        (typeof(TokenPurpleSandwichBiscuitIntermediate), typeof(TokenGoldSandwichBiscuitPremium)),
        (typeof(TokenPurpleSandwichBiscuitIntermediate), typeof(TokenPurpleSandwichBiscuitMouse)),
        (typeof(TokenBlueFlashlightGeneral), typeof(TokenPurpleFlashlightStronglight)),
        (typeof(TokenPurpleFlashlightStronglight), typeof(TokenGoldFlashlightFlashburst)),
        (typeof(TokenPurpleArtKnifeBeginner), typeof(TokenGoldArtKnifeSharp)),
        (typeof(TokenPurpleArtKnifeBeginner), typeof(TokenGoldArtKnifeEnchanted)),
        (typeof(TokenBlueHandheldFanSmall), typeof(TokenPurpleHandheldFanLarge)),
        (typeof(TokenPurpleFlyingStar), typeof(TokenGoldFlyingStar)),
        (typeof(TokenPurpleAdrenalineGeneral), typeof(TokenGoldAdrenalineEfficient)),
        (typeof(TokenBlueMedicalKitEmergencyTreatment), typeof(TokenGoldMedicalKitCompleteTreatment))
    ];
    private static readonly HashSet<Type> TerminalGoldTokenTypes = Promotions
        .Where(mapping => mapping.Target.Name.StartsWith("TokenGold", StringComparison.Ordinal)
            && Promotions.All(next => next.Source != mapping.Target))
        .Select(mapping => mapping.Target)
        .ToHashSet();

    public static IReadOnlyList<TokenBreakthroughCandidate> CreateCandidates(Player owner, int completedCount, int slot)
    {
        var owned = owner.Relics.Select(Canonical).ToList();
        var ownedIds = owned.Select(r => r.Id).ToHashSet();

        if (slot == 0)
        {
            var source = owned.Where(r => r.Rarity == RelicRarity.Common && !TokenRelicRegistry.IsTokenRelic(r))
                .OrderBy(r => Stable(owner, completedCount, slot, "source", r.Id.Entry)).ThenBy(r => r.Id.Entry, StringComparer.Ordinal).FirstOrDefault();
            var target = TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, RelicRarity.Common, true)
                .Where(r => !ownedIds.Contains(Canonical(r).Id) && !ChargeTokenRegistry.IsChargeToken(r)
                    && IsExplicitProgressionRelic(r))
                .OrderBy(r => Stable(owner, completedCount, slot, r.Id.Entry)).ThenBy(r => r.Id.Entry, StringComparer.Ordinal)
                .FirstOrDefault();
            if (source != null && target != null)
                return [new(source, target, 0.5m)];
            return [];
        }

        var choices = new List<TokenBreakthroughCandidate>();
        foreach (var (sourceType, targetType) in Promotions)
        {
            var source = owned.Where(sourceType.IsInstanceOfType)
                .OrderBy(r => Stable(owner, completedCount, slot, "promotion-source", r.Id.Entry))
                .ThenBy(r => r.Id.Entry, StringComparer.Ordinal).FirstOrDefault();
            if (source == null)
                continue;
            var target = ModelDb.GetById<RelicModel>(ModelDb.GetId(targetType));
            if (ownedIds.Contains(Canonical(target).Id) || ChargeTokenRegistry.IsChargeToken(target)
                || !TokenRelicRegistry.IsRelicAvailableForRun(owner.RunState, target))
                continue;
            choices.Add(new(source, Canonical(target), QualityFactor(source)));
        }

        var selected = choices.OrderBy(c => Stable(owner, completedCount, slot, c.Source.Id.Entry, c.Target.Id.Entry))
            .ThenBy(c => c.Source.Id.Entry, StringComparer.Ordinal).ThenBy(c => c.Target.Id.Entry, StringComparer.Ordinal).ToList();
        var availableAncients = ProphecySoulDevourHelper.CreateSafeBaseGameAncientRelics(owner)
            .Where(r => !ownedIds.Contains(Canonical(r).Id))
            .Where(r => !PersonMultiplayerEffectHelper.IsRelicBannedForOwner(owner, r))
            .OrderBy(r => r.Id.Entry, StringComparer.Ordinal).ToList();
        var goldSources = owned.Where(r => TerminalGoldTokenTypes.Any(type => type.IsInstanceOfType(r)))
            .Where(r => !ChargeTokenRegistry.IsChargeToken(r))
            .OrderBy(r => r.Id.Entry, StringComparer.Ordinal).ToList();
        if (availableAncients.Count > 0 && goldSources.Count > 0
            && Stable(owner, completedCount, slot, "ancient-roll") % 10 == 0)
            selected.Add(new(goldSources[0], availableAncients[Stable(owner, completedCount, slot, "ancient-target") % availableAncients.Count], 5));

        return selected;
    }

    public static IReadOnlyList<TokenBreakthroughCandidate> CreateNormalRelicCandidates(Player owner, int completedCount, int slot)
    {
        var ownedIds = owner.Relics.Select(Canonical).Select(relic => relic.Id).ToHashSet();
        var sources = owner.Relics.Select(Canonical)
            .Where(relic => relic.Rarity == RelicRarity.Common && !TokenRelicRegistry.IsTokenRelic(relic))
            .OrderBy(relic => Stable(owner, completedCount, slot, "normal-source", relic.Id.Entry))
            .ThenBy(relic => relic.Id.Entry, StringComparer.Ordinal).ToList();
        var targets = TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, RelicRarity.Common, true)
            .Where(relic => !ownedIds.Contains(Canonical(relic).Id) && !ChargeTokenRegistry.IsChargeToken(relic)
                && IsExplicitProgressionRelic(relic))
            .OrderBy(relic => Stable(owner, completedCount, slot, "normal-target", relic.Id.Entry))
            .ThenBy(relic => relic.Id.Entry, StringComparer.Ordinal).ToList();

        return sources.SelectMany(source => targets.Select(target =>
                new TokenBreakthroughCandidate(source, Canonical(target), 0.5m)))
            .OrderBy(candidate => Stable(owner, completedCount, slot, candidate.Source.Id.Entry, candidate.Target.Id.Entry))
            .ThenBy(candidate => candidate.Source.Id.Entry, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Target.Id.Entry, StringComparer.Ordinal).ToList();
    }

    public static int Cost(TokenBreakthroughCandidate candidate, int completedCount) =>
        (int)Math.Round(100m * (1m + 0.1m * completedCount) * candidate.QualityFactor, MidpointRounding.AwayFromZero);

    private static decimal QualityFactor(RelicModel source) => !TokenRelicRegistry.IsTokenRelic(source) ? 0.5m : source.Rarity switch
    {
        RelicRarity.Common => 1m,
        RelicRarity.Uncommon => 2m,
        RelicRarity.Rare => 4m,
        _ => 4m
    };

    private static RelicModel Canonical(RelicModel relic) => relic.CanonicalInstance ?? relic;

    private static bool IsExplicitProgressionRelic(RelicModel relic) => Promotions.Any(mapping =>
        mapping.Source.IsInstanceOfType(relic) || mapping.Target.IsInstanceOfType(relic));

    private static int Stable(Player owner, int count, int slot, params string?[] values) =>
        DeterministicMultiplayerChoiceHelper.RollDeterministically(0, int.MaxValue,
            new object?[] { MainFile.ModId, nameof(TokenBreakthroughHelper), owner.RunState?.Rng.StringSeed,
                owner.RunState?.CurrentActIndex, owner.RunState?.TotalFloor, owner.NetId, count, slot }
                .Concat(values).ToArray());
}
