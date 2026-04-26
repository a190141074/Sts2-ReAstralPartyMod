using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class CrossedTwinCarpPower : AstralPartyPowerModel
{
    private const decimal VigorAmount = 6m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        await PowerCmd.Apply<VigorPower>(Owner, VigorAmount, applier, cardSource, false);
    }

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
        if (target.Side == Owner.Side)
            return;
        if (result.TotalDamage <= 0m)
            return;

        if (result.BlockedDamage > 0m)
        {
            Flash();
            await CreatureCmd.Damage(
                choiceContext,
                target,
                result.BlockedDamage,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                Owner,
                cardSource);
        }

        await PowerCmd.Remove(this);
    }
}