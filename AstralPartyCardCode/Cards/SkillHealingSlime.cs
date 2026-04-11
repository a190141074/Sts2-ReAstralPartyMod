using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(ColorlessCardPool))]
public class SkillHealingSlime : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new PowerVar<HalfLifeHealPower>(3m) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new List<IHoverTip> { HoverTipFactory.FromPower<HalfLifeHealPower>() };

    public SkillHealingSlime() : base(
        0,
        CardType.Skill,
        CardRarity.Uncommon,
        TargetType.Self
    )
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HalfLifeHealPower"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target ?? Owner?.Creature;
        if (target == null || Owner?.Creature == null)
            return;

        // Self-target cards may not populate cardPlay.Target, so fall back to the owner creature.
        await PowerCmd.Apply<HalfLifeHealPower>(
            target,
            DynamicVars["HalfLifeHealPower"].BaseValue,
            Owner.Creature,
            this,
            false
        );
    }
}