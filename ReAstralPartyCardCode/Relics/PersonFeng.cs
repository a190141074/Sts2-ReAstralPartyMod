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
public class PersonFeng : CooldownPersonaRelicBase
{
    private const int MaxGatheringStrengthStacks = 5;
    private const int BaseAttackConsumption = 1;
    private const int ExtraConsumptionAtFiveStacks = 2;

    [SavedProperty] public int AstralParty_PersonFengCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonFengPendingCombatStartCard { get; set; }
    [SavedProperty] public bool AstralParty_PersonFengPlayedAttackThisTurn { get; set; }

    private CardModel? _pendingAttackCard;
    private bool _pendingAttackShouldSplash;
    private bool _pendingAttackSplashResolved;
    private bool _isResolvingSplashDamage;

    protected override int CounterValue
    {
        get => AstralParty_PersonFengCounter;
        set => AstralParty_PersonFengCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonFengPendingCombatStartCard;
        set => AstralParty_PersonFengPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillChannelEnergy>(),
        HoverTipFactory.FromPower<GatheringStrengthPower>(),
        HoverTipFactory.FromPower<ChannelEnergyAttackBoostPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        ResetCombatState();
        await SetGatheringStrengthAmount(1, null);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (_isResolvingSplashDamage)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        AstralParty_PersonFengPlayedAttackThisTurn = true;
        _pendingAttackCard = cardPlay.Card;
        _pendingAttackShouldSplash = GetGatheringStrengthAmount() == MaxGatheringStrengthStacks;
        _pendingAttackSplashResolved = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (_isResolvingSplashDamage)
            return;
        if (!_pendingAttackShouldSplash || _pendingAttackSplashResolved)
            return;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return;
        if (cardSource != _pendingAttackCard)
            return;

        _pendingAttackSplashResolved = true;

        var splashDamage = result.TotalDamage + result.OverkillDamage;
        if (splashDamage <= 0m)
            return;

        var splashTargets = Owner.Creature.CombatState
            .GetTeammatesOf(result.Receiver)
            .Except([target])
            .Where(enemy => enemy.IsHittable)
            .ToList();
        if (splashTargets.Count == 0)
            return;

        Flash();
        _isResolvingSplashDamage = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                splashTargets,
                splashDamage,
                ValueProp.Unpowered | ValueProp.Move,
                Owner.Creature,
                _pendingAttackCard);
        }
        finally
        {
            _isResolvingSplashDamage = false;
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (cardPlay.Card != _pendingAttackCard)
            return;

        try
        {
            if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
                return;

            var currentStacks = GetGatheringStrengthAmount();
            if (currentStacks <= 0)
                return;

            var amountToConsume =
                BaseAttackConsumption + (_pendingAttackShouldSplash ? ExtraConsumptionAtFiveStacks : 0);
            await SetGatheringStrengthAmount(currentStacks - amountToConsume, cardPlay.Card);
        }
        finally
        {
            ClearPendingAttackState();
        }
    }

    protected override async Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side)
            return;

        if (!AstralParty_PersonFengPlayedAttackThisTurn)
        {
            var gainAmount = GetTurnEndGainAmount();
            var currentStacks = GetGatheringStrengthAmount();
            var newStacks = currentStacks + gainAmount;
            if (newStacks > currentStacks)
            {
                Flash();
                await SetGatheringStrengthAmount(newStacks, null);
            }
        }

        AstralParty_PersonFengPlayedAttackThisTurn = false;
        ClearPendingAttackState();
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillChannelEnergy>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task SetGatheringStrengthAmount(int amount, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;

        var clampedAmount = Math.Clamp(amount, 0, MaxGatheringStrengthStacks);
        var existingPower = Owner.Creature.GetPower<GatheringStrengthPower>();

        if (clampedAmount <= 0)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);
            return;
        }

        await PowerCmd.SetAmount<GatheringStrengthPower>(Owner.Creature, clampedAmount, Owner.Creature, cardSource);
    }

    private int GetGatheringStrengthAmount()
    {
        return Owner?.Creature == null
            ? 0
            : Math.Max((int)Owner.Creature.GetPowerAmount<GatheringStrengthPower>(), 0);
    }

    private int GetTurnEndGainAmount()
    {
        var roomType = Owner?.Creature?.CombatState?.Encounter?.RoomType;
        return roomType is RoomType.Elite or RoomType.Boss ? 2 : 1;
    }

    private void ClearPendingAttackState()
    {
        _pendingAttackCard = null;
        _pendingAttackShouldSplash = false;
        _pendingAttackSplashResolved = false;
    }

    private void ResetCombatState()
    {
        AstralParty_PersonFengPlayedAttackThisTurn = false;
        ClearPendingAttackState();
        _isResolvingSplashDamage = false;
    }
}
