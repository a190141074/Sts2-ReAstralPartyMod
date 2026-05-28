using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class DorothyNodePower : AstralPartyPowerModel
{
    private const int MinNode = 1;
    private const int MaxNode = 10;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;

        var nextNode = NodePowerRollHelper.RollNodeValue(player, this, nameof(DorothyNodePower), MinNode, MaxNode);
        if (Amount != nextNode)
            await PowerCmd.SetAmount<DorothyNodePower>(Owner, nextNode, Owner, null);
    }
}
