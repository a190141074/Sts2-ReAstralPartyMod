using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class MascotGirlMimiTokenMemoryHelper
{
    public static IReadOnlyList<RelicModel> GetBridgeableUnownedTokenRelics(
        Player owner,
        Creature? creature,
        bool includeSeriesRelics = true)
    {
        var ownedRelicIds = owner.Relics
            .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToHashSet();

        return TokenRelicRegistry.GetCanonicalTokenRelics()
            .Where(relic => !ownedRelicIds.Contains(relic.Id))
            .Where(relic => creature == null || TokenRelicBridgeHelper.GetExistingBridgePower(creature, relic.Id) == null)
            .Where(relic => TokenRelicBridgeHelper.CanBridgeTokenRelic(relic, out _))
            .Where(relic => includeSeriesRelics || !TokenRelicRegistry.IsSeriesTokenRelic(relic))
            .ToList();
    }

    public static bool PlayerOwnsTokenRelic(Player? owner, ModelId tokenRelicId)
    {
        if (owner == null)
            return false;

        return owner.Relics.Any(relic => (relic.CanonicalInstance?.Id ?? relic.Id) == tokenRelicId);
    }

    public static PersonalityDerivativeMascotGirlMimiTokenMemory? GetMemoryRelic(Player? owner)
    {
        return owner?.GetRelic<PersonalityDerivativeMascotGirlMimiTokenMemory>();
    }
}
