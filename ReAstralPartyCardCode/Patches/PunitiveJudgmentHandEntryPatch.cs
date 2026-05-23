using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class PunitiveJudgmentHandEntryPatch : IPatchMethod
{
    public static string PatchId => "punitive_judgment_hand_entry_patch";

    public static string Description =>
        "Gameplay patch: upgrade and add replay to Punitive Judgment when the Sinkou set is complete and the card enters hand";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(CardModel), nameof(CardModel.AfterCardChangedPiles),
                [typeof(CardModel), typeof(PileType), typeof(AbstractModel)])
        ];
    }

    public static void Postfix(CardModel __instance, CardModel card, PileType oldPileType)
    {
        if (!ReferenceEquals(__instance, card))
            return;
        if (__instance.Pile?.Type != PileType.Hand || oldPileType == PileType.Hand)
            return;
        if (__instance.Owner?.GetRelic<Relics.JewelryEchoOfDivineLight>() == null)
            return;
        if (!SinkouSetHelper.HasFullListeningToSolarRoarSet(__instance.Owner))
            return;

        SinkouSetHelper.UpgradePunitiveJudgmentInHand(__instance).GetAwaiter().GetResult();
    }
}
