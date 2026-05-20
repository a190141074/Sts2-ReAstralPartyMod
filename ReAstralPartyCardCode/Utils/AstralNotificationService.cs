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

public enum AstralNotificationArea
{
    Startup,
    Settings,
    Telemetry,
    PersonaSelection,
    Gameplay,
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

    public static void ShowDiagnosticInfo(
        AstralNotificationModule module,
        AstralNotificationArea area,
        int number,
        string body,
        string? stage = null)
    {
        ShowInfo(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage));
    }

    public static void ShowDiagnosticWarning(
        AstralNotificationModule module,
        AstralNotificationArea area,
        int number,
        string body,
        string? stage = null)
    {
        ShowWarning(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage));
    }

    public static void ShowDiagnosticError(
        AstralNotificationModule module,
        AstralNotificationArea area,
        int number,
        string body,
        string? stage = null)
    {
        ShowError(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage));
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

    private static string BuildDiagnosticTitle(AstralNotificationArea area, int number, string? stage)
    {
        var prefix = GetAreaPrefix(area);
        var areaTitle = GetAreaTitle(area);
        return string.IsNullOrWhiteSpace(stage)
            ? $"【{prefix}{number:000}】{areaTitle}"
            : $"【{prefix}{number:000}】{areaTitle}/{stage}";
    }

    private static string FormatDiagnosticBody(string body, string? stage)
    {
        return string.IsNullOrWhiteSpace(stage)
            ? body
            : $"{body}\n阶段：{stage}";
    }

    private static string GetAreaPrefix(AstralNotificationArea area)
    {
        return area switch
        {
            AstralNotificationArea.Startup => "U",
            AstralNotificationArea.Settings => "S",
            AstralNotificationArea.Telemetry => "T",
            AstralNotificationArea.PersonaSelection => "P",
            AstralNotificationArea.Gameplay => "G",
            AstralNotificationArea.Multiplayer => "M",
            _ => "X"
        };
    }

    private static string GetAreaTitle(AstralNotificationArea area)
    {
        return area switch
        {
            AstralNotificationArea.Startup => "启动",
            AstralNotificationArea.Settings => "设置",
            AstralNotificationArea.Telemetry => "遥测",
            AstralNotificationArea.PersonaSelection => "人格选择",
            AstralNotificationArea.Gameplay => "玩法",
            AstralNotificationArea.Multiplayer => "联机",
            _ => "通知"
        };
    }
}
