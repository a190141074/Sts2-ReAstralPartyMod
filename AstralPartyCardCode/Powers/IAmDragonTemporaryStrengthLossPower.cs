using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class IAmDragonTemporaryStrengthLossPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override bool IsInstanced => true;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "ASTRALPARTYMOD-I_AM_DRAGON_TEMPORARY_STRENGTH_LOSS_POWER.title");

    public override LocString Description =>
        new("powers", "ASTRALPARTYMOD-I_AM_DRAGON_TEMPORARY_STRENGTH_LOSS_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "ASTRALPARTYMOD-I_AM_DRAGON_TEMPORARY_STRENGTH_LOSS_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<CollectorsCardIAmDragon>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount <= 0)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, -Amount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null, true);
    }
}
