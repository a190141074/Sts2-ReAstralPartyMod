using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeAbyssWhisper : AstralPartyRelicModel
{
    private const decimal BossOpeningWhisperStacks = 4m;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WhisperPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null || !IsBossEncounter())
            return;

        Flash();
        await PowerCmd.Apply<WhisperPower>(Owner.Creature, BossOpeningWhisperStacks, Owner.Creature, null, false);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null)
            return;

        var targets = combatState.Creatures
            .Where(creature => creature.Side == side && creature.IsAlive)
            .ToList();
        if (targets.Count == 0)
            return;

        Flash();
        foreach (var target in targets)
            await PowerCmd.Apply<WhisperPower>(target, 1m, Owner?.Creature, null, false);
    }

    private bool IsBossEncounter()
    {
        var roomType = Owner?.Creature?.CombatState?.Encounter?.RoomType
                       ?? Owner?.RunState?.CurrentRoom?.RoomType;
        return roomType == RoomType.Boss;
    }
}
