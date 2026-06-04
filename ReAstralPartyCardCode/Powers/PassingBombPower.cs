using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class PassingBombPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player || Owner.Player == null)
            return;
        if (Owner.Side == CombatSide.Player || Amount <= 0m)
            return;

        var roll = (Owner.Player.RunState?.Rng?.CombatTargets?.NextInt(6) ?? 0) + 1;
        if (roll == 1)
        {
            var damage = 9m + (decimal)Math.Pow(2d, (double)Amount);
            using (SevenCursesDebuffProtectionHelper.EnterDebuffDamageContext())
                await CreatureCmd.Damage(choiceContext, Owner, damage, ValueProp.Unpowered, Applier, null);
            return;
        }

        await PowerCmd.ModifyAmount(this, 1m, Owner, null, true);

        var combatState = Owner.CombatState;
        if (combatState == null)
            return;

        var candidates = combatState.Creatures
            .Where(creature => creature.IsAlive && creature.Side == Owner.Side && creature != Owner)
            .OrderBy(creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(creature => creature.ModelId.ToString())
            .ThenBy(creature => creature.SlotName ?? string.Empty)
            .ToList();
        if (candidates.Count == 0)
            return;

        var rng = Owner.Player.RunState?.Rng?.CombatTargets;
        var target = rng == null ? candidates[0] : candidates[rng.NextInt(candidates.Count)];
        var amountToTransfer = Amount;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply(ModelDb.Power<PassingBombPower>().ToMutable(), target, amountToTransfer, Applier, null, false);
    }
}
