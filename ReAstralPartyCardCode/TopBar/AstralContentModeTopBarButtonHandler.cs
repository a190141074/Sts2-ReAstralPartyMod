using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Screens;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Screens;
using STS2RitsuLib.TopBar;

namespace ReAstralPartyMod.ReAstralPartyCardCode.TopBar;

[RegisterOwnedTopBarButton(
    LocalButtonStem,
    IconPath = IconPath,
    ButtonOrder = 1)]
public sealed class AstralContentModeTopBarButtonHandler : IModTopBarButtonHandler
{
    public const string LocalButtonStem = "global_switching_settings";
    public const string IconPath = "res://ReAstralPartyMod/images/ui/global_switching_settings.png";
    public static readonly string ButtonId =
        ModContentRegistry.GetQualifiedTopBarButtonId(MainFile.ModId, LocalButtonStem);

    public void OnClick(ModTopBarButtonContext context)
    {
        if (context.Player == null)
            return;

        context.ToggleCapstoneScreen(AstralContentModeSwitchScreen.GetOrCreate());
    }

    public bool IsVisible(ModTopBarButtonContext context)
    {
        return context.Player != null;
    }

    public bool IsOpen(ModTopBarButtonContext context)
    {
        return ModScreenService.CurrentCapstoneScreen is AstralContentModeSwitchScreen;
    }

    public int GetCount(ModTopBarButtonContext context)
    {
        return -1;
    }
}
