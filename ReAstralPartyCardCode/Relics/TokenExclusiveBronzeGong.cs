using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveBronzeGong : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BronzeGongPower>(),
        HoverTipFactory.FromPower<ReversedScalesHolographicPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralWaterTownSeriesId)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (!PersonaRelicHelper.IsPersonaSkillCard(cardPlay.Card))
            return;

        Flash();
        foreach (var player in Owner.Creature.CombatState.Players)
        {
            if (player.Creature == null || !player.Creature.IsAlive)
                continue;

            await PowerCmd.Apply<BronzeGongPower>(player.Creature, 1m, Owner.Creature, cardPlay.Card, false);
        }
    }
}
