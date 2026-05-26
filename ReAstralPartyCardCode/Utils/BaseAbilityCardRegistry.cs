using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class BaseAbilityCardRegistry
{
    private static readonly Lazy<IReadOnlyList<Type>> CachedTypes = new(DiscoverBaseAbilityTypes);

    public static IReadOnlyList<Type> GetCandidateTypes()
    {
        return CachedTypes.Value;
    }

    public static IReadOnlyList<Type> GetCandidateTypes(Player? owner)
    {
        return GetCandidateTypes()
            .Where(type => BaseAbilityHelper.IsCardTypeAvailableForPlayer(type, owner))
            .ToArray();
    }

    public static CardModel? GetStableRandomCardModel(IRunState runState, params object?[] contextParts)
    {
        var candidates = GetCandidateTypes();
        if (candidates.Count == 0)
            return null;

        var selectedType = AstralStableRandom.Pick(
            candidates,
            type => type.Name,
            runState,
            contextParts);
        if (selectedType == null)
            return null;

        return ModelDb.GetById<CardModel>(ModelDb.GetId(selectedType));
    }

    public static CardModel? GetStableRandomCardModel(Player? owner, params object?[] contextParts)
    {
        if (owner?.RunState == null)
            return null;

        var candidates = GetCandidateTypes(owner);
        if (candidates.Count == 0)
            return null;

        var selectedType = AstralStableRandom.Pick(
            candidates,
            type => type.Name,
            owner.RunState,
            contextParts);
        if (selectedType == null)
            return null;

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
