using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class DiceSeriesHelper
{
    private static readonly Type[] DiceRelicTypes =
    [
        typeof(TokenBlueDie4),
        typeof(TokenBlueDie6),
        typeof(TokenBlueDie8),
        typeof(TokenBlueDie10),
        typeof(TokenBlueDie12),
        typeof(TokenBlueDie20)
    ];

    private static readonly IReadOnlyList<RelicModel> CanonicalDiceRelics = DiceRelicTypes
        .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
        .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
        .ToList();

    private static readonly HashSet<ModelId> DiceRelicIds = CanonicalDiceRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    public static IReadOnlyList<RelicModel> GetCanonicalDiceRelics()
    {
        return CanonicalDiceRelics;
    }

    public static bool IsDiceSeriesRelic(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return DiceRelicIds.Contains(id);
    }

    public static bool OwnsFullDiceSet(Player? owner)
    {
        return owner != null && GetMissingDiceRelics(owner).Count == 0;
    }

    public static IReadOnlyList<RelicModel> GetMissingDiceRelics(Player owner)
    {
        var ownedIds = owner.Relics
            .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToHashSet();

        return CanonicalDiceRelics
            .Where(relic => !ownedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
            .ToList();
    }

    public static RelicModel? GetNextMissingDiceRelic(Player? owner)
    {
        return owner == null ? null : GetMissingDiceRelics(owner).FirstOrDefault();
    }

    public static int CountOwnedDiceRelics(Player? owner)
    {
        return owner == null ? 0 : CanonicalDiceRelics.Count - GetMissingDiceRelics(owner).Count;
    }
}
