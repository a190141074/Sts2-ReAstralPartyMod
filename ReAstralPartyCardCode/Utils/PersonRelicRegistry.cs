using System;
using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonRelicRegistry
{
    private static readonly IReadOnlyList<RelicModel> AllPersonLikeRelics = DeterministicTypeCatalog
        .GetAssignableTypes<PersonRelicBase>(typeof(PersonRelicBase).Assembly)
        .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
        .DistinctBy(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .OrderBy(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
        .ToList();

    private static readonly IReadOnlyList<RelicModel> PersonRelics = AllPersonLikeRelics
        .Where(relic => !IsVariantPersonRelicId(relic.CanonicalInstance?.Id ?? relic.Id))
        .ToList();

    private static readonly IReadOnlyList<RelicModel> VariantPersonRelics = AllPersonLikeRelics
        .Where(relic => IsVariantPersonRelicId(relic.CanonicalInstance?.Id ?? relic.Id))
        .ToList();

    private static readonly HashSet<ModelId> PersonRelicIds = PersonRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    private static readonly HashSet<ModelId> VariantPersonRelicIds = VariantPersonRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    public static IReadOnlyList<RelicModel> GetCanonicalPersonRelics()
    {
        return FilterSoftCompatRelics(PersonRelics);
    }

    public static IReadOnlyList<RelicModel> GetCanonicalPersonaRelics()
    {
        return GetCanonicalPersonRelics();
    }

    public static IReadOnlyList<RelicModel> GetCanonicalPersonRelicsFiltered(
        IReadOnlySet<ModelId>? bannedPersonRelicIds)
    {
        return FilterBannedRelics(PersonRelics, bannedPersonRelicIds);
    }

    public static IReadOnlyList<RelicModel> GetCanonicalPersonaRelicsFiltered(
        IReadOnlySet<ModelId>? bannedPersonRelicIds)
    {
        return GetCanonicalPersonRelicsFiltered(bannedPersonRelicIds);
    }

    public static IReadOnlyList<RelicModel> GetCanonicalVariantPersonRelics()
    {
        return FilterSoftCompatRelics(VariantPersonRelics);
    }

    public static IReadOnlyList<RelicModel> GetCanonicalVariantPersonaRelics()
    {
        return GetCanonicalVariantPersonRelics();
    }

    public static IReadOnlyList<RelicModel> GetStartingVariantPersonRelics()
    {
        return GetCanonicalVariantPersonRelics();
    }

    public static IReadOnlyList<RelicModel> GetStartingVariantPersonaRelics()
    {
        return GetStartingVariantPersonRelics();
    }

    public static IReadOnlyList<RelicModel> GetStartingBuiltInVariantPersonRelics()
    {
        return GetCanonicalVariantPersonRelics()
            .Where(relic => !CompatContentGate.IsExternalCompatRelic(relic))
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetStartingBuiltInVariantPersonaRelics()
    {
        return GetStartingBuiltInVariantPersonRelics();
    }

    public static bool IsPersonRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return PersonRelicIds.Contains(id);
    }

    public static bool IsPersonaRelic(RelicModel? relic)
    {
        return IsPersonRelic(relic);
    }

    public static bool IsVariantPersonRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return VariantPersonRelicIds.Contains(id);
    }

    public static bool IsVariantPersonaRelic(RelicModel? relic)
    {
        return IsVariantPersonRelic(relic);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonRelics(Player owner)
    {
        return GetAvailablePersonRelics(owner, bannedPersonRelicIds: null);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(Player owner)
    {
        return GetAvailablePersonRelics(owner);
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonRelics(
        Player owner,
        IReadOnlySet<ModelId>? bannedPersonRelicIds)
    {
        var ownedRelicIds = owner.Relics
            .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToHashSet();

        return GetCanonicalPersonRelicsFiltered(bannedPersonRelicIds)
            .Where(relic => !ownedRelicIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
            .ToList();
    }

    public static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(
        Player owner,
        IReadOnlySet<ModelId>? bannedPersonRelicIds)
    {
        return GetAvailablePersonRelics(owner, bannedPersonRelicIds);
    }

    private static IReadOnlyList<RelicModel> FilterBannedRelics(
        IReadOnlyList<RelicModel> source,
        IReadOnlySet<ModelId>? bannedPersonRelicIds)
    {
        if (bannedPersonRelicIds == null || bannedPersonRelicIds.Count == 0)
            return source;

        var filtered = BannedRelicRegistry.FilterBannedRelics(source, bannedPersonRelicIds);

        return filtered.Count > 0 ? filtered : source;
    }

    private static IReadOnlyList<RelicModel> FilterSoftCompatRelics(IReadOnlyList<RelicModel> source)
    {
        return source
            .Where(CompatContentGate.IsGameplayRelicAvailable)
            .Where(relic => AstralRelicAvailabilityHelper.IsAllowedByContentMode(
                ReAstralPartyModSettingsManager.GetCurrentContentMode(),
                relic))
            .ToList();
    }

    private static bool IsVariantPersonRelicId(ModelId id)
    {
        return id.Entry.Contains("_VARIANT_PERSON_", StringComparison.Ordinal);
    }
}
