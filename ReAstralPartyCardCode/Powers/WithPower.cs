using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class WithPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => false;

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return "res://ReAstralPartyMod/images/powers/with_power.png";
        yield return "res://ReAstralPartyMod/images/powers/withpower.png";

        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner || props.HasFlag(ValueProp.Unpowered))
            return 1m;

        return 1m + Amount / 200m;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner || props.HasFlag(ValueProp.Unpowered))
            return 0m;

        return Amount / 50m;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (Owner == null || card.Owner?.Creature != Owner || card.Type != CardType.Skill || Amount < 300m)
            return (pileType, position);

        return (PileType.Exhaust, position);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || cardPlay.Card.Owner?.Creature != Owner)
            return;

        if (Amount >= 200m && (cardPlay.Card.Type == CardType.Skill || cardPlay.Card.Type == CardType.Power))
        {
            var hpLoss = 1m;
            if (Amount >= 300m)
                hpLoss += Amount / 100m;

            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                cardPlay.Card);
        }

        if (Amount >= 300m && cardPlay.Card.Type == CardType.Attack)
            await CreatureCmd.Heal(Owner, 3m);
    }

    public override async Task AfterTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount < 200m)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            13m,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            Owner);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || Owner?.Player == null || amount <= 0m || Amount < 100m)
            return;
        if (Owner.Player.GetRelic<VariantPersonManosabaLinHiro>() == null)
            return;

        await EnsureDeathRewindRewardAsync();
    }

    private async Task EnsureDeathRewindRewardAsync()
    {
        var ownerPlayer = Owner?.Player;
        if (ownerPlayer?.RunState == null)
            return;

        var existingDeckCards = EventDeckCardHelper.GetRunDeckCards(ownerPlayer);
        if (existingDeckCards.Any(card => card.CanonicalInstance?.Id == ModelDb.Card<DeathRewind>().Id || card.Id == ModelDb.Card<DeathRewind>().Id))
            return;

        var canonical = ModelDb.Card<DeathRewind>();
        var runDeckCard = ownerPlayer.RunState.CreateCard(canonical, ownerPlayer);
        var addedToDeck = await EventDeckCardHelper.AddCardToRunDeckAsync(ownerPlayer, runDeckCard, true);
        if (!addedToDeck)
            return;

        if (ownerPlayer.Creature?.CombatState == null)
            return;

        var combatCard = ownerPlayer.Creature.CombatState.CreateCard(canonical, ownerPlayer);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(combatCard, true, CardPilePosition.Top, this);
    }
}
