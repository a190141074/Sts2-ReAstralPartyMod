using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillPowerfulPityPlus : AstralPartyCardModel
{
    private const int MaxSelectedCards = 4;
    private static readonly LocString SelectionPrompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_POWERFUL_PITY_PLUS.select_prompt");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralKeywords.AstralUnique];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        AstralKeywords.CreateHoverTip(AstralKeywords.IrrefusableId)
    ];

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    public SkillPowerfulPityPlus() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var target = cardPlay.Target?.Player;
        if (owner == null || !BaseAbilityHelper.IsOtherLivingPlayerTarget(owner, cardPlay.Target))
            return;

        var selectedCards = await SelectDeckCards(owner);
        selectedCards = await EventDeckCardMutationHelper.SynchronizeTransferSelection(
            owner,
            selectedCards,
            "skill_powerful_pity_plus");
        var transferredCount = IrrefusableCardHelper.QueueTransfers(owner, target!, selectedCards);

        var eternalStarlightGranted = transferredCount == MaxSelectedCards ? 2 : 1;
        await TokenEternalStarlight.GrantStacks(owner, eternalStarlightGranted);
        if (transferredCount == MaxSelectedCards)
            await PlayerCmd.GainEnergy(1m, target!);

        MainFile.Logger.Info(
            $"[SkillPowerfulPityPlus] transfer queued | owner={owner.NetId} | target={target!.NetId} | selected={selectedCards.Count} | transfers={transferredCount} | eternal_starlight={eternalStarlightGranted}");
    }

    private static async Task<IReadOnlyList<CardModel>> SelectDeckCards(Player owner)
    {
        var eligibleDeckCards = EventDeckCardHelper.GetRunDeckCards(owner)
            .Where(card => !FallenHeavenlyKingDeckHelper.IsHeavenlyKingDeckCard(card))
            .ToList();
        if (eligibleDeckCards.Count == 0)
            return [];

        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, Math.Min(MaxSelectedCards, eligibleDeckCards.Count))
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        var selected = await CardSelectCmd.FromDeckGeneric(
            owner,
            prefs,
            static card => !FallenHeavenlyKingDeckHelper.IsHeavenlyKingDeckCard(card),
            null!);
        return selected.Distinct().Take(MaxSelectedCards).ToList();
    }
}
