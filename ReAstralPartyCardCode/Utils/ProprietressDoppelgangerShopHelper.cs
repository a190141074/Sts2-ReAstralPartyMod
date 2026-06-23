using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ProprietressDoppelgangerShopHelper
{
    private const decimal CommissionRatio = 0.20m;

    public static bool IsEligibleMerchantPurchase(Player? buyer)
    {
        return buyer?.RunState?.CurrentRoom is MerchantRoom;
    }

    public static async Task TryGrantCommissionAsync(Player? buyer, int spentGold)
    {
        if (buyer == null || spentGold <= 0 || !IsEligibleMerchantPurchase(buyer))
            return;

        var commission = StableNumericStateHelper.FloorToNonNegativeInt(spentGold * CommissionRatio);
        if (commission <= 0)
            return;

        foreach (var recipient in GetCommissionRecipients(buyer))
        {
            var relic = recipient.GetRelic<PersonProprietressDoppelganger>();
            relic?.Flash();
            await PersonMultiplayerEffectHelper.GainGoldDeterministic(commission, recipient);
        }
    }

    private static IReadOnlyList<Player> GetCommissionRecipients(Player buyer)
    {
        return buyer.RunState.Players
            .Where(player => player != buyer)
            .Where(player => player.GetRelic<PersonProprietressDoppelganger>() != null)
            .OrderBy(player => player.NetId)
            .ToList();
    }
}
