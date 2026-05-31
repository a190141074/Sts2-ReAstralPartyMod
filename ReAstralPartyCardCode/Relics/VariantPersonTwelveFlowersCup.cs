using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
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
public class VariantPersonTwelveFlowersCup : CooldownPersonaRelicBase
{
    private const decimal AttackDamageBonusPerEnemy = 0.15m;
    private const int MaxPassiveCeremonialBombTriggersPerTurn = 3;
    private const int FallenFlowerEnergyCooldownRounds = 2;
    private const int UninitializedRound = -999;

    private static readonly LocString RetainSelectionPrompt =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_TWELVE_FLOWERS_CUP.retain_select_prompt");

    [SavedProperty] public int AstralParty_VariantPersonTwelveFlowersCupCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonTwelveFlowersCupPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonTwelveFlowersCupJingRuiGainCount { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonTwelveFlowersCupJingRuiBombRewardCount { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonTwelveFlowersCupLastFallenFlowerEnergyRound { get; set; } =
        UninitializedRound;

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonTwelveFlowersCupCounter;
        set => AstralParty_VariantPersonTwelveFlowersCupCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonTwelveFlowersCupPendingCombatStartCard;
        set => AstralParty_VariantPersonTwelveFlowersCupPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 4;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTwelveFragrantDream>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativePoemGathering>(),
        HoverTipFactory.FromPower<JingRuiPower>(),
        HoverTipFactory.FromPower<CeremonialBombPower>(),
        HoverTipFactory.FromPower<VigilCounterPower>(),
        HoverTipFactory.FromPower<FallenFlowerPower>(),
        HoverTipFactory.FromPower<FlowerHiddenUnseenPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonTwelveFlowersCupJingRuiGainCount = 0;
        AstralParty_VariantPersonTwelveFlowersCupJingRuiBombRewardCount = 0;
        AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn = 0;
        AstralParty_VariantPersonTwelveFlowersCupLastFallenFlowerEnergyRound = UninitializedRound;
        await EnsureDerivativeAndContext();
        ResetCombatState();
    }

    public override async Task BeforeCombatStart()
    {
        await EnsureDerivativeAndContext();
        ResetCombatState();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        ResetCombatState();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn = 0;
        await VigilCounterCombatHelper.EnsureContextPower(player);
        await GrantCeremonialBombsFromCumulativeProgress();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;
        if (VigilCounterAutoPlayHelper.IsCurrentlyAutoPlaying(cardPlay.Card))
            return Task.CompletedTask;
        if (!IsBlockedManualAttackCost(cardPlay.Card))
            return Task.CompletedTask;

        MainFile.Logger.Warn(
            $"[VariantPersonTwelveFlowersCup] Blocked manual play fallback triggered for {cardPlay.Card.Id.Entry}.");
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            return;
        if (target.Player == null || result.UnblockedDamage <= 0m)
            return;
        if (VigilCounterCombatHelper.IsSuppressingPlayerDamageTriggers)
            return;

        Flash();
        AstralParty_VariantPersonTwelveFlowersCupJingRuiGainCount++;
        await PowerCmd.Apply<JingRuiPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        await GrantCeremonialBombsFromCumulativeProgress();
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner || Owner.Creature?.CombatState == null)
            return;
        if (!Hook.ShouldFlush(Owner.Creature.CombatState, player))
            return;

        await SelectAttackCardsToRetain(choiceContext);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Attack)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side || amount <= 0m)
            return 0m;

        var enemyCount = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature).Count;
        if (enemyCount <= 1)
            return 0m;

        return amount * AttackDamageBonusPerEnemy * enemyCount;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTwelveFragrantDream>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public async Task TryGrantFallenFlowerEnergy(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            return;

        var roundNumber = Owner.Creature.CombatState?.RoundNumber ?? 0;
        if (roundNumber - AstralParty_VariantPersonTwelveFlowersCupLastFallenFlowerEnergyRound
            < FallenFlowerEnergyCooldownRounds)
            return;

        AstralParty_VariantPersonTwelveFlowersCupLastFallenFlowerEnergyRound = roundNumber;
        Flash();
        await PlayerCmd.GainEnergy(2m, Owner);
    }

    internal static bool IsBlockedManualAttackCost(CardModel card)
    {
        return card.EnergyCost.CostsX || card.EnergyCost.GetResolved() >= 1;
    }

    internal static bool IsLowCostAttack(CardModel card)
    {
        return !card.EnergyCost.CostsX && card.EnergyCost.GetResolved() <= 1;
    }

    private async Task EnsureDerivativeAndContext()
    {
        if (Owner == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativePoemGathering>(Owner);
        await VigilCounterCombatHelper.EnsureContextPower(Owner);
    }

    private void ResetCombatState()
    {
        AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn = 0;
        AstralParty_VariantPersonTwelveFlowersCupLastFallenFlowerEnergyRound = UninitializedRound;
    }

    private async Task GrantCeremonialBombsFromCumulativeProgress()
    {
        if (Owner?.Creature == null)
            return;

        var totalEligibleRewards = AstralParty_VariantPersonTwelveFlowersCupJingRuiGainCount / 3;
        var pendingRewards = totalEligibleRewards - AstralParty_VariantPersonTwelveFlowersCupJingRuiBombRewardCount;
        var turnRoom = MaxPassiveCeremonialBombTriggersPerTurn
                       - AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn;
        var grantCount = Math.Min(Math.Max(pendingRewards, 0), Math.Max(turnRoom, 0));
        if (grantCount <= 0)
            return;

        AstralParty_VariantPersonTwelveFlowersCupJingRuiBombRewardCount += grantCount;
        AstralParty_VariantPersonTwelveFlowersCupPassiveBombTriggersThisTurn += grantCount;
        await PowerCmd.Apply<CeremonialBombPower>(Owner.Creature, grantCount, Owner.Creature, null, false);
    }

    private async Task SelectAttackCardsToRetain(PlayerChoiceContext choiceContext)
    {
        if (Owner == null)
            return;

        var eligibleAttackCount = PileType.Hand.GetPile(Owner).Cards.Count(card =>
            card.Type == CardType.Attack && !card.ShouldRetainThisTurn);
        if (eligibleAttackCount == 0)
            return;

        var prefs = new CardSelectorPrefs(RetainSelectionPrompt, 0, eligibleAttackCount)
        {
            Cancelable = true
        };

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            card => card.Type == CardType.Attack && !card.ShouldRetainThisTurn,
            this);

        foreach (var card in selectedCards)
        {
            card.GiveSingleTurnRetain();
        }
    }
}
