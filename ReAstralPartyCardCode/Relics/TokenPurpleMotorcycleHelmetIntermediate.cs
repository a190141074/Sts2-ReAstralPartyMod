using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleMotorcycleHelmetIntermediate : AstralPartyRelicModel
{
    private const decimal CombatStartReversedScales = 1m;
    private const decimal DefendPointPerTurn = 1m;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ReversedScalesHolographicPower>(),
        HoverTipFactory.FromPower<DefendPointPower>()
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
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        Flash();
        await DefendPointPower.GrantFromRelic(Owner.Creature, DefendPointPerTurn, this);
    }
}
