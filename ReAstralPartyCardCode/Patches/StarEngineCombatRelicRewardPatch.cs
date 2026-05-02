using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(RelicReward), nameof(RelicReward.Populate))]
public static class StarEngineCombatRelicRewardPatch
{
    private static readonly FieldInfo RarityField =
        AccessTools.Field(typeof(RelicReward), "_rarity")
        ?? throw new MissingFieldException(typeof(RelicReward).FullName, "_rarity");

    private static readonly FieldInfo PredeterminedRelicField =
        AccessTools.Field(typeof(RelicReward), "_predeterminedRelic")
        ?? throw new MissingFieldException(typeof(RelicReward).FullName, "_predeterminedRelic");

    private static readonly FieldInfo RelicField =
        AccessTools.Field(typeof(RelicReward), "_relic")
        ?? throw new MissingFieldException(typeof(RelicReward).FullName, "_relic");

    private static readonly FieldInfo RngOverrideField =
        AccessTools.Field(typeof(Reward), "_rngOverride")
        ?? throw new MissingFieldException(typeof(Reward).FullName, "_rngOverride");

    [HarmonyPrefix]
    public static bool Prefix(RelicReward __instance, ref Task __result)
    {
        if (!ShouldUseTokenRelic(__instance))
            return true;

        var rarity = (RelicRarity)RarityField.GetValue(__instance)!;
        var rngOverride = (Rng?)RngOverrideField.GetValue(__instance);
        var selectionRng = rngOverride ?? __instance.Player.PlayerRng.Rewards;
        var rolledRarity = rarity == RelicRarity.None
            ? RelicFactory.RollRarity(selectionRng)
            : rarity;

        var tokenRelic =
            (TokenRelicRegistry.GetRandomTokenRelicForTreasure(rolledRarity, selectionRng)
             ?? RelicFactory.FallbackRelic)
            .ToMutable();

        PredeterminedRelicField.SetValue(__instance, tokenRelic);
        RelicField.SetValue(__instance, tokenRelic);
        __result = Task.CompletedTask;
        return false;
    }

    private static bool ShouldUseTokenRelic(RelicReward reward)
    {
        if (!StarEngineModifier.IsActive(reward.Player.RunState))
            return false;

        return reward.Player.RunState.CurrentRoom is CombatRoom or EventRoom;
    }
}
