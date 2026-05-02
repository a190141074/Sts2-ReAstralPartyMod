using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonNinja : CooldownPersonaRelicBase
{
    private const int CopyQuotaPerTurn = 9;

    [SavedProperty] public int AstralParty_PersonNinjaCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonNinjaPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonNinjaSkillCardsPlayedThisTurn { get; set; }

    private CardModel? _lastCopyableSkillCard;

    protected override int CounterValue
    {
        get => AstralParty_PersonNinjaCounter;
        set => AstralParty_PersonNinjaCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonNinjaPendingCombatStartCard;
        set => AstralParty_PersonNinjaPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillNinjutsuCombo>(),
        HoverTipFactory.FromPower<CopyQuotaPower>(),
        HoverTipFactory.FromPower<EsotericEmpowerPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonNinjaSkillCardsPlayedThisTurn = 0;
        _lastCopyableSkillCard = null;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeNinjaGarrote>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_PersonNinjaSkillCardsPlayedThisTurn = 0;
        _lastCopyableSkillCard = null;
        await PowerCmd.SetAmount<CopyQuotaPower>(Owner.Creature, CopyQuotaPerTurn, Owner.Creature, null);
    }

    protected override async Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        AstralParty_PersonNinjaSkillCardsPlayedThisTurn = 0;
        _lastCopyableSkillCard = null;
        await PowerCmd.SetAmount<CopyQuotaPower>(Owner.Creature, CopyQuotaPerTurn, Owner.Creature, null);
    }

    protected override Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return Task.CompletedTask;
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonNinjaSkillCardsPlayedThisTurn = 0;
        _lastCopyableSkillCard = null;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Skill)
            return;

        AstralParty_PersonNinjaSkillCardsPlayedThisTurn++;

        if (!AstralPartyCardModel.ShouldAutoApplyCooldown(cardPlay.Card))
            _lastCopyableSkillCard = cardPlay.Card;

        var remainingQuota = await ConsumeCopyQuota();
        if (AstralParty_PersonNinjaSkillCardsPlayedThisTurn % 3 != 0)
            return;
        if (remainingQuota <= 0)
            return;
        if (_lastCopyableSkillCard == null)
            return;

        var copiedCard = _lastCopyableSkillCard.CreateClone();
        if (!_lastCopyableSkillCard.Keywords.Contains(CardKeyword.Exhaust)
            && !copiedCard.Keywords.Contains(CardKeyword.Exhaust))
        {
            CardCmd.ApplyKeyword(copiedCard, CardKeyword.Exhaust);
        }

        Flash();
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
            copiedCard,
            true,
            CardPilePosition.Top,
            this);
    }

    internal void ReduceCooldown(int amount)
    {
        ReduceCooldownProgress(amount);
    }

    private async Task<int> ConsumeCopyQuota()
    {
        if (Owner?.Creature == null)
            return 0;

        var currentAmount = Math.Max((int)Owner.Creature.GetPowerAmount<CopyQuotaPower>(), 0);
        var remainingAmount = Math.Max(currentAmount - 1, 0);
        await PowerCmd.SetAmount<CopyQuotaPower>(Owner.Creature, remainingAmount, Owner.Creature, null);
        return remainingAmount;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillNinjutsuCombo>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
