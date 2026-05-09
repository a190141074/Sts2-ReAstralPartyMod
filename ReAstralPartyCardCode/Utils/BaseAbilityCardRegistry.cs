using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class BaseAbilityCardRegistry
{
    private static readonly Lazy<IReadOnlyList<Type>> CachedTypes = new(DiscoverBaseAbilityTypes);

    public static IReadOnlyList<Type> GetCandidateTypes()
    {
        return CachedTypes.Value;
    }

    public static CardModel? GetDeterministicCardModel(params object?[] contextParts)
    {
        var candidates = GetCandidateTypes();
        if (candidates.Count == 0)
            return null;

        var ordered = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            candidates,
            type => type.Name,
            contextParts);
        var selectedType = ordered[0];
        return ModelDb.GetById<CardModel>(ModelDb.GetId(selectedType));
    }

    private static IReadOnlyList<Type> DiscoverBaseAbilityTypes()
    {
        return DeterministicTypeCatalog
            .GetSortedTypes(typeof(AstralPartyCardModel).Assembly)
            .Where(type =>
                !type.IsAbstract
                && typeof(AstralPartyCardModel).IsAssignableFrom(type)
                && type.Name.StartsWith("BaseAbility", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
