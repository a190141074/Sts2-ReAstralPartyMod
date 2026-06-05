using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ExclusiveRelicUnlockHelper
{
    private static readonly IReadOnlyList<RelicModel> ExclusiveRelics =
    [
        ModelDb.Relic<TokenBlueDie4>(),
        ModelDb.Relic<TokenBlueDie8>(),
        ModelDb.Relic<TokenBlueDie10>(),
        ModelDb.Relic<TokenBlueDie12>(),
        ModelDb.Relic<TokenBlueDie20>(),
        ModelDb.Relic<PvzRareHyperTemporalNut>(),
        ModelDb.Relic<PvzRareSunshineNut>(),
        ModelDb.Relic<PvzRareBigMouthedNut>(),
        ModelDb.Relic<PvzRareAngmaoNut>(),
        ModelDb.Relic<PvzUltimateHyperSpacetimeNut>(),
        ModelDb.Relic<PvzUltimateSunshineEmperorNut>(),
        ModelDb.Relic<EnigmaticEtheriumIngot>(),
        ModelDb.Relic<EnigmaticNefariousEssence>(),
        ModelDb.Relic<EnigmaticNetheriteIngot>(),
        ModelDb.Relic<EnigmaticRedstoneDust>(),
        ModelDb.Relic<EnigmaticGhastTear>(),
        ModelDb.Relic<EnigmaticBlazePowder>(),
        ModelDb.Relic<EnigmaticEnderEye>(),
        ModelDb.Relic<EnigmaticEarthHeart>(),
        ModelDb.Relic<EnigmaticPhantomMembrane>(),
        ModelDb.Relic<EnigmaticDarkestScroll>(),
        ModelDb.Relic<EnigmaticEnchantedBook>(),
        ModelDb.Relic<EnigmaticDye>(),
        ModelDb.Relic<EnigmaticEnchantedFeather>(),
        ModelDb.Relic<EnigmaticGemRing>(),
        ModelDb.Relic<EnigmaticGoldIngot>(),
        ModelDb.Relic<EnigmaticTriccScroll>(),
        ModelDb.Relic<EnigmaticExperienceBottle>(),
        ModelDb.Relic<EnigmaticEmerald>(),
        ModelDb.Relic<EnigmaticNetherStar>(),
        ModelDb.Relic<EnigmaticLapisLazuli>(),
        ModelDb.Relic<EnigmaticCryingObsidian>(),
        ModelDb.Relic<EnigmaticAbyssalHeart>(),
        ModelDb.Relic<EnigmaticAstralDust>(),
        ModelDb.Relic<EnigmaticHeartOfTheSea>(),
        ModelDb.Relic<EnigmaticSynthesisTwistedHeart>(),
        ModelDb.Relic<EnigmaticSynthesisCosmicHeart>(),
        ModelDb.Relic<EnigmaticSynthesisEtheriumHelmet>(),
        ModelDb.Relic<EnigmaticSynthesisEtheriumCuirass>(),
        ModelDb.Relic<EnigmaticSynthesisEtheriumGreaves>(),
        ModelDb.Relic<EnigmaticSynthesisEtheriumBoots>(),
        ModelDb.Relic<EnigmaticSynthesisTheTwist>(),
        ModelDb.Relic<EnigmaticSynthesisAvariceScroll>(),
        ModelDb.Relic<EnigmaticSynthesisRecallPotion>(),
        ModelDb.Relic<EnigmaticSynthesisEnchanterPearl>(),
        ModelDb.Relic<EnigmaticSynthesisEscapeScroll>(),
        ModelDb.Relic<EnigmaticSynthesisTheInfinitum>()
    ];

    private static readonly HashSet<ModelId> ExclusiveRelicIds = ExclusiveRelics
        .Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    public static IReadOnlyList<RelicModel> GetExclusiveSourceRelics()
    {
        return ExclusiveRelics;
    }

    public static bool IsExclusiveSourceRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return ExclusiveRelicIds.Contains(id);
    }

    public static void MarkRelicUnlockedForCurrentRunAndProfile(Player? owner, RelicModel? relic)
    {
        if (owner == null || relic == null)
            return;
        if (!IsExclusiveSourceRelic(relic))
            return;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        MarkSeenInProgress(canonicalRelic);
        MainFile.Logger.Info(
            $"[ExclusiveRelicUnlock] Marking runtime unlock for {canonicalRelic.Id.Entry} | owner={owner.NetId}");
    }

    public static void MarkRelicsSeenAndUnlockedForCollections(
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> unlockedRelics)
    {
        if (!ContainsAnyExclusiveRelic(seenRelics) && !ContainsAnyExclusiveRelic(unlockedRelics))
            return;

        foreach (var relic in ExclusiveRelics)
        {
            seenRelics.Add(relic);
            unlockedRelics.Add(relic);
        }
    }

    public static bool ShouldForceUnlockedRewardPresentation(RelicModel? relic)
    {
        return IsExclusiveSourceRelic(relic);
    }

    public static bool IsDiceSeriesRelic(RelicModel? relic)
    {
        return relic != null && DiceSeriesHelper.IsDiceSeriesRelic(relic);
    }

    private static bool ContainsAnyExclusiveRelic(IEnumerable<RelicModel> source)
    {
        return source.Any(IsExclusiveSourceRelic);
    }

    private static void MarkSeenInProgress(RelicModel relic)
    {
        var canonicalRelic = relic.CanonicalInstance ?? relic;
        var progress = SaveManager.Instance?.Progress;
        if (progress == null)
            return;

        progress.MarkRelicAsSeen(canonicalRelic.Id);
    }
}
