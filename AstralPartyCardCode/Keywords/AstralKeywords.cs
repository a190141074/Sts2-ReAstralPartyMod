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

    [CustomEnum("ASTRAL_MIXED")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralMixed;

    [CustomEnum("ASTRAL_ETERNAL_STARLIGHT_SET")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralEternalStarlightSet;

    [CustomEnum("SOLAR_BOMBARDMENT")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword SolarBombardment;

    [CustomEnum("ASTRAL_ORBITAL_BOMBARDMENT_MATERIAL")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralOrbitalBombardmentMaterial;
}
