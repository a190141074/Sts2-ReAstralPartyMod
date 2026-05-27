using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilitySelfDestruct : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilitySelfDestruct() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null)
            return;

        await CreatureCmd.Damage(choiceContext, Owner.Creature, 3m, ValueProp.Unblockable | ValueProp.Unpowered, this);
        var enemies = CombatState.GetOpponentsOf(Owner.Creature).ToList();
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive)
                continue;

            await CreatureCmd.Damage(choiceContext, enemy, 9m, ValueProp.Move, Owner.Creature, this);
        }
    }
}
