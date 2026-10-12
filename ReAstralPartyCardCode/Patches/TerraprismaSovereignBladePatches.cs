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
            isAutoPlay,
            resources,
            skipCardPileVisuals);
        return false;
    }

    private static async Task DirectLoadAndMoveAsync(
        LoadTerraprismaPower power,
        CardModel card,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        ResourceInfo resources,
        bool skipCardPileVisuals)
    {
        var contextPushed = false;
        choiceContext.PushModel(card);
        contextPushed = true;
        var mounted = false;
        var owner = card.Owner;
        try
        {
            await CombatManager.Instance.WaitForUnpause();
            if (card.CombatState == null || card.Owner?.Creature?.IsDead == true)
            {
                await PlayOriginalAsync(card, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
                return;
            }

            if (owner == null)
            {
                await PlayOriginalAsync(card, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
                return;
            }

            await CardPileCmd.AddDuringManualCardPlay(card);

            mounted = await power.DirectLoadAsync(choiceContext, card);
            if (!mounted)
            {
                await PlayOriginalAsync(card, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
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
                await PlayOriginalAsync(card, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
                return;
            }

            // A successful mount replaces the original card's effect, but the
            // original card still has to leave the play pile before the action
            // can finish. Retry the official result-pile path after a partial
            // failure so the card cannot remain in the play area.
            try
            {
                await card.MoveToResultPileWithoutPlaying(choiceContext);
                if (owner != null)
                    await CombatManager.Instance.CheckForEmptyHand(choiceContext, owner);
            }
            catch (Exception recoveryEx)
            {
                MainFile.Logger.Error(
                    $"[Terraprisma] mounted card recovery failed | card={card.Id.Entry} | " +
                    $"pile={card.Pile?.Type.ToString() ?? "None"} | error={recoveryEx}");
                // Do not let the action appear successful after the official
                // result-pile transition failed. The caller must observe the
                // fault and close the action instead of leaving a card in an
                // indeterminate play-pile state.
                throw;
            }
        }
        finally
        {
            if (contextPushed)
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
