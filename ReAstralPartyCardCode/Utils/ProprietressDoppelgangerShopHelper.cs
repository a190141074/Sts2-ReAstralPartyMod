using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ProprietressDoppelgangerShopHelper
{
    private const decimal RefundRatio = 0.30m;

    public static bool IsEligibleMerchantPurchase(Player? buyer)
    {
        return buyer?.RunState?.CurrentRoom is MerchantRoom;
    }

    public static async Task TryProcessPurchaseAsync(Player? buyer, int spentGold)
    {
        if (buyer == null || spentGold <= 0 || !IsEligibleMerchantPurchase(buyer))
            return;

        var relic = buyer.GetRelic<PersonProprietressDoppelganger>();
        if (relic == null)
            return;

        relic.RecordMerchantSpend(spentGold);
        relic.Flash();

        var refund = StableNumericStateHelper.FloorToNonNegativeInt(spentGold * RefundRatio);
        if (refund > 0)
            await PersonMultiplayerEffectHelper.GainGoldDeterministic(refund, buyer);
    }
}
