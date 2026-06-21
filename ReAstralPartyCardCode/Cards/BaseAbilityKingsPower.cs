using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityKingsPower : BaseAbilityCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<KingsPowerStrengthPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityKingsPower() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        await CreatureCmd.Damage(choiceContext, Owner.Creature, 4m, ValueProp.Unblockable | ValueProp.Unpowered, this);
        await PowerCmd.Apply(ModelDb.Power<KingsPowerStrengthPower>().ToMutable(), Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
