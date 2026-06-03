using System;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Commands;

public sealed class RapEtheriumIngotConsoleCmd : AbstractConsoleCmd
{
    public const string CommandName = "rapingot";

    public override string CmdName => CommandName;
    public override string Args => "<amount>";
    public override string Description => "[ReAstralPartyMod] Add Etherium Ingot stacks to the local player";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length < 1)
            return new CmdResult(false, "Usage: rapingot <amount>");
        if (!int.TryParse(args[0], out var amount) || amount <= 0)
            return new CmdResult(false, "Amount must be a positive integer.");

        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return new CmdResult(false, "No active run.");

        var player = LocalContext.GetMe(runState.Players) ?? runState.Players.FirstOrDefault();
        if (player == null)
            return new CmdResult(false, "No local player.");

        TaskHelper.RunSafely(EnigmaticEtheriumIngot.GrantStacks(player, amount));
        return new CmdResult(true, $"Granted +{amount} Etherium Ingot stack(s).");
    }
}
