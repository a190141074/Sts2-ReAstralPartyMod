using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class AbsoluteFormPower : AstralPartyPowerModel
{
    [SavedProperty] public int AstralParty_AbsoluteFormAppliedRoundNumber { get; set; }
    [SavedProperty] public int AstralParty_AbsoluteFormLastCurseRound { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);

        var roundNumber = Owner?.CombatState?.RoundNumber ?? 0;
        AstralParty_AbsoluteFormAppliedRoundNumber = roundNumber;
        AstralParty_AbsoluteFormLastCurseRound = roundNumber;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.CombatState == null || player.Creature?.CombatState == null)
            return;

        var currentRound = Owner.CombatState.RoundNumber;
        var curseInterval = GetCurseIntervalRounds();
        if (currentRound - AstralParty_AbsoluteFormLastCurseRound >= curseInterval)
        {
            AstralParty_AbsoluteFormLastCurseRound = currentRound;
            var effectIndex = 0;
            foreach (var alivePlayer in EventCombatTargetHelper.GetAlivePlayers(Owner.CombatState))
                await AbsoluteFormHelper.AddRandomCurseToDiscard(alivePlayer, this, currentRound + effectIndex++);
        }

        if (currentRound != AstralParty_AbsoluteFormAppliedRoundNumber)
            return;
        if (Owner.Player == null || player == Owner.Player)
            return;
        if (!player.Creature.IsAlive)
            return;

        PlayerCmd.EndTurn(player, canBackOut: false);
    }

    private int GetCurseIntervalRounds()
    {
        var playerCount = Owner?.CombatState == null
            ? 1
            : EventCombatTargetHelper.GetAlivePlayers(Owner.CombatState).Count();

        return Math.Max(1, Math.Min(playerCount - 1, 3));
    }
}
