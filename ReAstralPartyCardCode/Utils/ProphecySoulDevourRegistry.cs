using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ProphecySoulDevourRegistry
{
    private static readonly IReadOnlyList<ProphecySoulDevourDefinition> Definitions =
    [
        Create(ProphecySoulDevourKind.UndergroundTrade, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.TreasureHunting, ProphecySoulDevourPersistence.Permanent),
        Create(ProphecySoulDevourKind.MimicLarva, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.Ascension, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.GoldMiner, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.HumanWaveTactics, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.EventStop, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.EnergyConversion, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.FastFluctuation, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.PhaseReaction, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.DivineMiracle, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.MatrixRecycling, ProphecySoulDevourPersistence.Permanent),
        Create(ProphecySoulDevourKind.HiddenStrikeCard, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.HiddenStrikeRelic, ProphecySoulDevourPersistence.OneShot),
        Create(ProphecySoulDevourKind.AncientRuins, ProphecySoulDevourPersistence.Permanent),
        Create(ProphecySoulDevourKind.MineralRecovery, ProphecySoulDevourPersistence.Permanent),
        Create(ProphecySoulDevourKind.EnergySavingStrategy, ProphecySoulDevourPersistence.Permanent, true)
    ];

    private static readonly IReadOnlyDictionary<ProphecySoulDevourKind, ProphecySoulDevourDefinition> DefinitionsByKind =
        Definitions.ToDictionary(static definition => definition.Kind);

    public static IReadOnlyList<ProphecySoulDevourDefinition> All => Definitions;
    public static LocString SelectionHeader => new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_PROPHECY_SOUL_DEVOUR.selectionScreenHeader");
    public static LocString SelectionSubtitle => new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_PROPHECY_SOUL_DEVOUR.selectionScreenSubtitle");
    public static LocString RefreshButtonText => new("relics", "RE_ASTRAL_PARTY_MOD_RELIC_PROPHECY_SOUL_DEVOUR.selectionScreenRefresh");
    public static LocString AncientRuinsRestSiteDescription => new("rest_site_ui", "OPTION_RE_ASTRAL_PARTY_MOD_OPTION_PROPHECY_SOUL_DEVOUR_ANCIENT_RUINS.description");

    public static ProphecySoulDevourDefinition Get(ProphecySoulDevourKind kind)
    {
        if (!DefinitionsByKind.TryGetValue(kind, out var definition))
            throw new InvalidOperationException($"Unknown prophecy soul devour kind: {kind}");

        return definition;
    }

    public static bool TryGet(ProphecySoulDevourKind kind, out ProphecySoulDevourDefinition definition)
    {
        return DefinitionsByKind.TryGetValue(kind, out definition!);
    }

    private static ProphecySoulDevourDefinition Create(
        ProphecySoulDevourKind kind,
        ProphecySoulDevourPersistence persistence,
        bool allowRepeatPermanent = false)
    {
        var stem = kind switch
        {
            ProphecySoulDevourKind.UndergroundTrade => "UNDERGROUND_TRADE",
            ProphecySoulDevourKind.TreasureHunting => "TREASURE_HUNTING",
            ProphecySoulDevourKind.MimicLarva => "MIMIC_LARVA",
            ProphecySoulDevourKind.Ascension => "ASCENSION",
            ProphecySoulDevourKind.GoldMiner => "GOLD_MINER",
            ProphecySoulDevourKind.HumanWaveTactics => "HUMAN_WAVE_TACTICS",
            ProphecySoulDevourKind.EventStop => "EVENT_STOP",
            ProphecySoulDevourKind.EnergyConversion => "ENERGY_CONVERSION",
            ProphecySoulDevourKind.FastFluctuation => "FAST_FLUCTUATION",
            ProphecySoulDevourKind.PhaseReaction => "PHASE_REACTION",
            ProphecySoulDevourKind.DivineMiracle => "DIVINE_MIRACLE",
            ProphecySoulDevourKind.MatrixRecycling => "MATRIX_RECYCLING",
            ProphecySoulDevourKind.HiddenStrikeCard => "HIDDEN_STRIKE_CARD",
            ProphecySoulDevourKind.HiddenStrikeRelic => "HIDDEN_STRIKE_RELIC",
            ProphecySoulDevourKind.AncientRuins => "ANCIENT_RUINS",
            ProphecySoulDevourKind.MineralRecovery => "MINERAL_RECOVERY",
            ProphecySoulDevourKind.EnergySavingStrategy => "ENERGY_SAVING_STRATEGY",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return new ProphecySoulDevourDefinition(
            kind,
            persistence,
            allowRepeatPermanent,
            $"RE_ASTRAL_PARTY_MOD_PROPHECY_SOUL_DEVOUR_PROPHECY.{stem}.title",
            $"RE_ASTRAL_PARTY_MOD_PROPHECY_SOUL_DEVOUR_PROPHECY.{stem}.description");
    }
}
