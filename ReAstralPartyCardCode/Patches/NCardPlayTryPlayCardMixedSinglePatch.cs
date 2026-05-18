using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class NCardPlayTryPlayCardMixedSinglePatch : IPatchMethod
{
    private static readonly Func<NCardPlay, CardModel?> GetCard =
        AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

    private static readonly Action<NCardPlay, CardModel> CannotPlayThisCardFtueCheck =
        AccessTools.MethodDelegate<Action<NCardPlay, CardModel>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", [typeof(CardModel)]));

    private static readonly Action<NCardPlay, bool> InvokeCleanup =
        AccessTools.MethodDelegate<Action<NCardPlay, bool>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)])!);

    public static string PatchId => "card_mixed_single_try_play_card";
    public static string Description => "Keep manual selected target for SkillFortuneMischance";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
    }

    public static bool Prefix(NCardPlay __instance, Creature? target)
    {
        var card = GetCard(__instance);
        if (!MixedSingleTargetingRuntime.IsMixedSingleTargetCard(card))
            return true;

        if (target == null || !MixedSingleTargetingRuntime.IsValidTarget(target))
        {
            __instance.CancelPlayCard();
            return false;
        }

        if (!__instance.Holder.CardModel!.CanPlayTargeting(target))
        {
            CannotPlayThisCardFtueCheck(__instance, __instance.Holder.CardModel!);
            __instance.CancelPlayCard();
            return false;
        }

        var played = card!.TryManualPlay(target);
        if (!played)
        {
            __instance.CancelPlayCard();
            return false;
        }

        if (__instance.Holder.IsInsideTree())
        {
            var size = __instance.GetViewport().GetVisibleRect().Size;
            __instance.Holder.SetTargetPosition(new(size.X / 2f, size.Y - __instance.Holder.Size.Y));
        }

        InvokeCleanup(__instance, true);
        return false;
    }
}
