using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenExclusiveTimePocketWatch : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_TokenExclusiveTimePocketWatchUsedThisRun { get; set; }
    [SavedProperty] public bool AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn { get; set; }
    [SavedProperty] public bool AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RingingPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFinalArenaSeriesId)
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn = false;
        AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn = false;
        AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature || amount <= 0m)
            return amount;
        if (!IsEnemyTurnActive())
            return amount;
        if (AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn)
            return 0m;
        if (AstralParty_TokenExclusiveTimePocketWatchUsedThisRun)
            return amount;
        if (amount < Owner.Creature.CurrentHp)
            return amount;

        AstralParty_TokenExclusiveTimePocketWatchUsedThisRun = true;
        AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn = true;
        AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution = true;
        Flash();
        return 0m;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (side == Owner.Creature.Side)
            return Task.CompletedTask;

        AstralParty_TokenExclusiveTimePocketWatchImmuneThisEnemyTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;
        if (!AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution)
            return;

        AstralParty_TokenExclusiveTimePocketWatchPendingNextOwnerTurnResolution = false;
        Flash();
        await PowerCmd.Apply<RingingPower>(Owner.Creature, 1m, Owner.Creature, null, false);

        foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature))
            await CreatureCmd.Stun(enemy);
    }

    private bool IsEnemyTurnActive()
    {
        return Owner?.Creature?.CombatState is { CurrentSide: { } currentSide }
               && Owner.Creature.Side != currentSide;
    }
}
