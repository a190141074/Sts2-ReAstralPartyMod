using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillIronVirgin : AstralPartyCardModel
{
    private const decimal BoundaryStrengthBonus = 3m;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IronVirginWardPower>(),
        HoverTipFactory.FromPower<BoundaryReinforcementPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillIronVirgin() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
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

        await PowerCmd.Apply<IronVirginWardPower>(Owner.Creature, 1m, Owner.Creature, this, false);

        var relic = Owner.GetRelic<PersonSocialFearNun>();
        if (relic != null && !relic.DidBoundaryApplyThisTurn())
            await AstralTemporaryStrengthPower.Apply(Owner.Creature, BoundaryStrengthBonus, this, Owner.Creature, this);
    }
}
