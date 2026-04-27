using AstralPartyMod.AstralPartyCardCode.Modifiers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), typeof(RelicModel), typeof(Player), typeof(int))]
public static class StarEngineEventRelicObtainPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref RelicModel relic, Player player)
    {
        if (!StarEngineModifier.IsActive(player.RunState))
            return;
        if (player.RunState.CurrentRoom is not EventRoom)
            return;
        if (TokenRelicRegistry.IsTokenRelic(relic))
            return;

        relic = (TokenRelicRegistry.GetRandomTokenRelicForTreasure(
                RelicFactory.RollRarity(player),
                player.PlayerRng.Rewards)
            ?? RelicFactory.FallbackRelic).ToMutable();
    }
}
