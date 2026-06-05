using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class TrueStartingNeowGateHelper
{
    private const string TrueNeowEntry = "NEOW";
    private static readonly ConditionalWeakTable<AncientEventModel, GateLogState> GateLogs = [];

    internal static bool IsTrueStartingNeow(
        AncientEventModel ancient,
        out RunState? runState,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(ancient);

        if (!TryResolveRunState(ancient, out runState) || runState == null)
        {
            reason = "run state was unavailable";
            return false;
        }

        var eventId = ancient.Id.Entry;
        if (!string.Equals(eventId, TrueNeowEntry, StringComparison.Ordinal))
        {
            reason = $"event id '{eventId}' is not true Neow";
            return false;
        }

        if (runState.CurrentActIndex != 0)
        {
            reason = $"act index {runState.CurrentActIndex} is not the starting act";
            return false;
        }

        reason = "accepted";
        return true;
    }

    internal static bool ShouldInjectReadyPage(
        AncientEventModel ancient,
        out RunState runState,
        out string reason)
    {
        if (!IsTrueStartingNeow(ancient, out var resolvedRunState, out reason) || resolvedRunState == null)
        {
            runState = null!;
            return false;
        }

        runState = resolvedRunState;
        return StartingPersonaRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out reason);
    }

    internal static bool ShouldExposeCustomNeowOptions(
        AncientEventModel ancient,
        out RunState runState,
        out string reason)
    {
        if (!IsTrueStartingNeow(ancient, out var resolvedRunState, out reason) || resolvedRunState == null)
        {
            runState = null!;
            return false;
        }

        runState = resolvedRunState;
        reason = "accepted";
        return true;
    }

    internal static void LogGateDecision(
        string source,
        string purpose,
        AncientEventModel ancient,
        bool accepted,
        string reason,
        RunState? runState)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(purpose);
        ArgumentNullException.ThrowIfNull(ancient);
        ArgumentNullException.ThrowIfNull(reason);

        var state = GateLogs.GetOrCreateValue(ancient);
        var decisionKey = $"{source}|{purpose}|{accepted}";
        lock (state.LoggedKeys)
        {
            if (!state.LoggedKeys.Add(decisionKey))
                return;
        }

        if (accepted)
        {
            MainFile.Logger.Info(
                $"[{source}] True Neow gate accepted | purpose={purpose} eventId={ancient.Id.Entry} act={runState?.CurrentActIndex ?? -1}.");
            return;
        }

        MainFile.Logger.Info(
            $"[{source}] True Neow gate rejected | purpose={purpose} eventId={ancient.Id.Entry} reason={reason}.");
    }

    private static bool TryResolveRunState(AncientEventModel ancient, out RunState? runState)
    {
        if (ancient.Owner?.RunState is RunState ownerRunState)
        {
            runState = ownerRunState;
            return true;
        }

        runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        return runState != null;
    }

    private sealed class GateLogState
    {
        public HashSet<string> LoggedKeys { get; } = [];
    }
}
