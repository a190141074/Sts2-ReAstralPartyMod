using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace AstralPartyMod.AstralPartyCardCode.Keywords;

public static class AstralKeywords
{
    [CustomEnum("ASTRAL_EVENT")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralEvent;

    [CustomEnum("ASTRAL_STEPS")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralSteps;

    [CustomEnum("ASTRAL_TEMPORARY")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralTemporary;
}