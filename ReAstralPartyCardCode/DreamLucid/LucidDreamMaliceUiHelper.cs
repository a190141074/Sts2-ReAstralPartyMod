using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

internal static class LucidDreamMaliceUiHelper
{
    public static HoverTip? BuildHoverTip(IRunState? runState)
    {
        if (runState == null || !ReAstralPartyModSettingsManager.HasAnyLucidDreamMaliceEnabled(runState))
            return null;

        var lines = new List<string>();

        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamFishScalesMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamSevereWoundOneMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamSevereWoundTwoMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamMadLifeMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamSwampOfFateMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamOverpopulationMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamCautiousJellyfishMalice(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.title");

        return BuildHoverTipFromEnabledTitles(lines);
    }

    public static HoverTip? BuildHoverTip(
        bool enableFishScalesMalice,
        bool enableSevereWoundOneMalice,
        bool enableSevereWoundTwoMalice,
        bool enableMadLifeMalice,
        bool enableSwampOfFateMalice,
        bool enableOverpopulationMalice,
        bool enableCautiousJellyfishMalice)
    {
        var lines = new List<string>();

        AddEnabledLine(lines, enableFishScalesMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.title");
        AddEnabledLine(lines, enableSevereWoundOneMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.title");
        AddEnabledLine(lines, enableSevereWoundTwoMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.title");
        AddEnabledLine(lines, enableMadLifeMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.title");
        AddEnabledLine(lines, enableSwampOfFateMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.title");
        AddEnabledLine(lines, enableOverpopulationMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.title");
        AddEnabledLine(lines, enableCautiousJellyfishMalice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.title");

        return BuildHoverTipFromEnabledTitles(lines);
    }

    private static HoverTip? BuildHoverTipFromEnabledTitles(IReadOnlyCollection<string> lines)
    {
        if (lines.Count == 0)
            return null;

        return new HoverTip(
            new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.title"),
            string.Join("\n", lines));
    }

    private static void AddEnabledLine(
        ICollection<string> lines,
        bool enabled,
        string titleKey)
    {
        if (!enabled)
            return;

        lines.Add("+" + new LocString("settings_ui", titleKey).GetRawText());
    }
}
