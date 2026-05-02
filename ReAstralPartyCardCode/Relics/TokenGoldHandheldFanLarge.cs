using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldHandheldFanLarge : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>()
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (!IsPersonaSkillCard(cardPlay.Card))
            return;

        Flash();
        await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Draw(choiceContext, 1m, Owner));

        var target = Owner.RunState.Rng.CombatTargets.NextItem(
            Owner.Creature.CombatState.GetOpponentsOf(Owner.Creature).Where(creature => creature.IsAlive)
        );
        if (target == null)
            return;

        await PowerCmd.Apply<MarkLockPower>(target, 1m, Owner.Creature, cardPlay.Card, false);
    }

    private static bool IsPersonaSkillCard(CardModel card)
    {
        return card.Type == CardType.Skill && AstralPartyCardModel.ShouldAutoApplyCooldown(card);
    }
}