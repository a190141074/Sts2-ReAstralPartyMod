using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ManaAmplificationHelper
{
    public static int GetCurrent(Player? owner)
    {
        return owner?.GetRelic<VariantPersonWamdus>()?.GetManaAmplification() ?? 0;
    }

    public static void Add(Player? owner, int amount)
    {
        if (owner?.GetRelic<VariantPersonWamdus>() is not { } relic || amount <= 0)
            return;

        relic.AddManaAmplification(amount);
    }

    public static bool TryGetRelic(Player? owner, out VariantPersonWamdus? relic)
    {
        relic = owner?.GetRelic<VariantPersonWamdus>();
        return relic != null;
    }
}
