using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonSkillCardFilter
{
    public static bool IsPersonSkill(CardModel? card)
    {
        if (card == null)
            return false;

        return AstralCardTags.HasPersonSkillTag(card);
    }

    public static bool AllowNaturalObtain(CardModel? card)
    {
        return AllowNaturalObtain(card, null);
    }

    public static bool IsCollectorsCard(CardModel? card)
    {
        return AstralCardTags.HasCollectorsTag(card);
    }

    public static bool AllowNaturalObtain(CardModel? card, IRunState? runState)
    {
        if (IsPersonSkill(card))
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
