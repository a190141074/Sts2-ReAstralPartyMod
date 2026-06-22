using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class RelicCollectionVisibilityFilterHelper
{
    private static readonly HashSet<string> LoggedContexts = [];

    public static IReadOnlyList<RelicModel> FilterForCompendium(IEnumerable<RelicModel> relics, string context)
    {
        var settings = ReAstralPartyModSettingsManager.ReadLocalSettings();
        var bannedIds = DeserializeModelIds(settings.BannedRelicIds);
        var mode = ReAstralPartyModSettingsManager.GetCurrentContentMode();
        var filtered = AstralRelicAvailabilityHelper.FilterVisibleRelics(
            bannedIds,
            mode,
            ReAstralPartyModSettingsManager.GetEnableMoonPropRelics(null),
            ReAstralPartyModSettingsManager.GetEnableJewelryRelics(null),
            relics);

        var sourceCount = relics.Count();
        if (LoggedContexts.Add($"{mode}:{context}:{sourceCount}:{filtered.Count}"))
        {
            MainFile.Logger.Info(
                $"[RelicCollectionVisibilityFilter] mode={mode} context={context} source={sourceCount} filtered={filtered.Count}");
        }

        return filtered;
    }

    private static IReadOnlySet<ModelId> DeserializeModelIds(IEnumerable<string>? serializedIds)
    {
        var result = new HashSet<ModelId>();
        if (serializedIds == null)
            return result;

        foreach (var serializedId in serializedIds)
        {
            if (string.IsNullOrWhiteSpace(serializedId))
                continue;

            try
            {
                var parts = serializedId.Split('.', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                    result.Add(new ModelId(parts[0], parts[1]));
            }
            catch
            {
                // Ignore malformed ids in local compendium filtering.
            }
        }

        return result;
    }
}

public sealed class RelicCollectionAddRelicsFilterPatch : IPatchMethod
{
    public static string PatchId => "relic_collection_add_relics_filter";

    public static string Description => "Filter disabled relics before the compendium caches them";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NRelicCollection), nameof(NRelicCollection.AddRelics), [typeof(IEnumerable<RelicModel>)])];
    }

    public static void Prefix(ref IEnumerable<RelicModel> relics)
    {
        relics = RelicCollectionVisibilityFilterHelper.FilterForCompendium(relics, "collection_add");
    }
}

public sealed class RelicCollectionLoadRelicNodesFilterPatch : IPatchMethod
{
    public static string PatchId => "relic_collection_load_relic_nodes_filter";

    public static string Description => "Filter disabled relics before the compendium creates node entries";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NRelicCollectionCategory), "LoadRelicNodes", [typeof(IEnumerable<RelicModel>), typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)])];
    }

    public static void Prefix(ref IEnumerable<RelicModel> relics)
    {
        relics = RelicCollectionVisibilityFilterHelper.FilterForCompendium(relics, "load_nodes");
    }
}
