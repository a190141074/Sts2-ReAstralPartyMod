using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureGainBlockWhisperPatch : IPatchMethod
{
    public static string PatchId => "creature_gain_block_whisper";

    public static string Description => "Reduce actual Block gain for units affected by Whisper";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock))];
    }

    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (amount <= 0m)
            return true;

        amount = WhisperPower.AdjustBlockAmount(creature, amount);
        if (amount > 0m &&
            amount < 1m &&
            creature.Player?.Creature == creature &&
            creature.Player.GetRelic<EnigmaticSevenCurses>() != null)
        {
            amount = 1m;
        }
        return true;
    }
}
