using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Ancient;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class StartingPersonaNeowReadyFlow
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

    private sealed record ReadyPageState(
        string RunKey,
        RunState RunState,
        IReadOnlyList<EventOption> OriginalOptions);

    internal static void TryReplaceInitialState(Neow neow)
    {
        if (!ShouldInjectReadyPage(neow, out var runState, out var reason))
        {
            MainFile.Logger.Info($"[StartingPersonaNeowReadyFlow] Skipped Neow ready page: {reason}.");
            return;
        }

        lock (SyncLock)
        {
            if (ReadyPages.ContainsKey(neow))
                return;

            var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
            var originalOptions = NeowOptionInjectionHelper.EnsureSelectedCustomOptionPresent(
                neow,
                neow.CurrentOptions,
                "ready_page_cache");
            ReadyPages[neow] = new ReadyPageState(
                runKey,
                runState,
                originalOptions);
        }

        var readyOptions = new[]
        {
            CreateReadyOption(neow, runState)
        };

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page injection.");

        SetEventStateMethod.Invoke(neow, [CreateReadyPageDescription(), readyOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Ready page injected | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)}.");
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
        if (!TryResolveRunState(neow, out runState))
        {
            reason = "Neow owner or run state was unavailable";
            return false;
        }

        return StartingPersonaRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out reason);
    }

    private static EventOption CreateReadyOption(Neow neow, RunState runState)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
        {
            MainFile.Logger.Info(
                $"[StartingPersonaNeowReadyFlow] Host can start persona selection | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)} role=Singleplayer.");
            return new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
                .ThatWontSaveToChoiceHistory();
        }

        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(netService);
        if (role == LobbyGameplayNetRole.Host)
        {
            MainFile.Logger.Info(
                $"[StartingPersonaNeowReadyFlow] Host can start persona selection | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)} role=Host.");
            return new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
                .ThatWontSaveToChoiceHistory();
        }

        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Non-host ready page shown with host-gated interaction | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)} role={role}.");
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
                    $"[StartingPersonaNeowReadyFlow] Ignored non-host ready activation | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)}.");
                return;
            }

            if (netService != null && netService.Type == NetGameType.Host)
            {
                MainFile.Logger.Info(
                    $"[StartingPersonaNeowReadyFlow] Host started persona selection from Neow ready page | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)}.");
                StartingPersonaHostLaunchSync.BroadcastLaunch(runState);
            }

            await HandleReadyLaunchAsync(
                StartingPersonaRelicSelectionPatch.GetRunKey(runState),
                netService?.Type == NetGameType.Host ? "host_local_click" : "singleplayer_local_click");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[StartingPersonaNeowReadyFlow] Ready page selection flow failed: {ex}");
            RestoreOriginalOptionsForRun(StartingPersonaRelicSelectionPatch.GetRunKey(runState), "ready_selection_failed");
        }
    }

    internal static bool TryInterceptReadyOptionRelease(NEventOptionButton optionButton)
    {
        if (optionButton.Event is not Neow neow)
            return false;
        if (!IsReadyOptionTextKey(optionButton.Option?.TextKey))
            return false;
        if (!TryResolveRunState(neow, out var runState) || runState == null)
            return false;

        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Intercepted ready option release outside default event flow | runKey={StartingPersonaRelicSelectionPatch.GetRunKey(runState)}.");
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
        if (!TryResolveRunState(neow, out var runState) || runState == null)
            return false;

        var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Swallowed ready option at event room entry | runKey={runKey} index={index}.");
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

    internal static Task HandleReadyLaunchAsync(string runKey, string sourceTag)
    {
        MainFile.Logger.Info($"[StartingPersonaNeowReadyFlow] Ready launch received | runKey={runKey} source={sourceTag}.");
        lock (SyncLock)
        {
            if (SelectionTasksByRun.TryGetValue(runKey, out var existingTask))
                return existingTask;

            if (!TryResolveReadyRunState(runKey, out var runState))
            {
                MainFile.Logger.Warn(
                    $"[StartingPersonaNeowReadyFlow] Ready launch ignored because no run state was available | runKey={runKey} source={sourceTag}.");
                return Task.CompletedTask;
            }

            LaunchingRunKeys.Add(runKey);
            var createdTask = RunSelectionTaskAsync(runState, runKey, sourceTag);
            SelectionTasksByRun[runKey] = createdTask;
            return createdTask;
        }
    }

    private static async Task<bool> RunSelectionTaskAsync(RunState runState, string runKey, string sourceTag)
    {
        try
        {
            var opened = await StartingPersonaRelicSelectionPatch.OpenSelectionOverlayAsync(runState, $"neow_ready_launch:{sourceTag}");
            if (opened)
            {
                RestoreOriginalOptionsForRun(runKey, "post_selection_restore");
                MainFile.Logger.Info(
                    $"[StartingPersonaNeowReadyFlow] Restored cached Neow initial page after persona overlay | runKey={runKey} source={sourceTag}.");
            }
            else
            {
                RestoreOriginalOptionsForRun(runKey, "ready_launch_overlay_not_opened");
                MainFile.Logger.Info(
                    $"[StartingPersonaNeowReadyFlow] Restored cached Neow options because persona overlay did not open | runKey={runKey} source={sourceTag}.");
            }
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

    private static bool TryResolveRunState(Neow neow, out RunState? runState)
    {
        if (neow.Owner?.RunState is RunState ownerRunState)
        {
            runState = ownerRunState;
            return true;
        }

        runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        return runState is not null;
    }

    private static bool TryResolveReadyRunState(string runKey, out RunState? runState)
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
        if (debugRunState != null && StartingPersonaRelicSelectionPatch.GetRunKey(debugRunState) == runKey)
        {
            runState = debugRunState;
            return true;
        }

        runState = null;
        return false;
    }

    private static void RestoreOriginalOptionsForRun(string runKey, string reason)
    {
        List<KeyValuePair<Neow, ReadyPageState>> neowsToRestore;
        lock (SyncLock)
        {
            neowsToRestore = ReadyPages
                .Where(pair => pair.Value.RunKey == runKey)
                .ToList();

            foreach (var pair in neowsToRestore)
                ReadyPages.Remove(pair.Key);
        }

        foreach (var pair in neowsToRestore)
        {
            var neow = pair.Key;
            var state = pair.Value;
            var restoredOptions = NeowOptionInjectionHelper.EnsureSelectedCustomOptionPresent(
                neow,
                state.OriginalOptions,
                "ready_page_restore");
            if (restoredOptions.Count == 0)
            {
                neow.StartPreFinished();
                MainFile.Logger.Info(
                    $"[StartingPersonaNeowReadyFlow] Ready page restored by finishing Neow immediately because the original option page was empty | runKey={state.RunKey} reason={reason}.");
                continue;
            }

            if (SetEventStateMethod == null)
                throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page restoration.");

            SetEventStateMethod.Invoke(neow, [neow.Description ?? neow.InitialDescription, restoredOptions]);
            MainFile.Logger.Info(
                $"[StartingPersonaNeowReadyFlow] Restored Neow original option page | runKey={state.RunKey} reason={reason} optionCount={restoredOptions.Count}.");
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

        StartingPersonaNeowReadyFlow.TryReplaceInitialState(neow);
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnRelease")]
internal static class StartingPersonaNeowReadyOptionReleasePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventOptionButton __instance)
    {
        return !StartingPersonaNeowReadyFlow.TryInterceptReadyOptionRelease(__instance);
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "_Ready")]
internal static class StartingPersonaNeowReadyOptionUiPatch
{
    [HarmonyPostfix]
    public static void Postfix(NEventOptionButton __instance)
    {
        StartingPersonaNeowReadyFlow.ApplyLocalReadyOptionUi(__instance);
    }
}

[HarmonyPatch(typeof(NEventRoom), "OptionButtonClicked")]
internal static class StartingPersonaNeowReadyEventRoomGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventRoom __instance, EventOption option, int index)
    {
        return !StartingPersonaNeowReadyFlow.TrySwallowReadyOptionAtEventRoomEntry(__instance, option, index);
    }
}
