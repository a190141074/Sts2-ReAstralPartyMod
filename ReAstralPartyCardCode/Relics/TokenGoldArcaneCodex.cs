using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldArcaneCodex : AstralPartyRelicModel
{
    private const int SkillsRequiredPerTrigger = 3;

    [SavedProperty] public int AstralParty_TokenGoldArcaneCodexSkillCountThisTurn { get; set; }
    [SavedProperty] public bool AstralParty_TokenGoldArcaneCodexTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => AstralParty_TokenGoldArcaneCodexTriggeredThisTurn
        ? SkillsRequiredPerTrigger
        : Math.Min(AstralParty_TokenGoldArcaneCodexSkillCountThisTurn, SkillsRequiredPerTrigger);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetTurnState();
    }

    public override Task BeforeCombatStart()
    {
        ResetTurnState();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetTurnState();
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        ResetTurnState();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;
        if (AstralParty_TokenGoldArcaneCodexTriggeredThisTurn)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Skill)
            return;
        if (AstralPartyCardModel.ShouldAutoApplyCooldown(cardPlay.Card))
            return;

        AstralParty_TokenGoldArcaneCodexSkillCountThisTurn++;
        InvokeDisplayAmountChanged();

        if (AstralParty_TokenGoldArcaneCodexSkillCountThisTurn < SkillsRequiredPerTrigger)
            return;

        AstralParty_TokenGoldArcaneCodexTriggeredThisTurn = true;
        InvokeDisplayAmountChanged();

        Flash();
        await PersonMultiplayerEffectHelper.CopyCardToHandOrRedirectLivingFolioAsync(
            Owner,
            cardPlay.Card,
            this,
            CardPilePosition.Top,
            setFreeThisTurn: true);
    }

    private void ResetTurnState()
    {
        AstralParty_TokenGoldArcaneCodexSkillCountThisTurn = 0;
        AstralParty_TokenGoldArcaneCodexTriggeredThisTurn = false;
        InvokeDisplayAmountChanged();
    }
}
