using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

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