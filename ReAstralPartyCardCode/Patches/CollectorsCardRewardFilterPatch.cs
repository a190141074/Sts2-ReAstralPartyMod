using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Patching.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CollectorsCardRewardFilterPatch : IPatchMethod
{
    private static readonly FieldInfo? CardsField =
        AccessTools.Field(typeof(CardReward), "_cards");

    private static readonly FieldInfo? CurrentScreenField =
        AccessTools.Field(typeof(CardReward), "_currentlyShownScreen");

    private static readonly MethodInfo? RefreshOptionsMethod =
        AccessTools.Method(
            typeof(MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardRewardSelectionScreen),
            "RefreshOptions");

    private static readonly MethodInfo? GenerateAlternativesMethod =
        AccessTools.Method(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate), [typeof(CardReward)]);

    public static string PatchId => "collectors_card_reward_filter";

    public static string Description => "Filter disabled collectors cards out of final card reward results";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardReward), nameof(CardReward.Populate), Type.EmptyTypes)];
    }

    public static void Postfix(CardReward __instance)
    {
        if (CardsField?.GetValue(__instance) is not IList cardsList)
            return;

        var player = __instance.Player;
        if (player == null)
            return;

        var removedAny = false;
        for (var index = cardsList.Count - 1; index >= 0; index--)
        {
            if (cardsList[index] is not CardCreationResult result)
                continue;
            if (PersonSkillCardFilter.AllowNaturalObtain(result.Card, player.RunState))
                continue;

            cardsList.RemoveAt(index);
            removedAny = true;
        }

        if (!removedAny)
            return;

        MainFile.Logger.Info(
            $"[{MainFile.ModId}] Filtered disabled persona/collectors cards from final reward results for player {player.NetId}.");

        if (CurrentScreenField?.GetValue(__instance) is not { } screen)
            return;
        if (RefreshOptionsMethod == null || GenerateAlternativesMethod == null)
            return;

        var rewardCards = cardsList.Cast<CardCreationResult>().ToList();
        var alternatives = GenerateAlternativesMethod.Invoke(null, [__instance]);
        RefreshOptionsMethod.Invoke(screen, [rewardCards, alternatives!]);
    }
}
