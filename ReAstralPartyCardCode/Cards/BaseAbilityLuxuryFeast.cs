using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class BaseAbilityLuxuryFeast : BaseAbilityCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BaseAbilityLuxuryFeast() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(CombatState))
            await CreatureCmd.Heal(player.Creature, 3m, true);
    }
}
