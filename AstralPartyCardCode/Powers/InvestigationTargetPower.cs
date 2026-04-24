using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class InvestigationTargetPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<ConcealingPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount
    )
    {
        modifiedAmount = amount;

        if (canonicalPower is not InvestigationTargetPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount = 0m;
        return true;
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
        if (target != Owner)
            return;
        if (!result.WasTargetKilled)
            return;

        var triggerPlayer = dealer?.Player ?? dealer?.PetOwner ?? cardSource?.Owner ?? Applier?.Player;
        if (triggerPlayer?.Creature?.CombatState == null)
            return;

        Flash();
        await ConcealingInvestigationHelper.TriggerRandomInvestigationEvent(choiceContext, triggerPlayer);
    }
}
