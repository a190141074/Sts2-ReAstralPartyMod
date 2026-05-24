using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityDirectedDemolition : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityDirectedDemolition() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null || cardPlay.Target == null)
            return;

        var primaryTarget = cardPlay.Target;
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature))
        {
            if (!enemy.IsAlive)
                continue;

            await CreatureCmd.Damage(choiceContext, enemy, 4m, ValueProp.Move, Owner.Creature, this);
        }
    }
}
