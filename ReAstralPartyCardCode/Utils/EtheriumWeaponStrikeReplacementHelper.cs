using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal enum EtheriumWeaponKind
{
    None = 0,
    Scythe = 1,
    Axe = 2,
    Sword = 3
}

internal static class EtheriumWeaponStrikeReplacementHelper
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

    [ThreadStatic]
    private static bool _isResolving;

    public static EtheriumWeaponKind GetActiveWeapon(Player? owner)
    {
        if (owner == null)
            return EtheriumWeaponKind.None;

        var active = EtheriumWeaponKind.None;
        foreach (var relic in owner.Relics.Where(static relic => !relic.IsMelted))
        {
            if (relic is EnigmaticSynthesisEtheriumScythe)
                active = EtheriumWeaponKind.Scythe;
            else if (relic is EnigmaticSynthesisEtheriumAxe)
                active = EtheriumWeaponKind.Axe;
            else if (relic is EnigmaticSynthesisEtheriumSword)
                active = EtheriumWeaponKind.Sword;
        }

        return active;
    }

    public static void ReplaceDeckStrikesForCurrentWeapon(Player? owner)
    {
        if (owner == null)
            return;

        var weapon = GetActiveWeapon(owner);
        if (weapon == EtheriumWeaponKind.None)
            return;

        var cards = EventDeckCardHelper.GetRunDeckCards(owner)
            .Where(card => IsReplaceableStrike(owner, card))
            .ToList();
        foreach (var card in cards)
            TryReplaceDeckCard(owner, card, weapon);
    }

    public static bool TryResolveAddedCard(Player owner, CardModel? addedCard)
    {
        if (_isResolving || addedCard == null)
            return false;

        var weapon = GetActiveWeapon(owner);
        if (weapon == EtheriumWeaponKind.None)
            return false;
        if (!IsReplaceableStrike(owner, addedCard))
            return false;

        return TryReplaceDeckCard(owner, addedCard, weapon);
    }

    public static bool IsReplaceableStrike(Player? owner, CardModel? card)
    {
        return IsBaseStrike(owner, card) || IsEtheriumStrike(card);
    }

    private static bool IsBaseStrike(Player? owner, CardModel? card)
    {
        return BaseStarterCardReplacementHelper.IsBaseStrike(owner, card);
    }

    private static bool IsEtheriumStrike(CardModel? card)
    {
        if (card == null)
            return false;

        var id = card.CanonicalInstance?.Id ?? card.Id;
        return id == ModelDb.GetId<EnigmaticStrikeEtheriumScythe>()
               || id == ModelDb.GetId<EnigmaticStrikeEtheriumAxe>()
               || id == ModelDb.GetId<EnigmaticStrikeEtheriumSword>();
    }

    private static bool TryReplaceDeckCard(Player owner, CardModel sourceCard, EtheriumWeaponKind weapon)
    {
        var targetId = GetTargetCardId(weapon);
        if (targetId == ModelId.none)
            return false;
        if ((sourceCard.CanonicalInstance?.Id ?? sourceCard.Id) == targetId)
            return false;

        var replacement = ModelDb.GetById<CardModel>(targetId).ToMutable();
        replacement.Owner = owner;
        replacement.FloorAddedToDeck = sourceCard.FloorAddedToDeck;
        CopyUpgradeState(sourceCard, replacement);

        _isResolving = true;
        try
        {
            var replaced = ReplaceRunStateCard(owner, sourceCard, replacement)
                           | ReplacePlayerDeckCard(owner, sourceCard, replacement);
            if (!replaced)
                return false;

            SaveManager.Instance?.MarkCardAsSeen(replacement.CanonicalInstance ?? replacement);
            EnigmaticOblivionDeckHelper.TryResolveAddedCard(owner, replacement);
            return true;
        }
        finally
        {
            _isResolving = false;
        }
    }

    private static bool ReplaceRunStateCard(Player owner, CardModel sourceCard, CardModel replacement)
    {
        var runState = owner.RunState;
        if (runState == null)
            return false;

        foreach (var memberName in RunDeckMemberNames)
        {
            var member = runState.GetType().GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(runState, member) is not IList list)
                continue;

            var index = list.IndexOf(sourceCard);
            if (index < 0)
                continue;

            list[index] = replacement;
            return true;
        }

        return false;
    }

    private static bool ReplacePlayerDeckCard(Player owner, CardModel sourceCard, CardModel replacement)
    {
        var deck = owner.Deck;
        if (deck == null)
            return false;

        if (deck.Cards is IList deckCards)
        {
            var index = deckCards.IndexOf(sourceCard);
            if (index >= 0)
            {
                deckCards[index] = replacement;
                return true;
            }
        }

        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(deck, member) is not IList list)
                continue;

            var index = list.IndexOf(sourceCard);
            if (index < 0)
                continue;

            list[index] = replacement;
            return true;
        }

        return false;
    }

    private static void CopyUpgradeState(CardModel source, CardModel replacement)
    {
        while (replacement.CurrentUpgradeLevel < source.CurrentUpgradeLevel)
        {
            replacement.UpgradeInternal();
            replacement.FinalizeUpgradeInternal();
        }
    }

    private static ModelId GetTargetCardId(EtheriumWeaponKind weapon)
    {
        return weapon switch
        {
            EtheriumWeaponKind.Scythe => ModelDb.GetId<EnigmaticStrikeEtheriumScythe>(),
            EtheriumWeaponKind.Axe => ModelDb.GetId<EnigmaticStrikeEtheriumAxe>(),
            EtheriumWeaponKind.Sword => ModelDb.GetId<EnigmaticStrikeEtheriumSword>(),
            _ => ModelId.none
        };
    }

    private static object? ReadMemberValue(object source, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo propertyInfo => propertyInfo.GetValue(source),
            FieldInfo fieldInfo => fieldInfo.GetValue(source),
            _ => null
        };
    }
}
