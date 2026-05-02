using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

[RegisterGoodModifier]
public class StarEngineModifier : ModifierModel
{
    public const string ModifierId = "RE_ASTRAL_PARTY_MOD_MODIFIER_STAR_ENGINE_MODE";

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    protected override string IconPath => "res://ReAstralPartyMod/images/astral_token.png";

    public override Func<Task>? GenerateNeowOption(EventModel eventModel)
    {
        return null;
    }

    public static bool IsActive(IRunState? runState)
    {
        return runState?.Modifiers.Any(modifier => modifier is StarEngineModifier) == true;
    }
}