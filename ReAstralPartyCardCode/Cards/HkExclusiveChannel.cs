using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class HkExclusiveChannel : AstralPartyCardModel
{
    public HkExclusiveChannel() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null)
            return;

        await PersonMultiplayerEffectHelper.DrawCardsForPlayer(choiceContext, 4m, Owner, this);
        foreach (var teammate in EventCombatTargetHelper.GetAlivePlayers(CombatState).Where(player => player != Owner))
            await TokenEternalStarlight.GrantStacks(teammate, 3);
    }
}
