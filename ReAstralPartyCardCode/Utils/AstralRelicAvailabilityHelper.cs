using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralRelicAvailabilityHelper
{
    private static readonly HashSet<ModelId> AstralRelicIds = DeterministicTypeCatalog
        .GetAssignableTypes<AstralPartyRelicModel>(typeof(AstralPartyRelicModel).Assembly)
        .Where(static type => !type.IsAbstract)
        .Select(ModelDb.GetId)
        .ToHashSet();

    private static readonly HashSet<ModelId> JewelryRelicIds =
    [
        ModelDb.GetId<JewelryEchoOfDivineLight>(),
        ModelDb.GetId<JewelryMiniatureMilen>(),
        ModelDb.GetId<JewelryNightSkin>(),
        ModelDb.GetId<JewelrySolarCrown>(),
        ModelDb.GetId<JewelryWorldTears>()
    ];

    public static bool IsMoonPropRelic(RelicModel? relic)
    {
        return MoonPropShopExtraRelicsHelper.IsMoonPropRelic(relic);
    }

    public static bool IsJewelryRelic(RelicModel? relic)
    {
        if (relic == null)
            return false;

        return JewelryRelicIds.Contains(GetCanonicalRelicId(relic));
    }

    public static bool IsRelicEnabledForRun(IRunState? runState, RelicModel? relic)
    {
        if (relic == null)
            return false;
        if (!CompatContentGate.IsGameplayRelicAvailable(relic))
            return false;
        if (BannedRelicRegistry.IsBanned(ReAstralPartyModSettingsManager.GetBannedRelicIds(runState), relic))
            return false;
        if (runState != null && !IsAllowedByContentMode(runState, relic))
            return false;
        if (IsMoonPropRelic(relic) && !ReAstralPartyModSettingsManager.GetEnableMoonPropRelics(runState))
            return false;
        if (IsJewelryRelic(relic) && !ReAstralPartyModSettingsManager.GetEnableJewelryRelics(runState))
            return false;

        return true;
    }

    public static IReadOnlyList<RelicModel> FilterVisibleRelics(IRunState? runState, IEnumerable<RelicModel> relics)
    {
        return relics
            .Where(relic => IsRelicEnabledForRun(runState, relic))
            .DistinctBy(GetCanonicalRelicId)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> FilterVisibleRelics(
        IReadOnlySet<ModelId>? bannedRelicIds,
        AstralContentMode mode,
        IEnumerable<RelicModel> relics)
    {
        return FilterVisibleRelics(
            bannedRelicIds,
            mode,
            enableMoonPropRelics: mode != AstralContentMode.Vanilla,
            enableJewelryRelics: mode != AstralContentMode.Vanilla,
            relics);
    }

    public static IReadOnlyList<RelicModel> FilterVisibleRelics(
        IReadOnlySet<ModelId>? bannedRelicIds,
        AstralContentMode mode,
        bool enableMoonPropRelics,
        bool enableJewelryRelics,
        IEnumerable<RelicModel> relics)
    {
        return relics
            .Where(CompatContentGate.IsGameplayRelicAvailable)
            .Where(relic => !BannedRelicRegistry.IsBanned(bannedRelicIds, relic))
            .Where(relic => IsAllowedByContentMode(mode, relic))
            .Where(relic => !IsMoonPropRelic(relic) || enableMoonPropRelics)
            .Where(relic => !IsJewelryRelic(relic) || enableJewelryRelics)
            .DistinctBy(GetCanonicalRelicId)
            .ToList();
    }

    public static bool IsAllowedByContentMode(IRunState? runState, RelicModel relic)
    {
        return IsAllowedByContentMode(ReAstralPartyModSettingsManager.GetCurrentContentMode(runState), relic);
    }

    public static bool IsAllowedByContentMode(AstralContentMode mode, RelicModel relic)
    {
        var relicId = GetCanonicalRelicId(relic);
        if (!AstralRelicIds.Contains(relicId))
            return true;

        if (AstralContentModeRegistry.NormalizeMode(mode) != AstralContentMode.Vanilla)
            return true;

        if (PersonRelicRegistry.IsPersonRelic(relic))
            return true;
        if (TokenRelicRegistry.IsTokenRelic(relic))
            return true;

        return false;
    }

    private static ModelId GetCanonicalRelicId(RelicModel relic)
    {
        return relic.CanonicalInstance?.Id ?? relic.Id;
    }
}
