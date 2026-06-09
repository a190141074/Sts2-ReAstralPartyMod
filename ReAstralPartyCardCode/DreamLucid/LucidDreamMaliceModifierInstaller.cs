using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

public static class LucidDreamMaliceModifierInstaller
{
    private static readonly MethodInfo? AddModifierDebugMethod =
        typeof(RunState).GetMethod(nameof(RunState.AddModifierDebug), BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo? ModifiersBackingField =
        typeof(RunState).GetField("<Modifiers>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    private static bool _lifecycleBridgeRegistered;
    private static readonly HashSet<string> LoadedRunKeysHandled = [];
    private static readonly object LifecycleGate = new();

    public static void RegisterLifecycleBridgeIfNeeded()
    {
        if (_lifecycleBridgeRegistered)
            return;

        _lifecycleBridgeRegistered = true;
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded, replayCurrentState: false);
        MainFile.Logger.Info("LucidDreamMalice lifecycle bridge registered.");
    }

    public static void EnsureInstalledForNewRun(RunState? runState)
    {
        EnsureInstalled(runState, loadedRun: false);
    }

    public static void EnsureInstalledForLoadedRun(RunState? runState)
    {
        EnsureInstalled(runState, loadedRun: true);
    }

    private static void EnsureInstalled(RunState? runState, bool loadedRun)
    {
        if (runState == null)
            return;

        if (!ReAstralPartyModSettingsManager.TryGetRunSnapshot(runState, out var snapshot))
            return;

        var existingModifier = LucidDreamMaliceModifier.Get(runState);
        if (!ReAstralPartyModSettingsManager.HasAnyLucidDreamEnabled(runState))
        {
            if (existingModifier == null)
                return;

            RemoveModifier(runState, existingModifier);
            MainFile.Logger.Info(
                $"LucidDreamMalice modifier removed | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count}");
            return;
        }

        if (existingModifier != null)
        {
            existingModifier.ApplySnapshot(snapshot);
            LucidDreamMaliceRuntimeHelper.InitializeDreamModeState(existingModifier, runState);
            if (existingModifier.EnableSmoothSailing)
            {
                LucidDreamMaliceRuntimeHelper.ApplySmoothSailingToMap(runState.Map);
                LucidDreamMaliceRuntimeHelper.RefreshMapScreenPointsIfNeeded(runState, runState.Map);
            }

            if (existingModifier.EnableDreamMode)
                LucidDreamMaliceRuntimeHelper.ApplyDreamModeToMap(runState, runState.Map);

            TaskHelper.RunSafely(LucidDreamMaliceRuntimeHelper.EnsureWildnessAppliedToActiveCombatAsync(runState));
            MainFile.Logger.Info(
                $"LucidDreamMalice modifier refreshed | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count} | enabledFlags={CountEnabledFlags(existingModifier)}");
            return;
        }

        if (AddModifierDebugMethod == null)
            throw new InvalidOperationException("RunState.AddModifierDebug was not found.");

        var modifier = (LucidDreamMaliceModifier)ModelDb.Modifier<LucidDreamMaliceModifier>().ToMutable();
        modifier.ApplySnapshot(snapshot);
        AddModifierDebugMethod.Invoke(runState, [modifier]);
        InitializeModifier(modifier, runState, loadedRun);
        LucidDreamMaliceRuntimeHelper.InitializeDreamModeState(modifier, runState);
        if (modifier.EnableSmoothSailing)
        {
            LucidDreamMaliceRuntimeHelper.ApplySmoothSailingToMap(runState.Map);
            LucidDreamMaliceRuntimeHelper.RefreshMapScreenPointsIfNeeded(runState, runState.Map);
        }

        if (modifier.EnableDreamMode)
            LucidDreamMaliceRuntimeHelper.ApplyDreamModeToMap(runState, runState.Map);

        TaskHelper.RunSafely(LucidDreamMaliceRuntimeHelper.EnsureWildnessAppliedToActiveCombatAsync(runState));

        MainFile.Logger.Info(
            $"LucidDreamMalice modifier installed | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count} | enabledFlags={CountEnabledFlags(modifier)}");
    }

    private static void OnRunLoaded(RunLoadedEvent evt)
    {
        var runState = evt.RunState;
        if (runState == null)
            return;

        var runKey = GetRunKey(runState);
        lock (LifecycleGate)
        {
            if (!LoadedRunKeysHandled.Add(runKey))
                return;
        }

        MainFile.Logger.Info($"LucidDreamMalice lifecycle restore triggered | runKey={runKey}.");
        EnsureInstalledForLoadedRun(runState);
    }

    private static void RemoveModifier(RunState runState, LucidDreamMaliceModifier modifier)
    {
        if (ModifiersBackingField == null)
            throw new InvalidOperationException("RunState.Modifiers backing field was not found.");

        var updatedModifiers = runState.Modifiers
            .Where(existing => existing != modifier)
            .ToList();
        ModifiersBackingField.SetValue(runState, updatedModifiers);
    }

    private static void InitializeModifier(
        ModifierModel modifier,
        RunState runState,
        bool loadedRun)
    {
        if (loadedRun)
            modifier.OnRunLoaded(runState);
        else
            modifier.OnRunCreated(runState);
    }

    private static int CountEnabledFlags(LucidDreamMaliceModifier modifier)
    {
        var count = 0;
        if (modifier.EnableFalseLifeline)
            count++;
        if (modifier.EnableSmoothSailing)
            count++;
        if (modifier.EnableDreamMode)
            count++;
        if (modifier.EnableFishScalesMalice)
            count++;
        if (modifier.EnableSevereWoundOneMalice)
            count++;
        if (modifier.EnableSevereWoundTwoMalice)
            count++;
        if (modifier.EnableMadLifeMalice)
            count++;
        if (modifier.EnableSwampOfFateMalice)
            count++;
        if (modifier.EnableOverpopulationMalice)
            count++;
        if (modifier.EnableCautiousJellyfishMalice)
            count++;
        if (modifier.EnableFaceDeathWithComposure)
            count++;
        if (modifier.EnableWildness)
            count++;
        if (modifier.EnableWildnessPhantom)
            count++;
        if (modifier.EnablePitchBlackImpulse)
            count++;
        if (modifier.EnableBubblePotionOfDreams)
            count++;
        if (modifier.EnableHarmlessWhisper)
            count++;
        return count;
    }

    private static string GetRunKey(RunState runState)
    {
        return $"{runState.Rng.StringSeed}|a{runState.CurrentActIndex}|p{runState.Players.Count}|{string.Join(",", runState.Players.Select(static player => player.NetId.ToString()))}";
    }
}
