using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class FateGuidanceCompatibilityHelper
{
    public static async Task GrantFateGuidanceAsync(
        PersonBlueWhale? sourceBlueWhale,
        Player? recipient,
        int amount,
        bool allowStoreForNextCombat)
    {
        if (sourceBlueWhale?.Owner == null || recipient?.Creature?.CombatState == null || amount <= 0)
            return;

        for (var i = 0; i < amount; i++)
            await GrantSingleFateGuidanceAsync(sourceBlueWhale, recipient, allowStoreForNextCombat);
    }

    private static async Task GrantSingleFateGuidanceAsync(
        PersonBlueWhale sourceBlueWhale,
        Player recipient,
        bool allowStoreForNextCombat)
    {
        var combatState = recipient.Creature?.CombatState;
        if (combatState == null)
            return;

        if (CombatManager.Instance.IsOverOrEnding)
        {
            if (allowStoreForNextCombat)
                sourceBlueWhale.AddPendingFateGuidanceForRecipient(recipient, 1);
            return;
        }

        var card = CreateFateGuidanceCard(combatState, recipient, sourceBlueWhale.Owner!.NetId);
        var addResult = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
        if (!addResult.success)
        {
            if (allowStoreForNextCombat)
                sourceBlueWhale.AddPendingFateGuidanceForRecipient(recipient, 1);
            return;
        }

        if (card.Pile?.Type == PileType.Hand)
        {
            await GeneratedCardObserver.NotifyCardAddedToHand(card, sourceBlueWhale);
            await XiaoLeiAwakeningHelper.TryGrantAwakeningForGrantedCard(sourceBlueWhale.Owner, recipient);
            return;
        }

        if (!CombatManager.Instance.IsOverOrEnding && recipient.Creature?.CombatState != null)
        {
            if (card.Pile != null)
                await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);

            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, null, skipCardPileVisuals: true);
            return;
        }

        if (allowStoreForNextCombat)
            sourceBlueWhale.AddPendingFateGuidanceForRecipient(recipient, 1);
    }

    private static SkillFateGuidance CreateFateGuidanceCard(
        CombatState combatState,
        Player recipient,
        ulong sourceBlueWhalePlayerNetId)
    {
        var card = combatState.CreateCard(ModelDb.Card<SkillFateGuidance>(), recipient) as SkillFateGuidance;
        if (card == null)
            throw new InvalidOperationException("Failed to create mutable SkillFateGuidance instance.");

        card.AstralParty_FateGuidanceSourceBlueWhalePlayerNetId = sourceBlueWhalePlayerNetId;
        return card;
    }
}
