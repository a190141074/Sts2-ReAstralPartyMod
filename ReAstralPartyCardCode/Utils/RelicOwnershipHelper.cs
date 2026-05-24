using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class RelicOwnershipHelper
{
    public static bool HasRelic<TRelic>(Player? owner)
        where TRelic : RelicModel
    {
        return owner?.GetRelic<TRelic>() != null;
    }

    public static bool HasAnyRelic(Player? owner, params Type[] relicTypes)
    {
        if (owner == null || relicTypes.Length == 0)
            return false;

        return relicTypes.Any(type => owner.Relics.Any(type.IsInstanceOfType));
    }

    public static bool HasAllRelics(Player? owner, params Type[] relicTypes)
    {
        if (owner == null || relicTypes.Length == 0)
            return false;

        return relicTypes.All(type => owner.Relics.Any(type.IsInstanceOfType));
    }

    public static Task RunByRelicOwnershipAsync<TRelic>(
        Player? owner,
        Func<Task> whenOwned,
        Func<Task>? whenMissing = null)
        where TRelic : RelicModel
    {
        return RunByConditionAsync(HasRelic<TRelic>(owner), whenOwned, whenMissing);
    }

    public static Task RunByAnyRelicOwnershipAsync(
        Player? owner,
        IEnumerable<Type> relicTypes,
        Func<Task> whenOwned,
        Func<Task>? whenMissing = null)
    {
        return RunByConditionAsync(HasAnyRelic(owner, relicTypes.ToArray()), whenOwned, whenMissing);
    }

    public static Task RunByAllRelicOwnershipAsync(
        Player? owner,
        IEnumerable<Type> relicTypes,
        Func<Task> whenOwned,
        Func<Task>? whenMissing = null)
    {
        return RunByConditionAsync(HasAllRelics(owner, relicTypes.ToArray()), whenOwned, whenMissing);
    }

    private static Task RunByConditionAsync(bool isOwned, Func<Task> whenOwned, Func<Task>? whenMissing)
    {
        return isOwned
            ? whenOwned()
            : whenMissing?.Invoke() ?? Task.CompletedTask;
    }
}
