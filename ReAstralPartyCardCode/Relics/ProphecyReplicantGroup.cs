using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Utils.Persistence;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class ProphecyReplicantGroup : AstralPartyRelicModel, IModRightClickableRelic
{
    private const int BasePaidActivationCost = 200;

    private static readonly LocString SelectionPrompt =
        new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_PROPHECY_REPLICANT_GROUP.select_prompt");

    [SavedProperty] public int AstralParty_ProphecyReplicantGroupRemainingStacks { get; set; }
    [SavedProperty] public int AstralParty_ProphecyReplicantGroupPaidActivationCount { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetRemainingStacks();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override Task AfterObtained()
    {
        InvokeDisplayAmountChanged();
        return base.AfterObtained();
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Player == Owner
               && Owner?.Creature?.CombatState != null;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        if (context.Player != Owner || Owner?.Creature?.CombatState == null)
            return false;

        var handCount = PileType.Hand.GetPile(Owner).Cards.Count;
        if (handCount <= 0)
            return false;

        return GetRemainingStacks() > 0 || Owner.Gold >= GetCurrentPaidCost();
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (context.Player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var owner = Owner;
        var hand = PileType.Hand.GetPile(owner).Cards;
        if (hand.Count == 0)
            return;

        PlayerChoiceContext choiceContext = context.PlayerChoiceContext is { } queuedChoiceContext
            ? queuedChoiceContext
            : new ThrowingPlayerChoiceContext();
        var selectedCard = await SelectCardFromHand(choiceContext, owner);
        if (selectedCard == null || Owner?.Creature?.CombatState == null)
            return;

        if (!await TrySpendActivationCost(owner))
            return;

        var clonedCard = CreateAutoplayClone(owner, selectedCard);
        var autoPlayTarget = ResolveAutoplayTarget(owner, clonedCard);
        await CardCmd.AutoPlay(choiceContext, clonedCard, autoPlayTarget, AutoPlayType.Default, false, true);
        Flash();
        InvokeDisplayAmountChanged();
    }

    internal void InitializeStacks(int stacks)
    {
        AstralParty_ProphecyReplicantGroupRemainingStacks = stacks;
        AstralParty_ProphecyReplicantGroupPaidActivationCount = 0;
        InvokeDisplayAmountChanged();
    }

    private int GetRemainingStacks()
    {
        return Math.Max(0, AstralParty_ProphecyReplicantGroupRemainingStacks);
    }

    private decimal GetCurrentPaidCost()
    {
        return BasePaidActivationCost * (1m + 0.5m * AstralParty_ProphecyReplicantGroupPaidActivationCount);
    }

    private async Task<CardModel?> SelectCardFromHand(PlayerChoiceContext choiceContext, Player owner)
    {
        var handCount = PileType.Hand.GetPile(owner).Cards.Count;
        if (handCount <= 0)
            return null;

        var prefs = new CardSelectorPrefs(SelectionPrompt, 1, 1)
        {
            Cancelable = true
        };
        var selectedCards = await DeterministicMultiplayerChoiceHelper.SelectHandCardsForPlayer(
            choiceContext,
            owner,
            prefs,
            selectionSource: this);
        return selectedCards.FirstOrDefault();
    }

    private async Task<bool> TrySpendActivationCost(Player owner)
    {
        if (GetRemainingStacks() > 0)
        {
            AstralParty_ProphecyReplicantGroupRemainingStacks =
                Math.Max(0, AstralParty_ProphecyReplicantGroupRemainingStacks - 1);
            return true;
        }

        var paidCost = GetCurrentPaidCost();
        if (owner.Gold < paidCost)
            return false;

        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(paidCost, owner, GoldLossType.Spent);
        AstralParty_ProphecyReplicantGroupPaidActivationCount++;
        return true;
    }

    private CardModel CreateAutoplayClone(Player owner, CardModel selectedCard)
    {
        var combatState = owner.Creature?.CombatState
                          ?? throw new InvalidOperationException("Cannot clone a hand card outside combat.");
        var canonicalCard = selectedCard.CanonicalInstance ?? selectedCard;
        var clonedCard = combatState.CreateCard(canonicalCard, owner);
        CopyUpgradeLevel(selectedCard, clonedCard);

        if (!clonedCard.Keywords.Contains(CardKeyword.Exhaust))
            CardCmd.ApplyKeyword(clonedCard, CardKeyword.Exhaust);

        return clonedCard;
    }

    private static Creature? ResolveAutoplayTarget(Player owner, CardModel clonedCard)
    {
        var ownerCreature = owner.Creature;
        if (ownerCreature?.CombatState == null)
            return ownerCreature;

        return clonedCard.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.RandomEnemy =>
                CombatTargetOrdering.GetLivingOpponentsStable(ownerCreature).FirstOrDefault(),
            _ => ownerCreature
        };
    }

    private static void CopyUpgradeLevel(CardModel sourceCard, CardModel clonedCard)
    {
        while (clonedCard.CurrentUpgradeLevel < sourceCard.CurrentUpgradeLevel)
        {
            clonedCard.UpgradeInternal();
            clonedCard.FinalizeUpgradeInternal();
        }
    }
}
