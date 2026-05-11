using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldMotorcycleHelmetPremium : AstralPartyRelicModel
{
    private const decimal CombatStartReversedScales = 2m;
    private const decimal CombatStartDexterity = 1m;
    private const decimal BlockPerTurn = 2m;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ReversedScalesHolographicPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<ReversedScalesHolographicPower>().ToMutable(),
            Owner.Creature,
            CombatStartReversedScales,
            Owner.Creature,
            null,
            false
        );
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, CombatStartDexterity, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, BlockPerTurn, ValueProp.Move, null);
    }
}
