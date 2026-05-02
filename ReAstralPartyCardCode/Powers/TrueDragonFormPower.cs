using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class TrueDragonFormPower : AstralPartyPowerModel
{
    private const decimal DamageBonus = 4m;
    private const decimal StrengthBonus = 2m;
    private const decimal DexterityBonus = 1m;
    private const decimal MaxHpBonus = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, StrengthBonus, applier, cardSource, true);
        await PowerCmd.Apply<DexterityPower>(Owner, DexterityBonus, applier, cardSource, true);
        await CreatureCmd.GainMaxHp(Owner, MaxHpBonus);
        await CreatureCmd.Heal(Owner, MaxHpBonus, false);
    }

    public override async Task AfterRemoved(Creature owner)
    {
        await PowerCmd.Apply<StrengthPower>(owner, -StrengthBonus, owner, null, true);
        await PowerCmd.Apply<DexterityPower>(owner, -DexterityBonus, owner, null, true);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 0m;
        if (amount <= 0m)
            return 0m;

        return DamageBonus;
    }
}