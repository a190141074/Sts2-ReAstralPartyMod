using System;
using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaRelicRegistry
{
    private static readonly IReadOnlyList<RelicModel> AllPersonaLikeRelics = DeterministicTypeCatalog
        .GetAssignableTypes<PersonaRelicBase>(typeof(PersonaRelicBase).Assembly)
        .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
        .DistinctBy(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .OrderBy(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
        .ToList();

    private static readonly IReadOnlyList<RelicModel> PersonaRelics = AllPersonaLikeRelics
        .Where(relic => !IsVariantPersonaRelicId(relic.CanonicalInstance?.Id ?? relic.Id))
        .ToList();

    private static readonly IReadOnlyList<RelicModel> VariantPersonaRelics = AllPersonaLikeRelics
        .Where(relic => IsVariantPersonaRelicId(relic.CanonicalInstance?.Id ?? relic.Id))
        .ToList();

    private static readonly HashSet<ModelId> PersonaRelicIds = PersonaRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    private static readonly HashSet<ModelId> VariantPersonaRelicIds = VariantPersonaRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    public static IReadOnlyList<RelicModel> GetCanonicalPersonaRelics()
    {
        return PersonaRelics;
    }

    public static IReadOnlyList<RelicModel> GetCanonicalPersonaRelicsFiltered(
        IReadOnlySet<ModelId>? bannedPersonaRelicIds)
    {
        return FilterBannedPersonaRelics(PersonaRelics, bannedPersonaRelicIds);
    }

    public static IReadOnlyList<RelicModel> GetCanonicalVariantPersonaRelics()
    {
        return VariantPersonaRelics;
    }

    public static bool IsPersonaRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return PersonaRelicIds.Contains(id);
    }

    public static bool IsVariantPersonaRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return VariantPersonaRelicIds.Contains(id);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(Player owner)
    {
        return GetAvailablePersonaRelics(owner, bannedPersonaRelicIds: null);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(
        Player owner,
        IReadOnlySet<ModelId>? bannedPersonaRelicIds)
    {
        var ownedRelicIds = owner.Relics
            .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToHashSet();

        return GetCanonicalPersonaRelicsFiltered(bannedPersonaRelicIds)
            .Where(relic => !ownedRelicIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
            .ToList();
    }

    private static IReadOnlyList<RelicModel> FilterBannedPersonaRelics(
        IReadOnlyList<RelicModel> source,
        IReadOnlySet<ModelId>? bannedPersonaRelicIds)
    {
        if (bannedPersonaRelicIds == null || bannedPersonaRelicIds.Count == 0)
            return source;

        var filtered = source
            .Where(relic => !bannedPersonaRelicIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
            .ToList();

        return filtered.Count > 0 ? filtered : source;
    }

    private static bool IsVariantPersonaRelicId(ModelId id)
    {
        return id.Entry.Contains("_VARIANT_PERSON_", StringComparison.Ordinal);
    }
}
