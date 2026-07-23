using System.Threading;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class PointPowerAuthorizationHelper
{
    private sealed class Scope
    {
        public required Creature Target { get; init; }
        public required Type PowerType { get; init; }
        public required RelicModel SourceRelic { get; init; }
    }

    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    public static bool IsAuthorizedPositiveGrant(Creature? target, Type powerType)
    {
        var scope = CurrentScope.Value;
        return target != null
               && scope != null
               && ReferenceEquals(scope.Target, target)
               && scope.PowerType == powerType;
    }

    public static async Task RunAuthorizedGrant(
        Creature target,
        RelicModel sourceRelic,
        Type powerType,
        Func<Task> action)
    {
        var previousScope = CurrentScope.Value;
        CurrentScope.Value = new Scope
        {
            Target = target,
            SourceRelic = sourceRelic,
            PowerType = powerType
        };

        try
        {
            await action();
        }
        finally
        {
            CurrentScope.Value = previousScope;
        }
    }
}
