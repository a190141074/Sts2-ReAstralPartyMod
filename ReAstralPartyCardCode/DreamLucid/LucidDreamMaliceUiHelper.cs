using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

internal static class LucidDreamMaliceUiHelper
{
    public static HoverTip? BuildHoverTip(IRunState? runState)
    {
        if (runState == null || !ReAstralPartyModSettingsManager.HasAnyLucidDreamEnabled(runState))
            return null;

        var lines = new List<string>();

        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamFalseLifeline(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.false_lifeline.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamSmoothSailing(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.smooth_sailing.title");
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
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamFaceDeathWithComposure(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.face_death_with_composure.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamWildness(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamWildnessPhantom(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness_phantom.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamPitchBlackImpulse(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.pitch_black_impulse.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamBubblePotionOfDreams(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.bubble_potion_of_dreams.title");
        AddEnabledLine(lines, ReAstralPartyModSettingsManager.GetEnableLucidDreamHarmlessWhisper(runState),
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.harmless_whisper.title");

        return BuildHoverTipFromEnabledTitles(lines);
    }

    public static HoverTip? BuildHoverTip(
        bool enableFalseLifeline,
        bool enableSmoothSailing,
        bool enableFishScalesMalice,
        bool enableSevereWoundOneMalice,
        bool enableSevereWoundTwoMalice,
        bool enableMadLifeMalice,
        bool enableSwampOfFateMalice,
        bool enableOverpopulationMalice,
        bool enableCautiousJellyfishMalice,
        bool enableFaceDeathWithComposure,
        bool enableWildness,
        bool enableWildnessPhantom,
        bool enablePitchBlackImpulse,
        bool enableBubblePotionOfDreams,
        bool enableHarmlessWhisper)
    {
        var lines = new List<string>();

        AddEnabledLine(lines, enableFalseLifeline,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.false_lifeline.title");
        AddEnabledLine(lines, enableSmoothSailing,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.smooth_sailing.title");
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
        AddEnabledLine(lines, enableFaceDeathWithComposure,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.face_death_with_composure.title");
        AddEnabledLine(lines, enableWildness,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness.title");
        AddEnabledLine(lines, enableWildnessPhantom,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness_phantom.title");
        AddEnabledLine(lines, enablePitchBlackImpulse,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.pitch_black_impulse.title");
        AddEnabledLine(lines, enableBubblePotionOfDreams,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.bubble_potion_of_dreams.title");
        AddEnabledLine(lines, enableHarmlessWhisper,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.harmless_whisper.title");

        return BuildHoverTipFromEnabledTitles(lines);
    }

    private static HoverTip? BuildHoverTipFromEnabledTitles(IReadOnlyCollection<string> lines)
    {
        if (lines.Count == 0)
            return null;

        return new HoverTip(
            new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream.title"),
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
