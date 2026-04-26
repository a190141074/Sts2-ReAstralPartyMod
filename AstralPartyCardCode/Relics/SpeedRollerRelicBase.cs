using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

public abstract class SpeedRollerRelicBase : AstralPartyRelicModel
{
    protected abstract decimal CombatStartDexterityBonus { get; }

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;
        if (CombatStartDexterityBonus <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, CombatStartDexterityBonus, Owner.Creature, null);
    }
}