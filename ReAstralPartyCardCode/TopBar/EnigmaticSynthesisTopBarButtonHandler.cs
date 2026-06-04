using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Screens;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Screens;
using STS2RitsuLib.TopBar;

namespace ReAstralPartyMod.ReAstralPartyCardCode.TopBar;

[RegisterOwnedTopBarButton(
    LocalButtonStem,
    IconPath = IconPath,
    ButtonOrder = 0)]
public sealed class EnigmaticSynthesisTopBarButtonHandler : IModTopBarButtonHandler
{
    public const string LocalButtonStem = "enigmatic_synthesis_formula";
    public const string IconPath = "res://ReAstralPartyMod/images/ui/enigmatic_synthesis_formula.png";
    public static readonly string ButtonId = ModContentRegistry.GetQualifiedTopBarButtonId(MainFile.ModId, LocalButtonStem);

    public void OnClick(ModTopBarButtonContext context)
    {
        if (context.Player == null)
            return;

        context.ToggleCapstoneScreen(EnigmaticSynthesisRecipeScreen.GetOrCreate(context.Player));
    }

    public bool IsVisible(ModTopBarButtonContext context)
    {
        return context.Player?.GetRelic<EnigmaticSevenBlessings>() != null;
    }

    public bool IsOpen(ModTopBarButtonContext context)
    {
        return ModScreenService.CurrentCapstoneScreen is EnigmaticSynthesisRecipeScreen;
    }

    public int GetCount(ModTopBarButtonContext context)
    {
        return -1;
    }
}
