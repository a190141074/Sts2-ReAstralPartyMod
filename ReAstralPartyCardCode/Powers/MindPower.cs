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

public class MindPower : AstralPartyPowerModel
{
    private const decimal ActiveThreshold = 3m;
    private const decimal StrengthBonus = 2m;
    private const decimal DexterityBonus = 2m;

    private sealed class Data
    {
        public decimal AppliedStrengthBonus;
        public decimal AppliedDexterityBonus;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStatBonuses(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStatBonuses(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null)
        {
            if (data.AppliedStrengthBonus != 0m)
                await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthBonus, oldOwner, null, true);
            if (data.AppliedDexterityBonus != 0m)
                await PowerCmd.Apply<DexterityPower>(oldOwner, -data.AppliedDexterityBonus, oldOwner, null, true);
        }

        data.AppliedStrengthBonus = 0m;
        data.AppliedDexterityBonus = 0m;
    }

    private async Task SyncStatBonuses(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredStrengthBonus = Amount > ActiveThreshold ? StrengthBonus : 0m;
        var desiredDexterityBonus = Amount > ActiveThreshold ? DexterityBonus : 0m;

        var strengthDelta = desiredStrengthBonus - data.AppliedStrengthBonus;
        var dexterityDelta = desiredDexterityBonus - data.AppliedDexterityBonus;

        if (strengthDelta != 0m)
        {
            data.AppliedStrengthBonus = desiredStrengthBonus;
            await PowerCmd.Apply<StrengthPower>(Owner, strengthDelta, applier, cardSource, true);
        }

        if (dexterityDelta != 0m)
        {
            data.AppliedDexterityBonus = desiredDexterityBonus;
            await PowerCmd.Apply<DexterityPower>(Owner, dexterityDelta, applier, cardSource, true);
        }
    }
}
