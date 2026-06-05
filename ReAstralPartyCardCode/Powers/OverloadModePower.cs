using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class OverloadModePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Amount;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return;
        if (cardSource?.Type != CardType.Attack)
            return;
        if (target.Side == Owner.Side)
            return;
        if (result.TotalDamage <= 0m)
            return;

        await StackableDebuffGrowthHelper.TryApplyOrGrowStackableDebuffAsync<PoisonPower>(
            target,
            1m,
            Owner,
            cardSource,
            false);
        await StackableDebuffGrowthHelper.TryApplyOrGrowStackableDebuffAsync<DoomPower>(
            target,
            1m,
            Owner,
            cardSource,
            false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
            return;

        await PowerCmd.TickDownDuration(this);
    }
}
