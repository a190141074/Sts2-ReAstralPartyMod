using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class ExtremeModeTurnLimitPatch : IPatchMethod
{
    private const int PlayerTurnLimit = 16;

    public static string PatchId => "extreme_mode_turn_limit_patch";

    public static string Description =>
        "Gameplay patch: lose combat when extreme mode is enabled and the player-side turn limit is exceeded";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(Hook), nameof(Hook.AfterTurnEnd),
                [typeof(ICombatState), typeof(CombatSide), typeof(IEnumerable<Creature>)])
        ];
    }

    public static void Postfix(ICombatState combatState, CombatSide side, ref Task __result)
    {
        __result = RunAfterTurnEnd(__result, combatState, side);
    }

    private static async Task RunAfterTurnEnd(Task originalTask, ICombatState combatState, CombatSide side)
    {
        await originalTask;

        if (side != CombatSide.Player)
            return;
        if (combatState?.RunState == null || combatState.RoundNumber < PlayerTurnLimit)
            return;
        if (!ReAstralPartyModSettingsManager.GetEnableExtremeMode(combatState.RunState))
            return;

        var combatManager = CombatManager.Instance;
        if (combatManager == null || !combatManager.IsInProgress || combatManager.IsEnding || combatManager.IsAboutToLose)
            return;
        if (combatState.RunState.IsGameOver)
            return;

        MainFile.Logger.Info(
            $"[ExtremeMode] Triggered at end of player turn {combatState.RoundNumber}; forcing combat loss.");
        AstralNotificationService.ShowWarning(
            AstralNotificationModule.Multiplayer,
            $"极限模式已触发：第{PlayerTurnLimit}回合玩家回合结束仍未获胜，本场战斗直接失败。",
            "极限模式");
        combatManager.LoseCombat();
    }
}
