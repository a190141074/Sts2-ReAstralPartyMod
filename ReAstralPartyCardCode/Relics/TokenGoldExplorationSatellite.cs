using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldExplorationSatellite : AstralPartyRelicModel
{
    private const int HandThreshold = 6;

    [SavedProperty] public bool AstralParty_TokenGoldExplorationSatellitePendingRailgun { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<BaseAbilityOrbitalRailgun>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.SolarBombardmentId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldExplorationSatellitePendingRailgun = false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;
        if (!IsAstralSkillCard(cardPlay.Card))
            return;

        Flash();
        await SkillSolarBombardment.FireBaseBombardment(choiceContext, Owner, cardPlay.Card);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;
        if (!AstralParty_TokenGoldExplorationSatellitePendingRailgun)
            return;

        AstralParty_TokenGoldExplorationSatellitePendingRailgun = false;
        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<BaseAbilityOrbitalRailgun>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        if (PileType.Hand.GetPile(Owner).Cards.Count < HandThreshold)
            AstralParty_TokenGoldExplorationSatellitePendingRailgun = true;

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenGoldExplorationSatellitePendingRailgun = false;
        return Task.CompletedTask;
    }

    private static bool IsAstralSkillCard(CardModel card)
    {
        return card.CanonicalInstance is AstralPartyCardModel
               && card.Type == CardType.Skill;
    }
}