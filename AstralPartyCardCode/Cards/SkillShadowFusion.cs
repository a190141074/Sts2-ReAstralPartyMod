using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
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

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillShadowFusion : AstralPartyCardModel
{
    private sealed class ShadowFusionTemporaryStrengthPower : AstralPartyPowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override bool IsInstanced => true;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => ModelDb.Card<SkillShadowFusion>().TitleLocString;

        public override LocString Description => new("powers", "TEMPORARY_STRENGTH_POWER.description");

        protected override string SmartDescriptionLocKey => "TEMPORARY_STRENGTH_POWER.smartDescription";


        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromCard<SkillShadowFusion>(),
            HoverTipFactory.FromPower<StrengthPower>()
        ];

        protected override string ResolveIconPath()
        {
            return GenerateIconPath<TwinShadowsPower>();
        }

        public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            if (Owner == null || Amount <= 0)
                return;

            await PowerCmd.Apply<StrengthPower>(Owner, Amount, applier, cardSource, true);
        }

        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            if (Owner == null || side != Owner.Side)
                return;

            Flash();
            await PowerCmd.Remove(this);
            await PowerCmd.Apply<StrengthPower>(Owner, -Amount, Owner, null);
        }
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TwinShadowsPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillShadowFusion() : base(
        1,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var ownerCreature = Owner?.Creature;
        var combatState = ownerCreature?.CombatState;
        if (ownerCreature == null || combatState == null)
            return;

        var shadows = combatState
            .GetOpponentsOf(ownerCreature)
            .Where(creature => creature.IsAlive)
            .SelectMany(creature => creature.GetPowerInstances<TwinShadowsPower>())
            .ToList();

        if (shadows.Count == 0)
            return;

        foreach (var shadow in shadows)
            await PowerCmd.Remove(shadow);

        await PowerCmd.Apply(
            ModelDb.Power<ShadowFusionTemporaryStrengthPower>().ToMutable(),
            ownerCreature,
            shadows.Count,
            ownerCreature,
            this
        );
    }
}