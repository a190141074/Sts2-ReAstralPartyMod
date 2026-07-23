using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SkillPointAuthorizationHelper
{
    private sealed class Scope
    {
        public required Creature Target { get; init; }
        public required RelicModel SourceRelic { get; init; }
    }

    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    public static bool IsAuthorizedPositiveGrant(Creature? target)
    {
        var scope = CurrentScope.Value;
        return target != null && scope != null && ReferenceEquals(scope.Target, target);
    }

    public static async Task RunAuthorizedGrant(Creature target, RelicModel sourceRelic, Func<Task> action)
    {
        var previousScope = CurrentScope.Value;
        CurrentScope.Value = new Scope
        {
            Target = target,
            SourceRelic = sourceRelic
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
