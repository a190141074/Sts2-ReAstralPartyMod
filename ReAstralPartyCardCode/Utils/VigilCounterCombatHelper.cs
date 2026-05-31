using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class VigilCounterCombatHelper
{
    private sealed class VigilCounterState
    {
        public required Player Owner { get; init; }
        public required Creature PrimaryTarget { get; init; }
        public required AbstractModel Source { get; init; }
        public required PileType OriginalPlayablePileType { get; init; }
        public required bool SelectedFromHand { get; init; }
        public required int HandCountBeforeTrigger { get; init; }
        public CardModel? ActiveCard { get; set; }
        public bool DidLeaveHandDuringResolution { get; set; }
        public bool AutoPlayedCardSuccessfully { get; set; }
        public PileType? ResolvedPileType { get; set; }
    }

    private static readonly AsyncLocal<VigilCounterState?> CurrentState = new();
    private static readonly AsyncLocal<int> SuppressedPlayerDamageTriggerDepth = new();

    public static bool IsSuppressingPlayerDamageTriggers => SuppressedPlayerDamageTriggerDepth.Value > 0;

    public static bool IsCurrentVigilAttack(Creature? dealer, CardModel? cardSource, Creature? expectedOwner)
    {
        var state = CurrentState.Value;
        if (state == null || expectedOwner == null)
            return false;
        if (dealer != expectedOwner)
            return false;
        if (cardSource == null || cardSource != state.ActiveCard)
            return false;

        return true;
    }

    public static bool IsCurrentVigilCard(CardModel? card, Creature? expectedOwner)
    {
        var state = CurrentState.Value;
        if (state == null || expectedOwner == null)
            return false;
        if (expectedOwner != state.Owner.Creature)
            return false;

        return card != null && card == state.ActiveCard;
    }

    public static async Task RunWithSuppressedPlayerDamageTriggers(Func<Task> action)
    {
        SuppressedPlayerDamageTriggerDepth.Value++;
        try
        {
            await action();
        }
        finally
        {
            SuppressedPlayerDamageTriggerDepth.Value = Math.Max(0, SuppressedPlayerDamageTriggerDepth.Value - 1);
        }
    }

    public static async Task EnsureContextPower(Player owner)
    {
        if (owner.Creature == null || owner.Creature.HasPower<VigilCounterContextPower>())
            return;

        await PowerCmd.Apply<VigilCounterContextPower>(owner.Creature, 1m, owner.Creature, null, false);
    }

    public static async Task<bool> TryTriggerAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature damageSource,
        AbstractModel source)
    {
        if (owner.Creature?.CombatState == null || !owner.Creature.IsAlive)
            return false;
        if (damageSource.Side == owner.Creature.Side || !damageSource.IsAlive)
            return false;
        if (CurrentState.Value != null)
            return false;

        await EnsureContextPower(owner);
        await PersonalityDerivativePoemGathering.OnVigilCounterTriggered(choiceContext, owner, damageSource, source);

        var cardToPlay = FindHandAttack(owner, damageSource);
        if (cardToPlay == null)
        {
            cardToPlay = FindDrawPileAttack(owner, damageSource);
            if (cardToPlay != null)
                await PersonaMultiplayerEffectHelper.MoveOwnedCombatCardToHandAndNotify(
                    cardToPlay,
                    CardPilePosition.Top,
                    source);
        }

        if (cardToPlay == null)
            return false;
        if (!VigilCounterAutoPlayHelper.TryEnterAutoPlay(cardToPlay))
            return false;

        var originalPlayablePileType = cardToPlay.Pile?.Type ?? PileType.Hand;
        var selectedFromHand = originalPlayablePileType == PileType.Hand;
        var handCountBeforeTrigger = PileType.Hand.GetPile(owner).Cards.Count;
        var discardCountBeforeTrigger = PileType.Discard.GetPile(owner).Cards.Count;
        var playCountBeforeTrigger = PileType.Play.GetPile(owner).Cards.Count;
        CurrentState.Value = new VigilCounterState
        {
            Owner = owner,
            PrimaryTarget = damageSource,
            Source = source,
            OriginalPlayablePileType = originalPlayablePileType,
            SelectedFromHand = selectedFromHand,
            HandCountBeforeTrigger = handCountBeforeTrigger,
            ActiveCard = cardToPlay
        };

        try
        {
            MainFile.Logger.Info(
                $"[VigilCounter] Selected card {cardToPlay.Id.Entry} from {originalPlayablePileType} against {damageSource.Name}. HandBefore={handCountBeforeTrigger}, DiscardBefore={discardCountBeforeTrigger}, PlayBefore={playCountBeforeTrigger}.");
            if (cardToPlay.Keywords.Contains(CardKeyword.Unplayable))
            {
                MainFile.Logger.Info(
                    $"[VigilCounter] Blocked unplayable card {cardToPlay.Id.Entry}.");
                return false;
            }

            if (!Hook.ShouldPlay(owner.Creature.CombatState, cardToPlay, out var preventer, AutoPlayType.Default))
            {
                MainFile.Logger.Info(
                    $"[VigilCounter] Auto-play blocked for {cardToPlay.Id.Entry} by {preventer?.GetType().Name ?? "<unknown>"}.");
                return false;
            }

            await PlayVigilCardAsync(choiceContext, cardToPlay, damageSource);
            await MoveVigilCardToDiscardIfNeeded(cardToPlay, source);
            var pileTypeAfterResolution = cardToPlay.Pile?.Type;
            var handCountAfterResolution = PileType.Hand.GetPile(owner).Cards.Count;
            var discardCountAfterResolution = PileType.Discard.GetPile(owner).Cards.Count;
            var playCountAfterResolution = PileType.Play.GetPile(owner).Cards.Count;
            var tableNodeStillPresent = NCard.FindOnTable(cardToPlay) != null;
            var leftHandDuringResolution = !selectedFromHand || handCountAfterResolution < handCountBeforeTrigger;
            var autoPlaySuccessfullyLeftPlayablePile = leftHandDuringResolution
                                                       && pileTypeAfterResolution is not (PileType.Hand or PileType.Play);
            CurrentState.Value.DidLeaveHandDuringResolution = leftHandDuringResolution;
            CurrentState.Value.ResolvedPileType = pileTypeAfterResolution;
            CurrentState.Value.AutoPlayedCardSuccessfully = autoPlaySuccessfullyLeftPlayablePile;
            MainFile.Logger.Info(
                $"[VigilCounter] Resolved {cardToPlay.Id.Entry}. OriginalPile={originalPlayablePileType}, FinalPile={pileTypeAfterResolution?.ToString() ?? "<none>"}, HandBefore={handCountBeforeTrigger}, HandAfter={handCountAfterResolution}, DiscardBefore={discardCountBeforeTrigger}, DiscardAfter={discardCountAfterResolution}, PlayBefore={playCountBeforeTrigger}, PlayAfter={playCountAfterResolution}, LeftHand={CurrentState.Value.DidLeaveHandDuringResolution}, NodeOnTableAfter={tableNodeStillPresent}.");
        }
        finally
        {
            VigilCounterAutoPlayHelper.ExitAutoPlay(cardToPlay);
            var finishedState = CurrentState.Value;
            CurrentState.Value = null;

            if (finishedState?.AutoPlayedCardSuccessfully == true)
                await PersonalityDerivativePoemGathering.AfterSuccessfulVigilCounterAutoPlay(
                    choiceContext,
                    owner,
                    source);
        }

        return true;
    }

    private static async Task PlayVigilCardAsync(
        PlayerChoiceContext choiceContext,
        CardModel cardToPlay,
        Creature damageSource)
    {
        if (CombatManager.Instance.IsOverOrEnding || cardToPlay.Owner.Creature.IsDead)
            return;

        var combatState = cardToPlay.CombatState ?? cardToPlay.Owner.Creature.CombatState;
        if (combatState == null)
            return;

        if (cardToPlay.Owner.PlayerCombatState is not { } playerCombatState)
            return;

        if (cardToPlay.EnergyCost.CostsX)
            cardToPlay.EnergyCost.CapturedXValue = playerCombatState.Energy;

        cardToPlay.LastStarsSpent = cardToPlay.HasStarCostX
            ? playerCombatState.Stars
            : Math.Max(0, cardToPlay.GetStarCostWithModifiers());

        await Hook.BeforeCardAutoPlayed(combatState, cardToPlay, damageSource, AutoPlayType.Default);
        var resources = new ResourceInfo
        {
            EnergySpent = 0,
            EnergyValue = cardToPlay.EnergyCost.GetAmountToSpend(),
            StarsSpent = 0,
            StarValue = Math.Max(0, cardToPlay.GetStarCostWithModifiers())
        };

        await cardToPlay.OnPlayWrapper(choiceContext, damageSource, false, resources, false);
    }

    private static async Task MoveVigilCardToDiscardIfNeeded(CardModel cardToPlay, AbstractModel source)
    {
        var currentPileType = cardToPlay.Pile?.Type;
        if (currentPileType is not (PileType.Hand or PileType.Play))
            return;

        MainFile.Logger.Info(
            $"[VigilCounter] Card {cardToPlay.Id.Entry} remained in {currentPileType}; forcing discard fallback.");
        await CardPileCmd.Add(cardToPlay, PileType.Discard, CardPilePosition.Bottom, source, false);
    }

    private static CardModel? FindHandAttack(Player owner, Creature target)
    {
        return PileType.Hand.GetPile(owner)
            .Cards
            .FirstOrDefault(card => CanUseAsVigilAttack(owner, card, target, false));
    }

    private static CardModel? FindDrawPileAttack(Player owner, Creature target)
    {
        return PileType.Draw.GetPile(owner)
            .Cards
            .FirstOrDefault(card => CanUseAsVigilAttack(owner, card, target, true));
    }

    private static bool CanUseAsVigilAttack(Player owner, CardModel card, Creature target, bool requiresLowCost)
    {
        if (card.Type != CardType.Attack)
            return false;
        if (card.Keywords.Contains(CardKeyword.Unplayable))
            return false;
        if (requiresLowCost && !VariantPersonTwelveFlowersCup.IsLowCostAttack(card))
            return false;
        if (card.Owner?.Creature == null || !card.Owner.Creature.IsAlive)
            return false;
        if (target.Side == card.Owner.Creature.Side || !target.IsAlive)
            return false;
        if (!HasEnoughResourcesForVigil(owner, card))
            return false;

        return card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy;
    }

    private static bool HasEnoughResourcesForVigil(Player owner, CardModel card)
    {
        if (owner.PlayerCombatState == null)
            return false;

        var requiredEnergy = GetRequiredVigilEnergy(owner, card);
        var requiredStars = GetRequiredVigilStars(owner, card);
        var availableEnergyAllowance = owner.PlayerCombatState.Energy + 1m;
        var availableStars = owner.PlayerCombatState.Stars;
        return requiredEnergy <= availableEnergyAllowance && requiredStars <= availableStars;
    }

    private static decimal GetRequiredVigilEnergy(Player owner, CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return Math.Max(0m, owner.PlayerCombatState?.Energy ?? 0m);

        return Math.Max(0m, card.EnergyCost.GetAmountToSpend());
    }

    private static decimal GetRequiredVigilStars(Player owner, CardModel card)
    {
        if (card.HasStarCostX)
            return Math.Max(0m, owner.PlayerCombatState?.Stars ?? 0m);

        return Math.Max(0m, card.GetStarCostWithModifiers());
    }
}
