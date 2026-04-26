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

    [CustomEnum("ASTRAL_DICE_SET")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralDiceSet;

    [CustomEnum("ASTRAL_FLASHLIGHT_SET")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralFlashlightSet;

    [CustomEnum("ASTRAL_BOXING_GLOVES_SET")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralBoxingGlovesSet;

    [CustomEnum("ASTRAL_DREAMSHIP_SERIES")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralDreamshipSeries;

    [CustomEnum("ASTRAL_SPIRIT_FESTIVAL_SERIES")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralSpiritFestivalSeries;

    [CustomEnum("ASTRAL_WATER_TOWN_SERIES")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralWaterTownSeries;

    [CustomEnum("ASTRAL_MAGIC_ACADEMY_SERIES")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralMagicAcademySeries;

    [CustomEnum("ASTRAL_DRAGON_PALACE_SERIES")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralDragonPalaceSeries;

    [CustomEnum("ASTRAL_GHOST_ALLEY_SET")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralGhostAlleySet;

    [CustomEnum("ASTRAL_COLLECTION")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralCollection;

    [CustomEnum("ASTRAL_UNIQUE")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralUnique;

    [CustomEnum("ASTRAL_COOLDOWN")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralCooldown;

    [CustomEnum("ASTRAL_INVESTIGATION_PROGRESS")] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword AstralInvestigationProgress;
}