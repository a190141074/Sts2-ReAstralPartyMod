using System.Threading;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class PlayerGainGoldEnigmaticCursedScrollPatch : IPatchMethod
{
    private static readonly AsyncLocal<int> Depth = new();

    public static string PatchId => "player_gain_gold_enigmatic_cursed_scroll";

    public static string Description => "Increase gained gold based on weighted curse count for Enigmatic Cursed Scroll";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(PlayerCmd), nameof(PlayerCmd.GainGold))];
    }

    public static bool Prefix(ref decimal amount, Player player)
    {
        if (Depth.Value > 0 || amount <= 0m)
            return true;

        Depth.Value++;
        try
        {
            amount = EnigmaticCursedScroll.AdjustGoldGainAmount(player, amount);
            return true;
        }
        finally
        {
            Depth.Value = Math.Max(0, Depth.Value - 1);
        }
    }
}
