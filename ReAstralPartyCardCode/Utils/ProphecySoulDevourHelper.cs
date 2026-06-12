using System.Text.Json;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ProphecySoulDevourHelper
{
    private const int DiscoveryChoiceCount = 3;
    private const int RefreshGoldCost = 10;
    private const int BattleThreshold = 3;
    private const int HiddenStrikeDelayFloors = 30;
    private const int PhaseReactionDelayFloors = 2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly PlayerChoiceContext NoChoiceContext = new NoChoicePlayerChoiceContext();
    private static readonly CardSelectorPrefs ProphecyDeckCardGridPrefs = new(new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_PROPHECY_SOUL_DEVOUR.selectionScreenHeader"), 1)
    {
        Cancelable = false,
        RequireManualConfirmation = true,
        PretendCardsCanBePlayed = true
    };

    public static string SerializeKinds(IEnumerable<ProphecySoulDevourKind> values)
    {
        return JsonSerializer.Serialize(values.Select(static value => (int)value).ToArray(), JsonOptions);
    }

    public static List<ProphecySoulDevourKind> DeserializeKinds(string value)
    {
        try
        {
            return (JsonSerializer.Deserialize<int[]>(value, JsonOptions) ?? [])
                .Select(static raw => Enum.IsDefined(typeof(ProphecySoulDevourKind), raw)
                    ? (ProphecySoulDevourKind)raw
                    : ProphecySoulDevourKind.None)
                .Where(static kind => kind != ProphecySoulDevourKind.None)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeDelayedCards(IEnumerable<ProphecySoulDevourDelayedCardGrant> values)
    {
        return JsonSerializer.Serialize(values.ToArray(), JsonOptions);
    }

    public static List<ProphecySoulDevourDelayedCardGrant> DeserializeDelayedCards(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ProphecySoulDevourDelayedCardGrant[]>(value, JsonOptions)?.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeDelayedRelics(IEnumerable<ProphecySoulDevourDelayedRelicGrant> values)
    {
        return JsonSerializer.Serialize(values.ToArray(), JsonOptions);
    }

    public static List<ProphecySoulDevourDelayedRelicGrant> DeserializeDelayedRelics(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ProphecySoulDevourDelayedRelicGrant[]>(value, JsonOptions)?.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeMimicSnapshots(IEnumerable<ProphecySoulDevourMimicSnapshot> values)
    {
        return JsonSerializer.Serialize(values.ToArray(), JsonOptions);
    }

    public static List<ProphecySoulDevourMimicSnapshot> DeserializeMimicSnapshots(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ProphecySoulDevourMimicSnapshot[]>(value, JsonOptions)?.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static bool HasPermanentProphecy(ProphecySoulDevour relic, ProphecySoulDevourKind kind)
    {
        return kind switch
        {
            ProphecySoulDevourKind.TreasureHunting => relic.AstralParty_ProphecySoulDevourTreasureHuntingPermanent,
            ProphecySoulDevourKind.MatrixRecycling => relic.AstralParty_ProphecySoulDevourMatrixRecyclingPermanent,
            ProphecySoulDevourKind.AncientRuins => relic.AstralParty_ProphecySoulDevourAncientRuinsPermanent,
            ProphecySoulDevourKind.MineralRecovery => relic.AstralParty_ProphecySoulDevourMineralRecoveryPermanent,
            ProphecySoulDevourKind.EnergySavingStrategy => relic.AstralParty_ProphecySoulDevourEnergySavingStrategyStacks > 0,
            _ => false
        };
    }

    public static void GrantPermanentProphecy(ProphecySoulDevour relic, ProphecySoulDevourKind kind)
    {
        switch (kind)
        {
            case ProphecySoulDevourKind.TreasureHunting:
                relic.AstralParty_ProphecySoulDevourTreasureHuntingPermanent = true;
                break;
            case ProphecySoulDevourKind.MatrixRecycling:
                relic.AstralParty_ProphecySoulDevourMatrixRecyclingPermanent = true;
                break;
            case ProphecySoulDevourKind.AncientRuins:
                relic.AstralParty_ProphecySoulDevourAncientRuinsPermanent = true;
                break;
            case ProphecySoulDevourKind.MineralRecovery:
                relic.AstralParty_ProphecySoulDevourMineralRecoveryPermanent = true;
                break;
            case ProphecySoulDevourKind.EnergySavingStrategy:
                relic.AstralParty_ProphecySoulDevourEnergySavingStrategyStacks++;
                break;
        }
    }

    public static void QueueImmediateDiscovery(ProphecySoulDevour relic, int count = 1)
    {
        relic.AstralParty_ProphecySoulDevourPendingDiscoveryCounter = Math.Max(0, relic.AstralParty_ProphecySoulDevourPendingDiscoveryCounter + Math.Max(0, count));
        relic.NotifyDisplayAmountChanged();
    }

    public static void AdvanceAfterCombat(ProphecySoulDevour relic)
    {
        relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter = Math.Max(1, relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter);
        relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter--;
        if (relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter > 0)
            return;

        relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter = BattleThreshold;
        relic.AstralParty_ProphecySoulDevourPendingDiscoveryCounter++;
        relic.NotifyDisplayAmountChanged();
    }

    public static void ApplyFastFluctuation(ProphecySoulDevour relic)
    {
        relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter =
            Math.Max(1, relic.AstralParty_ProphecySoulDevourDiscoveryBattleCounter - 1);
        relic.NotifyDisplayAmountChanged();
    }

    public static void QueuePhaseReaction(ProphecySoulDevour relic)
    {
        relic.PendingNodeDiscoveries.Add(PhaseReactionDelayFloors);
    }

    public static void AdvanceNodeProgress(ProphecySoulDevour relic, int climbedFloors)
    {
        if (climbedFloors <= 0)
            return;

        for (var i = relic.PendingNodeDiscoveries.Count - 1; i >= 0; i--)
        {
            var remaining = relic.PendingNodeDiscoveries[i] - climbedFloors;
            if (remaining <= 0)
            {
                relic.PendingNodeDiscoveries.RemoveAt(i);
                relic.AstralParty_ProphecySoulDevourPendingDiscoveryCounter++;
                relic.NotifyDisplayAmountChanged();
                continue;
            }

            relic.PendingNodeDiscoveries[i] = remaining;
        }

        for (var i = relic.PendingDelayedCards.Count - 1; i >= 0; i--)
        {
            var entry = relic.PendingDelayedCards[i];
            var remaining = entry.RemainingNodes - climbedFloors;
            if (remaining <= 0)
            {
                relic.ReadyDelayedCards.Add(entry with { RemainingNodes = 0 });
                relic.PendingDelayedCards.RemoveAt(i);
                continue;
            }

            relic.PendingDelayedCards[i] = entry with { RemainingNodes = remaining };
        }

        for (var i = relic.PendingDelayedRelics.Count - 1; i >= 0; i--)
        {
            var entry = relic.PendingDelayedRelics[i];
            var remaining = entry.RemainingNodes - climbedFloors;
            if (remaining <= 0)
            {
                relic.ReadyDelayedRelics.Add(entry with { RemainingNodes = 0 });
                relic.PendingDelayedRelics.RemoveAt(i);
                continue;
            }

            relic.PendingDelayedRelics[i] = entry with { RemainingNodes = remaining };
        }
    }

    public static IReadOnlyList<ProphecySoulDevourKind> BuildDiscoveryOptions(
        ProphecySoulDevour relic,
        int rerollOrdinal,
        IReadOnlySet<ProphecySoulDevourKind> previouslySeen)
    {
        var legal = GetLegalDefinitions(relic)
            .Where(definition => !previouslySeen.Contains(definition.Kind))
            .ToList();
        if (legal.Count == 0)
            return [];

        var ordered = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            legal,
            definition => definition.Kind.ToString(),
            MainFile.ModId,
            relic.Id.Entry,
            "discovery",
            relic.Owner?.RunState?.Rng.StringSeed,
            relic.Owner?.RunState?.CurrentActIndex,
            relic.Owner?.RunState?.TotalFloor,
            relic.Owner?.NetId,
            relic.AstralParty_ProphecySoulDevourDiscoverySequenceCounter,
            rerollOrdinal);
        return ordered.Take(Math.Min(DiscoveryChoiceCount, ordered.Count)).Select(static definition => definition.Kind).ToList();
    }

    public static IReadOnlyList<ProphecySoulDevourDefinition> GetLegalDefinitions(ProphecySoulDevour relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return [];

        var definitions = new List<ProphecySoulDevourDefinition>();
        foreach (var definition in ProphecySoulDevourRegistry.All)
        {
            if (definition.Persistence == ProphecySoulDevourPersistence.Permanent
                && !definition.AllowRepeatPermanent
                && HasPermanentProphecy(relic, definition.Kind))
                continue;

            if (!IsCurrentlyUsable(relic, definition.Kind))
                continue;

            definitions.Add(definition);
        }

        return definitions;
    }

    public static bool IsCurrentlyUsable(ProphecySoulDevour relic, ProphecySoulDevourKind kind)
    {
        var owner = relic.Owner;
        if (owner?.Creature == null)
            return false;

        return kind switch
        {
            ProphecySoulDevourKind.UndergroundTrade => owner.Creature.CurrentHp >= 10m,
            ProphecySoulDevourKind.MimicLarva => EventDeckCardHelper.GetRunDeckCards(owner).Count > 0,
            ProphecySoulDevourKind.Ascension => relic.AstralParty_ProphecySoulDevourPendingSmithBonusCounter < 5,
            ProphecySoulDevourKind.GoldMiner => EventDeckCardHelper.GetUpgradeableUnupgradedCards(owner).Count > 0,
            ProphecySoulDevourKind.EventStop => !relic.AstralParty_ProphecySoulDevourEventStopPending,
            ProphecySoulDevourKind.DivineMiracle => !relic.AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending,
            ProphecySoulDevourKind.HiddenStrikeCard => CreateSafeBaseGameAncientCards().Count > 0,
            ProphecySoulDevourKind.HiddenStrikeRelic => CreateSafeBaseGameAncientRelics(owner).Count > 0,
            _ => true
        };
    }

    public static async Task<RefreshableProphecySelectionResult?> OpenDiscoveryAsync(ProphecySoulDevour relic)
    {
        if (relic.Owner?.RunState is not RunState runState)
            return null;

        var allowRefresh = relic.AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending;
        var initialOptions = BuildDiscoveryOptions(relic, 0, new HashSet<ProphecySoulDevourKind>());
        if (initialOptions.Count == 0)
            return null;

        var runManager = RunManager.Instance;
        var gameType = runManager.NetService.Type;
        if (gameType is NetGameType.Singleplayer or NetGameType.None)
        {
            var localResult = await ShowLocalDiscoveryAsync(relic, initialOptions, allowRefresh);
            return localResult;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
            return await ShowLocalDiscoveryAsync(relic, initialOptions, allowRefresh);

        var choiceId = synchronizer.ReserveChoiceId(relic.Owner);
        var sessionKey = $"{AstralChoiceKind.RefreshableProphecySelection}|{relic.Id.Entry}|{relic.Owner.NetId}|{relic.AstralParty_ProphecySoulDevourDiscoverySequenceCounter}";
        if (relic.Owner.NetId == runManager.NetService.NetId)
        {
            var localResult = await ShowLocalDiscoveryAsync(relic, initialOptions, allowRefresh);
            if (localResult == null)
                return null;

            synchronizer.SyncLocalChoice(
                relic.Owner,
                choiceId,
                AstralChoiceProtocol.CreateIndexedEnvelope(
                    AstralChoiceKind.RefreshableProphecySelection,
                    runState,
                    sessionKey,
                    0,
                    EncodeDiscoveryPayload(localResult)));
            return localResult;
        }

        var remoteChoice = await DeterministicMultiplayerChoiceHelper.WaitForRemoteIndexedEnvelope(
            synchronizer,
            relic.Owner,
            choiceId,
            AstralChoiceKind.RefreshableProphecySelection,
            runState,
            sessionKey,
            "prophecy_soul_devour.discovery");
        if (remoteChoice == null || !TryDecodeDiscoveryPayload(remoteChoice.Value.Payload, out var selectedIndex, out var rerollHistory))
            return null;

        var finalOptions = RebuildDiscoveryOptionsFromHistory(relic, rerollHistory);
        if (selectedIndex < 0 || selectedIndex >= finalOptions.Count)
            return null;

        return new RefreshableProphecySelectionResult
        {
            SelectedProphecy = finalOptions[selectedIndex],
            SelectedIndex = selectedIndex,
            RefreshCost = RefreshGoldCost,
            RefreshCount = rerollHistory.Count,
            RerollHistory = rerollHistory,
            FinalOptions = finalOptions
        };
    }

    public static IReadOnlyList<ProphecySoulDevourKind> RebuildDiscoveryOptionsFromHistory(
        ProphecySoulDevour relic,
        IReadOnlyList<int> rerollHistory)
    {
        IReadOnlyList<ProphecySoulDevourKind> options = BuildDiscoveryOptions(relic, 0, new HashSet<ProphecySoulDevourKind>());
        var seen = options.ToHashSet();
        for (var rerollOrdinal = 0; rerollOrdinal < rerollHistory.Count; rerollOrdinal++)
        {
            options = BuildDiscoveryOptions(relic, rerollOrdinal + 1, seen);
            foreach (var option in options)
                seen.Add(option);
        }

        return options;
    }

    public static async Task ResolveProphecyAsync(
        PlayerChoiceContext? choiceContext,
        ProphecySoulDevour relic,
        ProphecySoulDevourKind kind)
    {
        var owner = relic.Owner;
        if (owner?.Creature == null)
            return;

        switch (kind)
        {
            case ProphecySoulDevourKind.UndergroundTrade:
                await CreatureCmd.SetCurrentHp(owner.Creature, owner.Creature.CurrentHp - 6m);
                await PersonaMultiplayerEffectHelper.GainGoldDeterministic(60m, owner);
                break;
            case ProphecySoulDevourKind.TreasureHunting:
            case ProphecySoulDevourKind.MatrixRecycling:
            case ProphecySoulDevourKind.AncientRuins:
            case ProphecySoulDevourKind.MineralRecovery:
            case ProphecySoulDevourKind.EnergySavingStrategy:
                GrantPermanentProphecy(relic, kind);
                break;
            case ProphecySoulDevourKind.MimicLarva:
                await ResolveMimicLarvaAsync(choiceContext, relic);
                break;
            case ProphecySoulDevourKind.Ascension:
                relic.AstralParty_ProphecySoulDevourPendingSmithBonusCounter = Math.Min(5, relic.AstralParty_ProphecySoulDevourPendingSmithBonusCounter + 1);
                break;
            case ProphecySoulDevourKind.GoldMiner:
                await ResolveGoldMinerAsync(choiceContext, relic);
                break;
            case ProphecySoulDevourKind.HumanWaveTactics:
                await ResolveCardDiscoveryAsync(choiceContext, relic, CardRarity.Common, $"{kind}.common_reward");
                break;
            case ProphecySoulDevourKind.EventStop:
                relic.AstralParty_ProphecySoulDevourEventStopPending = true;
                break;
            case ProphecySoulDevourKind.EnergyConversion:
                relic.AstralParty_ProphecySoulDevourDisableNextSmithPending = true;
                await PersonaMultiplayerEffectHelper.GainGoldDeterministic(75m, owner);
                break;
            case ProphecySoulDevourKind.FastFluctuation:
                ApplyFastFluctuation(relic);
                break;
            case ProphecySoulDevourKind.PhaseReaction:
                QueuePhaseReaction(relic);
                break;
            case ProphecySoulDevourKind.DivineMiracle:
                relic.AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending = true;
                break;
            case ProphecySoulDevourKind.HiddenStrikeCard:
                await ResolveHiddenStrikeCardAsync(choiceContext, relic);
                break;
            case ProphecySoulDevourKind.HiddenStrikeRelic:
                await ResolveHiddenStrikeRelicAsync(choiceContext, relic);
                break;
        }
    }

    public static IReadOnlyList<CardModel> CreateSafeBaseGameAncientCards()
    {
        return ModelDb.AllCards
            .Where(card => card.GetType().Assembly == typeof(CardModel).Assembly)
            .Where(card => card.Rarity == CardRarity.Ancient)
            .Where(card => card.Type != CardType.Status)
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First().CanonicalInstance ?? group.First())
            .OrderBy(card => (card.CanonicalInstance?.Id ?? card.Id).Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<RelicModel> CreateSafeBaseGameAncientRelics(Player owner)
    {
        var unlockState = owner.RunState?.UnlockState;
        if (unlockState == null)
            return [];

        var result = new List<RelicModel>();
        foreach (var pool in ModelDb.AllRelicPools)
        {
            foreach (var relic in pool.GetUnlockedRelics(unlockState))
            {
                if (relic.GetType().Assembly != typeof(RelicModel).Assembly)
                    continue;
                if (relic.Rarity != RelicRarity.Ancient)
                    continue;

                var canonical = relic.CanonicalInstance ?? relic;
                if (result.Any(existing => (existing.CanonicalInstance?.Id ?? existing.Id) == canonical.Id))
                    continue;

                result.Add(canonical);
            }
        }

        return result.OrderBy(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<CardModel> CreateDeterministicRewardStyleCards(
        Player owner,
        CardRarity rarity,
        string context)
    {
        var options = CardCreationOptions.ForRoom(owner, RoomType.Monster);
        return options.GetPossibleCards(owner)
            .Where(card => card.Rarity == rarity)
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First().CanonicalInstance ?? group.First())
            .OrderBy(card => DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                int.MaxValue,
                MainFile.ModId,
                nameof(ProphecySoulDevourHelper),
                context,
                owner.RunState?.Rng.StringSeed,
                owner.RunState?.CurrentActIndex,
                owner.RunState?.TotalFloor,
                owner.NetId,
                (card.CanonicalInstance?.Id ?? card.Id).Entry))
            .ThenBy(card => (card.CanonicalInstance?.Id ?? card.Id).Entry, StringComparer.Ordinal)
            .Take(3)
            .ToList();
    }

    private static async Task ResolveMimicLarvaAsync(PlayerChoiceContext? choiceContext, ProphecySoulDevour relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return;

        var cards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (cards.Count == 0)
            return;

        var context = choiceContext ?? NoChoiceContext;
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayerGrid(
            context,
            owner,
            cards,
            ProphecyDeckCardGridPrefs,
            $"{relic.Id.Entry}.mimic_larva");
        if (selected == null)
            return;

        relic.MimicSnapshots.Add(new ProphecySoulDevourMimicSnapshot((selected.CanonicalInstance ?? selected).Id, selected.CurrentUpgradeLevel));
    }

    private static async Task ResolveGoldMinerAsync(PlayerChoiceContext? choiceContext, ProphecySoulDevour relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return;

        var selectableCards = EventDeckCardHelper.GetUpgradeableUnupgradedCards(owner);
        if (selectableCards.Count == 0)
            return;

        var context = choiceContext ?? NoChoiceContext;
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayerGrid(
            context,
            owner,
            selectableCards,
            ProphecyDeckCardGridPrefs,
            $"{relic.Id.Entry}.gold_miner");
        if (selected == null)
            return;

        var liveCard = EventDeckCardHelper.GetRunDeckCards(owner)
            .FirstOrDefault(card => (card.CanonicalInstance?.Id ?? card.Id) == (selected.CanonicalInstance?.Id ?? selected.Id));
        if (liveCard == null)
            return;

        await EventDeckCardMutationHelper.UpgradeSingleWithSmithPreview(owner, liveCard, $"{relic.Id.Entry}.gold_miner");
    }

    internal static async Task ResolveCardDiscoveryAsync(
        PlayerChoiceContext? choiceContext,
        ProphecySoulDevour relic,
        CardRarity rarity,
        string context)
    {
        var owner = relic.Owner;
        if (owner == null)
            return;

        var options = CreateDeterministicRewardStyleCards(owner, rarity, context);
        if (options.Count == 0)
            return;

        var choiceCtx = choiceContext ?? NoChoiceContext;
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceCtx,
            owner,
            options,
            false,
            $"{relic.Id.Entry}.{context}");
        if (selected == null)
            return;

        var mutableCard = (selected.CanonicalInstance ?? selected).ToMutable();
        mutableCard.FloorAddedToDeck = Math.Max(owner.RunState?.TotalFloor ?? 1, 1);
        await EventDeckCardHelper.AddCardToRunDeckAsync(owner, mutableCard);
    }

    private static async Task ResolveHiddenStrikeCardAsync(PlayerChoiceContext? choiceContext, ProphecySoulDevour relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return;

        var options = CreateSafeBaseGameAncientCards();
        if (options.Count == 0)
            return;

        var choiceCtx = choiceContext ?? NoChoiceContext;
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayerGrid(
            choiceCtx,
            owner,
            options,
            ProphecyDeckCardGridPrefs,
            $"{relic.Id.Entry}.hidden_strike_card");
        if (selected == null)
            return;

        relic.PendingDelayedCards.Add(new ProphecySoulDevourDelayedCardGrant(
            (selected.CanonicalInstance ?? selected).Id,
            selected.CurrentUpgradeLevel,
            HiddenStrikeDelayFloors));
    }

    private static async Task ResolveHiddenStrikeRelicAsync(PlayerChoiceContext? choiceContext, ProphecySoulDevour relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return;

        var options = CreateSafeBaseGameAncientRelics(owner);
        if (options.Count == 0)
            return;

        using var _ = RelicSelectionHeaderContext.Push(
            ProphecySoulDevourRegistry.Get(ProphecySoulDevourKind.HiddenStrikeRelic).TitleLocString.GetRawText());
        var selected = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            owner,
            options,
            $"{relic.Id.Entry}.hidden_strike_relic.{relic.AstralParty_ProphecySoulDevourHiddenRelicSelectionSequenceCounter++}");
        if (selected == null)
            return;

        relic.PendingDelayedRelics.Add(new ProphecySoulDevourDelayedRelicGrant(
            (selected.CanonicalInstance ?? selected).Id,
            HiddenStrikeDelayFloors));
    }

    private static async Task<RefreshableProphecySelectionResult?> ShowLocalDiscoveryAsync(
        ProphecySoulDevour relic,
        IReadOnlyList<ProphecySoulDevourKind> initialOptions,
        bool allowRefresh)
    {
        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
            return null;

        var screen = RefreshableProphecySelectionScreen.Create(
            relic.Owner!,
            initialOptions,
            ProphecySoulDevourRegistry.SelectionHeader.GetRawText(),
            ProphecySoulDevourRegistry.SelectionSubtitle.GetRawText(),
            RefreshGoldCost,
            allowRefresh,
            (_, rerollOrdinal, seen) => BuildDiscoveryOptions(relic, rerollOrdinal + 1, seen));
        overlayStack.Push(screen);
        var result = await screen.WaitForResult();
        screen.Close();
        await screen.WaitUntilClosedAsync();
        return result;
    }

    private static List<int> EncodeDiscoveryPayload(RefreshableProphecySelectionResult result)
    {
        var payload = new List<int>(4 + result.RerollHistory.Count)
        {
            result.SelectedIndex,
            result.RerollHistory.Count
        };
        payload.AddRange(result.RerollHistory);
        return payload;
    }

    private static bool TryDecodeDiscoveryPayload(
        IReadOnlyList<int> payload,
        out int selectedIndex,
        out IReadOnlyList<int> rerollHistory)
    {
        selectedIndex = -1;
        rerollHistory = [];
        if (payload.Count < 2)
            return false;

        selectedIndex = payload[0];
        var rerollCount = Math.Max(0, payload[1]);
        if (payload.Count < rerollCount + 2)
            return false;

        rerollHistory = payload.Skip(2).Take(rerollCount).ToArray();
        return true;
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        for (var i = 0; i < 60; i++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var i = 0; i < 60; i++)
        {
            if (NOverlayStack.Instance != null)
                return NOverlayStack.Instance;

            await Task.Yield();
        }

        return NOverlayStack.Instance;
    }

    private sealed class NoChoicePlayerChoiceContext : PlayerChoiceContext
    {
        public override Task SignalPlayerChoiceBegun(PlayerChoiceOptions options)
        {
            return Task.CompletedTask;
        }

        public override Task SignalPlayerChoiceEnded()
        {
            return Task.CompletedTask;
        }
    }
}
