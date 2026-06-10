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
    PersonaRelic,
    TokenRelic,
    NeowDiagnostics,
    LucidDreamDiagnostics,
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
        RunWithArea(area,
            () => ShowInfo(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage)));
    }

    public static void ShowDiagnosticWarning(
        AstralNotificationModule module,
        AstralNotificationArea area,
        int number,
        string body,
        string? stage = null)
    {
        RunWithArea(area,
            () => ShowWarning(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage)));
    }

    public static void ShowDiagnosticError(
        AstralNotificationModule module,
        AstralNotificationArea area,
        int number,
        string body,
        string? stage = null)
    {
        RunWithArea(area,
            () => ShowError(module, FormatDiagnosticBody(body, stage), BuildDiagnosticTitle(area, number, stage)));
    }

    private static bool IsEnabled(AstralNotificationModule module)
    {
        return module switch
        {
            AstralNotificationModule.Startup => ReAstralPartyModSettingsManager.EnableStartupNotifications,
            AstralNotificationModule.Settings => ReAstralPartyModSettingsManager.EnableSettingsNotifications,
            AstralNotificationModule.Telemetry => ReAstralPartyModSettingsManager.EnableTelemetryNotifications,
            AstralNotificationModule.Multiplayer => IsMultiplayerAreaEnabledForCurrentCall(),
            _ => true
        };
    }

    [ThreadStatic] private static AstralNotificationArea? _activeAreaOverride;

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
            AstralNotificationArea.PersonaRelic => "P",
            AstralNotificationArea.TokenRelic => "R",
            AstralNotificationArea.NeowDiagnostics => "N",
            AstralNotificationArea.LucidDreamDiagnostics => "D",
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
            AstralNotificationArea.PersonaRelic => "人格遗物",
            AstralNotificationArea.TokenRelic => "筹码遗物",
            AstralNotificationArea.NeowDiagnostics => "联机诊断",
            AstralNotificationArea.LucidDreamDiagnostics => "清醒梦诊断",
            AstralNotificationArea.Gameplay => "玩法",
            AstralNotificationArea.Multiplayer => "联机",
            _ => "通知"
        };
    }

    private static bool IsMultiplayerAreaEnabledForCurrentCall()
    {
        return _activeAreaOverride switch
        {
            AstralNotificationArea.PersonaRelic => ReAstralPartyModSettingsManager.EnablePersonaRelicNotifications,
            AstralNotificationArea.TokenRelic => ReAstralPartyModSettingsManager.EnableTokenRelicNotifications,
            AstralNotificationArea.NeowDiagnostics => ReAstralPartyModSettingsManager.EnableNeowDiagnosticsNotifications,
            AstralNotificationArea.LucidDreamDiagnostics => ReAstralPartyModSettingsManager.EnableNeowDiagnosticsNotifications,
            _ => ReAstralPartyModSettingsManager.EnableMultiplayerNotifications
        };
    }

    private static void RunWithArea(AstralNotificationArea area, Action action)
    {
        var previous = _activeAreaOverride;
        _activeAreaOverride = area;
        try
        {
            action();
        }
        finally
        {
            _activeAreaOverride = previous;
        }
    }
}
