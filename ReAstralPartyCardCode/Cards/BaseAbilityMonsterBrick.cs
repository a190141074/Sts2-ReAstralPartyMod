using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityMonsterBrick : BaseAbilityCardModel
{
    private const decimal BaseDamage = 5m;
    private const decimal BonusDamage = 3m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityMonsterBrick() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        var target = cardPlay.Target;
        var hadNoBlock = target.Block <= 0m;
        await CreatureCmd.Damage(choiceContext, target, BaseDamage, ValueProp.Move, Owner.Creature, this);
        if (hadNoBlock)
            await CreatureCmd.Damage(choiceContext, target, BonusDamage, ValueProp.Move, Owner.Creature, this);
    }
}
