using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CardCmdAutoPlayMixedSinglePatch : IPatchMethod
{
    public static string PatchId => "card_mixed_single_auto_play";
    public static string Description => "Resolve null target autoplayer fallback for SkillFortuneMischance";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardCmd), nameof(CardCmd.AutoPlay))];
    }

    public static void Prefix(MegaCrit.Sts2.Core.Models.CardModel card, ref Creature? target)
    {
        if (!MixedSingleTargetingRuntime.IsMixedSingleTargetCard(card) || target != null)
            return;

        var candidates = MixedSingleTargetingRuntime.GetCandidates(card);
        if (candidates.Count == 0)
            return;

        target = card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
    }
}
