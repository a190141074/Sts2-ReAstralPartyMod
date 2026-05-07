using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleSandwichBiscuitIntermediate : AstralPartyRelicModel
{
    private const decimal MaxHpBonus = 15m;
    private const decimal CounterAmount = 1m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CounterPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner?.Creature == null || !LocalContext.IsMe(Owner))
            return;

        Flash();
        await PandaMaxHpHelper.GainMaxHpFromRelic(Owner.Creature, MaxHpBonus, false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;
        if (!ShouldGrantCounter())
            return;

        Flash();
        await PowerCmd.Apply<CounterPower>(Owner.Creature, CounterAmount, Owner.Creature, null, false);
    }

    private bool ShouldGrantCounter()
    {
        var creature = Owner?.Creature;
        if (creature == null || creature.MaxHp <= 0)
            return false;

        return creature.HasPower<BloodthirstPower>() || LowHpStateHelper.IsBelowHalfHp(creature);
    }
}
