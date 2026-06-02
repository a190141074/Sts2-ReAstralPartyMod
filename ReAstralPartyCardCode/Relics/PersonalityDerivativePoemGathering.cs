using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativePoemGathering : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CeremonialBombPower>(),
        HoverTipFactory.FromPower<VigilCounterPower>(),
        HoverTipFactory.FromPower<FlowerHiddenUnseenPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner != null)
            await VigilCounterCombatHelper.EnsureContextPower(Owner);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        var gainAmount = 1m + Owner.Creature.CombatState.Players.Count / 2;
        Flash();
        await PowerCmd.Apply<CeremonialBombPower>(Owner.Creature, gainAmount, Owner.Creature, null, false);
    }

    public static async Task OnVigilCounterTriggered(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature target,
        MegaCrit.Sts2.Core.Models.AbstractModel source)
    {
        var derivative = owner.GetRelic<PersonalityDerivativePoemGathering>();
        if (derivative == null || owner.Creature == null || !owner.Creature.IsAlive)
            return;

        derivative.Flash();
        var hpLoss = Math.Max(1m, Math.Ceiling(owner.Creature.CurrentHp * 0.1m));
        await VigilCounterCombatHelper.RunWithSuppressedPlayerDamageTriggers(() =>
            CreatureCmd.Damage(
                choiceContext,
                owner.Creature,
                hpLoss,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable | MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,
                owner.Creature,
                null));

        if (target.IsAlive)
            await PowerCmd.Apply<FlowerHiddenUnseenPower>(target, 1m, owner.Creature, null, false);
        var healAmount = Math.Max(1m, Math.Ceiling(owner.Creature.MaxHp * 0.1m));
        await CreatureCmd.Heal(owner.Creature, healAmount, true);
    }
}
