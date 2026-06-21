using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public enum BannedRelicCategory
{
    Persona = 0,
    VariantPersona = 1,
    PersonalityDerivative = 2,
    Token = 3,
    Other = 4
}

public static class BannedRelicRegistry
{
    private static readonly IReadOnlyList<RelicModel> AllAstralRelics = BuildAllAstralRelics();

    private static readonly IReadOnlyList<RelicModel> PersonalityDerivativeRelics = AllAstralRelics
        .Where(IsPersonalityDerivativeRelicInternal)
        .ToList();

    private static readonly HashSet<ModelId> PersonalityDerivativeRelicIds = PersonalityDerivativeRelics
        .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
        .ToHashSet();

    private static readonly IReadOnlyList<RelicModel> OtherRelics = AllAstralRelics
        .Where(relic => !PersonaRelicRegistry.IsPersonaRelic(relic))
        .Where(relic => !PersonaRelicRegistry.IsVariantPersonaRelic(relic))
        .Where(relic => !IsPersonalityDerivativeRelicInternal(relic))
        .Where(relic => !TokenRelicRegistry.IsTokenRelic(relic))
        .ToList();

    private static readonly IReadOnlyList<RelicModel> CanonicalBannableRelics =
    [
        .. PersonaRelicRegistry.GetCanonicalPersonaRelics(),
        .. PersonaRelicRegistry.GetCanonicalVariantPersonaRelics(),
        .. PersonalityDerivativeRelics,
        .. TokenRelicRegistry.GetCanonicalTokenRelics(),
        .. OtherRelics
    ];

    public static IReadOnlyList<BannedRelicCategory> Categories { get; } =
    [
        BannedRelicCategory.Persona,
        BannedRelicCategory.VariantPersona,
        BannedRelicCategory.PersonalityDerivative,
        BannedRelicCategory.Token,
        BannedRelicCategory.Other
    ];

    public static IReadOnlyList<RelicModel> GetCanonicalBannableRelics()
    {
        return CanonicalBannableRelics;
    }

    public static IReadOnlyList<RelicModel> GetCanonicalRelics(BannedRelicCategory category)
    {
        return category switch
        {
            BannedRelicCategory.Persona => PersonaRelicRegistry.GetCanonicalPersonaRelics(),
            BannedRelicCategory.VariantPersona => PersonaRelicRegistry.GetCanonicalVariantPersonaRelics(),
            BannedRelicCategory.PersonalityDerivative => PersonalityDerivativeRelics,
            BannedRelicCategory.Token => TokenRelicRegistry.GetCanonicalTokenRelics(),
            BannedRelicCategory.Other => OtherRelics,
            _ => []
        };
    }

    public static bool IsBanned(IReadOnlySet<ModelId>? bannedRelicIds, RelicModel? relic)
    {
        if (relic == null || bannedRelicIds == null || bannedRelicIds.Count == 0)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return bannedRelicIds.Contains(id);
    }

    public static IReadOnlyList<RelicModel> FilterBannedRelics(
        IEnumerable<RelicModel> source,
        IReadOnlySet<ModelId>? bannedRelicIds)
    {
        if (bannedRelicIds == null || bannedRelicIds.Count == 0)
            return source.ToList();

        return source
            .Where(relic => !IsBanned(bannedRelicIds, relic))
            .ToList();
    }

    public static bool IsPersonalityDerivativeRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return PersonalityDerivativeRelicIds.Contains(id);
    }

    private static bool IsPersonalityDerivativeRelicInternal(RelicModel relic)
    {
        var id = relic.CanonicalInstance?.Id ?? relic.Id;
        return id.Entry.Contains("_PERSONALITY_DERIVATIVE_", StringComparison.Ordinal);
    }

    private static IReadOnlyList<RelicModel> BuildAllAstralRelics()
    {
        var relics = DeterministicTypeCatalog
            .GetAssignableTypes<AstralPartyRelicModel>(typeof(AstralPartyRelicModel).Assembly)
            .Where(static type => !type.IsAbstract)
            .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToList();

        TryAppendCanonicalRelic<MoonPropBeadsOfFealty>(relics);

        return relics
            .DistinctBy(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .Where(CompatContentGate.IsGameplayRelicAvailable)
            .OrderBy(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
            .ToList();
    }

    private static void TryAppendCanonicalRelic<TRelic>(ICollection<RelicModel> relics)
        where TRelic : RelicModel
    {
        try
        {
            relics.Add(ModelDb.Relic<TRelic>());
        }
        catch
        {
            // Keep the registry usable even if a specific relic failed to register.
        }
    }
}
