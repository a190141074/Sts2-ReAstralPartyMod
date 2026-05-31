using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CeremonialBombPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => Amount;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Player == null || Amount <= 0m)
            return;
        if (target.Player == null || result.TotalDamage <= 0m)
            return;
        if (VigilCounterCombatHelper.IsSuppressingPlayerDamageTriggers)
            return;

        Flash();
        await PowerCmd.Decrement(this);

        if (dealer == null || !dealer.IsAlive || dealer.Side == Owner.Side)
            return;

        await VigilCounterCombatHelper.TryTriggerAsync(choiceContext, Owner.Player, dealer, this);
    }
}
