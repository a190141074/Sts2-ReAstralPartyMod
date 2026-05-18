using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class BaiZeBlessingPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return 0m;
        if (cardSource?.Type != CardType.Attack)
            return 0m;
        if (target == null || target.Side == Owner.Side)
            return 0m;

        return Owner.GetPowerAmount<HalfLifeHealPower>();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public static bool ShouldConvertHeal(Creature? target, decimal amount)
    {
        return target is { IsAlive: true }
               && amount > 0m
               && target.GetPower<BaiZeBlessingPower>() != null
               && target.CurrentHp >= target.MaxHp;
    }
}
