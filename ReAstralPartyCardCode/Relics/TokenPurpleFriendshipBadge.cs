using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleFriendshipBadge : AstralPartyRelicModel
{
    private int _pendingTargetHeals;
    private int _pendingOwnerHeals;
    private bool _isResolvingPendingHeals;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (!ShouldTrigger(canonicalPower, target, amount, applier))
            return false;

        Flash();

        if (canonicalPower is HalfLifeHealPower)
        {
            modifiedAmount += 1m;
            _pendingOwnerHeals++;
        }
        else
        {
            _pendingTargetHeals++;
            _pendingOwnerHeals++;
        }

        return canonicalPower is HalfLifeHealPower;
    }

    private bool ShouldTrigger(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier)
    {
        if (PersonaMultiplayerEffectHelper.IsResolvingDerivedSupportPower)
            return false;
        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
            return false;
        if (applier != Owner.Creature)
            return false;
        if (target == Owner.Creature)
            return false;
        if (amount <= 0m)
            return false;

        return canonicalPower is HalfLifeHealPower or StarLightPower;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_isResolvingPendingHeals)
            return;
        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
        {
            ResetPendingState();
            return;
        }
        if (applier != Owner.Creature)
            return;
        if (power.Owner == Owner.Creature)
            return;
        if (amount <= 0m)
            return;
        if (power is not HalfLifeHealPower and not StarLightPower)
            return;

        await DerivedHealResolutionHelper.RunBatchedAsync(() =>
        {
            _isResolvingPendingHeals = true;
            try
            {
                if (power is StarLightPower && _pendingTargetHeals > 0 && power.Owner.IsAlive)
                {
                    DerivedHealResolutionHelper.EnqueueHalfLifeHeal(
                        power.Owner,
                        _pendingTargetHeals,
                        Owner.Creature,
                        null);
                    _pendingTargetHeals = 0;
                }

                if (_pendingOwnerHeals > 0 && Owner.Creature.IsAlive)
                {
                    DerivedHealResolutionHelper.EnqueueHalfLifeHeal(
                        Owner.Creature,
                        _pendingOwnerHeals,
                        Owner.Creature,
                        null);
                    _pendingOwnerHeals = 0;
                }
            }
            finally
            {
                _isResolvingPendingHeals = false;
            }
        }, "friendship_badge");
    }

    private void ResetPendingState()
    {
        _pendingTargetHeals = 0;
        _pendingOwnerHeals = 0;
    }
}
