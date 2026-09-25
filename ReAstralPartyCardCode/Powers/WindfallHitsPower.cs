using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class WindfallHitsPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player == null || Amount <= 0m)
            return;

        Flash();
        room.AddExtraReward(Owner.Player, new GoldReward(Amount, Owner.Player, false));
        await Task.CompletedTask;
    }
}
