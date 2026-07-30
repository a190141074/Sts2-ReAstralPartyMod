using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Commands;

public sealed class AstralMutatorConsoleCmd : AbstractConsoleCmd
{
    public const string CommandName = "astralmutator";

    public override string CmdName => CommandName;
    public override string Args => "";
    public override string Description => "[ReAstralPartyMod] Reroll Final Arena distortion components for the current run";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length != 0)
            return new CmdResult(false, "Usage: astralmutator");

        var runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        if (runState == null)
            return new CmdResult(false, "No active run.");

        var netRole = LobbyGameplayNetRoleHelper.GetCurrentRole();
        if (netRole != LobbyGameplayNetRole.Singleplayer)
            return new CmdResult(false, "astralmutator is only available in singleplayer.");

        if (!TokenSeriesAvailabilityHelper.TryGetState(runState, out var state) ||
            !state.OpenSeries.Contains(TokenSeriesAvailabilityHelper.TokenSeries.FinalArena))
            return new CmdResult(false, "Final Arena is not open.");

        var components = FinalArenaSeriesHelper.RegenerateComponents(runState);
        _ = FinalArenaSeriesHelper.EnsureInitializedAndAppliedAsync(runState, false, false);
        return new CmdResult(
            true,
            $"Rerolled Final Arena distortion components: {BuildComponentSummary(components)}");
    }

    private static string BuildComponentSummary(IReadOnlyList<FinalArenaDistortionComponent> components)
    {
        return string.Join(", ", components.Select(static component =>
        {
            var title = new LocString(
                "relics",
                $"RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT.{FinalArenaSeriesHelper.GetLocStem(component)}.title");
            title.Add("Difficulty", FinalArenaSeriesHelper.GetDifficulty(component));
            return title.GetFormattedText();
        }));
    }
}
