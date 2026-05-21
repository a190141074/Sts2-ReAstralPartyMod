using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

internal static class CompatContentGate
{
    public static bool IsExternalCompatRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        return canonicalRelic switch
        {
            VariantPersonWindchaserThePlaneswalker => true,
            _ => false
        };
    }

    public static bool IsGameplayRelicAvailable(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        return canonicalRelic switch
        {
            VariantPersonWindchaserThePlaneswalker => WindchaserCompat.IsLoaded(),
            _ => true
        };
    }

    public static bool IsCompendiumCardVisible(CardModel? card)
    {
        if (card == null)
            return false;

        return card switch
        {
            SkillGrantSpark => WindchaserCompat.IsLoaded(),
            _ => true
        };
    }

    public static bool ShouldForceStartingVariantPersonaForRun(RunStateLike runState)
    {
        return WindchaserCompat.IsLoaded()
               && runState.Players.Any(WindchaserCompat.IsCharacter);
    }

    internal readonly record struct RunStateLike(IReadOnlyList<MegaCrit.Sts2.Core.Entities.Players.Player> Players);
}
