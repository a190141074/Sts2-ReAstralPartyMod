using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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
        public CardModel? ActiveCard { get; set; }
        public bool AutoPlayedCardSuccessfully { get; set; }
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

        CurrentState.Value = new VigilCounterState
        {
            Owner = owner,
            PrimaryTarget = damageSource,
            Source = source,
            ActiveCard = cardToPlay
        };

        try
        {
            await CardCmd.AutoPlay(choiceContext, cardToPlay, damageSource, AutoPlayType.Default, false, true);
            CurrentState.Value.AutoPlayedCardSuccessfully = true;
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

    private static CardModel? FindHandAttack(Player owner, Creature target)
    {
        return PileType.Hand.GetPile(owner)
            .Cards
            .FirstOrDefault(card => CanUseAsVigilAttack(card, target, false));
    }

    private static CardModel? FindDrawPileAttack(Player owner, Creature target)
    {
        return PileType.Draw.GetPile(owner)
            .Cards
            .FirstOrDefault(card => CanUseAsVigilAttack(card, target, true));
    }

    private static bool CanUseAsVigilAttack(CardModel card, Creature target, bool requiresLowCost)
    {
        if (card.Type != CardType.Attack)
            return false;
        if (requiresLowCost && !VariantPersonTwelveFlowersCup.IsLowCostAttack(card))
            return false;
        if (card.Owner?.Creature == null || !card.Owner.Creature.IsAlive)
            return false;
        if (target.Side == card.Owner.Creature.Side || !target.IsAlive)
            return false;

        return card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy;
    }
}
