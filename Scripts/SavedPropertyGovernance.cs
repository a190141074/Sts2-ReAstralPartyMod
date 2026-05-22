using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod;

internal static class SavedPropertyGovernance
{
    private static readonly string[] ForbiddenNameMarkers =
    [
        "Ui",
        "UI",
        "Hover",
        "Preview",
        "Display",
        "Visual",
        "Tooltip",
        "Cache",
        "TempSelection",
        "PendingLocal"
    ];

    private static readonly (string Category, string[] Markers)[] CategoryRules =
    [
        ("PendingCombatStart", ["PendingCombatStart"]),
        ("TriggeredThisTurn", ["TriggeredThisTurn"]),
        ("LastProcessedRound", ["LastProcessedRound"]),
        ("Permanent", ["Permanent"]),
        ("Observed", ["Observed"]),
        ("Counter", ["Counter"]),
        ("Stacks", ["Stacks"]),
        ("Progress", ["Progress"]),
        ("ThisCombat", ["ThisCombat"]),
        ("ThisTurn", ["ThisTurn"]),
        ("Bonus", ["Bonus"]),
        ("Active", ["Active"]),
        ("NetBinding", ["NetIdRaw", "NetId"]),
        ("UsageCount", ["UseCount", "Uses", "TriggerCount", "CardsPlayed", "Refunds", "GrowthCount", "RemainingTriggers"]),
        ("ResourceAmount", ["Amount", "Damage", "Strength", "Dexterity", "Energy", "Block", "Heal"]),
        ("RunProgression", ["Floor", "ActIndex", "Round", "Steps", "Charges", "Charge", "Quota", "Marked", "Coords", "Consecutive", "Visited", "PendingExtraTurn", "ThisRun", "AscensionCount", "RemainingRerolls"]),
        ("EconomyTracking", ["Spent", "Earned", "Accumulated", "Applied", "RewardCount"]),
        ("StateFlag", ["IgnoreBlock", "Mode", "Initialization", "PaysOn", "FirstAttack", "Pending", "FusionProcessed", "Json", "RelicId"])
    ];

    public static void LogGovernanceSummary(Assembly assembly)
    {
        var diagnostics = InspectAssembly(assembly);
        MainFile.Logger.Info(
            $"{MainFile.ModId} saved property governance | types={diagnostics.TypeCount} | properties={diagnostics.PropertyCount} | suspicious={diagnostics.SuspiciousCount} | uncategorized={diagnostics.UncategorizedCount} | categorized={diagnostics.CategorizedCount} | categories={diagnostics.CategorySummary} | fingerprint={diagnostics.Fingerprint}");

        foreach (var warning in diagnostics.Warnings)
            MainFile.Logger.Warn($"{MainFile.ModId} saved property governance warning | {warning}");
    }

    private static GovernanceDiagnostics InspectAssembly(Assembly assembly)
    {
        var warnings = new List<string>();
        var typeCount = 0;
        var propertyCount = 0;
        var suspiciousCount = 0;
        var categorizedCount = 0;
        var uncategorizedWarnings = new List<string>();
        var categoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var fingerprintParts = new List<string>();

        var modelTypes = DeterministicTypeCatalog
            .GetAssignableTypes<AbstractModel>(assembly, HasSavedProperties)
            .ToArray();

        foreach (var modelType in modelTypes)
        {
            typeCount++;
            foreach (var property in GetSavedProperties(modelType))
            {
                propertyCount++;
                var matchedCategory = ResolveCategory(property.Name);
                if (matchedCategory != null)
                {
                    categorizedCount++;
                    categoryCounts.TryGetValue(matchedCategory, out var categoryCount);
                    categoryCounts[matchedCategory] = categoryCount + 1;
                }
                else
                    uncategorizedWarnings.Add($"{modelType.FullName}.{property.Name}");

                fingerprintParts.Add($"{modelType.FullName}.{property.Name}:{matchedCategory ?? "uncategorized"}");

                if (!ForbiddenNameMarkers.Any(marker =>
                        property.Name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    continue;

                suspiciousCount++;
                warnings.Add($"{modelType.FullName}.{property.Name}");
            }
        }

        warnings.AddRange(uncategorizedWarnings.Select(static warning => $"uncategorized:{warning}"));
        var categorySummary = string.Join(",",
            categoryCounts.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}:{pair.Value}"));
        var fingerprint = ComputeFingerprint(fingerprintParts);
        return new GovernanceDiagnostics(typeCount, propertyCount, categorizedCount, suspiciousCount,
            uncategorizedWarnings.Count, warnings, categorySummary, fingerprint);
    }

    private static bool HasSavedProperties(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(static property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }

    private static IReadOnlyList<PropertyInfo> GetSavedProperties(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.GetCustomAttribute<SavedPropertyAttribute>() != null)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ResolveCategory(string propertyName)
    {
        foreach (var (category, markers) in CategoryRules)
        {
            if (markers.Any(marker => propertyName.Contains(marker, StringComparison.Ordinal)))
                return category;
        }

        return null;
    }

    private sealed record GovernanceDiagnostics(
        int TypeCount,
        int PropertyCount,
        int CategorizedCount,
        int SuspiciousCount,
        int UncategorizedCount,
        IReadOnlyList<string> Warnings,
        string CategorySummary,
        string Fingerprint);

    private static string ComputeFingerprint(IEnumerable<string> parts)
    {
        unchecked
        {
            var hash = 17;
            foreach (var part in parts.OrderBy(static part => part, StringComparer.Ordinal))
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(part);

            return hash.ToString("X8");
        }
    }
}
