using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Nodes.Debug;
using ReAstralPartyMod.ReAstralPartyCardCode.Commands;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class DevConsoleUltimateChargeCommandPatch : IPatchMethod
{
    private static readonly FieldInfo? NDevConsoleField =
        typeof(NDevConsole).GetField("_devConsole",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo? CommandsField =
        typeof(DevConsole).GetField("_commands", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    public static string PatchId => "dev_console_ultimate_charge_command_patch";

    public static string Description =>
        "Gameplay patch: inject Astral Party commands into the official dev console";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NDevConsole), "_Ready")];
    }

    public static void Postfix(NDevConsole __instance)
    {
        try
        {
            if (NDevConsoleField?.GetValue(__instance) is not DevConsole devConsole)
            {
                MainFile.Logger.Warn("Failed to register dev console commands: dev console backend unavailable.");
                return;
            }

            if (CommandsField?.GetValue(devConsole) is not IDictionary commands)
            {
                MainFile.Logger.Warn("Failed to register dev console commands: command table unavailable.");
                return;
            }

            RegisterCommand(commands, RapUltChargeHandConsoleCmd.CommandName, new RapUltChargeHandConsoleCmd());
            RegisterCommand(commands, AstralDlcConsoleCmd.CommandName, new AstralDlcConsoleCmd());
            RegisterCommand(commands, AstralMutatorConsoleCmd.CommandName, new AstralMutatorConsoleCmd());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to register dev console commands: {ex}");
        }
    }

    private static void RegisterCommand(IDictionary commands, string commandName, AbstractConsoleCmd command)
    {
        if (commands.Contains(commandName))
            return;

        commands[commandName] = command;
        MainFile.Logger.Info($"Registered dev console command: {commandName}");
    }
}
