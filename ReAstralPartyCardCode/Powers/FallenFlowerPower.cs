using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FallenFlowerPower : AstralPartyPowerModel
{
    private const decimal DamageTakenBonusPercent = 0.12m;
    private const decimal Duration = 2m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => (int)Amount;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not FallenFlowerPower)
            return false;
        if (target != Owner || amount <= 0m)
            return false;

        modifiedAmount = Math.Max(Duration - Amount, 0m);
        return true;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m || Amount <= 0m)
            return 0m;

        return amount * DamageTakenBonusPercent;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner != null && side == Owner.Side)
            await PowerCmd.TickDownDuration(this);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (target != Owner || !result.WasTargetKilled)
            return;

        var relicOwner = Applier?.Player?.GetRelic<VariantPersonTwelveFlowersCup>();
        if (relicOwner == null)
            return;

        await relicOwner.TryGrantFallenFlowerEnergy(choiceContext);
    }
}
