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
    private readonly HashSet<Creature> _pendingHealTargets = [];
    private bool _pendingOwnerHeal;
    private bool _flushScheduled;

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
            modifiedAmount += 1m;
        else
            _pendingHealTargets.Add(target);

        if (Owner?.Creature != null && target != Owner.Creature)
            _pendingOwnerHeal = true;

        ScheduleFlush();
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

    private void ScheduleFlush()
    {
        if (_flushScheduled)
            return;

        _flushScheduled = true;
        _ = FlushPendingHealsAsync();
    }

    private async Task FlushPendingHealsAsync()
    {
        await Task.Yield();

        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
        {
            ResetPendingState();
            return;
        }

        var pendingTargets = _pendingHealTargets.ToList();
        var shouldHealOwner = _pendingOwnerHeal;
        ResetPendingState();

        await PersonaMultiplayerEffectHelper.RunAsDerivedSupportPower(async () =>
        {
            foreach (var target in pendingTargets)
                if (target.IsAlive)
                    await PowerCmd.Apply<HalfLifeHealPower>(target, 1m, Owner.Creature, null, false);

            if (shouldHealOwner && Owner.Creature.IsAlive)
                await PowerCmd.Apply<HalfLifeHealPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        });
    }

    private void ResetPendingState()
    {
        _pendingHealTargets.Clear();
        _pendingOwnerHeal = false;
        _flushScheduled = false;
    }
}
