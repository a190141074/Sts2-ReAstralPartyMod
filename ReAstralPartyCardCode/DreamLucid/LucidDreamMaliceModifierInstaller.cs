using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

public static class LucidDreamMaliceModifierInstaller
{
    private static readonly MethodInfo? AddModifierDebugMethod =
        typeof(RunState).GetMethod(nameof(RunState.AddModifierDebug), BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo? ModifiersBackingField =
        typeof(RunState).GetField("<Modifiers>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

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
        if (!ReAstralPartyModSettingsManager.HasAnyLucidDreamMaliceEnabled(runState))
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

        MainFile.Logger.Info(
            $"LucidDreamMalice modifier installed | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count} | enabledFlags={CountEnabledFlags(modifier)}");
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
        return count;
    }
}
