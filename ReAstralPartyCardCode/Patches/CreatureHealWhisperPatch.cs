using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealWhisperPatch : IPatchMethod
{
    public static string PatchId => "creature_heal_whisper";

    public static string Description => "Reduce actual healing for units affected by Whisper";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (amount <= 0m)
            return true;

        amount = WhisperPower.AdjustHealAmount(creature, amount);
        amount = LucidDreamMaliceRuntimeHelper.AdjustHealAmount(creature, amount);
        return true;
    }
}
