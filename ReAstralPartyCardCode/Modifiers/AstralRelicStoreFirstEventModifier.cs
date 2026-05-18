using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

[RegisterGoodModifier]
public sealed class AstralRelicStoreFirstEventModifier : ModifierModel
{
    public override bool ShouldReceiveCombatHooks => false;

    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        var runState = RunState;
        var storeEvent = ModelDb.Event<AstralRelicStore>();
        var storeId = storeEvent.Id;

        if (!IsSecondAct(runState))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore modify-next-event skipped | reason=not_second_act | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={currentEvent.Id.Entry}");
            return currentEvent;
        }

        if (currentEvent.Id == storeId)
        {
            MainFile.Logger.Info(
                $"AstralRelicStore modify-next-event skipped | reason=already_store | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={currentEvent.Id.Entry}");
            return currentEvent;
        }

        if (AstralRelicStore.HasBeenConsumedThisAct(runState))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore modify-next-event skipped | reason=consumed_this_act | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={currentEvent.Id.Entry}");
            return currentEvent;
        }

        if (runState.VisitedEventIds.Contains(storeId))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore modify-next-event skipped | reason=visited | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | event={currentEvent.Id.Entry}");
            return currentEvent;
        }

        AstralRelicStore.MarkConsumedForCurrentAct(runState);
        MainFile.Logger.Info(
            $"AstralRelicStore modify-next-event applied | act={runState.Act.Id.Entry} | actIndex={runState.CurrentActIndex} | original={currentEvent.Id.Entry} | replacement={storeId.Entry}");
        return storeEvent;
    }
    private static bool IsSecondAct(RunState runState)
    {
        return runState.CurrentActIndex == 1
               || runState.Act is Hive
               || runState.Act is Underdocks;
    }
}
