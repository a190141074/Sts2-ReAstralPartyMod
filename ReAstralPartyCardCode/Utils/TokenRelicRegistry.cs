using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

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
        typeof(TokenExclusiveCrossedTwinCarp),
        typeof(TokenExclusiveCursedSword),
        typeof(TokenExclusiveDreamshipModel),
        typeof(TokenExclusiveInfiniteSnake),
        typeof(TokenExclusiveLittleCarpDoll),
        typeof(TokenExclusiveLittleSnakeDoll),
        typeof(TokenExclusivePiercingGun),
        typeof(TokenExclusivePsychedelicSeafoodSoup),
        typeof(TokenExclusiveStormTalisman),
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

    public static bool IsTokenRelic(RelicModel relic)
    {
        return TokenRelicTypes.Any(type => ModelDb.GetId(type) == relic.CanonicalInstance.Id);
    }

    public static bool IsSeriesTokenRelic(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return SeriesTokenRelicTypes.Any(type => ModelDb.GetId(type) == id);
    }

    public static RelicModel? GetRandomTokenRelicForTreasure(RelicRarity rolledRarity, Rng rng)
    {
        var candidates = GetFallbackCandidates(rolledRarity);
        return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
    }

    private static IReadOnlyList<RelicModel> GetFallbackCandidates(RelicRarity rolledRarity)
    {
        foreach (var rarity in GetRarityFallbackOrder(rolledRarity))
        {
            var candidates = GetTokenRelicsByRarity(rarity);
            if (candidates.Count > 0)
                return candidates;
        }

        return GetCanonicalTokenRelics();
    }

    private static IEnumerable<RelicRarity> GetRarityFallbackOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Uncommon, RelicRarity.Common, RelicRarity.Rare })
            if (fallback != rolledRarity)
                yield return fallback;
    }
}
