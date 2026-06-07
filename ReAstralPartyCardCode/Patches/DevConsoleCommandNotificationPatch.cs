using System;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class DevConsoleCommandNotificationPatch : IPatchMethod
{
    public static string PatchId => "dev_console_command_notification_patch";

    public static bool IsCritical => false;

    public static string Description =>
        "Gameplay patch: broadcast a multiplayer notification when a dev-console command succeeds";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(DevConsole), "ProcessCommand", [typeof(string)])];
    }

    public static void Postfix(string inputValue, CmdResult __result)
    {
        if (!__result.success)
            return;

        var commandText = NormalizeInput(inputValue);
        if (string.IsNullOrWhiteSpace(commandText))
            return;

        var commandName = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(commandName) || string.Equals(commandName, "help", StringComparison.OrdinalIgnoreCase))
            return;

        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return;

        var player = ResolveLocalPlayer(runState);
        if (player == null)
            return;

        DevConsoleCommandNotificationSync.NotifySuccessfulLocalCommand(player, commandText);
    }

    private static Player? ResolveLocalPlayer(IRunState runState)
    {
        return LocalContext.GetMe(runState.Players) ?? runState.Players.FirstOrDefault();
    }

    private static string NormalizeInput(string? inputValue)
    {
        return string.IsNullOrWhiteSpace(inputValue)
            ? string.Empty
            : inputValue.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
