using HarmonyLib;
using System.Threading;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

 [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class TerraprismaDirectLoadPatch
{
    private static readonly AsyncLocal<int> FallbackDepth = new();

    [HarmonyPrefix]
    public static bool Prefix(
        CardModel __instance,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        ResourceInfo resources,
        bool skipCardPileVisuals,
        ref Task __result)
    {
        var power = __instance.Owner?.Creature?.GetPower<LoadTerraprismaPower>();
        if (FallbackDepth.Value > 0
            || power == null
            || !power.TryBeginDirectLoad(__instance, isAutoPlay))
            return true;

        __result = DirectLoadAndMoveAsync(
            power,
            __instance,
            choiceContext,
            target,
            resources,
            skipCardPileVisuals);
        return false;
    }

    private static async Task DirectLoadAndMoveAsync(
        LoadTerraprismaPower power,
        CardModel card,
        PlayerChoiceContext choiceContext,
        Creature? target,
        ResourceInfo resources,
        bool skipCardPileVisuals)
    {
        choiceContext.PushModel(card);
        var mounted = false;
        try
        {
            await CombatManager.Instance.WaitForUnpause();
            if (card.CombatState == null || card.Owner?.Creature?.IsDead == true)
            {
                await PlayOriginalAsync(card, choiceContext, target, false, resources, skipCardPileVisuals);
                return;
            }

            var owner = card.Owner;
            if (owner == null)
            {
                await PlayOriginalAsync(card, choiceContext, target, false, resources, skipCardPileVisuals);
                return;
            }

            await CardPileCmd.AddDuringManualCardPlay(card);

            mounted = await power.DirectLoadAsync(choiceContext, card);
            if (!mounted)
            {
                await PlayOriginalAsync(card, choiceContext, target, false, resources, skipCardPileVisuals);
                return;
            }
            await card.MoveToResultPileWithoutPlaying(choiceContext);
            await CombatManager.Instance.CheckForEmptyHand(choiceContext, owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"[Terraprisma] direct load failed | card={card.Id.Entry} | mounted={mounted} | error={ex}");
            if (!mounted)
            {
                power.CancelDirectLoad(card);
                await PlayOriginalAsync(card, choiceContext, target, false, resources, skipCardPileVisuals);
            }
        }
        finally
        {
            choiceContext.PopModel(card);
        }
    }

    private static async Task PlayOriginalAsync(
        CardModel card,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        ResourceInfo resources,
        bool skipCardPileVisuals)
    {
        FallbackDepth.Value++;
        try
        {
            await card.OnPlayWrapper(choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
        }
        finally
        {
            FallbackDepth.Value--;
        }
    }
}
