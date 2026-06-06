using System.Collections;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StokovStarterBundleHelper
{
    private sealed record CharacterStarterBundle(
        Type CharacterType,
        Type StarterRelicType,
        Type UpgradeableStarterCardType);

    private sealed record StarterCardTransformMapping(Type OriginalType, Type UpgradedType);

    private sealed record StarterRelicRefinementMapping(Type OriginalType, Type UpgradedType);

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

    private static readonly CharacterStarterBundle[] StarterBundles =
    [
        new(
            typeof(Ironclad),
            typeof(BurningBlood),
            typeof(Bash)),
        new(
            typeof(Silent),
            typeof(RingOfTheSnake),
            typeof(Neutralize)),
        new(
            typeof(Defect),
            typeof(CrackedCore),
            typeof(Dualcast)),
        new(
            typeof(Necrobinder),
            typeof(BoundPhylactery),
            typeof(Unleash)),
        new(
            typeof(Regent),
            typeof(DivineRight),
            typeof(FallingStar))
    ];

    private static readonly StarterCardTransformMapping[] StarterCardTransformMappings =
    [
        new(typeof(Bash), typeof(Break)),
        new(typeof(Neutralize), typeof(Suppress)),
        new(typeof(Unleash), typeof(Protector)),
        new(typeof(FallingStar), typeof(MeteorShower)),
        new(typeof(Dualcast), typeof(Quadcast))
    ];

    private static readonly StarterRelicRefinementMapping[] StarterRelicRefinementMappings =
    [
        new(typeof(BurningBlood), typeof(BlackBlood)),
        new(typeof(RingOfTheSnake), typeof(RingOfTheDrake)),
        new(typeof(CrackedCore), typeof(InfusedCore)),
        new(typeof(BoundPhylactery), typeof(PhylacteryUnbound)),
        new(typeof(DivineRight), typeof(DivineDestiny))
    ];

    private static bool _registered;

    public static void RegisterAll()
    {
        if (_registered)
            return;

        foreach (var mapping in StarterCardTransformMappings)
            RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(mapping.OriginalType, mapping.UpgradedType);

        foreach (var mapping in StarterRelicRefinementMappings)
            RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(mapping.OriginalType, mapping.UpgradedType);

        var transformRegistry = RitsuLibFramework.GetCardTransformRegistry(MainFile.ModId);
        transformRegistry.Register(
            "stokov_starter_card_upgrade_propagation",
            context => context.Original.Owner?.GetRelic<VariantPersonStokov>() != null,
            async context =>
            {
                var owner = context.Original.Owner;
                var stokov = owner?.GetRelic<VariantPersonStokov>();
                if (owner == null || stokov == null)
                    return;

                await TryPropagateStarterCardUpgradeAsync(stokov, context.Original);
            });

        _registered = true;
    }

    public static async Task GrantStarterBundleAsync(VariantPersonStokov stokov)
    {
        var owner = stokov.Owner;
        if (owner == null)
            return;

        var trackedStarterRelicIds = new HashSet<string>(StringComparer.Ordinal);
        var trackedUpgradeableStarterCardIds = new HashSet<string>(StringComparer.Ordinal);
        var currentCharacterId = owner.Character.Id;
        var floorAddedToDeck = Math.Max(owner.RunState?.TotalFloor ?? 1, 1);

        foreach (var bundle in StarterBundles)
        {
            var bundleCharacterId = ModelDb.GetId(bundle.CharacterType);
            if (currentCharacterId == bundleCharacterId)
                continue;

            var starterRelic = ModelDb.GetById<RelicModel>(ModelDb.GetId(bundle.StarterRelicType));
            var starterRelicId = (starterRelic.CanonicalInstance?.Id ?? starterRelic.Id).ToString();
            trackedStarterRelicIds.Add(starterRelicId);
            if (owner.Relics.All(relic => (relic.CanonicalInstance?.Id ?? relic.Id) != (starterRelic.CanonicalInstance?.Id ?? starterRelic.Id)))
                await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, starterRelic);

            var canonicalCard = ModelDb.GetById<CardModel>(ModelDb.GetId(bundle.UpgradeableStarterCardType));
            var mutableCard = canonicalCard.ToMutable();
            mutableCard.Owner = owner;
            mutableCard.FloorAddedToDeck = floorAddedToDeck;
            TryAddCardToRunDeck(owner, mutableCard);
            SaveManager.Instance?.MarkCardAsSeen(mutableCard.CanonicalInstance ?? mutableCard);
            trackedUpgradeableStarterCardIds.Add(ModelDb.GetId(bundle.UpgradeableStarterCardType).ToString());
        }

        stokov.SetTrackedStarterRelicIds(trackedStarterRelicIds);
        stokov.SetTrackedStarterCardIds(trackedUpgradeableStarterCardIds);
    }

    public static bool IsTrackedStarterRelic(VariantPersonStokov stokov, ModelId relicId)
    {
        return stokov.GetTrackedStarterRelicIds().Contains(relicId.ToString());
    }

    public static bool IsTrackedStarterCard(VariantPersonStokov stokov, CardModel card)
    {
        var id = card.CanonicalInstance?.Id ?? card.Id;
        return stokov.GetTrackedStarterCardIds().Contains(id.ToString());
    }

    public static async Task TryPropagateStarterCardUpgradeAsync(VariantPersonStokov stokov, CardModel transformedOriginal)
    {
        if (!VariantPersonStokov.TryEnterCardPropagation())
            return;

        try
        {
            if (!IsTrackedStarterCard(stokov, transformedOriginal))
                return;

            var owner = stokov.Owner;
            if (owner == null)
                return;

            var transformedOriginalId = transformedOriginal.CanonicalInstance?.Id ?? transformedOriginal.Id;
            foreach (var trackedCardIdRaw in stokov.GetTrackedStarterCardIds())
            {
                if (!TryDeserializeModelId(trackedCardIdRaw, out var trackedCardId))
                    continue;
                if (trackedCardId == transformedOriginalId)
                    continue;
                if (!TryGetStarterCardUpgradeTargetId(trackedCardId, out var upgradedCardId))
                    continue;

                var targetCard = EventDeckCardHelper.GetRunDeckCards(owner)
                    .FirstOrDefault(card =>
                        (card.CanonicalInstance?.Id ?? card.Id) == trackedCardId
                        && card.CurrentUpgradeLevel == 0);
                if (targetCard == null)
                    continue;

                var replacement = ModelDb.GetById<CardModel>(upgradedCardId).ToMutable();
                replacement.Owner = owner;
                await CardCmd.Transform(targetCard, replacement);
            }
        }
        finally
        {
            VariantPersonStokov.ExitCardPropagation();
        }
    }

    public static async Task TryPropagateStarterRelicUpgradeAsync(Player owner, ModelId transformedOriginalRelicId)
    {
        var stokov = owner.GetRelic<VariantPersonStokov>();
        if (stokov == null || !IsTrackedStarterRelic(stokov, transformedOriginalRelicId))
            return;
        if (!VariantPersonStokov.TryEnterRelicPropagation())
            return;

        try
        {
            foreach (var trackedRelicIdRaw in stokov.GetTrackedStarterRelicIds())
            {
                if (!TryDeserializeModelId(trackedRelicIdRaw, out var trackedRelicId))
                    continue;
                if (trackedRelicId == transformedOriginalRelicId)
                    continue;
                if (!TryGetStarterRelicUpgradeTargetId(trackedRelicId, out var upgradedRelicId))
                    continue;

                var ownedStarterRelic = owner.Relics.FirstOrDefault(relic =>
                    (relic.CanonicalInstance?.Id ?? relic.Id) == trackedRelicId);
                if (ownedStarterRelic == null)
                    continue;

                var replacement = ModelDb.GetById<RelicModel>(upgradedRelicId).ToMutable();
                await RelicCmd.Replace(ownedStarterRelic, replacement);
            }
        }
        finally
        {
            VariantPersonStokov.ExitRelicPropagation();
        }
    }

    public static string SerializeIdSet(IEnumerable<string> ids)
    {
        return JsonSerializer.Serialize(ids.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    public static HashSet<string> DeserializeIdSet(string value)
    {
        try
        {
            var ids = JsonSerializer.Deserialize<string[]>(value) ?? [];
            return ids.ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
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
            EnigmaticOblivionDeckHelper.TryResolveAddedCard(owner, card);
            EtheriumWeaponStrikeReplacementHelper.TryResolveAddedCard(owner, card);
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
        EnigmaticOblivionDeckHelper.TryResolveAddedCard(owner, card);
        EtheriumWeaponStrikeReplacementHelper.TryResolveAddedCard(owner, card);
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

    private static bool TryDeserializeModelId(string serialized, out ModelId modelId)
    {
        try
        {
            modelId = ModelId.Deserialize(serialized);
            return true;
        }
        catch
        {
            modelId = ModelId.none;
            return false;
        }
    }

    private static bool TryGetStarterCardUpgradeTargetId(ModelId originalId, out ModelId upgradedId)
    {
        foreach (var mapping in StarterCardTransformMappings)
        {
            if (ModelDb.GetId(mapping.OriginalType) != originalId)
                continue;

            upgradedId = ModelDb.GetId(mapping.UpgradedType);
            return true;
        }

        upgradedId = ModelId.none;
        return false;
    }

    private static bool TryGetStarterRelicUpgradeTargetId(ModelId originalId, out ModelId upgradedId)
    {
        foreach (var mapping in StarterRelicRefinementMappings)
        {
            if (ModelDb.GetId(mapping.OriginalType) != originalId)
                continue;

            upgradedId = ModelDb.GetId(mapping.UpgradedType);
            return true;
        }

        upgradedId = ModelId.none;
        return false;
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
