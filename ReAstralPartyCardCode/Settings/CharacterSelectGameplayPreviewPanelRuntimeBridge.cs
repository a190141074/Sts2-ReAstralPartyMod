using System;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

internal static class CharacterSelectGameplayPreviewPanelRuntimeBridge
{
    public static event Action? RefreshRequested;

    public static void RequestRefreshAll()
    {
        RefreshRequested?.Invoke();
    }
}

