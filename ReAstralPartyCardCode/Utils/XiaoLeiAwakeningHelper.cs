using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class XiaoLeiAwakeningHelper
{
    public static async Task TryGrantAwakeningForGrantedCard(Player? grantor, Player? recipient, int amount = 1)
    {
        if (grantor == null || recipient == null)
            return;
        if (grantor == recipient)
            return;
        if (amount <= 0)
            return;

        var relic = grantor.GetRelic<PersonXiaoLei>();
        if (relic == null)
            return;

        await relic.GrantDragonAwakening(amount);
    }
}
