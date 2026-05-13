using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldMagicQuiver : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_TokenGoldMagicQuiverTriggeredThisTurn { get; set; }

    private CardModel? _trackedSkillCard;
    private bool _shouldCopyTrackedSkillCard;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        _trackedSkillCard = null;
        _shouldCopyTrackedSkillCard = false;
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        _trackedSkillCard = null;
        _shouldCopyTrackedSkillCard = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        _trackedSkillCard = null;
        _shouldCopyTrackedSkillCard = false;
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = false;
        _trackedSkillCard = null;
        _shouldCopyTrackedSkillCard = false;

        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner == null)
            return Task.CompletedTask;
        if (_trackedSkillCard != null)
            return Task.CompletedTask;
        if (AstralParty_TokenGoldMagicQuiverTriggeredThisTurn)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card.Type != CardType.Skill)
            return Task.CompletedTask;

        _trackedSkillCard = cardPlay.Card;
        _shouldCopyTrackedSkillCard = false;
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
        if (AstralParty_TokenGoldMagicQuiverTriggeredThisTurn)
            return;
        if (!IsTrackedSkillDamage(target, result.UnblockedDamage, dealer, cardSource))
            return;
        if (target.GetPowerAmount<MarkLockPower>() <= 0m)
            return;

        var owner = Owner;
        if (owner?.Creature == null)
            return;

        AstralParty_TokenGoldMagicQuiverTriggeredThisTurn = true;
        _shouldCopyTrackedSkillCard = true;
        Flash();

        await PowerCmd.Apply<MarkLockPower>(target, 1m, owner.Creature, cardSource, false);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != _trackedSkillCard)
            return;

        try
        {
            if (!_shouldCopyTrackedSkillCard)
                return;

            if (await PersonaMultiplayerEffectHelper.TryRedirectLivingFolioCopyToDerivativeStacks(
                Owner,
                cardPlay.Card,
                this))
                return;

            var copiedCard = cardPlay.Card.CreateClone();
            if (!cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust)
                && !copiedCard.Keywords.Contains(CardKeyword.Exhaust))
            {
                CardCmd.ApplyKeyword(copiedCard, CardKeyword.Exhaust);
            }

            await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(
                copiedCard,
                true,
                CardPilePosition.Bottom,
                this);
        }
        finally
        {
            _trackedSkillCard = null;
            _shouldCopyTrackedSkillCard = false;
        }
    }

    private bool IsTrackedSkillDamage(Creature? target, decimal amount, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource == null || cardSource != _trackedSkillCard)
            return false;

        return cardSource.Owner == Owner && cardSource.Type == CardType.Skill;
    }
}
