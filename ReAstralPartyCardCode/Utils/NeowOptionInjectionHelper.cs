using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using STS2RitsuLib;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class NeowOptionInjectionHelper
{
    private const string DreamFaceTheShadowTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.DREAM_FACE_THE_SHADOW";
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

    private static readonly MethodInfo? DoneMethod = typeof(AncientEventModel)
        .GetMethod("Done", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly IReadOnlyList<IHoverTip> ForgottenRoarHoverTips =
        [HoverTipFactory.FromCard<UltimateSkillForgottenRoar>()];

    public static void Register()
    {
        MainFile.Logger.Info(
            "[NeowOptionInjectionHelper] Registering Dream Face the Shadow through RitsuLib ancient-option registry.");
        RitsuLibFramework.RegisterAncientOption<Neow>(
            MainFile.ModId,
            ModAncientOptionRule.Single(CreateDreamFaceTheShadowOption));
    }

    private static EventOption? CreateDreamFaceTheShadowOption(AncientEventModel ancient)
    {
        return new EventOption(ancient, () => ChooseDreamFaceTheShadow(ancient), DreamFaceTheShadowTextKey)
        {
            HoverTips = ForgottenRoarHoverTips
        };
    }

    private static async Task ChooseDreamFaceTheShadow(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Dream Face the Shadow was chosen.");

        await AddDreamFaceTheShadowCardToDeck(owner);
        CompleteAncient(ancient);
    }

    private static Task AddDreamFaceTheShadowCardToDeck(Player owner)
    {
        return CardGainAttribution.RunWithSource(
            null,
            () =>
            {
                var mutableCard = ModelDb.Card<UltimateSkillForgottenRoar>().ToMutable();
                mutableCard.Owner = owner;
                mutableCard.FloorAddedToDeck = 1;
                if (!TryAddCardToRunDeck(owner, mutableCard))
                    throw new InvalidOperationException(
                        $"Failed to add Forgotten Roar to run deck for player {owner.NetId}.");

                SaveManager.Instance?.MarkCardAsSeen(mutableCard.CanonicalInstance ?? mutableCard);

                return Task.CompletedTask;
            });
    }

    private static bool TryAddCardToRunDeck(Player owner, CardModel card)
    {
        if (owner.RunState == null)
        {
            MainFile.Logger.Warn($"[NeowOptionInjectionHelper] Failed to add Forgotten Roar: owner {owner.NetId} had no RunState.");
            return false;
        }

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
            MainFile.Logger.Info(
                $"[NeowOptionInjectionHelper] Added Forgotten Roar to run deck | owner={owner.NetId} | member={memberName} | runDeckCount={list.Count} | playerDeckCount={owner.Deck?.Cards.Count ?? 0}");
            return true;
        }

        if (TryAddCardToPlayerDeckCompatibility(owner, card))
        {
            MainFile.Logger.Warn(
                $"[NeowOptionInjectionHelper] Added Forgotten Roar via Player.Deck compatibility path | owner={owner.NetId} | playerDeckCount={owner.Deck?.Cards.Count ?? 0}");
            return true;
        }

        MainFile.Logger.Warn($"[NeowOptionInjectionHelper] Failed to locate run deck container for owner {owner.NetId}.");
        return false;
    }

    private static void SyncPlayerDeck(Player owner, CardModel card)
    {
        if (owner.Deck?.Cards == null)
            return;

        if (owner.Deck.Cards.Contains(card))
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

        if (deck.Cards.Contains(card))
            return true;

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

    private static object? ReadMemberValue(object source, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo propertyInfo => propertyInfo.GetValue(source),
            FieldInfo fieldInfo => fieldInfo.GetValue(source),
            _ => null
        };
    }

    private static void CompleteAncient(AncientEventModel ancient)
    {
        if (DoneMethod == null)
            throw new InvalidOperationException("Failed to resolve AncientEventModel.Done via reflection.");

        DoneMethod.Invoke(ancient, null);
    }
}
