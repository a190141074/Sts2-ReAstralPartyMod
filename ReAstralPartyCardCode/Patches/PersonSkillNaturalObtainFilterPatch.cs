using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class PersonSkillNaturalObtainFilterPatch : IPatchMethod
{
    public static string PatchId => "persona_skill_natural_obtain_filter";

    public static string Description => "Filter persona skill cards out of natural card obtain candidate lists";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards), [typeof(MegaCrit.Sts2.Core.Entities.Players.Player)])];
    }

    public static IEnumerable<CardModel> Postfix(IEnumerable<CardModel> __result, Player player)
    {
        return __result.Where(card => PersonSkillCardFilter.AllowNaturalObtain(card, player?.RunState)).ToList();
    }
}
