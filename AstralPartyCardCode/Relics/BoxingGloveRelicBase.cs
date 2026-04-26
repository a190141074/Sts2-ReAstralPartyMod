using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

public abstract class BoxingGloveRelicBase : AstralPartyRelicModel
{
    protected abstract decimal CombatStartStrengthBonus { get; }

    protected abstract decimal TurnStartVigorBonus { get; }

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralBoxingGlovesSet)
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        var totalStrengthToGain = CombatStartStrengthBonus;
        if (BoxingGlovesRelicHelper.ShouldHandleSharedSet(this))
            totalStrengthToGain += BoxingGlovesRelicHelper.GetSetCombatStartStrengthBonus(Owner);

        if (totalStrengthToGain <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, totalStrengthToGain, Owner.Creature, null, true);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;
        if (TurnStartVigorBonus <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<VigorPower>(Owner.Creature, TurnStartVigorBonus, Owner.Creature, null, false);
    }
}