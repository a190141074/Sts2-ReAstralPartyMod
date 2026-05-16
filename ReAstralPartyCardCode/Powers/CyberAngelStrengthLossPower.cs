using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CyberAngelStrengthLossPower : AstralPartyPowerModel
{
    private decimal _appliedStrengthPenalty;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override PowerAssetProfile AssetProfile => Icons(GenerateIconPath<FanPower>());

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthPenalty(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner == null || _appliedStrengthPenalty == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrengthPenalty, oldOwner, null, true);
        _appliedStrengthPenalty = 0m;
    }

    private async Task SyncStrengthPenalty(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var desiredPenalty = -Amount;
        var delta = desiredPenalty - _appliedStrengthPenalty;
        if (delta == 0m)
            return;

        _appliedStrengthPenalty = desiredPenalty;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }
}
