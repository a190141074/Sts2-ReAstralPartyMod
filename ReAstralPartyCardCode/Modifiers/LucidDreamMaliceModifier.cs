using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

[RegisterGoodModifier]
public sealed class LucidDreamMaliceModifier : ModifierModel
{
    [SavedProperty]
    public bool EnableFishScalesMalice { get; set; }

    [SavedProperty]
    public bool EnableSevereWoundOneMalice { get; set; }

    [SavedProperty]
    public bool EnableSevereWoundTwoMalice { get; set; }

    [SavedProperty]
    public bool EnableMadLifeMalice { get; set; }

    [SavedProperty]
    public bool EnableSwampOfFateMalice { get; set; }

    [SavedProperty]
    public bool EnableOverpopulationMalice { get; set; }

    [SavedProperty]
    public bool EnableCautiousJellyfishMalice { get; set; }

    public override bool ShouldReceiveCombatHooks => false;

    public bool HasAnyEnabled =>
        EnableFishScalesMalice
        || EnableSevereWoundOneMalice
        || EnableSevereWoundTwoMalice
        || EnableMadLifeMalice
        || EnableSwampOfFateMalice
        || EnableOverpopulationMalice
        || EnableCautiousJellyfishMalice;

    public void ApplySnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        EnableFishScalesMalice = snapshot.EnableLucidDreamFishScalesMalice;
        EnableSevereWoundOneMalice = snapshot.EnableLucidDreamSevereWoundOneMalice;
        EnableSevereWoundTwoMalice = snapshot.EnableLucidDreamSevereWoundTwoMalice;
        EnableMadLifeMalice = snapshot.EnableLucidDreamMadLifeMalice;
        EnableSwampOfFateMalice = snapshot.EnableLucidDreamSwampOfFateMalice;
        EnableOverpopulationMalice = snapshot.EnableLucidDreamOverpopulationMalice;
        EnableCautiousJellyfishMalice = snapshot.EnableLucidDreamCautiousJellyfishMalice;
    }

    public static LucidDreamMaliceModifier? Get(RunState? runState)
    {
        return runState?.Modifiers.OfType<LucidDreamMaliceModifier>().FirstOrDefault();
    }
}
