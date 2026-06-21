using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonWindchaserThePlaneswalker : PersonRelicBase
{
    private const int CardsPerTrigger = 10;

    [SavedProperty] public int AstralParty_VariantPersonWindchaserCardsPlayedThisTurn { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonWindchaserPendingNextTurnRewards { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonWindchaserLastProcessedRound { get; set; }

    public override bool ShowCounter => false;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => AstralParty_VariantPersonWindchaserPendingNextTurnRewards;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillGrantSpark>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonWindchaserCardsPlayedThisTurn = 0;
        AstralParty_VariantPersonWindchaserPendingNextTurnRewards = 0;
        AstralParty_VariantPersonWindchaserLastProcessedRound = 0;
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_VariantPersonWindchaserCardsPlayedThisTurn = 0;
        AstralParty_VariantPersonWindchaserPendingNextTurnRewards = 0;
        AstralParty_VariantPersonWindchaserLastProcessedRound = 0;
        RefreshCooldownDisplay();
        if (Owner?.Creature?.CombatState == null || !WindchaserCompat.IsLoaded())
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillGrantSpark>(), Owner);
        CardCmd.Upgrade(card);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;

        AstralParty_VariantPersonWindchaserCardsPlayedThisTurn++;
        while (AstralParty_VariantPersonWindchaserCardsPlayedThisTurn >= CardsPerTrigger)
        {
            AstralParty_VariantPersonWindchaserCardsPlayedThisTurn -= CardsPerTrigger;
            AstralParty_VariantPersonWindchaserPendingNextTurnRewards++;
            Flash();
        }

        RefreshCooldownDisplay();
        await Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_VariantPersonWindchaserLastProcessedRound == roundNumber)
            return;

        AstralParty_VariantPersonWindchaserLastProcessedRound = roundNumber;
        AstralParty_VariantPersonWindchaserCardsPlayedThisTurn = 0;

        var pendingRewards = AstralParty_VariantPersonWindchaserPendingNextTurnRewards;
        if (pendingRewards <= 0)
        {
            RefreshCooldownDisplay();
            return;
        }

        AstralParty_VariantPersonWindchaserPendingNextTurnRewards = 0;
        await PersonMultiplayerEffectHelper.DrawCardsForPlayer(choiceContext, pendingRewards, Owner, this);
        await PlayerCmd.GainEnergy(pendingRewards, Owner);
        RefreshCooldownDisplay();
    }

    public override async Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        AstralParty_VariantPersonWindchaserCardsPlayedThisTurn = 0;
        AstralParty_VariantPersonWindchaserPendingNextTurnRewards = 0;
        AstralParty_VariantPersonWindchaserLastProcessedRound = 0;
        RefreshCooldownDisplay();
        await Task.CompletedTask;
    }

    private void RefreshCooldownDisplay()
    {
        InvokeDisplayAmountChanged();
    }
}
