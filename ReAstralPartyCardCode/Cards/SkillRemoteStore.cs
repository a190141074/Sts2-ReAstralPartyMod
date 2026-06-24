using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillRemoteStore : AstralPartyCardModel
{
    private const int CardPrice = 10;

    private static readonly LocString SelectionPrompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_REMOTE_STORE.select_prompt");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillRemoteStore() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var owner = Owner;
        var combatState = owner?.Creature?.CombatState;
        var runState = owner?.RunState;
        if (owner == null || combatState == null || runState == null)
            return;

        var offers = RemoteStoreCardHelper.BuildDeterministicOffer(owner, Id.Entry);
        if (offers.Count == 0)
            return;

        var maxSelectable = Math.Min(
            offers.Count,
            StableNumericStateHelper.FloorDivisionToNonNegativeInt(owner.Gold, CardPrice));
        if (maxSelectable <= 0)
            return;

        var displayOptions = RemoteStoreCardHelper.CreateDisplayOptions(owner, offers);
        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, maxSelectable)
        {
            Cancelable = true
        };

        var selectedOffers = (await CardSelectCmd.FromSimpleGrid(choiceContext, displayOptions, owner, prefs))
            .Select(RemoteStoreCardHelper.ResolveSelectedOffer)
            .Where(static offer => offer != null)
            .Cast<RemoteStoreCardOffer>()
            .ToList();
        if (selectedOffers.Count == 0)
            return;

        var totalCost = selectedOffers.Count * CardPrice;
        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(totalCost, owner, GoldLossType.Spent);

        foreach (var selectedOffer in selectedOffers)
        {
            var freshCard = combatState.CreateCard(selectedOffer.CanonicalCard.CanonicalInstance ?? selectedOffer.CanonicalCard, owner);
            await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(freshCard, true, CardPilePosition.Top, this);
        }
    }
}

internal sealed record RemoteStoreCardOffer(string SlotKey, CardModel CanonicalCard);

internal static class RemoteStoreCardHelper
{
    private const string DemonFormEntry = "DEMON_FORM";

    private static readonly Type[] ColorlessPoolTypes =
    [
        typeof(ColorlessCardPool)
    ];

    private static readonly Dictionary<CardModel, RemoteStoreCardOffer> DisplayOptionMap = new(ReferenceEqualityComparer.Instance);

    public static IReadOnlyList<RemoteStoreCardOffer> BuildDeterministicOffer(MegaCrit.Sts2.Core.Entities.Players.Player owner, string contextId)
    {
        var ownedIds = new HashSet<ModelId>();
        var offers = new List<RemoteStoreCardOffer>(10);

        TryAddOffer(offers, ownedIds, GetCharacterPoolCandidates(owner, CardType.Attack), owner, contextId, "attack");
        TryAddOffer(offers, ownedIds, GetCharacterPoolCandidates(owner, CardType.Skill), owner, contextId, "skill");
        TryAddOffer(offers, ownedIds, GetCharacterPoolCandidates(owner, CardType.Power), owner, contextId, "power");
        TryAddOffer(offers, ownedIds, GetColorlessCandidates(owner), owner, contextId, "colorless");
        TryAddOffer(offers, ownedIds, GetBaseAbilityCandidates(owner), owner, contextId, "base_ability");
        AddFixedOffer(offers, ResolveDemonForm(), "demon_form_1");
        AddFixedOffer(offers, ResolveDemonForm(), "demon_form_2");
        AddFixedOffer(offers, ResolveDemonForm(), "demon_form_3");
        AddFixedOffer(offers, ResolveDemonForm(), "demon_form_4");
        AddFixedOffer(offers, ResolveDemonForm(), "demon_form_5");

        return offers;
    }

    public static IReadOnlyList<CardModel> CreateDisplayOptions(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        IReadOnlyList<RemoteStoreCardOffer> offers)
    {
        DisplayOptionMap.Clear();

        var displayOptions = new List<CardModel>(offers.Count);
        foreach (var offer in offers)
        {
            var displayCard = (offer.CanonicalCard.CanonicalInstance ?? offer.CanonicalCard).ToMutable();
            displayCard.Owner = owner;
            DisplayOptionMap[displayCard] = offer;
            displayOptions.Add(displayCard);
        }

        return displayOptions;
    }

    public static RemoteStoreCardOffer? ResolveSelectedOffer(CardModel selectedCard)
    {
        return DisplayOptionMap.GetValueOrDefault(selectedCard);
    }

    private static void TryAddOffer(
        ICollection<RemoteStoreCardOffer> offers,
        ISet<ModelId> excludedIds,
        IEnumerable<CardModel> candidates,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        string contextId,
        string slotKey)
    {
        var ordered = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                CanonicalDistinct(candidates)
                    .Where(card => !excludedIds.Contains(GetCanonicalId(card))),
                static card => GetCanonicalId(card).Entry,
                MainFile.ModId,
                nameof(SkillRemoteStore),
                contextId,
                owner.RunState?.Rng.StringSeed ?? "<null_seed>",
                owner.NetId,
                owner.RunState?.CurrentActIndex ?? -1,
                owner.RunState?.TotalFloor ?? -1,
                slotKey)
            .ToList();
        var selected = ordered.FirstOrDefault();
        if (selected == null)
            return;

        offers.Add(new RemoteStoreCardOffer(slotKey, selected.CanonicalInstance ?? selected));
        excludedIds.Add(GetCanonicalId(selected));
    }

    private static void AddFixedOffer(
        ICollection<RemoteStoreCardOffer> offers,
        CardModel? canonicalCard,
        string slotKey)
    {
        if (canonicalCard == null)
            return;

        offers.Add(new RemoteStoreCardOffer(slotKey, canonicalCard.CanonicalInstance ?? canonicalCard));
    }

    private static IEnumerable<CardModel> GetCharacterPoolCandidates(
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        CardType requiredType)
    {
        return owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Type == requiredType)
            .Where(IsEligibleRemoteStoreCard);
    }

    private static IEnumerable<CardModel> GetColorlessCandidates(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return ModelDb.AllCardPools
            .Where(pool => ColorlessPoolTypes.Any(type => type.IsInstanceOfType(pool)))
            .SelectMany(pool => pool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint))
            .Where(IsEligibleRemoteStoreCard);
    }

    private static IEnumerable<CardModel> GetBaseAbilityCandidates(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return BaseAbilityCardRegistry.GetCandidateTypes(owner)
            .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
            .Where(static card => card != null)
            .Select(static card => card.CanonicalInstance ?? card);
    }

    private static CardModel? ResolveDemonForm()
    {
        if (!OptionalModModelResolver.TryFindCardByEntry(DemonFormEntry, out var card))
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] SkillRemoteStore could not resolve fixed remote store card '{DemonFormEntry}'.");
            return null;
        }

        return card.CanonicalInstance ?? card;
    }

    private static IEnumerable<CardModel> CanonicalDistinct(IEnumerable<CardModel> cards)
    {
        return cards
            .GroupBy(GetCanonicalId)
            .Select(group => group.First());
    }

    private static bool IsEligibleRemoteStoreCard(CardModel card)
    {
        if (card.Type is CardType.Status or CardType.Curse)
            return false;
        if (card.Rarity == CardRarity.Token)
            return false;
        if (!card.ShouldShowInCardLibrary)
            return false;
        if (!IsBaseGameCard(card))
            return false;

        return card.EnergyCost.CostsX || card.EnergyCost.GetResolved() >= 1m;
    }

    private static bool IsBaseGameCard(CardModel card)
    {
        return card.GetType().Assembly == typeof(CardModel).Assembly;
    }

    private static ModelId GetCanonicalId(CardModel card)
    {
        return (card.CanonicalInstance ?? card).Id;
    }
}
