using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class AstralRelicStoreEventOverridePatch
{
    public static void PullNextEventPostfix(ActModel __instance, RunState runState, ref EventModel __result)
    {
        var storeId = ModelDb.Event<AstralRelicStore>().Id;
        var storeVisited = runState.VisitedEventIds.Contains(storeId);
        if (!ShouldForceStoreEvent(__instance, runState, __result))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore skipped | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={__result.Id.Entry} | visitedEvents={runState.VisitedEventIds.Count} | storeVisited={storeVisited}");
            return;
        }

        __result = ModelDb.Event<AstralRelicStore>();
        MainFile.Logger.Info(
            $"AstralRelicStore override applied | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={__result.Id.Entry} | visitedEvents={runState.VisitedEventIds.Count} | pendingUntilFirstActualEventPull=true");
    }

    private static bool ShouldForceStoreEvent(ActModel act, RunState? runState, EventModel? currentEvent)
    {
        if (runState == null || currentEvent == null)
            return false;

        if (!IsSecondAct(runState, act))
            return false;

        var storeId = ModelDb.Event<AstralRelicStore>().Id;
        if (currentEvent.Id == storeId || runState.VisitedEventIds.Contains(storeId))
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
