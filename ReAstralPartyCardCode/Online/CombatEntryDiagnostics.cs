using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class CombatEntryDiagnostics
{
    private static bool _registered;
    private static int _combatSequence;

    public static void Register()
    {
        if (_registered)
            return;

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(
            OnCombatStarting,
            false);
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(
            OnCombatEnded,
            false);
        _registered = true;
    }

    private static void OnCombatStarting(CombatStartingEvent lifecycleEvent)
    {
        Interlocked.Increment(ref _combatSequence);
        Mark("combat_starting", lifecycleEvent.RunState);
    }

    private static void OnCombatEnded(CombatEndedEvent lifecycleEvent)
    {
        Mark("combat_ended", lifecycleEvent.RunState);
    }

    public static void Mark(string phase, Player? player)
    {
        var players = player?.Creature?.CombatState?.Players
            .OrderBy(static item => item.NetId)
            .Select(static item => item.NetId.ToString())
            .ToArray() ?? [];
        Log(phase, player?.NetId.ToString() ?? "-", players);
    }

    private static void Mark(string phase, IRunState runState)
    {
        var players = runState.Players
            .OrderBy(static player => player.NetId)
            .Select(static player => player.NetId.ToString())
            .ToArray();
        Log(phase, "-", players);
    }

    private static void Log(string phase, string playerId, string[] players)
    {
        var combat = CombatManager.Instance;

        MainFile.Logger.Info(
            $"[CombatEntry] phase={phase} player={playerId} net={RunManager.Instance.NetService.Type} " +
            $"starting={combat.IsStarting} in_progress={combat.IsInProgress} " +
            $"players={string.Join(',', players)}");
    }
}
