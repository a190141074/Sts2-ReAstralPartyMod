using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FanPower : AstralPartyPowerModel
{
    private const decimal StrengthPenalty = -1m;
    private const decimal IdolTargetAdditionalPenalty = -1m;

    private sealed class Data
    {
        public decimal AppliedStrengthPenalty;
    }

    public override PowerType Type => PowerType.Debuff;

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

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthPenalty != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthPenalty, oldOwner, null, true);

        data.AppliedStrengthPenalty = 0m;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return 0m;
        if (target == null || target.Side == Owner.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;

        return KawaiiPersonaHelper.IsIdolTarget(target)
            ? IdolTargetAdditionalPenalty
            : 0m;
    }

    private async Task SyncStrengthPenalty(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredPenalty = StrengthPenalty;
        var delta = desiredPenalty - data.AppliedStrengthPenalty;
        if (delta == 0m)
            return;

        data.AppliedStrengthPenalty = desiredPenalty;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }
}
