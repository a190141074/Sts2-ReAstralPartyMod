using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

public static class FinalArenaDistortionComponentModifierInstaller
{
    private static readonly MethodInfo? AddModifierDebugMethod =
        typeof(RunState).GetMethod(nameof(RunState.AddModifierDebug), BindingFlags.Instance | BindingFlags.Public);

    public static void EnsureInstalled(RunState? runState, bool loadedRun)
    {
        if (runState == null)
            return;

        if (runState.Modifiers.Any(static modifier => modifier is FinalArenaDistortionComponentModifier))
            return;

        if (AddModifierDebugMethod == null)
            throw new InvalidOperationException("RunState.AddModifierDebug was not found.");

        var modifier = ModelDb.Modifier<FinalArenaDistortionComponentModifier>().ToMutable();
        AddModifierDebugMethod.Invoke(runState, [modifier]);
        if (loadedRun)
            modifier.OnRunLoaded(runState);
        else
            modifier.OnRunCreated(runState);
    }
}
