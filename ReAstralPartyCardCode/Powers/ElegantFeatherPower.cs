using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ElegantFeatherPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal AppliedStrengthBonus;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not ElegantFeatherPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        return false;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthBonus(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthBonus(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthBonus != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthBonus, oldOwner, null, true);

        data.AppliedStrengthBonus = 0m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return;
        if (Amount <= 0m)
            return;
        if (result.UnblockedDamage <= 0m)
            return;

        Flash();
        await PowerCmd.TickDownDuration(this);
    }

    private async Task SyncStrengthBonus(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredStrengthBonus = Math.Max(Amount, 0m);
        var delta = desiredStrengthBonus - data.AppliedStrengthBonus;
        if (delta == 0m)
            return;

        data.AppliedStrengthBonus = desiredStrengthBonus;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }
}
