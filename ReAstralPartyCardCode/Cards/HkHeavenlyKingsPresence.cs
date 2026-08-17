using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class HkHeavenlyKingsPresence : AstralPartyCardModel
{
    public HkHeavenlyKingsPresence() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null)
            return;

        var block = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(CombatState))
            await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Move, cardPlay);
    }
}
