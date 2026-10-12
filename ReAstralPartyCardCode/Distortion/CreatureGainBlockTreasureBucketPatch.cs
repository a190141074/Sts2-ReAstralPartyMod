using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

public sealed class CreatureGainBlockTreasureBucketPatch : IPatchMethod
{
    public static string PatchId => "creature_gain_block_treasure_bucket";

    public static string Description => "Prevent block gain after Treasure Bucket revival has triggered";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock))];
    }

    public static bool Prefix(Creature creature, ref decimal amount)
    {
        if (amount <= 0m)
            return true;
        if (!TreasureBucketPower.ShouldPreventBlockGain(creature))
            return true;

        amount = 0m;
        return true;
    }
}
