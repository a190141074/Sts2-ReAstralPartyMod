using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillHealingSlime : AstralPartyCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new PowerVar<HalfLifeHealPower>(3m) };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new List<IHoverTip> { HoverTipFactory.FromPower<HalfLifeHealPower>() };

    public SkillHealingSlime() : base(
        0,
        CardType.Skill,
        CardRarity.Ancient,
        TargetType.AnyPlayer
    )
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HalfLifeHealPower"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var target = cardPlay.Target ?? Owner.Creature;
        if (target == null || Owner?.Creature == null)
            return;

        await PowerCmd.Apply<HalfLifeHealPower>(
            target,
            DynamicVars["HalfLifeHealPower"].BaseValue,
            Owner.Creature,
            this,
            false
        );
    }
}

