using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueAtm : AstralPartyRelicModel
{
    private int _pendingSelfStarLight;
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
        return true;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_isResolvingAtm)
            return;
        if (_pendingSelfStarLight <= 0)
            return;
        if (Owner?.Creature == null)
        {
            ResetPendingState();
            return;
        }
        if (power.Owner == Owner.Creature)
            return;
        if (power is not StarLightPower)
            return;
        if (applier != Owner.Creature)
            return;
        if (amount <= 0m)
            return;

        _pendingSelfStarLight--;

        _isResolvingAtm = true;
        try
        {
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                Owner.Creature,
                1m,
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
    }
}
