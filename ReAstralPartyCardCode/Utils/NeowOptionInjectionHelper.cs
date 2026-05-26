using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using STS2RitsuLib;
using STS2RitsuLib.Scaffolding.Ancients.Options;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class NeowOptionInjectionHelper
{
    private const string FaceTheShadowTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.INITIAL.options.FACE_THE_SHADOW";
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
            "[NeowOptionInjectionHelper] Registering Face the Shadow through RitsuLib ancient-option registry.");
        RitsuLibFramework.RegisterAncientOption<Neow>(
            MainFile.ModId,
            ModAncientOptionRule.Single(CreateForgottenRoarOption));
    }

    private static EventOption? CreateForgottenRoarOption(AncientEventModel ancient)
    {
        return new EventOption(ancient, () => ChooseForgottenRoar(ancient), FaceTheShadowTextKey)
        {
            HoverTips = ForgottenRoarHoverTips
        };
    }

    private static async Task ChooseForgottenRoar(AncientEventModel ancient)
    {
        var owner = ancient.Owner
                    ?? throw new InvalidOperationException(
                        "Neow had no owner when Face the Shadow was chosen.");

        await AddForgottenRoarToDeck(owner);
        CompleteAncient(ancient);
    }

    private static Task AddForgottenRoarToDeck(Player owner)
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
