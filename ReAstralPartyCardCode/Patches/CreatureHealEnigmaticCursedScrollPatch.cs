using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealEnigmaticCursedScrollPatch : IPatchMethod
{
    public static string PatchId => "creature_heal_enigmatic_cursed_scroll";

    public static string Description => "Increase healing received by Enigmatic Cursed Scroll owner based on weighted curse count";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (amount <= 0m)
            return true;

        amount = EnigmaticCursedScroll.AdjustHealAmount(creature, amount);
        return true;
    }
}
