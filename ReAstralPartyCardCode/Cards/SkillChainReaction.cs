using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillChainReaction : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillBite>()
    ];

    public SkillChainReaction() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (var player in PersonaMultiplayerEffectHelper.GetStableCombatPlayers(Owner))
        {
            if (player == Owner)
                continue;

            var cardsToDraw = PileType.Hand.GetPile(player).Cards.Count < 4 ? 2m : 1m;
            var drawnCards =
                await PersonaMultiplayerEffectHelper.DrawCardsForPlayer(choiceContext, cardsToDraw, player, this);
            await XiaoLeiAwakeningHelper.TryGrantAwakeningForGrantedCard(Owner, player, drawnCards.Count());
        }

        var bite = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillBite>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(bite, true, CardPilePosition.Top, this);
    }
}

