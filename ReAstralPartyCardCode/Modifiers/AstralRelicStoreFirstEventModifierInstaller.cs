using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

public static class AstralRelicStoreFirstEventModifierInstaller
{
    private static readonly MethodInfo? AddModifierDebugMethod =
        typeof(RunState).GetMethod(nameof(RunState.AddModifierDebug), BindingFlags.Instance | BindingFlags.Public);

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

        if (runState.Modifiers.Any(static modifier => modifier is AstralRelicStoreFirstEventModifier))
        {
            MainFile.Logger.Info(
                $"AstralRelicStore modifier install skipped | reason=already_present | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count}");
            return;
        }

        if (AddModifierDebugMethod == null)
            throw new InvalidOperationException("RunState.AddModifierDebug was not found.");

        var modifier = ModelDb.Modifier<AstralRelicStoreFirstEventModifier>().ToMutable();
        AddModifierDebugMethod.Invoke(runState, [modifier]);
        InitializeModifier(modifier, runState, loadedRun);

        MainFile.Logger.Info(
            $"AstralRelicStore modifier install applied | loadedRun={loadedRun} | modifiers={runState.Modifiers.Count}");
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
}
