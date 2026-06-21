using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Ancient;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class StartingPersonNeowReadyFlow
{
    private const string ReadyPageDescriptionKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY.description";
    private const string ReadyOptionTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY.options.READY";
    private const string WaitingOptionTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY_WAITING.options.WAITING";

    private static readonly object SyncLock = new();
    private static readonly Dictionary<Neow, ReadyPageState> ReadyPages = [];
    private static readonly Dictionary<string, Task<bool>> SelectionTasksByRun = [];
    private static readonly HashSet<string> LaunchingRunKeys = [];
    private static readonly MethodInfo? SetEventStateMethod = AccessTools.Method(
        typeof(EventModel),
        "SetEventState",
        [typeof(LocString), typeof(IEnumerable<EventOption>)]);
    private static readonly MethodInfo? SetInitialEventStateMethod = AccessTools.Method(
        typeof(AncientEventModel),
        "SetInitialEventState",
        [typeof(bool)]);
    private static readonly MethodInfo? EventRoomSetupLayoutMethod =
        AccessTools.Method(typeof(NEventRoom), "SetupLayout");
    private static readonly MethodInfo? EventRoomRefreshEventStateMethod =
        AccessTools.Method(typeof(NEventRoom), "RefreshEventState", [typeof(EventModel)]);
    private static readonly MethodInfo? AncientLayoutInitializeVisualsMethod =
        AccessTools.Method(typeof(NAncientEventLayout), "InitializeVisuals");
    private static readonly MethodInfo? AncientLayoutUpdateBannerVisibilityMethod =
        AccessTools.Method(typeof(NAncientEventLayout), "UpdateBannerVisibility");
    private static readonly MethodInfo? AncientLayoutUpdateFakeNextButtonMethod =
        AccessTools.Method(typeof(NAncientEventLayout), "UpdateFakeNextButton");
    private static readonly MethodInfo? ControlUpdateMinimumSizeMethod =
        AccessTools.Method(typeof(Control), "UpdateMinimumSize");
    private static readonly AsyncLocal<int> InternalNeowRefreshDepth = new();

    private sealed record ReadyPageState(
        string RunKey,
        RunState RunState,
        IReadOnlyList<EventOption> OriginalOptions,
        LocString OriginalDescription,
        bool Restored = false);

    internal static bool IsInternalNeowRefreshActive => InternalNeowRefreshDepth.Value > 0;

    internal static bool TryRestoreCachedNeowOptionsBeforeRemoteChoice(uint optionIndex, ulong senderId)
    {
        if (RunManager.Instance?.NetService?.Type != NetGameType.Client)
            return false;
        if (RunManager.Instance?.EventSynchronizer?.GetLocalEvent() is not Neow neow)
            return false;

        ReadyPageState? state;
        lock (SyncLock)
        {
            if (!ReadyPages.TryGetValue(neow, out state))
                return false;
        }

        var localOptionCount = neow.CurrentOptions?.Count ?? 0;
        if (optionIndex < localOptionCount)
            return false;

        // EventSynchronizer validates the remote option index immediately against CurrentOptions.
        // If the client is still on the 1-button ready page, restore the real Neow page first.
        RestoreOriginalOptionsOrFinish(neow, "remote_choice_pre_restore");
        MainFile.Logger.Warn(
            $"[StartingPersonNeowReadyFlow] Restored cached Neow option page before remote choice processing | runKey={state.RunKey} sender={senderId} optionIndex={optionIndex} localCount={localOptionCount} cachedCount={state.OriginalOptions.Count}.");
        return true;
    }

    internal static void TryReplaceInitialState(Neow neow)
    {
        if (!ShouldInjectReadyPage(neow, out var runState, out var reason))
        {
            MainFile.Logger.Info($"[StartingPersonNeowReadyFlow] Skipped Neow ready page: {reason}.");
            return;
        }

        lock (SyncLock)
        {
            if (ReadyPages.ContainsKey(neow))
                return;

            var runKey = StartingPersonRelicSelectionPatch.GetRunKey(runState);
            var originalOptions = NeowOptionInjectionHelper.EnsureSelectedCustomOptionPresent(
                neow,
                neow.CurrentOptions,
                "ready_page_cache");
            ReadyPages[neow] = new ReadyPageState(
                runKey,
                runState,
                originalOptions,
                neow.Description ?? neow.InitialDescription);
        }

        var readyOptions = new[]
        {
            CreateReadyOption(neow, runState)
        };

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page injection.");

        SetEventStateMethod.Invoke(neow, [CreateReadyPageDescription(), readyOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Ready page injected | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)}.");
    }

    internal static LocString CreateReadyPageDescription()
    {
        return new LocString("ancients", ReadyPageDescriptionKey);
    }

    internal static bool IsReadyOptionTextKey(string? textKey)
    {
        return string.Equals(textKey, ReadyOptionTextKey, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldInjectReadyPage(Neow neow, out RunState runState, out string reason)
    {
        if (!TryResolveRunState(neow, out var resolvedRunState))
        {
            reason = "Neow owner or run state was unavailable";
            runState = null!;
            return false;
        }

        runState = resolvedRunState;
        return StartingPersonRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out reason);
    }

    private static EventOption CreateReadyOption(Neow neow, RunState runState)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
        {
            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Host can start persona selection | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)} role=Singleplayer.");
            return new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
                .ThatWontSaveToChoiceHistory();
        }

        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(netService);
        if (role == LobbyGameplayNetRole.Host)
        {
            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Host can start persona selection | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)} role=Host.");
            return new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
                .ThatWontSaveToChoiceHistory();
        }

        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Non-host ready page shown with host-gated interaction | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)} role={role}.");
        return new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
            .ThatWontSaveToChoiceHistory();
    }

    private static async Task OnReadyChosenAsync(Neow neow, RunState runState)
    {
        try
        {
            var netService = RunManager.Instance?.NetService;
            if (netService != null && netService.Type == NetGameType.Client)
            {
                MainFile.Logger.Info(
                    $"[StartingPersonNeowReadyFlow] Ignored non-host ready activation | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)}.");
                return;
            }

            if (netService != null && netService.Type == NetGameType.Host)
            {
                MainFile.Logger.Info(
                    $"[StartingPersonNeowReadyFlow] Host started persona selection from Neow ready page | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)}.");
                StartingPersonHostLaunchSync.BroadcastLaunch(runState);
            }

            await HandleReadyLaunchAsync(
                StartingPersonRelicSelectionPatch.GetRunKey(runState),
                netService?.Type == NetGameType.Host ? "host_local_click" : "singleplayer_local_click");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[StartingPersonNeowReadyFlow] Ready page selection flow failed: {ex}");
            RestoreOriginalOptionsOrFinish(neow, "ready_selection_failed");
        }
    }

    internal static bool TryInterceptReadyOptionRelease(NEventOptionButton optionButton)
    {
        if (optionButton.Event is not Neow neow)
            return false;
        if (!IsReadyOptionTextKey(optionButton.Option?.TextKey))
            return false;
        if (!TryResolveRunState(neow, out var runState))
            return false;

        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Intercepted ready option release outside default event flow | runKey={StartingPersonRelicSelectionPatch.GetRunKey(runState)}.");
        TaskHelper.RunSafely(OnReadyChosenAsync(neow, runState));
        return true;
    }

    internal static bool TrySwallowReadyOptionAtEventRoomEntry(NEventRoom eventRoom, EventOption option, int index)
    {
        ArgumentNullException.ThrowIfNull(eventRoom);
        ArgumentNullException.ThrowIfNull(option);

        if (!IsReadyOptionTextKey(option.TextKey))
            return false;
        if (RunManager.Instance?.EventSynchronizer?.GetLocalEvent() is not Neow neow)
            return false;
        if (!TryResolveRunState(neow, out var runState))
            return false;

        var runKey = StartingPersonRelicSelectionPatch.GetRunKey(runState);
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Swallowed ready option at event room entry | runKey={runKey} index={index}.");
        return true;
    }

    internal static void ApplyLocalReadyOptionUi(NEventOptionButton optionButton)
    {
        if (!IsReadyOptionTextKey(optionButton.Option?.TextKey))
            return;

        var netService = RunManager.Instance?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return;

        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(netService);
        if (role == LobbyGameplayNetRole.Host)
            return;

        optionButton.MouseFilter = Control.MouseFilterEnum.Ignore;

        var label = optionButton.GetNodeOrNull<Control>("%Text");
        if (label is not null)
        {
            var waitingTitle = new LocString("ancients", $"{WaitingOptionTextKey}.title").GetFormattedText();
            var waitingDescription = new LocString("ancients", $"{WaitingOptionTextKey}.description").GetFormattedText();
            label.Set("text", $"[red][b]{waitingTitle}[/b][/red]\n{waitingDescription}");
        }

        optionButton.Modulate = new Color(optionButton.Modulate, 0.85f);
    }

    internal static Task HandleReadyLaunchAsync(
        string runKey,
        string sourceTag,
        IReadOnlyList<string>? serializedRelicOptionIds = null)
    {
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Ready launch received | runKey={runKey} source={sourceTag} optionPayloadCount={serializedRelicOptionIds?.Count ?? 0}.");
        lock (SyncLock)
        {
            if (SelectionTasksByRun.TryGetValue(runKey, out var existingTask))
                return existingTask;

            if (!TryResolveReadyRunState(runKey, out var runState))
            {
                MainFile.Logger.Warn(
                    $"[StartingPersonNeowReadyFlow] Ready launch ignored because no run state was available | runKey={runKey} source={sourceTag}.");
                return Task.CompletedTask;
            }

            LaunchingRunKeys.Add(runKey);
            var createdTask = RunSelectionTaskAsync(runState, runKey, sourceTag, serializedRelicOptionIds);
            SelectionTasksByRun[runKey] = createdTask;
            return createdTask;
        }
    }

    private static async Task<bool> RunSelectionTaskAsync(
        RunState runState,
        string runKey,
        string sourceTag,
        IReadOnlyList<string>? serializedRelicOptionIds)
    {
        try
        {
            var overrideRelicOptions =
                StartingPersonRelicSelectionPatch.ResolveRelicOptionsFromSerializedIds(serializedRelicOptionIds, sourceTag);
            if (serializedRelicOptionIds != null && serializedRelicOptionIds.Count > 0 && overrideRelicOptions.Count == 0)
            {
                MainFile.Logger.Warn(
                    $"[StartingPersonNeowReadyFlow] Host persona option payload resolved to 0 options; falling back to local generation | runKey={runKey} source={sourceTag}.");
            }

            var opened = await StartingPersonRelicSelectionPatch.OpenSelectionOverlayAsync(
                runState,
                $"neow_ready_launch:{sourceTag}",
                overrideRelicOptions.Count > 0 ? overrideRelicOptions : null);
            if (opened)
            {
                RebuildInitialOptionsForRun(runKey, "post_selection_rebuild");
                MainFile.Logger.Info(
                    $"[StartingPersonNeowReadyFlow] Rebuilt Neow initial page after persona overlay | runKey={runKey} source={sourceTag}.");
            }
            else
            {
                RestoreOriginalOptionsForRun(runKey, "ready_launch_overlay_not_opened");
                MainFile.Logger.Info(
                    $"[StartingPersonNeowReadyFlow] Restored cached Neow options because persona overlay did not open | runKey={runKey} source={sourceTag}.");
            }

            await RefreshRestoredNeowUiAfterOverlayClosedAsync(runState, runKey, "post_overlay_verify");
            return opened;
        }
        finally
        {
            lock (SyncLock)
            {
                LaunchingRunKeys.Remove(runKey);
                SelectionTasksByRun.Remove(runKey);
            }
        }
    }

    private static bool TryResolveRunState(Neow neow, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RunState? runState)
    {
        if (neow.Owner?.RunState is RunState ownerRunState)
        {
            runState = ownerRunState;
            return true;
        }

        runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        return runState is not null;
    }

    private static bool TryResolveReadyRunState(string runKey, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RunState? runState)
    {
        lock (SyncLock)
        {
            foreach (var state in ReadyPages.Values)
            {
                if (state.RunKey != runKey)
                    continue;

                runState = state.RunState;
                return true;
            }
        }

        var debugRunState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        if (debugRunState != null && StartingPersonRelicSelectionPatch.GetRunKey(debugRunState) == runKey)
        {
            runState = debugRunState;
            return true;
        }

        runState = null;
        return false;
    }

    private static void RestoreOriginalOptionsForRun(string runKey, string reason)
    {
        List<Neow> neowsToRestore;
        lock (SyncLock)
        {
            neowsToRestore = ReadyPages
                .Where(pair => pair.Value.RunKey == runKey)
                .Select(static pair => pair.Key)
                .ToList();
        }

        foreach (var neow in neowsToRestore)
            RestoreOriginalOptionsOrFinish(neow, reason);
    }

    private static void RebuildInitialOptionsForRun(string runKey, string reason)
    {
        List<Neow> neowsToRebuild;
        lock (SyncLock)
        {
            neowsToRebuild = ReadyPages
                .Where(pair => pair.Value.RunKey == runKey)
                .Select(static pair => pair.Key)
                .ToList();
        }

        foreach (var neow in neowsToRebuild)
            RebuildInitialOptionsOrFallback(neow, reason);
    }

    private static void RestoreOriginalOptionsOrFinish(Neow neow, string reason)
    {
        ReadyPageState? state;
        lock (SyncLock)
        {
            if (!ReadyPages.Remove(neow, out state))
                return;
        }

        var restoredOptions = NeowOptionInjectionHelper.EnsureSelectedCustomOptionPresent(
            neow,
            state.OriginalOptions,
            "ready_page_restore");
        if (restoredOptions.Count == 0)
        {
            neow.StartPreFinished();
            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Ready page restored by finishing Neow immediately because the original option page was empty | runKey={state.RunKey} reason={reason}.");
            return;
        }

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page restoration.");

        SetEventStateMethod.Invoke(neow, [state.OriginalDescription, restoredOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Restored Neow original option page | runKey={state.RunKey} reason={reason} optionCount={restoredOptions.Count}.");
    }

    private static void RebuildInitialOptionsOrFallback(Neow neow, string reason)
    {
        ReadyPageState? state;
        lock (SyncLock)
        {
            if (!ReadyPages.Remove(neow, out state))
                return;
        }

        if (SetInitialEventStateMethod == null)
        {
            RestoreCachedStateOrFinish(neow, state, $"{reason}:missing_set_initial_event_state");
            return;
        }

        try
        {
            SetInitialEventStateMethod.Invoke(neow, [false]);
            var rebuiltOptionCount = neow.CurrentOptions?.Count ?? 0;
            var cachedOptionCount = state.OriginalOptions.Count;
            if (rebuiltOptionCount < cachedOptionCount)
            {
                MainFile.Logger.Warn(
                    $"[StartingPersonNeowReadyFlow] Rebuilt Neow initial option page produced fewer options than the cached page; falling back to cached options | runKey={state.RunKey} reason={reason} rebuilt={rebuiltOptionCount} cached={cachedOptionCount}.");
                RestoreCachedStateOrFinish(neow, state, $"{reason}:fallback_cached_after_short_rebuild");
                return;
            }

            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Rebuilt Neow initial option page from source event state | runKey={state.RunKey} reason={reason} optionCount={rebuiltOptionCount}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[StartingPersonNeowReadyFlow] Failed to rebuild Neow initial option page; falling back to cached options | runKey={state.RunKey} reason={reason}: {ex}");
            RestoreCachedStateOrFinish(neow, state, $"{reason}:fallback_cached");
        }
    }

    private static void RestoreCachedStateOrFinish(Neow neow, ReadyPageState state, string reason)
    {
        var restoredOptions = NeowOptionInjectionHelper.EnsureSelectedCustomOptionPresent(
            neow,
            state.OriginalOptions,
            "ready_page_restore");
        if (restoredOptions.Count == 0)
        {
            neow.StartPreFinished();
            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Ready page restored by finishing Neow immediately because the cached option page was empty | runKey={state.RunKey} reason={reason}.");
            return;
        }

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page restoration.");

        SetEventStateMethod.Invoke(neow, [state.OriginalDescription, restoredOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Restored cached Neow option page | runKey={state.RunKey} reason={reason} optionCount={restoredOptions.Count}.");
    }

    private static async Task RefreshRestoredNeowUiAsync(RunState runState, string runKey, string stage)
    {
        if (NGame.Instance?.IsInsideTree() != true)
            return;

        await AwaitProcessFrameAsync();

        var eventRoom = FindCurrentEventRoom();
        var layoutNode = FindCurrentAncientLayout(eventRoom);
        AstralNeowDiagnosticHelper.ReportReadyRestoreUiSnapshot(runState, layoutNode, $"{stage}:before_refresh");

        var eventModel = TryResolveCurrentEventModel(runState);
        if (eventRoom != null && eventModel != null)
        {
            try
            {
                if (EventRoomSetupLayoutMethod?.Invoke(eventRoom, null) is Task setupTask)
                    await setupTask;

                using var _ = BeginInternalNeowRefreshScope(runKey, stage);
                EventRoomRefreshEventStateMethod?.Invoke(eventRoom, [eventModel]);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"[StartingPersonNeowReadyFlow] Failed to rebuild Neow event room UI | runKey={runKey} stage={stage}: {ex}");
            }
        }

        layoutNode = FindCurrentAncientLayout(eventRoom);
        if (layoutNode != null)
            ForceLayoutRefresh(layoutNode);

        await AwaitProcessFrameAsync();
        layoutNode = FindCurrentAncientLayout(eventRoom);
        AstralNeowDiagnosticHelper.ReportReadyRestoreUiSnapshot(runState, layoutNode, $"{stage}:after_refresh");
    }

    private static async Task RefreshRestoredNeowUiAfterOverlayClosedAsync(RunState runState, string runKey, string stage)
    {
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Neow restore frame barrier begin | runKey={runKey} stage={stage}.");
        await AwaitFramesAsync(2);
        await RefreshRestoredNeowUiAsync(runState, runKey, $"{stage}:post_close_before_refresh");
        await AwaitFramesAsync(1);
        await RefreshRestoredNeowUiAsync(runState, runKey, $"{stage}:post_close_after_refresh");
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Neow restore frame barrier end | runKey={runKey} stage={stage}.");
    }

    private static EventModel? TryResolveCurrentEventModel(RunState runState)
    {
        if (RunManager.Instance?.EventSynchronizer?.GetLocalEvent() is EventModel localEvent)
            return localEvent;

        var room = runState.CurrentRoom;
        return AccessTools.Property(room?.GetType(), "Event")?.GetValue(room) as EventModel
               ?? AccessTools.Property(room?.GetType(), "CurrentEvent")?.GetValue(room) as EventModel
               ?? AccessTools.Field(room?.GetType(), "_event")?.GetValue(room) as EventModel
               ?? AccessTools.Field(room?.GetType(), "eventModel")?.GetValue(room) as EventModel;
    }

    private static NEventRoom? FindCurrentEventRoom()
    {
        var currentScene = NGame.Instance?.GetTree()?.CurrentScene;
        if (currentScene == null)
            return null;
        if (currentScene is NEventRoom eventRoom)
            return eventRoom;

        return currentScene
            .FindChildren("*", nameof(NEventRoom), true, false)
            .OfType<NEventRoom>()
            .FirstOrDefault();
    }

    private static Node? FindCurrentAncientLayout(NEventRoom? eventRoom)
    {
        if (eventRoom?.Layout is Node directLayout)
            return directLayout;

        return eventRoom?
            .FindChildren("*", nameof(NAncientEventLayout), true, false)
            .OfType<Node>()
            .FirstOrDefault();
    }

    private static void ForceLayoutRefresh(Node layoutNode)
    {
        RefreshNodeLayout(layoutNode);
        foreach (var nodePath in new[] { "%ContentContainer", "%Content", "%DialogueContainer", "%OptionsContainer" })
        {
            if (layoutNode.GetNodeOrNull<Node>(nodePath) is { } child)
                RefreshNodeLayout(child);
        }

        if (layoutNode is NAncientEventLayout ancientLayout)
        {
            AncientLayoutInitializeVisualsMethod?.Invoke(ancientLayout, null);
            AncientLayoutUpdateBannerVisibilityMethod?.Invoke(ancientLayout, null);
            AncientLayoutUpdateFakeNextButtonMethod?.Invoke(ancientLayout, null);
        }

        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(layoutNode))
                return;

            RefreshNodeLayout(layoutNode);
            foreach (var nodePath in new[] { "%ContentContainer", "%Content", "%DialogueContainer", "%OptionsContainer" })
            {
                if (layoutNode.GetNodeOrNull<Node>(nodePath) is { } child)
                    RefreshNodeLayout(child);
            }
        }).CallDeferred();
    }

    private static void RefreshNodeLayout(Node node)
    {
        if (node is Control control)
        {
            ControlUpdateMinimumSizeMethod?.Invoke(control, null);
            control.QueueRedraw();
        }

        if (node is Container container)
            container.QueueSort();
    }

    private static async Task AwaitProcessFrameAsync()
    {
        if (NGame.Instance?.IsInsideTree() == true)
        {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            return;
        }

        await Task.Yield();
    }

    private static async Task AwaitFramesAsync(int frameCount)
    {
        for (var frame = 0; frame < frameCount; frame++)
            await AwaitProcessFrameAsync();
    }

    private static IDisposable BeginInternalNeowRefreshScope(string runKey, string stage)
    {
        InternalNeowRefreshDepth.Value++;
        MainFile.Logger.Info(
            $"[StartingPersonNeowReadyFlow] Entered internal Neow refresh scope | runKey={runKey} stage={stage} depth={InternalNeowRefreshDepth.Value}.");
        return new InternalNeowRefreshScope(runKey, stage);
    }

    private sealed class InternalNeowRefreshScope(string runKey, string stage) : IDisposable
    {
        public void Dispose()
        {
            InternalNeowRefreshDepth.Value = Math.Max(0, InternalNeowRefreshDepth.Value - 1);
            MainFile.Logger.Info(
                $"[StartingPersonNeowReadyFlow] Exited internal Neow refresh scope | runKey={runKey} stage={stage} depth={InternalNeowRefreshDepth.Value}.");
        }
    }
}

[HarmonyPatch(typeof(AncientEventModel), "SetInitialEventState")]
internal sealed class StartingPersonaNeowReadyInitialStatePatch
{
    [HarmonyPostfix]
    public static void Postfix(AncientEventModel __instance)
    {
        if (__instance is not Neow neow)
            return;

        StartingPersonNeowReadyFlow.TryReplaceInitialState(neow);
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnRelease")]
internal static class StartingPersonaNeowReadyOptionReleasePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventOptionButton __instance)
    {
        return !StartingPersonNeowReadyFlow.TryInterceptReadyOptionRelease(__instance);
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "_Ready")]
internal static class StartingPersonaNeowReadyOptionUiPatch
{
    [HarmonyPostfix]
    public static void Postfix(NEventOptionButton __instance)
    {
        StartingPersonNeowReadyFlow.ApplyLocalReadyOptionUi(__instance);
    }
}

[HarmonyPatch(typeof(NEventRoom), "OptionButtonClicked")]
internal static class StartingPersonaNeowReadyEventRoomGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventRoom __instance, EventOption option, int index)
    {
        return !StartingPersonNeowReadyFlow.TrySwallowReadyOptionAtEventRoomEntry(__instance, option, index);
    }
}

[HarmonyPatch(typeof(EventSynchronizer), "HandleEventOptionChosenMessage")]
internal static class StartingPersonaNeowReadyRemoteChoiceRestorePatch
{
    [HarmonyPrefix]
    public static void Prefix(OptionIndexChosenMessage message, ulong senderId)
    {
        if (message.type != OptionIndexType.Event)
            return;

        StartingPersonNeowReadyFlow.TryRestoreCachedNeowOptionsBeforeRemoteChoice(message.optionIndex, senderId);
    }
}
