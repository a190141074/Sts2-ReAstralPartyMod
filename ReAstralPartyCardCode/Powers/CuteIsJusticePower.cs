using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CuteIsJusticePower : AstralPartyPowerModel
{
    private decimal _appliedStrengthBonus;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthBonus();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthBonus();
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner == null || _appliedStrengthBonus == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrengthBonus, oldOwner, null, true);
        _appliedStrengthBonus = 0m;
    }

    private async Task SyncStrengthBonus()
    {
        if (Owner == null)
            return;

        var desiredStrength = Amount;
        var delta = desiredStrength - _appliedStrengthBonus;
        if (delta == 0m)
            return;

        _appliedStrengthBonus = desiredStrength;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, Owner, null, true);
    }
}