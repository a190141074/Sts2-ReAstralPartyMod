using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeScionGrace : AstralPartyRelicModel
{
    private const int RoyalPrerogativeCombatCountPerAct = 1;

    [SavedProperty] public int AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeCombats { get; set; }

    [SavedProperty] public int AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeActIndex { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Royalties>(),
        HoverTipFactory.FromCard<SkillPowerfulPity>(),
        HoverTipFactory.FromCard<Wish>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null || Owner.RunState == null)
            return;

        RefreshPendingRoyalPrerogativeForCurrentAct();

        if (AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeCombats <= 0)
            return;

        var combatState = Owner.Creature.CombatState;
        var stablePlayers = PersonaMultiplayerEffectHelper.GetStableCombatPlayers(Owner);
        if (stablePlayers.Count == 0)
            return;

        Flash();
        foreach (var player in stablePlayers)
        {
            if (player?.Creature?.CombatState == null)
                continue;

            var card = combatState.CreateCard(ModelDb.Card<Royalties>(), player);
            await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
                card,
                true,
                CardPilePosition.Top,
                this);
        }

        AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeCombats--;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;

        var cardId = (cardPlay.Card.CanonicalInstance ?? cardPlay.Card).Id;
        if (cardId != ModelDb.GetId<SkillPowerfulPity>())
            return;

        var wishCard = Owner.Creature.CombatState.CreateCard(ModelDb.Card<Wish>(), Owner);
        Flash();
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
            wishCard,
            true,
            CardPilePosition.Top,
            this);
    }

    private void RefreshPendingRoyalPrerogativeForCurrentAct()
    {
        if (Owner?.RunState == null)
            return;

        var currentActIndex = Owner.RunState.CurrentActIndex;
        if (AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeActIndex == currentActIndex)
            return;

        AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeActIndex = currentActIndex;
        AstralParty_PersonalityDerivativeScionGracePendingRoyalPrerogativeCombats = RoyalPrerogativeCombatCountPerAct;
    }
}
