using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaSkillCardFilter
{
    public static bool IsPersonaSkill(CardModel? card)
    {
        if (card == null)
            return false;

        var type = card.GetType();
        return string.Equals(type.Namespace, "ReAstralPartyMod.ReAstralPartyCardCode.Cards", StringComparison.Ordinal)
               && type.Name.StartsWith("Skill", StringComparison.Ordinal);
    }

    public static bool AllowNaturalObtain(CardModel? card)
    {
        return !IsPersonaSkill(card);
    }

    public static bool AllowCompendiumDisplay(CardModel? card)
    {
        return CompatContentGate.IsCompendiumCardVisible(card);
    }

    public static Func<CardModel, bool> CreateCardPoolFilter()
    {
        return AllowNaturalObtain;
    }
}
