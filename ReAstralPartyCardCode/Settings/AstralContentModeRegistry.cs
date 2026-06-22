using System;
using System.Collections.Generic;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public enum AstralContentMode
{
    Vanilla = 0,
    Modpack = 1
}

public enum AstralGameplaySettingKey
{
    StartingInitialPoint = 0,
    StartingAstralRelicStore = 1,
    StartingPersonaSelection = 2,
    StartingPersonMode = 3,
    TokenSeriesMode = 4,
    AllPersonas = 5,
    ExtremeMode = 6,
    VariantPersonas = 7,
    AllVariantPersonas = 8,
    DreamSeriesEvents = 9,
    EnigmaticSeriesEvents = 10,
    MoonPropShopSlots = 11,
    MoonPropRelics = 12,
    JewelryRelics = 13,
    NeowExtraOption = 14,
    NeowExtraOptionSelectionMode = 15,
    LucidDream = 16,
    StartingRingOfSevenCurses = 17,
    CollectorsCards = 18
}

public enum AstralModeAvailability
{
    Editable = 0,
    LockedVisible = 1,
    Hidden = 2
}

public sealed record AstralGameplaySettingDefinition(
    AstralGameplaySettingKey Key,
    string SettingsEntryId,
    string LabelLocKey,
    string DescriptionLocKey,
    AstralModeAvailability VanillaAvailability,
    AstralModeAvailability RoomAvailabilityWhenVanilla);

public sealed record AstralContentModeDefinition(
    AstralContentMode Mode,
    string TitleLocKey,
    string DescriptionLocKey);

public static class AstralContentModeRegistry
{
    private static readonly AstralContentModeDefinition[] ModeDefinitions =
    [
        new(
            AstralContentMode.Vanilla,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode.vanilla.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode.vanilla.description"),
        new(
            AstralContentMode.Modpack,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode.modpack.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode.modpack.description")
    ];

    private static readonly AstralGameplaySettingDefinition[] SettingDefinitions =
    [
        new(
            AstralGameplaySettingKey.StartingInitialPoint,
            "enable_starting_initial_point",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.StartingAstralRelicStore,
            "enable_starting_astral_relic_store",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_astral_relic_store.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_astral_relic_store.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.StartingPersonaSelection,
            "enable_starting_persona_selection",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.StartingPersonMode,
            "starting_persona_mode",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.TokenSeriesMode,
            "token_series_mode",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.AllPersonas,
            "enable_all_personas",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.ExtremeMode,
            "enable_extreme_mode",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.description",
            AstralModeAvailability.Editable,
            AstralModeAvailability.Editable),
        new(
            AstralGameplaySettingKey.VariantPersonas,
            "enable_variant_personas",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_variant_personas.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_variant_personas.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.AllVariantPersonas,
            "enable_all_variant_personas",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.DreamSeriesEvents,
            "enable_dream_series_events",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.EnigmaticSeriesEvents,
            "enable_enigmatic_series_events",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.MoonPropShopSlots,
            "enable_moon_prop_shop_slots",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_shop_slots.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_shop_slots.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.MoonPropRelics,
            "enable_moon_prop_relics",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_relics.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_relics.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.JewelryRelics,
            "enable_jewelry_relics",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_jewelry_relics.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_jewelry_relics.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.NeowExtraOption,
            "enable_neow_extra_option",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.NeowExtraOptionSelectionMode,
            "neow_extra_option_selection_mode",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.LucidDream,
            "enable_lucid_dream",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_lucid_dream.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_lucid_dream.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.CollectorsCards,
            "enable_collectors_cards",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_collectors_cards.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_collectors_cards.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden),
        new(
            AstralGameplaySettingKey.StartingRingOfSevenCurses,
            "enable_starting_ring_of_seven_curses",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.label",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.description",
            AstralModeAvailability.LockedVisible,
            AstralModeAvailability.Hidden)
    ];

    private static readonly Dictionary<AstralGameplaySettingKey, AstralGameplaySettingDefinition> ByKey =
        BuildByKey();
    private static readonly Dictionary<string, AstralGameplaySettingDefinition> ByEntryId =
        BuildByEntryId();

    public static IReadOnlyList<AstralContentModeDefinition> Modes => ModeDefinitions;
    public static IReadOnlyList<AstralGameplaySettingDefinition> Settings => SettingDefinitions;

    public static AstralContentMode NormalizeMode(AstralContentMode mode)
    {
        return Enum.IsDefined(typeof(AstralContentMode), mode) ? mode : AstralContentMode.Vanilla;
    }

    public static AstralContentModeDefinition GetModeDefinition(AstralContentMode mode)
    {
        var normalized = NormalizeMode(mode);
        foreach (var definition in ModeDefinitions)
        {
            if (definition.Mode == normalized)
                return definition;
        }

        return ModeDefinitions[0];
    }

    public static AstralGameplaySettingDefinition GetSetting(AstralGameplaySettingKey key)
    {
        return ByKey[key];
    }

    public static bool TryGetSettingByEntryId(
        string entryId,
        out AstralGameplaySettingDefinition definition)
    {
        return ByEntryId.TryGetValue(entryId, out definition!);
    }

    public static bool IsEnabledByMode(
        AstralContentMode mode,
        AstralGameplaySettingKey key)
    {
        return GetAvailability(mode, key, roomSurface: false) == AstralModeAvailability.Editable;
    }

    public static AstralModeAvailability GetAvailability(
        AstralContentMode mode,
        AstralGameplaySettingKey key,
        bool roomSurface)
    {
        var definition = GetSetting(key);
        if (NormalizeMode(mode) == AstralContentMode.Modpack)
            return AstralModeAvailability.Editable;

        return roomSurface ? definition.RoomAvailabilityWhenVanilla : definition.VanillaAvailability;
    }

    private static Dictionary<AstralGameplaySettingKey, AstralGameplaySettingDefinition> BuildByKey()
    {
        var result = new Dictionary<AstralGameplaySettingKey, AstralGameplaySettingDefinition>();
        foreach (var definition in SettingDefinitions)
            result[definition.Key] = definition;

        return result;
    }

    private static Dictionary<string, AstralGameplaySettingDefinition> BuildByEntryId()
    {
        var result = new Dictionary<string, AstralGameplaySettingDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in SettingDefinitions)
            result[definition.SettingsEntryId] = definition;

        return result;
    }
}
