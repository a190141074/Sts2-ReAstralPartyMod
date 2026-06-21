using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Combat.HandSize;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(MaxHandSizeCalculator), nameof(MaxHandSizeCalculator.ApplyHookListenerModifiers))]
public class PersonMaxHandSizeGuardPatch
{
    private static int _guardLogCount;

    [HarmonyPostfix]
    public static void Postfix(Player player, int currentMaxHandSize, ref int __result)
    {
        if (player.Relics.Count == 0)
            return;

        var guardedAmount = ApplyDirectRelicModifiers(player, currentMaxHandSize);
        if (guardedAmount <= __result)
            return;

        var originalAmount = __result;
        __result = guardedAmount;
        if (_guardLogCount++ < 8)
        {
            MainFile.Logger?.Info(
                $"[{MainFile.ModId}] Persona max-hand-size guard raised {player.NetId} from {originalAmount} to {guardedAmount}.");
        }
    }

    private static int ApplyDirectRelicModifiers(Player player, int currentMaxHandSize)
    {
        var amount = currentMaxHandSize;

        foreach (var relic in player.Relics.OfType<IMaxHandSizeModifier>())
            amount = relic.ModifyMaxHandSize(player, amount);

        foreach (var relic in player.Relics.OfType<IMaxHandSizeModifier>())
            amount = relic.ModifyMaxHandSizeLate(player, amount);

        return Math.Max(0, amount);
    }
}
