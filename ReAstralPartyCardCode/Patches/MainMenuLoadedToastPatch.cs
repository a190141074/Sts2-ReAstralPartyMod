using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Ui.Toast;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class MainMenuLoadedToastPatch : IPatchMethod
{
    private static bool _shown;
    private static bool _telemetryShown;

    public static string PatchId => "main_menu_loaded_toast";

    public static string Description => "Main menu toast: show mod loaded after the main menu appears";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMainMenu), "_Ready")];
    }

    public static void Postfix()
    {
        if (_shown)
            return;

        _shown = true;
        Callable.From(ShowStartupToasts).CallDeferred();
    }

    private static void ShowStartupToasts()
    {
        RitsuToastService.ShowInfo("已加载", "星引擎模组·ReAstralPartyMod");
        ShowTelemetryToastIfEnabled();
    }

    private static void ShowTelemetryToastIfEnabled()
    {
        if (_telemetryShown)
            return;

        if (!ReAstralPartyModSettingsManager.EnableTelemetry)
            return;

        _telemetryShown = true;
        RitsuToastService.ShowInfo(
            "可以在【设置】-【ritsulib设置】-【星引擎模组】设置-【遥测】部分关闭",
            "已开启匿名数据收集用于模组平衡");
    }
}
