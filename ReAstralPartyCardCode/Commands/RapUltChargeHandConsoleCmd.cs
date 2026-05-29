using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Commands;

public sealed class RapUltChargeHandConsoleCmd : AbstractConsoleCmd
{
    public const string CommandName = "rapultchargehand";

    public override string CmdName => CommandName;
    public override string Args => "";
    public override string Description => "[ReAstralPartyMod] Add +100 charge to all ultimate cards in your hand";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return new CmdResult(false, "No active run.");

        var player = LocalContext.GetMe(runState.Players) ?? runState.Players.FirstOrDefault();
        if (player?.PlayerCombatState?.Hand == null)
            return new CmdResult(false, "No active combat hand.");

        var chargedCount = 0;
        foreach (var ultimateCard in player.PlayerCombatState.Hand.Cards.ToList().OfType<UltimateSkillCardModel>())
        {
            ultimateCard.AddCharge(100);
            chargedCount++;
        }

        return new CmdResult(true, $"Charged {chargedCount} ultimate card(s) in hand by +100.");
    }
}
