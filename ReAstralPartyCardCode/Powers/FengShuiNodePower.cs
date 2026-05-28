using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FengShuiNodePower : AstralPartyPowerModel
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

        var nextNode = NodePowerRollHelper.RollNodeValue(player, this, nameof(FengShuiNodePower), MinNode, MaxNode);
        if (Amount != nextNode)
            await PowerCmd.SetAmount<FengShuiNodePower>(Owner, nextNode, Owner, null);

        if (nextNode == 1)
        {
            var combatState = Owner.CombatState;
            if (combatState == null)
                return;

            var allCreatures = combatState.Creatures.Where(creature => creature.IsAlive).ToList();
            if (allCreatures.Count > 0)
            {
                Flash();
                await CreatureCmd.Damage(choiceContext, allCreatures, 1m, ValueProp.Unpowered, Owner, null);
            }

            player.GetRelic<PersonalityDerivativeFortuneMischance>()?.AddStacksCapped(1);
            return;
        }

        if (nextNode == 6 && player.GetRelic<PersonalityDerivativeFortuneMischance>() is { } derivative)
            derivative.AddStacksCapped(1);
    }
}

