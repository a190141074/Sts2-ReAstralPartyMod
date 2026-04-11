using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

/// <summary>
/// 半条命治疗 - 回合开始时恢复生命，治疗量每回合减半
/// </summary>
public class HalfLifeHealPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        // 只在拥有者回合开始时触发
        if (Owner == null || side != Owner.Side)
            return;

        if (Amount <= 0)
            return;

        // 记录当前治疗量
        decimal healAmount = Amount;

        // 显示动画
        Flash();

        // 恢复生命
        await CreatureCmd.Heal(Owner, healAmount, true);

        // 治疗量减半（向下取整，最小为0）
        Amount = Math.Max((int)Math.Floor(Amount / 2m), 0);
        InvokeDisplayAmountChanged();
    }
}