using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillDragonsRoar : AstralPartyCardModel
{
    private sealed class DragonsRoarTemporaryStrengthPower : AstralPartyPowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override bool IsInstanced => true;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => ModelDb.Card<SkillDragonsRoar>().TitleLocString;

        public override LocString Description => new("powers", "TEMPORARY_STRENGTH_POWER.description");

        protected override string SmartDescriptionLocKey => "TEMPORARY_STRENGTH_POWER.smartDescription";

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromCard<SkillDragonsRoar>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override string ResolveIconPath()
        {
            return GenerateIconPath<DragonRoarWardPower>();
        }

        public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier,
            CardModel? cardSource)
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

    private const decimal DebuffAmount = 1m;
    private const decimal TemporaryStrengthAmount = 3m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DragonRoarWardPower>()
    ];

    public SkillDragonsRoar() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DebuffAmount, Owner.Creature, this, false);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, DebuffAmount, Owner.Creature, this, false);
        await PowerCmd.Apply(
            ModelDb.Power<DragonsRoarTemporaryStrengthPower>().ToMutable(),
            Owner.Creature,
            TemporaryStrengthAmount,
            Owner.Creature,
            this,
            false);
        await PowerCmd.Apply<DragonRoarWardPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
