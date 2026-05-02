using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class BoxingGloveRelicBase : AstralPartyRelicModel
{
    protected abstract decimal CombatStartStrengthBonus { get; }

    protected abstract decimal TurnStartVigorBonus { get; }

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralBoxingGlovesSetId)
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