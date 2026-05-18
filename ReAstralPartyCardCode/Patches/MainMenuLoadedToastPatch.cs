using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Ui.Toast;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class MainMenuLoadedToastPatch : IPatchMethod
{
    private static bool _shown;

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
        Callable.From(() => RitsuToastService.ShowInfo("已加载", "星引擎模组·ReAstralPartyMod")).CallDeferred();
    }
}
