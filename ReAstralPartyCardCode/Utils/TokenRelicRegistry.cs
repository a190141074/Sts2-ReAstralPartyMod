using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenRelicRegistry
{
    private static readonly Type[] TokenRelicTypes =
    [
        typeof(TokenBlueAtm),
        typeof(TokenBlueBankCardGeneral),
        typeof(TokenBlueBoxingGloveGeneral),
        typeof(TokenBlueDie10),
        typeof(TokenBlueDie12),
        typeof(TokenBlueDie20),
        typeof(TokenBlueDie4),
        typeof(TokenBlueDie6),
        typeof(TokenBlueDie8),
        typeof(TokenBlueElegantFeather),
        typeof(TokenBlueFlashlightGeneral),
        typeof(TokenBlueGiantAnchor),
        typeof(TokenBlueHandheldFanSmall),
        typeof(TokenBlueMarkSprayCan),
        typeof(TokenBlueMedicalKitEmergencyTreatment),
        typeof(TokenBlueMembersReferenceStandard),
        typeof(TokenBlueMotorcycleHelmetGeneral),
        typeof(TokenBluePiggyBank),
        typeof(TokenBlueSandwichBiscuitGeneral),
        typeof(TokenBlueSpeedRollerGeneral),
        typeof(TokenBlueTargetBoard),
        typeof(TokenEternalStarlight),
        typeof(TokenExclusiveAncientWand),
        typeof(TokenExclusiveBoutiqueSwordShield),
        typeof(TokenExclusiveBronzeGong),
        typeof(TokenExclusiveCrossedTwinCarp),
        typeof(TokenExclusiveCursedSword),
        typeof(TokenExclusiveDreamshipModel),
        typeof(TokenExclusiveInfiniteSnake),
        typeof(TokenExclusiveLittleCarpDoll),
        typeof(TokenExclusiveLittleSnakeDoll),
        typeof(TokenExclusivePiercingGun),
        typeof(TokenExclusivePsychedelicSeafoodSoup),
        typeof(TokenExclusiveStormTalisman),
        typeof(TokenExclusiveTimer),
        typeof(TokenExclusiveTrident),
        typeof(TokenExclusiveVengeanceHalberd),
        typeof(TokenExclusiveZuoTeaCake),
        typeof(TokenGoldAdrenalineEfficient),
        typeof(TokenGoldArtKnifeEnchanted),
        typeof(TokenGoldArtKnifeSharp),
        typeof(TokenGoldBankCardPremium),
        typeof(TokenGoldBigStewBowl),
        typeof(TokenGoldBoxingGlovePremium),
        typeof(TokenGoldBufferShield),
        typeof(TokenGoldEagleEyeScope),
        typeof(TokenGoldExplorationSatellite),
        typeof(TokenGoldExtraBattery),
        typeof(TokenGoldFlashlightFlashburst),
        typeof(TokenGoldHandheldFanLarge),
        typeof(TokenGoldInitialPoint),
        typeof(TokenGoldArcaneCodex),
        typeof(TokenGoldMagicQuiver),
        typeof(TokenGoldMedicalKitCompleteTreatment),
        typeof(TokenGoldMembersReferenceUltimate),
        typeof(TokenGoldMotorcycleHelmetPremium),
        typeof(TokenGoldNinjaShuriken),
        typeof(TokenGoldSandwichBiscuitPremium),
        typeof(TokenGoldSpeedRollerPremium),
        typeof(TokenGoldStarCoinHammer),
        typeof(TokenGoldVitamin),
        typeof(TokenPurpleAdrenalineGeneral),
        typeof(TokenPurpleArtKnifeBeginner),
        typeof(TokenPurpleBankCardIntermediate),
        typeof(TokenPurpleBasicScope),
        typeof(TokenPurpleBigBackpack),
        typeof(TokenPurpleBoxingGloveIntermediate),
        typeof(TokenPurpleFlashlightStronglight),
        typeof(TokenPurpleFriendshipBadge),
        typeof(TokenPurpleMembersReferencePremium),
        typeof(TokenPurpleMotorcycleHelmetIntermediate),
        typeof(TokenPurpleSandwichBiscuitIntermediate),
        typeof(TokenPurpleSmartWatch),
        typeof(TokenPurpleSpeedRollerIntermediate),
        typeof(TokenPurpleTastyCandy)
    ];

    private static readonly Type[] SeriesTokenRelicTypes =
    [
        typeof(TokenBlueGiantAnchor),
        typeof(TokenExclusiveAncientWand),
        typeof(TokenExclusiveBoutiqueSwordShield),
        typeof(TokenExclusiveBronzeGong),
        typeof(TokenExclusiveCandyMembershipCard),
        typeof(TokenExclusiveCrossedTwinCarp),
        typeof(TokenExclusiveCursedSword),
        typeof(TokenExclusiveDreamshipModel),
        typeof(TokenExclusiveInfiniteSnake),
        typeof(TokenExclusiveLittleCarpDoll),
        typeof(TokenExclusiveLittleSnakeDoll),
        typeof(TokenExclusivePiercingGun),
        typeof(TokenExclusivePsychedelicSeafoodSoup),
        typeof(TokenExclusiveStormTalisman),
        typeof(TokenExclusiveTimer),
        typeof(TokenExclusiveTrident),
        typeof(TokenExclusiveVengeanceHalberd),
        typeof(TokenExclusiveZuoTeaCake)
    ];

    public static IReadOnlyList<RelicModel> GetCanonicalTokenRelics()
    {
        return TokenRelicTypes
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetTokenRelicsByRarity(RelicRarity rarity)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetAvailableTokenRelicsByRarity(
        IRunState? runState,
        RelicRarity rarity,
        bool excludeDice = false)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .Where(relic => !excludeDice || !DiceSeriesHelper.IsDiceSeriesRelic(relic))
            .Where(relic => TokenSeriesAvailabilityHelper.IsRelicAvailableForRun(runState, relic))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetNonDiceTokenRelicsByRarity(RelicRarity rarity)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .Where(relic => !DiceSeriesHelper.IsDiceSeriesRelic(relic))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetAvailableNonDiceTokenRelicsByRarity(IRunState? runState, RelicRarity rarity)
    {
        return GetAvailableTokenRelicsByRarity(runState, rarity, excludeDice: true);
    }

    public static bool IsTokenRelic(RelicModel relic)
    {
        return TokenRelicTypes.Any(type => ModelDb.GetId(type) == relic.CanonicalInstance.Id);
    }

    public static bool IsSeriesTokenRelic(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return SeriesTokenRelicTypes.Any(type => ModelDb.GetId(type) == id);
    }

    public static bool IsDiceSeriesRelic(RelicModel relic)
    {
        return DiceSeriesHelper.IsDiceSeriesRelic(relic);
    }

    public static bool IsBankCardTokenRelic(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return id == ModelDb.GetId<TokenBlueBankCardGeneral>()
               || id == ModelDb.GetId<TokenPurpleBankCardIntermediate>()
               || id == ModelDb.GetId<TokenGoldBankCardPremium>()
               || id == ModelDb.GetId<TokenPurpleFlashlightStronglight>()
               || id == ModelDb.GetId<TokenGoldFlashlightFlashburst>()
               || id == ModelDb.GetId<TokenGoldStarCoinHammer>();
    }

    public static RelicModel? GetRandomTokenRelicForTreasure(IRunState? runState, RelicRarity rolledRarity, Rng rng)
    {
        var candidates = GetFallbackCandidates(runState, rolledRarity);
        return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
    }

    public static bool IsRelicAvailableForRun(IRunState? runState, RelicModel relic)
    {
        return TokenSeriesAvailabilityHelper.IsRelicAvailableForRun(runState, relic);
    }

    private static IReadOnlyList<RelicModel> GetFallbackCandidates(IRunState? runState, RelicRarity rolledRarity)
    {
        foreach (var rarity in GetRarityFallbackOrder(rolledRarity))
        {
            var candidates = GetAvailableTokenRelicsByRarity(runState, rarity);
            if (candidates.Count > 0)
                return candidates;
        }

        return TokenSeriesAvailabilityHelper.FilterAvailableForRun(runState, GetCanonicalTokenRelics());
    }

    private static IEnumerable<RelicRarity> GetRarityFallbackOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Uncommon, RelicRarity.Common, RelicRarity.Rare })
            if (fallback != rolledRarity)
                yield return fallback;
    }
}
