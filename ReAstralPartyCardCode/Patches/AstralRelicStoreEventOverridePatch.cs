using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class AstralRelicStoreEventOverridePatch : IPatchMethod
{
    public static string PatchId => "astral_relic_store_event_override_patch";

    public static string Description =>
        "Gameplay patch: force the first actual second-act event pull to become Astral Relic Store";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(ActModel), nameof(ActModel.PullNextEvent), [typeof(RunState)])];
    }

    public static void Postfix(ActModel __instance, RunState runState, ref EventModel __result)
    {
        var storeEvent = ModelDb.Event<AstralRelicStore>();
        var storeId = storeEvent.Id;
        var storeVisited = runState.VisitedEventIds.Contains(storeId);
        var storeConsumedThisAct = AstralRelicStore.HasBeenConsumedThisAct(runState);
        if (!ShouldForceStoreEvent(__instance, runState, __result))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore skipped | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={__result.Id.Entry} | visitedEvents={runState.VisitedEventIds.Count} | storeVisited={storeVisited} | storeConsumedThisAct={storeConsumedThisAct}");
            return;
        }

        AstralRelicStore.MarkConsumedForCurrentAct(runState);
        __result = storeEvent;
        MainFile.Logger.Info(
            $"AstralRelicStore override applied | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={__result.Id.Entry} | visitedEvents={runState.VisitedEventIds.Count} | storeConsumedThisAct=true | pendingUntilFirstActualEventPull=false");
    }

    private static bool ShouldForceStoreEvent(ActModel act, RunState? runState, EventModel? currentEvent)
    {
        if (runState == null || currentEvent == null)
            return false;

        if (!IsSecondAct(runState, act))
            return false;

        var storeEvent = ModelDb.Event<AstralRelicStore>();
        var storeId = storeEvent.Id;
        if (currentEvent.Id == storeId
            || runState.VisitedEventIds.Contains(storeId)
            || AstralRelicStore.HasBeenConsumedThisAct(runState))
            return false;

        return true;
    }

    private static bool IsSecondAct(RunState runState, ActModel act)
    {
        return runState.CurrentActIndex == 1
               || act is Hive
               || act is Underdocks
               || runState.Act is Hive
               || runState.Act is Underdocks;
    }
}
