using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealBaiZeBlessingPatch : IPatchMethod
{
    public static string PatchId => "creature_heal_bai_ze_blessing";
    public static string Description => "Convert full-health healing into HalfLifeHeal while BaiZeBlessing is active";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (!BaiZeBlessingPower.ShouldConvertHeal(creature, amount))
            return true;

        var grantAmount = amount;
        amount = 0m;
        _ = PowerCmd.Apply<HalfLifeHealPower>(creature, grantAmount, creature, null, false);
        return true;
    }
}
