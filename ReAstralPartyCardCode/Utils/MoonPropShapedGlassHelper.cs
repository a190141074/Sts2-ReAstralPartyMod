using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class MoonPropShapedGlassHelper
{
    private const decimal CurrentHpCapRatio = 0.5m;
    private static readonly AsyncLocal<int> ClampDepth = new();

    public static bool HasActiveCap(Creature? creature)
    {
        return creature?.Player?.GetRelic<MoonPropShapedGlass>() != null;
    }

    public static decimal GetCurrentHpCap(Creature creature)
    {
        return creature.MaxHp * CurrentHpCapRatio;
    }

    public static async Task TryClampCurrentHpAsync(Creature? creature)
    {
        if (creature == null || ClampDepth.Value > 0 || !HasActiveCap(creature))
            return;

        var hpCap = GetCurrentHpCap(creature);
        if (creature.CurrentHp <= hpCap)
            return;

        ClampDepth.Value++;
        try
        {
            await CreatureCmd.SetCurrentHp(creature, hpCap);
        }
        finally
        {
            ClampDepth.Value = Math.Max(0, ClampDepth.Value - 1);
        }
    }
}
