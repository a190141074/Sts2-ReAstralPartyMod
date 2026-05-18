using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class NControllerCardPlaySingleTargetingMixedSinglePatch : IPatchMethod
{
    private static readonly Func<NCardPlay, CardModel?> GetCard =
        AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));
    private static readonly Func<NCardPlay, NCard?> GetCardNode =
        AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));
    private static readonly Action<NCardPlay, NCreature> OnCreatureHover =
        AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureHover", [typeof(NCreature)]));
    private static readonly Action<NCardPlay, NCreature> OnCreatureUnhover =
        AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureUnhover", [typeof(NCreature)]));
    private static readonly Action<NCardPlay, MegaCrit.Sts2.Core.Entities.Creatures.Creature?> TryPlayCard =
        AccessTools.MethodDelegate<Action<NCardPlay, MegaCrit.Sts2.Core.Entities.Creatures.Creature?>>(AccessTools.DeclaredMethod(typeof(NCardPlay), "TryPlayCard", [typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature)]));

    public static string PatchId => "card_mixed_single_controller_single_targeting";
    public static string Description => "Provide mixed player and enemy target list for SkillFortuneMischance";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NControllerCardPlay), "SingleCreatureTargeting", [typeof(TargetType)])];
    }

    public static bool Prefix(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
    {
        var card = GetCard(__instance);
        if (!MixedSingleTargetingRuntime.IsMixedSingleTargetCard(card) || targetType != TargetType.AnyPlayer)
            return true;

        __result = RunTargeting(__instance);
        return false;
    }

    private static async Task RunTargeting(NControllerCardPlay instance)
    {
        var card = GetCard(instance);
        var cardNode = GetCardNode(instance);
        if (card == null || cardNode == null)
        {
            instance.CancelPlayCard();
            return;
        }

        var targetManager = NTargetManager.Instance;
        var hoverCallable = Callable.From((NCreature c) => OnCreatureHover(instance, c));
        var unhoverCallable = Callable.From((NCreature c) => OnCreatureUnhover(instance, c));
        targetManager.Connect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
        targetManager.Connect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);
        targetManager.StartTargeting(TargetType.AnyPlayer, cardNode, TargetMode.Controller,
            () => !GodotObject.IsInstanceValid(instance) || !NControllerManager.Instance!.IsUsingController, null);

        var nodes = MixedSingleTargetingRuntime.GetCandidates(card)
            .Select(c => NCombatRoom.Instance!.GetCreatureNode(c))
            .OfType<NCreature>()
            .ToList();
        if (nodes.Count == 0)
        {
            instance.CancelPlayCard();
            return;
        }

        NCombatRoom.Instance!.RestrictControllerNavigation(nodes.Select(n => n.Hitbox));
        nodes.First().Hitbox.GrabFocus();
        var selected = (NCreature?)await targetManager.SelectionFinished();

        if (GodotObject.IsInstanceValid(instance))
        {
            targetManager.Disconnect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
            targetManager.Disconnect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);
            if (selected != null)
                TryPlayCard(instance, selected.Entity);
            else
                instance.CancelPlayCard();
        }
    }
}
