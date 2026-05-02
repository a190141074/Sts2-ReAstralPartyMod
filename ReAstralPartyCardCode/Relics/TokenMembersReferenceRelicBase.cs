using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class TokenMembersReferenceRelicBase : AstralPartyRelicModel
{
    private const int TriggerTurnInterval = 3;

    protected virtual int CardsToDraw => 0;

    protected virtual int EnergyToGain => 0;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (roundNumber <= 0 || roundNumber % TriggerTurnInterval != 0)
            return;

        if (CardsToDraw <= 0 && EnergyToGain <= 0)
            return;

        Flash();

        if (CardsToDraw > 0)
            await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Draw(choiceContext, CardsToDraw, Owner));

        if (EnergyToGain > 0)
            await PlayerCmd.GainEnergy(EnergyToGain, Owner);
    }
}