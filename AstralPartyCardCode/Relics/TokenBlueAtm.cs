using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueAtm : AstralPartyRelicModel
{
    private int _pendingSelfStarLight;
    private bool _flushScheduled;
    private bool _isResolvingAtm;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (_isResolvingAtm)
            return false;
        if (Owner?.Creature == null)
            return false;
        if (canonicalPower is not StarLightPower)
            return false;
        if (applier != Owner.Creature)
            return false;
        if (target == Owner.Creature)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount += 1m;
        _pendingSelfStarLight += 1;
        Flash();
        ScheduleFlush();
        return true;
    }

    private void ScheduleFlush()
    {
        if (_flushScheduled)
            return;

        _flushScheduled = true;
        _ = FlushPendingStarLightAsync();
    }

    private async Task FlushPendingStarLightAsync()
    {
        await Task.Yield();

        if (Owner?.Creature == null || _pendingSelfStarLight <= 0)
        {
            ResetPendingState();
            return;
        }

        var pendingAmount = _pendingSelfStarLight;
        ResetPendingState();

        _isResolvingAtm = true;
        try
        {
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                Owner.Creature,
                pendingAmount,
                Owner.Creature,
                null,
                false
            );
        }
        finally
        {
            _isResolvingAtm = false;
        }
    }

    private void ResetPendingState()
    {
        _pendingSelfStarLight = 0;
        _flushScheduled = false;
    }
}
