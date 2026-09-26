using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenRelicRegistry
{
    private static readonly Type[] TokenRelicTypes =
    [
        typeof(TokenBlueAtm),
        typeof(TokenBlueBankCardGeneral),
        typeof(TokenBlueBookmark),
        typeof(TokenBlueBoxingGloveGeneral),
        typeof(TokenBlueDie10),
        typeof(TokenBlueDie12),
        typeof(TokenBlueDie20),
        typeof(TokenBlueDie4),
        typeof(TokenBlueDie6),
        typeof(TokenBlueDie8),
        typeof(TokenBlueElegantFeather),
        typeof(TokenBlueEnergyRecycler),
        typeof(TokenBlueElectricBlade),
        typeof(TokenBlueFlashlightGeneral),
        typeof(TokenBlueGiantAnchor),
        typeof(TokenBlueHandheldFanSmall),
        typeof(TokenBlueMarkSprayCan),
        typeof(TokenBlueMedicalKitEmergencyTreatment),
        typeof(TokenBlueMotorcycleHelmetGeneral),
        typeof(TokenBluePiggyBank),
        typeof(TokenBlueSandwichBiscuitGeneral),
        typeof(TokenBlueSpeedRollerGeneral),
        typeof(TokenBlueTargetBoard),
        typeof(TokenBlueWarpEngine),
        typeof(TokenEternalStarlight),
        typeof(TokenExclusiveAncientWand),
        typeof(TokenExclusiveAdaptiveCore),
        typeof(TokenExclusiveBoutiqueSwordShield),
        typeof(TokenExclusiveBronzeGong),
        typeof(TokenExclusiveCandyMembershipCard),
        typeof(TokenExclusiveCrossedTwinCarp),
        typeof(TokenExclusiveCursedSword),
        typeof(TokenExclusiveDreamshipModel),
        typeof(TokenExclusiveEntertainmentTerminal),
        typeof(TokenExclusiveFitnessTracker),
        typeof(TokenExclusiveInfiniteSnake),
        typeof(TokenExclusiveLittleCarpDoll),
        typeof(TokenExclusiveLittleSnakeDoll),
        typeof(TokenExclusiveOriginalConsciousness),
        typeof(TokenExclusivePiercingGun),
        typeof(TokenExclusivePsychedelicSeafoodSoup),
        typeof(TokenExclusiveStunGun),
        typeof(TokenExclusiveStormTalisman),
        typeof(TokenExclusiveTimePocketWatch),
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
        typeof(TokenGoldColorfulSprayCan),
        typeof(TokenGoldBufferShield),
        typeof(TokenGoldEagleEyeScope),
        typeof(TokenGoldExplorationSatellite),
        typeof(TokenGoldExtraBattery),
        typeof(TokenGoldFlashlightFlashburst),
        typeof(TokenGoldFlyingStar),
        typeof(TokenGoldInitialPoint),
        typeof(TokenGoldArcaneCodex),
        typeof(TokenGoldMagicQuiver),
        typeof(TokenGoldMedicalKitCompleteTreatment),
        typeof(TokenGoldMotorcycleHelmetPremium),
        typeof(TokenGoldNinjaShuriken),
        typeof(TokenGoldPerpetualMotionMachine),
        typeof(TokenGoldElectromagneticGun),
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
        typeof(TokenPurpleFlyingStar),
        typeof(TokenPurpleFriendshipBadge),
        typeof(TokenPurpleHandheldFanLarge),
        typeof(TokenPurpleAirBag),
        typeof(TokenPurpleShockGloves),
        typeof(TokenPurpleElectricCore),
        typeof(TokenPurpleMembersReferencePremium),
        typeof(TokenPurpleMotorcycleHelmetIntermediate),
        typeof(TokenPurpleSandwichBiscuitIntermediate),
        typeof(TokenPurpleSandwichBiscuitMouse),
        typeof(TokenPurpleSharpeningStone),
        typeof(TokenPurpleSmartWatch),
        typeof(TokenPurpleSpeedRollerIntermediate),
        typeof(TokenPurpleTastyCandy)
    ];

    private static readonly IReadOnlyList<RelicModel> CanonicalTokenRelics = TokenRelicTypes
        .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
        .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
        .ToList();

    private static readonly HashSet<ModelId> TokenRelicIds = CanonicalTokenRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    private static readonly HashSet<ModelId> ExcludedFromRandomTokenPools =
    [
        ModelDb.GetId<TokenEternalStarlight>(),
        ModelDb.GetId<TokenGoldInitialPoint>()
    ];

    public static IReadOnlyList<RelicModel> GetCanonicalTokenRelics()
    {
        return CanonicalTokenRelics;
    }

    public static IReadOnlyList<RelicModel> GetTokenRelicsByRarity(RelicRarity rarity)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .Where(IsTokenRelicPoolCandidate)
            .Where(relic => !BannedRelicRegistry.IsBanned(
                ReAstralPartyModSettingsManager.BannedRelicIds,
                relic))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetAvailableTokenRelicsByRarity(
        IRunState? runState,
        RelicRarity rarity,
        bool excludeDice = false,
        bool includeChargeTokens = false)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .Where(IsTokenRelicPoolCandidate)
            .Where(relic => !excludeDice || !DiceSeriesHelper.IsDiceSeriesRelic(relic))
            .Where(relic => includeChargeTokens || !ChargeTokenRegistry.IsChargeToken(relic))
            .Where(relic => TokenSeriesAvailabilityHelper.IsRelicAvailableForRun(runState, relic))
            .Where(relic => !BannedRelicRegistry.IsBanned(
                ReAstralPartyModSettingsManager.GetBannedRelicIds(runState),
                relic))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetNonDiceTokenRelicsByRarity(RelicRarity rarity)
    {
        return GetCanonicalTokenRelics()
            .Where(relic => relic.Rarity == rarity)
            .Where(IsTokenRelicPoolCandidate)
            .Where(relic => !DiceSeriesHelper.IsDiceSeriesRelic(relic))
            .Where(relic => !BannedRelicRegistry.IsBanned(
                ReAstralPartyModSettingsManager.BannedRelicIds,
                relic))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetAvailableNonDiceTokenRelicsByRarity(IRunState? runState,
        RelicRarity rarity, bool includeChargeTokens = false)
    {
        return GetAvailableTokenRelicsByRarity(runState, rarity, true, includeChargeTokens);
    }

    public static bool IsTokenRelic(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return TokenRelicIds.Contains(id);
    }

    public static bool IsTokenRelicPoolCandidate(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return !ExcludedFromRandomTokenPools.Contains(id)
               && !ExclusiveRelicUnlockHelper.IsExclusiveSourceRelic(relic);
    }

    public static bool IsSeriesTokenRelic(RelicModel relic)
    {
        return TokenSeriesAvailabilityHelper.IsSeriesTokenRelic(relic);
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
        return TokenSeriesAvailabilityHelper.IsRelicAvailableForRun(runState, relic)
               && !BannedRelicRegistry.IsBanned(ReAstralPartyModSettingsManager.GetBannedRelicIds(runState), relic);
    }

    private static IReadOnlyList<RelicModel> GetFallbackCandidates(IRunState? runState, RelicRarity rolledRarity)
    {
        foreach (var rarity in GetRarityFallbackOrder(rolledRarity))
        {
            var candidates = GetAvailableTokenRelicsByRarity(runState, rarity);
            if (candidates.Count > 0)
                return candidates;
        }

        return TokenSeriesAvailabilityHelper.FilterAvailableForRun(runState, GetCanonicalTokenRelics())
            .Where(IsTokenRelicPoolCandidate)
            .Where(relic => !BannedRelicRegistry.IsBanned(
                ReAstralPartyModSettingsManager.GetBannedRelicIds(runState),
                relic))
            .ToList();
    }

    private static IEnumerable<RelicRarity> GetRarityFallbackOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Uncommon, RelicRarity.Common, RelicRarity.Rare })
            if (fallback != rolledRarity)
                yield return fallback;
    }
}
