using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class DivineThronePower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal AppliedStrengthBonus;
    }

    private const decimal StrengthBonus = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
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

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount <= 0m)
            return;

        await PowerCmd.TickDownDuration(this);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthBonus != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthBonus, oldOwner, null, true);

        data.AppliedStrengthBonus = 0m;
    }

    private async Task SyncStrengthBonus(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredStrengthBonus = Amount > 0m ? StrengthBonus : 0m;
        var delta = desiredStrengthBonus - data.AppliedStrengthBonus;
        if (delta == 0m)
            return;

        data.AppliedStrengthBonus = desiredStrengthBonus;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }
}
