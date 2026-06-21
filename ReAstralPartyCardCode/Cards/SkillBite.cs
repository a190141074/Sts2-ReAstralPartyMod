using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillBite : AstralPartyCardModel
{
    private sealed class BiteTemporaryStrengthPower : AstralPartyPowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override bool IsInstanced => true;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => ModelDb.Card<SkillBite>().TitleLocString;

        public override LocString Description => new("powers", "TEMPORARY_STRENGTH_POWER.description");

        protected override string SmartDescriptionLocKey => "TEMPORARY_STRENGTH_POWER.smartDescription";

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromCard<SkillBite>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override string ResolveIconPath()
        {
            return GenerateIconPath<DragonAwakeningPower>();
        }

        public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            if (Owner == null || Amount <= 0)
                return;

            await PowerCmd.Apply<StrengthPower>(Owner, Amount, applier, cardSource, true);
        }

        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext,
            MegaCrit.Sts2.Core.Combat.CombatSide side)
        {
            if (Owner == null || side != Owner.Side)
                return;

            await PowerCmd.Remove(this);
            await PowerCmd.Apply<StrengthPower>(Owner, -Amount, Owner, null, true);
        }
    }

    private const decimal AwakeningAmount = 1m;
    private const decimal MaxTemporaryStrength = 4m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DragonAwakeningPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public SkillBite() : base(0, CardType.Attack, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply<DragonAwakeningPower>(Owner.Creature, AwakeningAmount, Owner.Creature, this, false);

        var currentAwakening = Math.Min(Owner.Creature.GetPowerAmount<DragonAwakeningPower>(), MaxTemporaryStrength);
        if (currentAwakening <= 0m)
            return;

        await PowerCmd.Apply(
            ModelDb.Power<BiteTemporaryStrengthPower>().ToMutable(),
            Owner.Creature,
            currentAwakening,
            Owner.Creature,
            this,
            false);
    }
}

