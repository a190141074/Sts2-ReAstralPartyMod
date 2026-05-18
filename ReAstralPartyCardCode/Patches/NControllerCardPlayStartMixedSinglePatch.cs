using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class NControllerCardPlayStartMixedSinglePatch : IPatchMethod
{
    private static readonly Func<NCardPlay, CardModel?> GetCard =
        AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));
    private static readonly Func<NCardPlay, NCard?> GetCardNode =
        AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));
    private static readonly Action<NCardPlay> TryShowEvokingOrbs =
        AccessTools.MethodDelegate<Action<NCardPlay>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "TryShowEvokingOrbs"));
    private static readonly Action<NCardPlay> CenterCard =
        AccessTools.MethodDelegate<Action<NCardPlay>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "CenterCard"));
    private static readonly Action<NCardPlay, CardModel> CannotPlayThisCardFtueCheck =
        AccessTools.MethodDelegate<Action<NCardPlay, CardModel>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", [typeof(CardModel)]));
    private static readonly MethodInfo SingleCreatureTargetingMethod =
        AccessTools.DeclaredMethod(typeof(NControllerCardPlay), "SingleCreatureTargeting", [typeof(TargetType)]);

    public static string PatchId => "card_mixed_single_controller_start";
    public static string Description => "Route SkillFortuneMischance to controller single target mode";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start), Type.EmptyTypes)];
    }

    public static bool Prefix(NControllerCardPlay __instance)
    {
        var card = GetCard(__instance);
        if (!MixedSingleTargetingRuntime.IsMixedSingleTargetCard(card))
            return true;

        var cardNode = GetCardNode(__instance);
        if (card == null || cardNode == null)
            return false;

        NDebugAudioManager.Instance?.Play("card_select.mp3");
        NHoverTipSet.Remove(__instance.Holder);

        if (!card.CanPlay(out _, out _))
        {
            CannotPlayThisCardFtueCheck(__instance, card);
            __instance.CancelPlayCard();
            return false;
        }

        TryShowEvokingOrbs(__instance);
        cardNode.CardHighlight.AnimFlash();
        CenterCard(__instance);
        TaskHelper.RunSafely((Task)SingleCreatureTargetingMethod.Invoke(__instance, [TargetType.AnyPlayer])!);
        return false;
    }
}
