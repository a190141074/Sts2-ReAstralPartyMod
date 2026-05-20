using Godot;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Ui.Toast;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public enum AstralNotificationModule
{
    Startup,
    Settings,
    Telemetry,
    Multiplayer
}

internal static class AstralNotificationService
{
    public static void ShowInfo(AstralNotificationModule module, string body, string? title = null)
    {
        if (!IsEnabled(module))
            return;

        Callable.From(() => RitsuToastService.ShowInfo(body, title)).CallDeferred();
    }

    public static void ShowWarning(AstralNotificationModule module, string body, string? title = null)
    {
        if (!IsEnabled(module))
            return;

        Callable.From(() => RitsuToastService.ShowWarning(body, title)).CallDeferred();
    }

    public static void ShowError(AstralNotificationModule module, string body, string? title = null)
    {
        if (!IsEnabled(module))
            return;

        Callable.From(() => RitsuToastService.ShowError(body, title)).CallDeferred();
    }

    private static bool IsEnabled(AstralNotificationModule module)
    {
        return module switch
        {
            AstralNotificationModule.Startup => ReAstralPartyModSettingsManager.EnableStartupNotifications,
            AstralNotificationModule.Settings => ReAstralPartyModSettingsManager.EnableSettingsNotifications,
            AstralNotificationModule.Telemetry => ReAstralPartyModSettingsManager.EnableTelemetryNotifications,
            AstralNotificationModule.Multiplayer => ReAstralPartyModSettingsManager.EnableMultiplayerNotifications,
            _ => true
        };
    }
}
