using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class BloodthirstPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CuteIsJusticePower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await VampirePersonaHelper.SyncCuteIsJustice(Owner?.Player);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource
    )
    {
        if (Owner == null || dealer != Owner)
            return;
        if (target.Side == Owner.Side)
            return;
        if (cardSource == null || cardSource.Owner != Owner.Player || cardSource.Type != CardType.Attack)
            return;
        if (result.UnblockedDamage <= 0m)
            return;

        var healAmount = result.UnblockedDamage * 0.5m;
        if (healAmount > 0m)
        {
            Flash();
            await CreatureCmd.Heal(Owner, healAmount, true);
        }

        if (Amount > 1m)
            await PowerCmd.ModifyAmount(this, -1m, Owner, cardSource, true);
        else
            await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        await VampirePersonaHelper.SyncCuteIsJustice(oldOwner?.Player);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
