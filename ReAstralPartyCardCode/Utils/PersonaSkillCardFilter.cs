using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

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
        return AllowNaturalObtain(card, null);
    }

    public static bool IsCollectorsCard(CardModel? card)
    {
        if (card == null)
            return false;

        var type = card.GetType();
        return string.Equals(type.Namespace, "ReAstralPartyMod.ReAstralPartyCardCode.Cards", StringComparison.Ordinal)
               && type.Name.StartsWith("CollectorsCard", StringComparison.Ordinal);
    }

    public static bool AllowNaturalObtain(CardModel? card, IRunState? runState)
    {
        if (IsPersonaSkill(card))
            return false;

        if (IsCollectorsCard(card) && !ReAstralPartyModSettingsManager.GetEnableCollectorsCards(runState))
            return false;

        return true;
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
