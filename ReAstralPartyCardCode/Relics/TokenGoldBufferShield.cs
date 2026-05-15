using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldBufferShield : AstralPartyRelicModel
{
    private const decimal HealAmount = 1m;
    private const decimal StarLightAmount = 3m;

    private static readonly PropertyInfo? AttackTargetProperty =
        typeof(AttackCommand).GetProperty("Target")
        ?? typeof(AttackCommand).GetProperty("SingleTarget");

    private static readonly FieldInfo? AttackSingleTargetField =
        typeof(AttackCommand).GetField("_singleTarget", BindingFlags.Instance | BindingFlags.NonPublic);

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null)
            return;
        if (command.Attacker == null || command.Attacker.Side == Owner.Creature.Side)
            return;
        if (command.TargetSide != Owner.Creature.Side)
            return;

        var target = AttackTargetProperty?.GetValue(command) as Creature
                     ?? AttackSingleTargetField?.GetValue(command) as Creature;
        if (target != Owner.Creature)
            return;

        Flash();
        await PowerCmd.Apply<HalfLifeHealPower>(Owner.Creature, HealAmount, Owner.Creature, null, false);
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            StarLightAmount,
            Owner.Creature,
            null,
            false
        );
    }
}
