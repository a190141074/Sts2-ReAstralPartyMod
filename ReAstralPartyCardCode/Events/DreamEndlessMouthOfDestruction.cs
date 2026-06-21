using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Globalization;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterActEvent(typeof(Overgrowth))]
public sealed class DreamEndlessMouthOfDestruction : AstralPartyEventModel
{
    private const string LogTag = "DreamEndlessMouthOfDestruction";
    private const string FailureChanceVarName = "FailureChance";
    private const decimal MinimumGoldRequiredPerPlayer = 300m;
    private const decimal PaidStrengthenCost = 30m;
    private const int FailureChanceStepPercent = 10;
    private const int MaximumFailureChancePercent = 70;
    private const int MaxSynchronizerWaitFrames = 60;
    private const string InitialInfoTextKey =
        "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_ENDLESS_MOUTH_OF_DESTRUCTION.pages.INITIAL.options.INFO";
    private const string LoopInfoTextKey =
        "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_ENDLESS_MOUTH_OF_DESTRUCTION.pages.LOOP.options.INFO";

    private static readonly LocString StrengthenSelectionPrompt =
        new("events", "RE_ASTRAL_PARTY_MOD_EVENT_DREAM_ENDLESS_MOUTH_OF_DESTRUCTION.select_upgrade");

    [SavedProperty] public int AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts { get; set; }

    protected override string EventId => "dream_endless_mouth_of_destruction";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(0),
        new IntVar(FailureChanceVarName, 0)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return ReAstralPartyModSettingsManager.GetEnableDreamSeriesEvents(runState)
               && runState.Players.All(player => player.Gold >= MinimumGoldRequiredPerPlayer);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        MainFile.Logger.Info(
            $"[{LogTag}] {LogTag} event opened | owner={Owner?.NetId.ToString() ?? "null"} | attempts={AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts}");
        return CreateCurrentOptions(isInitialPage: true);
    }

    internal static bool TryBuildInfoOptionText(
        DreamEndlessMouthOfDestruction? eventModel,
        string? textKey,
        out string text)
    {
        text = string.Empty;
        if (eventModel == null || string.IsNullOrWhiteSpace(textKey))
            return false;

        if (!IsInfoTextKey(textKey))
            return false;

        var title = new LocString("events", $"{textKey}.title").GetFormattedText();
        var attemptNumber = eventModel.AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts + 1;
        var description = BuildInfoOptionDescription(attemptNumber);
        text = $"{title}\n{description}";
        return true;
    }

    internal static bool IsInfoTextKey(string? textKey)
    {
        return string.Equals(textKey, InitialInfoTextKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(textKey, LoopInfoTextKey, StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<EventOption> CreateCurrentOptions(bool isInitialPage)
    {
        RefreshInfoDynamicVars();
        var canStrengthen = CanStrengthen(out var strengthenOptionKey);
        var pageName = isInitialPage ? "INITIAL" : "LOOP";
        var infoKey = isInitialPage
            ? InitialOptionKey("INFO")
            : ModOptionKey(pageName, "INFO");
        var strengthenKey = isInitialPage
            ? InitialOptionKey(strengthenOptionKey)
            : ModOptionKey(pageName, strengthenOptionKey);
        var leaveKey = isInitialPage
            ? InitialOptionKey("LEAVE")
            : ModOptionKey(pageName, "LEAVE");

        return
        [
            CreateInfoOption(infoKey),
            new EventOption(this, canStrengthen ? Strengthen : null, strengthenKey),
            new EventOption(this, Leave, leaveKey)
        ];
    }

    private EventOption CreateInfoOption(string textKey)
    {
        return new EventOption(this, NoOpInfoOption, textKey)
            .ThatWontSaveToChoiceHistory();
    }

    private void RefreshInfoDynamicVars()
    {
        var attemptNumber = AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts + 1;

        if (DynamicVars.ContainsKey("Gold"))
            DynamicVars["Gold"].BaseValue = GetStrengthenCost(attemptNumber);

        if (DynamicVars.ContainsKey(FailureChanceVarName))
            DynamicVars[FailureChanceVarName].BaseValue = GetFailureChancePercent(attemptNumber);
    }

    private static string BuildInfoOptionDescription(int attemptNumber)
    {
        var cost = GetStrengthenCost(attemptNumber).ToString(CultureInfo.InvariantCulture);
        var failureChance = GetFailureChancePercent(attemptNumber).ToString(CultureInfo.InvariantCulture);
        var locale = TranslationServer.GetLocale();

        if (locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return $"消耗金币：[gold]{cost}[/gold]丨失败几率：[red]{failureChance}%[/red]丨失败代价：[red]随机移除[/red]";

        if (locale.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return $"消費ゴールド：[gold]{cost}[/gold]丨失敗確率：[red]{failureChance}%[/red]丨失敗代償：[red]ランダム除去[/red]";

        return $"Gold Cost: [gold]{cost}[/gold] | Failure Chance: [red]{failureChance}%[/red] | Failure Cost: [red]Random removal[/red]";
    }

    private bool CanStrengthen(out string optionKey)
    {
        ArgumentNullException.ThrowIfNull(Owner);

        if (EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner).Count == 0)
        {
            optionKey = "STRENGTHEN_LOCKED_NO_TARGET";
            return false;
        }

        var attemptNumber = AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts + 1;
        if (attemptNumber > 1 && Owner.Gold < GetStrengthenCost(attemptNumber))
        {
            optionKey = "STRENGTHEN_LOCKED_NO_GOLD";
            return false;
        }

        optionKey = "STRENGTHEN";
        return true;
    }

    private async Task Strengthen()
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var attemptNumber = AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts + 1;
        if (attemptNumber == 1)
        {
            await ResolveFirstStrengthen();
            return;
        }

        await ResolvePaidStrengthen(attemptNumber);
    }

    private async Task ResolveFirstStrengthen()
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var candidates = EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner);
        var selectedCard = PickDeterministicDeckCard(
            Owner,
            candidates,
            "first_strengthen",
            AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts + 1);
        if (selectedCard == null)
        {
            SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
            return;
        }

        await EventDeckCardMutationHelper.UpgradeSingleWithSmithPreview(
            Owner,
            selectedCard,
            "dream_endless_mouth_of_destruction.first_strengthen");
        MainFile.Logger.Info(
            $"[{LogTag}] {LogTag} strengthen success | owner={Owner.NetId} | attempt=1 | cost=0 | failureChance=0 | roll=0 | upgradedCard={selectedCard.Id.Entry}");
        AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts++;
        SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
    }

    private async Task ResolvePaidStrengthen(int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(Owner);

        var candidates = EventDeckCardHelper.GetUpgradeableUnupgradedCards(Owner);
        if (candidates.Count == 0)
        {
            SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
            return;
        }

        var selectedCards = await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(StrengthenSelectionPrompt, 1)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            },
            static card => card.CurrentUpgradeLevel == 0 && card.IsUpgradable);
        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null)
        {
            SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
            return;
        }

        var cost = GetStrengthenCost(attemptNumber);
        if (Owner.Gold < cost)
        {
            SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
            return;
        }

        await PersonMultiplayerEffectHelper.LoseGoldDeterministic(cost, Owner, GoldLossType.Spent);

        var failureChance = GetFailureChancePercent(attemptNumber);
        var roll = await ResolveSyncedGameRoll(Owner, attemptNumber, "failure_roll", 1, 11);
        var strengthenFailed = roll <= failureChance / 10;
        MainFile.Logger.Info(
            $"[{LogTag}] {LogTag} strengthen attempt | owner={Owner.NetId} | attempt={attemptNumber} | cost={cost} | failureChance={failureChance} | roll={roll}");

        if (strengthenFailed)
        {
            var removedCard = await PickRandomDeckCardAsync(
                Owner,
                EventDeckCardHelper.GetRunDeckCards(Owner),
                "failed_remove_roll",
                attemptNumber);
            if (removedCard != null)
            {
                await EventDeckCardRemovalPreviewHelper.PlayShatterPreviewAsync(Owner, removedCard);
                await EventDeckCardMutationHelper.Remove(
                    Owner,
                    [removedCard],
                    $"dream_endless_mouth_of_destruction.remove.{attemptNumber}");
            }

            MainFile.Logger.Info(
                $"[{LogTag}] {LogTag} strengthen failed | owner={Owner.NetId} | attempt={attemptNumber} | cost={cost} | failureChance={failureChance} | roll={roll} | removedCard={removedCard?.Id.Entry ?? "<null>"}");
        }
        else
        {
            await EventDeckCardMutationHelper.UpgradeSingleWithSmithPreview(
                Owner,
                selectedCard,
                $"dream_endless_mouth_of_destruction.upgrade.{attemptNumber}");
            MainFile.Logger.Info(
                $"[{LogTag}] {LogTag} strengthen success | owner={Owner.NetId} | attempt={attemptNumber} | cost={cost} | failureChance={failureChance} | roll={roll} | upgradedCard={selectedCard.Id.Entry}");
        }

        AstralParty_DreamEndlessMouthOfDestructionStrengthenAttempts++;
        SetEventState(PageDescription("LOOP"), CreateCurrentOptions(isInitialPage: false));
    }

    private Task NoOpInfoOption()
    {
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("FINISH"));
        return Task.CompletedTask;
    }

    private static CardModel? PickDeterministicDeckCard(
        Player owner,
        IReadOnlyList<CardModel> candidates,
        string purpose,
        int attemptNumber)
    {
        if (candidates.Count == 0)
            return null;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var orderedCandidates = candidates
            .Select(card => new
            {
                Card = card,
                Index = deckCards.IndexOf(card)
            })
            .Where(entry => entry.Index >= 0)
            .OrderBy(entry => entry.Index)
            .ThenBy(entry => entry.Card.Id.Entry, StringComparer.Ordinal)
            .Select(entry => entry.Card)
            .ToList();
        if (orderedCandidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            orderedCandidates.Count,
            "dream_endless_mouth_of_destruction",
            purpose,
            owner.RunState is RunState runState ? AstralChoiceProtocol.CreateRunScopeKey(runState) : "no_run",
            owner.NetId,
            attemptNumber);
        return orderedCandidates[selectedIndex];
    }

    private static async Task<CardModel?> PickRandomDeckCardAsync(
        Player owner,
        IReadOnlyList<CardModel> candidates,
        string purpose,
        int attemptNumber)
    {
        if (candidates.Count == 0)
            return null;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var orderedCandidates = candidates
            .Select(card => new
            {
                Card = card,
                Index = deckCards.IndexOf(card)
            })
            .Where(entry => entry.Index >= 0)
            .OrderBy(entry => entry.Index)
            .ThenBy(entry => entry.Card.Id.Entry, StringComparer.Ordinal)
            .Select(entry => entry.Card)
            .ToList();
        if (orderedCandidates.Count == 0)
            return null;

        var selectedIndex = await ResolveSyncedGameRoll(owner, attemptNumber, purpose, 0, orderedCandidates.Count);
        return selectedIndex >= 0 && selectedIndex < orderedCandidates.Count
            ? orderedCandidates[selectedIndex]
            : orderedCandidates[0];
    }

    private static async Task<int> ResolveSyncedGameRoll(
        Player owner,
        int attemptNumber,
        string purpose,
        int minInclusive,
        int maxExclusive)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var fallbackRoll = CreateDeterministicFallbackRoll(owner, attemptNumber, purpose, minInclusive, maxExclusive);
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (runManager == null
            || netService == null
            || netService.Type is NetGameType.None or NetGameType.Singleplayer
            || runManager.IsSingleplayerOrFakeMultiplayer)
            return ConsumeGameRollOrFallback(owner, attemptNumber, purpose, minInclusive, maxExclusive, fallbackRoll);

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            MainFile.Logger.Warn(
                $"[{LogTag}] {LogTag} random resolution fell back to deterministic roll because PlayerChoiceSynchronizer was unavailable | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | fallbackRoll={fallbackRoll}");
            return fallbackRoll;
        }

        var sessionKey =
            $"dream_endless_mouth_of_destruction.random|{purpose}|{owner.NetId}|{attemptNumber}|{minInclusive}|{maxExclusive}";
        var choiceId = synchronizer.ReserveChoiceId(owner);
        if (IsLocalPlayer(runManager, owner))
        {
            var localRoll = ConsumeGameRollOrFallback(owner, attemptNumber, purpose, minInclusive, maxExclusive, fallbackRoll);
            MainFile.Logger.Info(
                $"[{LogTag}] {LogTag} random resolution local | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | minInclusive={minInclusive} | maxExclusive={maxExclusive} | roll={localRoll}");
            synchronizer.SyncLocalChoice(
                owner,
                choiceId,
                AstralChoiceProtocol.CreateIndexedEnvelope(
                    AstralChoiceKind.EventRandomResolution,
                    owner.RunState as RunState,
                    sessionKey,
                    0,
                    [localRoll]));
            return localRoll;
        }

        var remoteChoice = await DeterministicMultiplayerChoiceHelper.WaitForRemoteIndexedEnvelope(
            synchronizer,
            owner,
            choiceId,
            AstralChoiceKind.EventRandomResolution,
            owner.RunState as RunState,
            sessionKey,
            $"{LogTag}.{purpose}");
        var remoteRoll = remoteChoice?.Payload.Count > 0 ? remoteChoice.Value.Payload[0] : -1;
        if (remoteRoll >= minInclusive && remoteRoll < maxExclusive)
        {
            var shadowRoll = ConsumeGameRollOrFallback(
                owner,
                attemptNumber,
                $"{purpose}_remote_shadow",
                minInclusive,
                maxExclusive,
                fallbackRoll);
            if (shadowRoll != remoteRoll)
            {
                MainFile.Logger.Error(
                    $"[{LogTag}] {LogTag} random resolution remote shadow mismatch | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | minInclusive={minInclusive} | maxExclusive={maxExclusive} | localShadowRoll={shadowRoll} | remotePayloadRoll={remoteRoll}");
            }

            MainFile.Logger.Info(
                $"[{LogTag}] {LogTag} random resolution remote | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | minInclusive={minInclusive} | maxExclusive={maxExclusive} | roll={remoteRoll} | shadowRoll={shadowRoll}");
            return remoteRoll;
        }

        MainFile.Logger.Warn(
            $"[{LogTag}] {LogTag} random resolution fell back to deterministic roll because remote payload was unavailable | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | fallbackRoll={fallbackRoll}");
        return fallbackRoll;
    }

    private static int ConsumeGameRollOrFallback(
        Player owner,
        int attemptNumber,
        string purpose,
        int minInclusive,
        int maxExclusive,
        int fallbackRoll)
    {
        var rng = owner.RunState?.Rng?.Niche;
        if (rng == null)
        {
            MainFile.Logger.Warn(
                $"[{LogTag}] {LogTag} random resolution fell back to deterministic roll because Niche RNG was unavailable | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | fallbackRoll={fallbackRoll}");
            return fallbackRoll;
        }

        var roll = minInclusive + rng.NextInt(maxExclusive - minInclusive);
        MainFile.Logger.Info(
            $"[{LogTag}] {LogTag} random resolution consumed niche rng | owner={owner.NetId} | attempt={attemptNumber} | purpose={purpose} | minInclusive={minInclusive} | maxExclusive={maxExclusive} | counter={rng.Counter} | roll={roll}");
        return roll;
    }

    private static int CreateDeterministicFallbackRoll(
        Player owner,
        int attemptNumber,
        string purpose,
        int minInclusive,
        int maxExclusive)
    {
        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            minInclusive,
            maxExclusive,
            "dream_endless_mouth_of_destruction",
            purpose,
            owner.RunState is RunState runState ? AstralChoiceProtocol.CreateRunScopeKey(runState) : "no_run",
            owner.NetId,
            attemptNumber,
            maxExclusive);
    }

    private static bool IsLocalPlayer(RunManager runManager, Player player)
    {
        return player.NetId != 0UL && player.NetId == runManager.NetService.NetId;
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        for (var i = 0; i < MaxSynchronizerWaitFrames; i++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
    }

    private static decimal GetStrengthenCost(int attemptNumber)
    {
        if (attemptNumber <= 1)
            return 0m;

        return PaidStrengthenCost;
    }

    private static int GetFailureChancePercent(int attemptNumber)
    {
        return Math.Min(MaximumFailureChancePercent, Math.Max(0, (attemptNumber - 1) * FailureChanceStepPercent));
    }
}
