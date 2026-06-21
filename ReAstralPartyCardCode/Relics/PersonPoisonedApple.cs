using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonPoisonedApple : CooldownPersonRelicBase
{
    private const decimal MarkedAttackBonusDamage = 3m;

    [SavedProperty] public int AstralParty_PersonPoisonedAppleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonPoisonedApplePendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonPoisonedAppleInvestigationTriggerCount { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonPoisonedAppleCounter;
        set => AstralParty_PersonPoisonedAppleCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonPoisonedApplePendingCombatStartCard;
        set => AstralParty_PersonPoisonedApplePendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillConcealingOperation>(),
        HoverTipFactory.FromPower<InvestigationTargetPower>(),
        HoverTipFactory.FromPower<MarkLockPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralInvestigationProgressId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonPoisonedAppleInvestigationTriggerCount = 0;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (!IsMarkedAttack(target, amount, dealer, cardSource))
            return 0m;

        return MarkedAttackBonusDamage;
    }

    public ConcealingInvestigationHelper.InvestigationStage GetCurrentInvestigationStage()
    {
        return ConcealingInvestigationHelper.GetInvestigationStageForTriggerCount(
            AstralParty_PersonPoisonedAppleInvestigationTriggerCount
        );
    }

    public bool HasCompletedTruthRevealProgress()
    {
        return GetCurrentInvestigationStage() == ConcealingInvestigationHelper.InvestigationStage.TruthUnveiled
               && AstralParty_PersonPoisonedAppleInvestigationTriggerCount >=
               ConcealingInvestigationHelper.GetStageAdvanceThreshold() * 4;
    }

    public void RecordInvestigationTrigger()
    {
        AstralParty_PersonPoisonedAppleInvestigationTriggerCount++;
        Flash();
    }

    private bool IsMarkedAttack(Creature? target, decimal amount, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource?.Owner != Owner)
            return false;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return false;

        return target.GetPowerAmount<MarkLockPower>() > 0m;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillConcealingOperation>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
