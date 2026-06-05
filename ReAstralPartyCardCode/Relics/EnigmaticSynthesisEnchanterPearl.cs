using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEnchanterPearl : EnigmaticUniqueMaterialRelicBase
{
    private static readonly string[] CardCollectionMemberNames =
    [
        "Cards",
        "_cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    private static readonly string[] RunDeckMemberNames =
    [
        "Deck",
        "Cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    [SavedProperty] public int AstralParty_EnigmaticSynthesisEnchanterPearlStacks { get; set; } = 1;
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence { get; set; }

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisEnchanterPearlStacks;
        set => AstralParty_EnigmaticSynthesisEnchanterPearlStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_enchanter_pearl";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || !HasSevenCurses())
            return false;

        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption == null)
            return false;

        smithOption.SmithCount += 1;
        return true;
    }

    internal void OnRestSiteOptionResolved(bool usedSmith)
    {
        if (!usedSmith || Owner == null || !HasSevenCurses())
            return;

        var curseCard = CreateRandomCurseCard();
        if (curseCard == null)
            return;

        curseCard.Owner = Owner;
        curseCard.FloorAddedToDeck = Math.Max(Owner.RunState?.TotalFloor ?? 1, 1);
        if (!TryAddCardToRunDeck(Owner, curseCard))
            return;

        Flash();
    }

    public static Task<EnigmaticSynthesisEnchanterPearl?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisEnchanterPearl>(owner, amount);
    }

    private bool HasSevenCurses()
    {
        return Owner?.GetRelic<EnigmaticSevenCurses>() != null;
    }

    private CardModel? CreateRandomCurseCard()
    {
        var candidates = ModelDb.AllCards
            .Where(card => card.Type == CardType.Curse)
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First())
            .OrderBy(card => (card.CanonicalInstance?.Id ?? card.Id).Entry, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            RelicId,
            "smith_random_curse",
            Owner?.NetId ?? 0UL,
            Owner?.RunState?.TotalFloor ?? 0,
            AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence);
        AstralParty_EnigmaticSynthesisEnchanterPearlCurseRollSequence++;

        return candidates[selectedIndex].ToMutable();
    }

    private static bool TryAddCardToRunDeck(Player owner, CardModel card)
    {
        if (owner.RunState == null)
            return TryAddCardToPlayerDeckCompatibility(owner, card);

        var runStateType = owner.RunState.GetType();
        foreach (var memberName in RunDeckMemberNames)
        {
            var member = runStateType.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(owner.RunState, member) is not IList list)
                continue;

            list.Add(card);
            SyncPlayerDeck(owner, card);
            return true;
        }

        return TryAddCardToPlayerDeckCompatibility(owner, card);
    }

    private static void SyncPlayerDeck(Player owner, CardModel card)
    {
        if (owner.Deck?.Cards == null || owner.Deck.Cards.Contains(card))
            return;

        owner.Deck.AddInternal(card);
    }

    private static bool TryAddCardToPlayerDeckCompatibility(Player owner, CardModel card)
    {
        var deck = owner.Deck;
        if (deck == null)
            return false;

        if (TryAddCardToList(deck.Cards, card))
            return true;

        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (TryAddCardToList(ReadMemberValue(deck, member), card))
                return true;
        }

        deck.AddInternal(card);
        return deck.Cards.Contains(card);
    }

    private static bool TryAddCardToList(object? source, CardModel card)
    {
        if (source is not IList list)
            return false;

        if (list.Contains(card))
            return true;

        list.Add(card);
        return list.Contains(card);
    }

    private static object? ReadMemberValue(object instance, MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => null
        };
    }
}
