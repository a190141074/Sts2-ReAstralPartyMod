using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal enum AstralNetPhase
{
    Lobby,
    StartRunBootstrap,
    MapOrRoom,
    Combat,
    RunEnd
}

internal static class AstralNetPhaseGuard
{
    public static void LogStartupDiagnostics()
    {
        MainFile.Logger.Info($"[{MainFile.ModId}] Astral net phase guard ready.");
    }

    public static bool IsMultiplayerDeterministicMode()
    {
        var runManager = RunManager.Instance;
        return runManager?.NetService?.Type is NetGameType.Host or NetGameType.Client;
    }

    public static bool IsAllowed(AstralNetPhase expectedPhase, out string actualPhase)
    {
        actualPhase = DetectPhase();
        return expectedPhase switch
        {
            AstralNetPhase.Lobby => actualPhase is "lobby" or "start_run_bootstrap",
            AstralNetPhase.StartRunBootstrap => actualPhase is "start_run_bootstrap" or "map_or_room",
            AstralNetPhase.MapOrRoom => actualPhase is "map_or_room" or "combat",
            AstralNetPhase.Combat => actualPhase == "combat",
            AstralNetPhase.RunEnd => actualPhase == "run_end",
            _ => false
        };
    }

    public static bool Guard(AstralNetPhase expectedPhase, string operation, bool logAllowed = false)
    {
        var allowed = IsAllowed(expectedPhase, out var actualPhase);
        if (allowed)
        {
            if (logAllowed)
                MainFile.Logger.Info(
                    $"[{MainFile.ModId}] Net phase guard allowed {operation}: phase={actualPhase} expected={expectedPhase}.");
            return true;
        }

        MainFile.Logger.Warn(
            $"[{MainFile.ModId}] Net phase guard rejected {operation}: phase={actualPhase} expected={expectedPhase}.");
        return false;
    }

    public static string DetectPhase()
    {
        var runManager = RunManager.Instance;
        if (CombatManager.Instance?.IsInProgress == true)
            return "combat";

        if (NOverlayStack.Instance == null || NGame.Instance == null)
            return "start_run_bootstrap";

        if (runManager?.DebugOnlyGetState() == null)
            return "start_run_bootstrap";

        if (NRun.Instance == null)
            return "run_end";

        return "map_or_room";
    }
}
