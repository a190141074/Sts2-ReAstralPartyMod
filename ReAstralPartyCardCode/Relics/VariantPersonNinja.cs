using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonNinja : CooldownPersonRelicBase,
    IRelicExtraIconAmountLabelsProvider,
    IRelicExtraIconAmountLabelsChangeSource
{
    private const int SkillsPerTurn = 9;

    [SavedProperty] public int AstralParty_VariantPersonNinjaCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonNinjaPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonNinjaSkillsPlayedThisTurn { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonNinjaGlobalSkillSequence { get; set; }

    private CardModel? _lastCopyableSkillCard;

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonNinjaCounter;
        set => AstralParty_VariantPersonNinjaCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonNinjaPendingCombatStartCard;
        set => AstralParty_VariantPersonNinjaPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillNinjutsuCombo>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeNinjaRealGarrote>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_VariantPersonNinjaSkillsPlayedThisTurn = 0;
        AstralParty_VariantPersonNinjaGlobalSkillSequence =
            Math.Max(0, AstralParty_VariantPersonNinjaGlobalSkillSequence);
        _lastCopyableSkillCard = null;

        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeNinjaRealGarrote>(Owner);
    }

    public override Task BeforeCombatStart()
    {
        ResetTurnSkillCounter();
        _lastCopyableSkillCard = null;
        return Task.CompletedTask;
    }

    protected override Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        ResetTurnSkillCounter();
        _lastCopyableSkillCard = null;
        return Task.CompletedTask;
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        _lastCopyableSkillCard = null;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Skill)
            return;
        if (cardPlay.IsAutoPlay)
            return;

        AstralParty_VariantPersonNinjaSkillsPlayedThisTurn++;
        _lastCopyableSkillCard = PersonRelicHelper.IsPersonSkillCard(cardPlay.Card)
            ? _lastCopyableSkillCard
            : cardPlay.Card;

        AdvanceGlobalSkillSequence();
        NotifyDisplayChanged();

        var derivative = Owner.GetRelic<PersonalityDerivativeNinjaRealGarrote>();
        var isGlobalThirdSkill = IsGlobalThirdSkill();
        if (isGlobalThirdSkill)
            derivative?.HandleThirdGlobalSkill();
        derivative?.NotifySkillSequenceChanged();

        if (!isGlobalThirdSkill || _lastCopyableSkillCard == null)
            return;

        Flash();
        await PersonMultiplayerEffectHelper.CopyCardToHandOrRedirectLivingFolioAsync(
            Owner,
            _lastCopyableSkillCard,
            this,
            CardPilePosition.Top);
    }

    internal int GetRemainingSkillQuota()
    {
        return Math.Max(0, SkillsPerTurn - AstralParty_VariantPersonNinjaSkillsPlayedThisTurn);
    }

    internal int GetGlobalSkillSequenceStage()
    {
        var globalSkillCount = AstralParty_VariantPersonNinjaGlobalSkillSequence;
        return globalSkillCount <= 0 ? 0 : (globalSkillCount - 1) % 3 + 1;
    }

    internal bool ShouldBlockSkillPlay(CardModel card)
    {
        return card.Owner == Owner
               && card.Type == CardType.Skill
               && AstralParty_VariantPersonNinjaSkillsPlayedThisTurn >= SkillsPerTurn;
    }

    internal static bool IsVariantNinjaSkill(CardModel? card)
    {
        return card?.Owner?.GetRelic<VariantPersonNinja>() != null
               && card.Type == CardType.Skill;
    }

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots()
    {
        return
        [
            ExtraIconAmountLabelSlot.At(
                ExtraIconAmountLabelCorner.TopRight,
                GetRemainingSkillQuota().ToString())
        ];
    }

    internal bool IsGlobalThirdSkill()
    {
        return AstralParty_VariantPersonNinjaGlobalSkillSequence > 0
               && AstralParty_VariantPersonNinjaGlobalSkillSequence % 3 == 0;
    }

    private void AdvanceGlobalSkillSequence()
    {
        if (AstralParty_VariantPersonNinjaGlobalSkillSequence < int.MaxValue)
            AstralParty_VariantPersonNinjaGlobalSkillSequence++;
    }

    private void ResetTurnSkillCounter()
    {
        if (AstralParty_VariantPersonNinjaSkillsPlayedThisTurn == 0)
            return;

        AstralParty_VariantPersonNinjaSkillsPlayedThisTurn = 0;
        NotifyDisplayChanged();
    }

    private void NotifyDisplayChanged()
    {
        InvokeDisplayAmountChanged();
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillNinjutsuCombo>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
