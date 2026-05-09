using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Relics;
using Godot;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation))]
public static class MultiplayerRelicAnimationSafetyPatch
{
    private static int _skipLogCount;

    [HarmonyPrefix]
    public static bool Prefix(NRelicInventoryHolder __instance, ref Task __result, out bool __state)
    {
        __state = false;
        if (IsNodeUsable(__instance))
        {
            __state = true;
            return true;
        }

        LogSkip("holder-not-in-tree");
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
            LogSkip("holder-left-tree");
        }
        catch (ObjectDisposedException) when (!GodotObject.IsInstanceValid(holder))
        {
            LogSkip("holder-disposed");
        }
    }

    private static bool IsNodeUsable(Node node)
    {
        return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
    }

    private static void LogSkip(string reason)
    {
        if (_skipLogCount++ < 5)
            Log.Warn($"[{MainFile.ModId}] Skipped relic acquired animation during multiplayer-safe cleanup: {reason}");
    }
}
