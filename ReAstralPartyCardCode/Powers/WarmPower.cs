using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class WarmPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal AppliedDexterityBonus;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncDexterityBonus(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncDexterityBonus(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedDexterityBonus != 0m)
            await PowerCmd.Apply<DexterityPower>(oldOwner, -data.AppliedDexterityBonus, oldOwner, null, true);

        data.AppliedDexterityBonus = 0m;
    }

    private async Task SyncDexterityBonus(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredDexterityBonus = Amount > 0m ? Amount : 0m;
        var delta = desiredDexterityBonus - data.AppliedDexterityBonus;
        if (delta == 0m)
            return;

        data.AppliedDexterityBonus = desiredDexterityBonus;
        await PowerCmd.Apply<DexterityPower>(Owner, delta, applier, cardSource, true);
    }
}
