using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class VampirePersonaHelper
{
    public static int GetCuteIsJusticeStacks(Creature? creature)
    {
        if (creature == null || creature.MaxHp <= 0m)
            return 0;

        var hpRatio = creature.CurrentHp / creature.MaxHp;
        var stacks = 0;
        if (hpRatio < 0.5m)
            stacks++;
        if (hpRatio < 0.25m)
            stacks++;
        if (hpRatio < 0.15m)
            stacks++;

        return stacks;
    }

    public static async Task SyncCuteIsJustice(Player? owner)
    {
        if (owner?.Creature?.CombatState == null)
            return;
        if (owner.GetRelic<PersonVampire>() == null)
            return;

        var desiredStacks = GetCuteIsJusticeStacks(owner.Creature);
        var existingPower = owner.Creature.GetPower<CuteIsJusticePower>();
        if (desiredStacks <= 0)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);

            return;
        }

        if (existingPower == null)
        {
            await PowerCmd.Apply(
                ModelDb.Power<CuteIsJusticePower>().ToMutable(),
                owner.Creature,
                desiredStacks,
                owner.Creature,
                null,
                false
            );
            return;
        }

        await PowerCmd.SetAmount<CuteIsJusticePower>(owner.Creature, desiredStacks, owner.Creature, null);
    }
}
