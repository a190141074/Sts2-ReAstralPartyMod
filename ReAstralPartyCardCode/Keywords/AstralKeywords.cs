using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Keywords;

public static class AstralKeywords
{
    private static readonly string[] OwnedKeywordStems =
    [
        AstralEventStem,
        AstralStepsStem,
        AstralTemporaryStem,
        AstralMixedStem,
        AstralEternalStarlightSetStem,
        SolarBombardmentStem,
        AstralOrbitalBombardmentMaterialStem,
        AstralDiceSetStem,
        AstralFlashlightSetStem,
        AstralBoxingGlovesSetStem,
        AstralDreamshipSeriesStem,
        AstralSpiritFestivalSeriesStem,
        AstralWaterTownSeriesStem,
        AstralMagicAcademySeriesStem,
        AstralDragonPalaceSeriesStem,
        AstralGhostAlleySetStem,
        AstralCollectionStem,
        AstralUniqueStem,
        AstralCooldownStem,
        AstralInvestigationProgressStem,
        AstralFusionClueStem
    ];

    private static bool _registered;

    public const string AstralEventStem = "ASTRAL_EVENT";
    public const string AstralStepsStem = "ASTRAL_STEPS";
    public const string AstralTemporaryStem = "ASTRAL_TEMPORARY";
    public const string AstralMixedStem = "ASTRAL_MIXED";
    public const string AstralEternalStarlightSetStem = "ASTRAL_ETERNAL_STARLIGHT_SET";
    public const string SolarBombardmentStem = "SOLAR_BOMBARDMENT";
    public const string AstralOrbitalBombardmentMaterialStem = "ASTRAL_ORBITAL_BOMBARDMENT_MATERIAL";
    public const string AstralDiceSetStem = "ASTRAL_DICE_SET";
    public const string AstralFlashlightSetStem = "ASTRAL_FLASHLIGHT_SET";
    public const string AstralBoxingGlovesSetStem = "ASTRAL_BOXING_GLOVES_SET";
    public const string AstralDreamshipSeriesStem = "ASTRAL_DREAMSHIP_SERIES";
    public const string AstralSpiritFestivalSeriesStem = "ASTRAL_SPIRIT_FESTIVAL_SERIES";
    public const string AstralWaterTownSeriesStem = "ASTRAL_WATER_TOWN_SERIES";
    public const string AstralMagicAcademySeriesStem = "ASTRAL_MAGIC_ACADEMY_SERIES";
    public const string AstralDragonPalaceSeriesStem = "ASTRAL_DRAGON_PALACE_SERIES";
    public const string AstralGhostAlleySetStem = "ASTRAL_GHOST_ALLEY_SET";
    public const string AstralCollectionStem = "ASTRAL_COLLECTION";
    public const string AstralUniqueStem = "ASTRAL_UNIQUE";
    public const string AstralCooldownStem = "ASTRAL_COOLDOWN";
    public const string AstralInvestigationProgressStem = "ASTRAL_INVESTIGATION_PROGRESS";
    public const string AstralFusionClueStem = "ASTRAL_FUSION_CLUE";

    public static string AstralEventId => GetId(AstralEventStem);
    public static string AstralStepsId => GetId(AstralStepsStem);
    public static string AstralTemporaryId => GetId(AstralTemporaryStem);
    public static string AstralMixedId => GetId(AstralMixedStem);
    public static string AstralEternalStarlightSetId => GetId(AstralEternalStarlightSetStem);
    public static string SolarBombardmentId => GetId(SolarBombardmentStem);
    public static string AstralOrbitalBombardmentMaterialId => GetId(AstralOrbitalBombardmentMaterialStem);
    public static string AstralDiceSetId => GetId(AstralDiceSetStem);
    public static string AstralFlashlightSetId => GetId(AstralFlashlightSetStem);
    public static string AstralBoxingGlovesSetId => GetId(AstralBoxingGlovesSetStem);
    public static string AstralDreamshipSeriesId => GetId(AstralDreamshipSeriesStem);
    public static string AstralSpiritFestivalSeriesId => GetId(AstralSpiritFestivalSeriesStem);
    public static string AstralWaterTownSeriesId => GetId(AstralWaterTownSeriesStem);
    public static string AstralMagicAcademySeriesId => GetId(AstralMagicAcademySeriesStem);
    public static string AstralDragonPalaceSeriesId => GetId(AstralDragonPalaceSeriesStem);
    public static string AstralGhostAlleySetId => GetId(AstralGhostAlleySetStem);
    public static string AstralCollectionId => GetId(AstralCollectionStem);
    public static string AstralUniqueId => GetId(AstralUniqueStem);
    public static string AstralCooldownId => GetId(AstralCooldownStem);
    public static string AstralInvestigationProgressId => GetId(AstralInvestigationProgressStem);
    public static string AstralFusionClueId => GetId(AstralFusionClueStem);

    public static CardKeyword AstralEvent => GetKeyword(AstralEventStem);
    public static CardKeyword AstralSteps => GetKeyword(AstralStepsStem);
    public static CardKeyword AstralTemporary => GetKeyword(AstralTemporaryStem);
    public static CardKeyword AstralMixed => GetKeyword(AstralMixedStem);
    public static CardKeyword AstralEternalStarlightSet => GetKeyword(AstralEternalStarlightSetStem);
    public static CardKeyword SolarBombardment => GetKeyword(SolarBombardmentStem);
    public static CardKeyword AstralOrbitalBombardmentMaterial => GetKeyword(AstralOrbitalBombardmentMaterialStem);
    public static CardKeyword AstralDiceSet => GetKeyword(AstralDiceSetStem);
    public static CardKeyword AstralFlashlightSet => GetKeyword(AstralFlashlightSetStem);
    public static CardKeyword AstralBoxingGlovesSet => GetKeyword(AstralBoxingGlovesSetStem);
    public static CardKeyword AstralDreamshipSeries => GetKeyword(AstralDreamshipSeriesStem);
    public static CardKeyword AstralSpiritFestivalSeries => GetKeyword(AstralSpiritFestivalSeriesStem);
    public static CardKeyword AstralWaterTownSeries => GetKeyword(AstralWaterTownSeriesStem);
    public static CardKeyword AstralMagicAcademySeries => GetKeyword(AstralMagicAcademySeriesStem);
    public static CardKeyword AstralDragonPalaceSeries => GetKeyword(AstralDragonPalaceSeriesStem);
    public static CardKeyword AstralGhostAlleySet => GetKeyword(AstralGhostAlleySetStem);
    public static CardKeyword AstralCollection => GetKeyword(AstralCollectionStem);
    public static CardKeyword AstralUnique => GetKeyword(AstralUniqueStem);
    public static CardKeyword AstralCooldown => GetKeyword(AstralCooldownStem);
    public static CardKeyword AstralInvestigationProgress => GetKeyword(AstralInvestigationProgressStem);
    public static CardKeyword AstralFusionClue => GetKeyword(AstralFusionClueStem);

    public static void RegisterAll()
    {
        if (_registered)
            return;

        var registry = ModKeywordRegistry.For(MainFile.ModId);
        foreach (var stem in OwnedKeywordStems)
            registry.RegisterCardKeywordOwnedByLocNamespace(stem);

        _registered = true;
    }

    public static string GetId(string stem)
    {
        return ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, stem);
    }

    public static CardKeyword GetKeyword(string stem)
    {
        return ModKeywordRegistry.GetCardKeyword(GetId(stem));
    }

    public static IHoverTip CreateHoverTip(string id)
    {
        return ModKeywordRegistry.CreateHoverTip(id);
    }
}
