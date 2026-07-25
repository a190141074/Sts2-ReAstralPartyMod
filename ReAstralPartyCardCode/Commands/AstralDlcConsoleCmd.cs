using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Commands;

public sealed class AstralDlcConsoleCmd : AbstractConsoleCmd
{
    public const string CommandName = "astraldlc";

    public override string CmdName => CommandName;
    public override string Args => "add <series> | remove <index> | replace <index> <series> | <series>";
    public override string Description => "[ReAstralPartyMod] Override open token extension series for the current run";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        if (runState == null)
            return new CmdResult(false, "No active run.");

        var netRole = LobbyGameplayNetRoleHelper.GetCurrentRole();
        if (netRole == LobbyGameplayNetRole.Client)
            return new CmdResult(false, "astraldlc is host-only in multiplayer.");

        if (args.Length == 0)
            return new CmdResult(false, BuildUsage());

        var action = args[0].Trim();
        if (!IsAction(action))
            return args.Length == 1
                ? AddSeries(runState, action, netRole)
                : new CmdResult(false, BuildUsage());

        return action.ToLowerInvariant() switch
        {
            "add" => args.Length == 2
                ? AddSeries(runState, args[1], netRole)
                : new CmdResult(false, BuildUsage()),
            "remove" => args.Length == 2
                ? RemoveSeries(runState, args[1], netRole)
                : new CmdResult(false, BuildUsage()),
            "replace" => args.Length == 3
                ? ReplaceSeries(runState, args[1], args[2], netRole)
                : new CmdResult(false, BuildUsage()),
            _ => new CmdResult(false, BuildUsage())
        };
    }

    private static CmdResult AddSeries(RunState runState, string seriesInput, LobbyGameplayNetRole netRole)
    {
        if (!TokenSeriesAvailabilityHelper.TryParseSeries(seriesInput, out var series))
            return new CmdResult(false, $"Unknown token series: {seriesInput}");

        var openSeries = TokenSeriesAvailabilityHelper.GetMutableOpenSeriesForCommand(runState).ToList();
        if (openSeries.Contains(series))
            return new CmdResult(false, $"Token series already open: {series}");

        openSeries.Add(series);
        return ApplyOverride(runState, openSeries, netRole, $"Added {series}.");
    }

    private static CmdResult RemoveSeries(RunState runState, string indexInput, LobbyGameplayNetRole netRole)
    {
        if (!TryParseOneBasedIndex(indexInput, out var index))
            return new CmdResult(false, $"Invalid extension index: {indexInput}");

        var openSeries = TokenSeriesAvailabilityHelper.GetMutableOpenSeriesForCommand(runState).ToList();
        if (index < 0 || index >= openSeries.Count)
            return new CmdResult(false, $"Extension index out of range. Current count: {openSeries.Count}.");

        var removed = openSeries[index];
        openSeries.RemoveAt(index);
        return ApplyOverride(runState, openSeries, netRole, $"Removed {removed}.");
    }

    private static CmdResult ReplaceSeries(
        RunState runState,
        string indexInput,
        string seriesInput,
        LobbyGameplayNetRole netRole)
    {
        if (!TryParseOneBasedIndex(indexInput, out var index))
            return new CmdResult(false, $"Invalid extension index: {indexInput}");

        if (!TokenSeriesAvailabilityHelper.TryParseSeries(seriesInput, out var series))
            return new CmdResult(false, $"Unknown token series: {seriesInput}");

        var openSeries = TokenSeriesAvailabilityHelper.GetMutableOpenSeriesForCommand(runState).ToList();
        if (index < 0 || index >= openSeries.Count)
            return new CmdResult(false, $"Extension index out of range. Current count: {openSeries.Count}.");

        if (openSeries.Where((existing, existingIndex) => existingIndex != index).Contains(series))
            return new CmdResult(false, $"Token series already open: {series}");

        var replaced = openSeries[index];
        openSeries[index] = series;
        return ApplyOverride(runState, openSeries, netRole, $"Replaced {replaced} with {series}.");
    }

    private static CmdResult ApplyOverride(
        RunState runState,
        IReadOnlyList<TokenSeriesAvailabilityHelper.TokenSeries> openSeries,
        LobbyGameplayNetRole netRole,
        string message)
    {
        TokenSeriesAvailabilityHelper.SetManualOpenSeries(runState, openSeries);
        if (!TokenSeriesAvailabilityHelper.TryGetState(runState, out var state))
            return new CmdResult(false, "Failed to read updated token series state.");

        if (netRole == LobbyGameplayNetRole.Host)
            AstralDlcOverrideSync.BroadcastOverride(state.OpenSeries);

        _ = FinalizeSuccessfulOverrideAsync(runState, state.OpenSeries);
        return new CmdResult(true, $"{message} {TokenSeriesAvailabilityHelper.BuildSeriesListSummary(state.OpenSeries)}");
    }

    private static async Task FinalizeSuccessfulOverrideAsync(
        RunState runState,
        IReadOnlyList<TokenSeriesAvailabilityHelper.TokenSeries> openSeries)
    {
        try
        {
            if (openSeries.Contains(TokenSeriesAvailabilityHelper.TokenSeries.FinalArena))
                await FinalArenaSeriesHelper.GrantStunGunIfNeeded(runState);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"astraldlc post-apply task failed: {ex}");
        }
    }

    private static bool IsAction(string action)
    {
        return string.Equals(action, "add", StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, "replace", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseOneBasedIndex(string input, out int zeroBasedIndex)
    {
        zeroBasedIndex = -1;
        if (!int.TryParse(input, out var oneBasedIndex) || oneBasedIndex <= 0)
            return false;

        zeroBasedIndex = oneBasedIndex - 1;
        return true;
    }

    private static string BuildUsage()
    {
        return "Usage: astraldlc add <series> | astraldlc remove <index> | astraldlc replace <index> <series> | astraldlc <series>";
    }
}
