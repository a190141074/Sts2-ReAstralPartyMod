using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class StartingPersonaNeowReadyFlow
{
    private const string ReadyPageDescriptionKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY.description";
    private const string ReadyOptionTextKey =
        "RE_ASTRAL_PARTY_MOD_ANCIENT_NEOW.pages.STARTING_PERSONA_READY.options.READY";

    private static readonly object SyncLock = new();
    private static readonly Dictionary<Neow, ReadyPageState> ReadyPages = [];
    private static readonly HashSet<string> ActiveReadyRunKeys = [];
    private static readonly Dictionary<string, Task<bool>> SelectionTasksByRun = [];
    private static readonly MethodInfo? SetEventStateMethod = AccessTools.Method(
        typeof(EventModel),
        "SetEventState",
        [typeof(LocString), typeof(IEnumerable<EventOption>)]);

    private sealed record ReadyPageState(IReadOnlyList<EventOption> OriginalOptions, LocString OriginalDescription);

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

            ReadyPages[neow] = new ReadyPageState(
                neow.CurrentOptions.ToList(),
                neow.Description ?? neow.InitialDescription);
        }

        ArmReadyFlow(runState);
        var readyOptions = new[]
        {
            new EventOption(neow, () => OnReadyChosenAsync(neow, runState), ReadyOptionTextKey)
                .ThatWontSaveToChoiceHistory()
        };

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page injection.");

        SetEventStateMethod.Invoke(neow, [CreateReadyPageDescription(), readyOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Injected Neow ready page for run {StartingPersonaRelicSelectionPatch.GetRunKey(runState)}.");
    }

    internal static bool IsReadyPageShared(Neow neow)
    {
        if (!TryResolveRunState(neow, out var runState))
            return false;

        lock (SyncLock)
        {
            return ActiveReadyRunKeys.Contains(StartingPersonaRelicSelectionPatch.GetRunKey(runState));
        }
    }

    internal static LocString CreateReadyPageDescription()
    {
        return new LocString("ancients", ReadyPageDescriptionKey);
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

    private static async Task OnReadyChosenAsync(Neow neow, RunState runState)
    {
        try
        {
            await GetOrCreateSelectionTask(runState);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[StartingPersonaNeowReadyFlow] Starting persona selection from Neow ready page failed: {ex}");
        }
        finally
        {
            RestoreOriginalOptionsOrFinish(neow);
        }
    }

    private static Task<bool> GetOrCreateSelectionTask(RunState runState)
    {
        var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
        lock (SyncLock)
        {
            if (SelectionTasksByRun.TryGetValue(runKey, out var existingTask))
                return existingTask;

            var createdTask = RunSelectionTaskAsync(runState, runKey);
            SelectionTasksByRun[runKey] = createdTask;
            return createdTask;
        }
    }

    private static async Task<bool> RunSelectionTaskAsync(RunState runState, string runKey)
    {
        try
        {
            return await StartingPersonaRelicSelectionPatch.OpenSelectionOverlayAsync(runState, "neow_ready_page");
        }
        finally
        {
            lock (SyncLock)
            {
                ActiveReadyRunKeys.Remove(runKey);
                SelectionTasksByRun.Remove(runKey);
            }
        }
    }

    private static void ArmReadyFlow(RunState runState)
    {
        var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
        lock (SyncLock)
        {
            ActiveReadyRunKeys.Add(runKey);
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

    private static void RestoreOriginalOptionsOrFinish(Neow neow)
    {
        ReadyPageState? state;
        lock (SyncLock)
        {
            if (!ReadyPages.Remove(neow, out state))
                return;
        }

        if (state.OriginalOptions.Count == 0)
        {
            neow.StartPreFinished();
            MainFile.Logger.Info("[StartingPersonaNeowReadyFlow] Restored Neow by finishing immediately because the original option page was empty.");
            return;
        }

        if (SetEventStateMethod == null)
            throw new InvalidOperationException("Failed to resolve EventModel.SetEventState for Neow ready-page restoration.");

        SetEventStateMethod.Invoke(neow, [state.OriginalDescription, state.OriginalOptions]);
        MainFile.Logger.Info(
            $"[StartingPersonaNeowReadyFlow] Restored Neow original option page with {state.OriginalOptions.Count} option(s).");
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

[HarmonyPatch(typeof(Neow), nameof(EventModel.IsShared), MethodType.Getter)]
internal sealed class StartingPersonaNeowReadyIsSharedPatch
{
    [HarmonyPostfix]
    public static void Postfix(Neow __instance, ref bool __result)
    {
        if (__result)
            return;

        if (StartingPersonaNeowReadyFlow.IsReadyPageShared(__instance))
            __result = true;
    }
}
