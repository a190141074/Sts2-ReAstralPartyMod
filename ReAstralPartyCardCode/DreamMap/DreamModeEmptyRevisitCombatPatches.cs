using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamMap;

public sealed class DreamModeCombatRoomEnterCachePatch : IPatchMethod
{
    public static string PatchId => "dream_mode_combat_room_enter_cache";

    public static string Description => "Cache the first real combat encounter per coord so empty revisits reuse the original room template";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CombatRoom), nameof(CombatRoom.EnterInternal), [typeof(IRunState), typeof(bool)])];
    }

    public static void Prefix(CombatRoom __instance, IRunState? runState, bool isRestoringRoomStackBase)
    {
        if (isRestoringRoomStackBase || runState is not RunState concreteRunState)
            return;
        if (__instance.IsPreFinished)
            return;

        LucidDreamMaliceRuntimeHelper.CacheDreamModeCombatTemplate(concreteRunState, __instance);
    }
}

public sealed class DreamModeEmptyRevisitGenerateMonstersPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_empty_revisit_generate_monsters";

    public static string Description => "Suppress monster generation for dream-mode empty revisit combat rooms";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(EncounterModel), nameof(EncounterModel.GenerateMonstersWithSlots), [typeof(RunState)])];
    }

    public static bool Prefix(RunState runState, EncounterModel __instance)
    {
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEmptyRevisitCombatRoom(runState))
            return true;

        var monstersField = AccessTools.Field(typeof(EncounterModel), "_monstersWithSlots");
        monstersField?.SetValue(__instance, Array.Empty<(MonsterModel, string)>());
        return false;
    }
}

public sealed class DreamModeEmptyRevisitRewardsPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_empty_revisit_rewards";

    public static string Description => "Suppress rewards for dream-mode empty revisit combat rooms";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(EncounterModel), nameof(EncounterModel.ShouldGiveRewards), Type.EmptyTypes, MethodType.Getter)];
    }

    public static void Postfix(ref bool __result)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEmptyRevisitCombatRoom(runState))
            return;

        __result = false;
    }
}

public sealed class DreamModeEmptyRevisitCombatRoomExitPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_empty_revisit_combat_room_exit";

    public static string Description => "Clear empty revisit combat state after leaving the temporary dream-mode combat room";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CombatRoom), nameof(CombatRoom.Exit), Type.EmptyTypes)];
    }

    public static void Postfix(CombatRoom __instance)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState() as RunState;
        var modifier = LucidDreamMaliceModifier.Get(runState);
        if (modifier?.IsInDreamModeEmptyRevisitCombatRoom != true)
            return;

        LucidDreamMaliceRuntimeHelper.SetDreamModeEmptyRevisitCombatRoom(modifier, false);
    }
}
