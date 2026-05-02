using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class StarLightPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player == null) return;
        if (Amount <= 0) return;

        Flash();
        room.AddExtraReward(Owner.Player, new GoldReward(Amount, Owner.Player, false));
        await Task.CompletedTask;
    }
}