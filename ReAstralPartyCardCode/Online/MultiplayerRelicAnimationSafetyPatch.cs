using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using Godot;
using System.Reflection;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation))]
public static class MultiplayerRelicAnimationSafetyPatch
{
    private static int _skipLogCount;
    private static int _personaToastCount;
    private static int _tokenToastCount;
    private static readonly FieldInfo? RelicField =
        AccessTools.Field(typeof(NRelicInventoryHolder), "_relic")
        ?? AccessTools.Field(typeof(NRelicInventoryHolder), "relic")
        ?? AccessTools.Field(typeof(NRelicInventoryHolder), "Relic");

    [HarmonyPrefix]
    public static bool Prefix(NRelicInventoryHolder __instance, ref Task __result, out bool __state)
    {
        __state = false;
        if (IsNodeUsable(__instance))
        {
            __state = true;
            return true;
        }

        ReportSkip(__instance, "holder-not-in-tree");
        __result = Task.CompletedTask;
        return false;
    }

    [HarmonyPostfix]
    public static void Postfix(NRelicInventoryHolder __instance, bool __state, ref Task __result)
    {
        if (!__state)
            return;

        __result = AwaitSafely(__result, __instance);
    }

    private static async Task AwaitSafely(Task originalTask, NRelicInventoryHolder holder)
    {
        try
        {
            await originalTask;
        }
        catch (NullReferenceException) when (!IsNodeUsable(holder))
        {
            ReportSkip(holder, "holder-left-tree");
        }
        catch (ObjectDisposedException) when (!GodotObject.IsInstanceValid(holder))
        {
            ReportSkip(holder, "holder-disposed");
        }
    }

    private static bool IsNodeUsable(Node node)
    {
        return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
    }

    private static void ReportSkip(NRelicInventoryHolder holder, string reason)
    {
        if (_skipLogCount++ < 5)
            Log.Warn($"[{MainFile.ModId}] Skipped relic acquired animation during multiplayer-safe cleanup: {reason}");

        if (!TryGetRelic(holder, out var relic))
            return;
        if (!AstralRelicDiagnosticHelper.TryClassifyAstralRelic(relic, out var area, out var kind, out _))
            return;

        if (kind == AstralRelicDiagnosticKind.Persona)
        {
            if (_personaToastCount++ >= 2)
                return;
        }
        else
        {
            if (_tokenToastCount++ >= 2)
                return;
        }

        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            area,
            kind == AstralRelicDiagnosticKind.Persona ? 130 : 5,
            $"遗物获得动画被安全跳过，遗物可能已正常获得但表现层未播放。\n遗物：{AstralRelicDiagnosticHelper.GetRelicDisplayName(relic)}",
            "获得动画");
    }

    private static bool TryGetRelic(NRelicInventoryHolder holder, out RelicModel relic)
    {
        relic = null!;
        if (RelicField?.GetValue(holder) is not RelicModel model)
            return false;

        relic = model;
        return true;
    }
}
