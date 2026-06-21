using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class VariantPersonSara : CooldownPersonRelicBase
{
    private const int ChargeMilestone = 7;
    private const int ExtraTurnChargeThreshold = 21;

    [SavedProperty] public int AstralParty_VariantPersonSaraCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonSaraPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonSaraCharge { get; set; }
    [SavedProperty] public bool AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonSaraLastProcessedRound { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonSaraPendingShatterStarFallbackCount { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonSaraCounter;
        set => AstralParty_VariantPersonSaraCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonSaraPendingCombatStartCard;
        set => AstralParty_VariantPersonSaraPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 5;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => Owner?.Creature?.CombatState != null
        ? GetClampedCounter()
        : Math.Max(AstralParty_VariantPersonSaraCharge, 0);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillShatterStar>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeDivineThrone>(),
        HoverTipFactory.FromPower<DivineSonPower>(),
        HoverTipFactory.FromPower<SaraNodePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonSaraCharge = 0;
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        AstralParty_VariantPersonSaraPendingShatterStarFallbackCount = 0;
        await AstralDivinePersonaHelper.EnsureDivineThrone(Owner);
        if (Owner?.Creature != null)
            await PowerCmd.SetAmount<SaraNodePower>(Owner.Creature, 1m, Owner.Creature, null);
        await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner!, 0);
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        if (Owner != null)
        {
            await PendingExtraTurnQueuePower.RestoreSaraShatterFallbackAtCombatStart(Owner);
            await AstralDivinePersonaHelper.EnsureDivineThrone(Owner);
            if (Owner.Creature != null && !Owner.Creature.HasPower<SaraNodePower>())
                await PowerCmd.SetAmount<SaraNodePower>(Owner.Creature, 1m, Owner.Creature, null);
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, AstralParty_VariantPersonSaraCharge);
            await AstralMoveAgainDisplayHelper.Sync(Owner);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return;
        if (AttackCardCostHelper.GetPlayedCost(cardPlay) < 1)
            return;

        var before = AstralParty_VariantPersonSaraCharge;
        AstralParty_VariantPersonSaraCharge++;
        var after = AstralParty_VariantPersonSaraCharge;
        if (after / ChargeMilestone > before / ChargeMilestone)
        {
            Flash();
            await AstralDivinePersonaHelper.SyncSaraMilestone(Owner, after, cardPlay.Card);
        }
        else
        {
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, after);
        }

        RefreshShatterStarDamageDisplays();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);

        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;
        if (AstralParty_VariantPersonSaraCharge < ExtraTurnChargeThreshold)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn
            || AstralParty_VariantPersonSaraLastProcessedRound == roundNumber)
            return;

        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = true;
        AstralParty_VariantPersonSaraLastProcessedRound = roundNumber;
        AstralParty_VariantPersonSaraCharge = 0;
        await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, 0);
        RefreshShatterStarDamageDisplays();
        await PendingExtraTurnQueuePower.EnqueueSaraChargeExtraTurn(Owner, grantEnergyOnExtraTurnStart: true);
        MainFile.Logger.Info(
            $"[VariantPersonSara] Queued Sara extra turn from 21 charge | owner={Owner?.NetId} | pending={PendingExtraTurnQueuePower.GetPendingCount(Owner)}");
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        AstralParty_VariantPersonSaraTriggeredExtraTurnThisTurn = false;
        AstralParty_VariantPersonSaraLastProcessedRound = 0;
        if (Owner != null)
        {
            await PendingExtraTurnQueuePower.ConvertSaraUnresolvedShatterToFallbackAtCombatEnd(Owner);
            await AstralDivinePersonaHelper.SyncSaraChargeDisplay(Owner, AstralParty_VariantPersonSaraCharge);
            await AstralMoveAgainDisplayHelper.Sync(Owner);
        }
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillShatterStar>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public void ReduceCooldownOne()
    {
        ReduceCooldownProgress(1);
    }

    public int GetCurrentCharge()
    {
        return Math.Max(AstralParty_VariantPersonSaraCharge, 0);
    }

    private void RefreshShatterStarDamageDisplays()
    {
        var handCards = Owner == null ? null : PileType.Hand.GetPile(Owner)?.Cards;
        if (handCards == null)
            return;

        foreach (var card in handCards.OfType<SkillShatterStar>())
            card.RefreshDisplayedDamage();
    }
}
