using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldVitamin : AstralPartyRelicModel
{
    private const int MaxTriggersPerTurn = 9;

    [SavedProperty] public int AstralParty_TokenGoldVitaminTriggerCountThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldVitaminTriggerCountThisTurn = 0;
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenGoldVitaminTriggerCountThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenGoldVitaminTriggerCountThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            AstralParty_TokenGoldVitaminTriggerCountThisTurn = 0;

        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (card.Owner != Owner)
            return;
        if (AstralParty_TokenGoldVitaminTriggerCountThisTurn >= MaxTriggersPerTurn)
            return;

        AstralParty_TokenGoldVitaminTriggerCountThisTurn++;
        Flash();
        await PowerCmd.Apply<HalfLifeHealPower>(Owner.Creature, 1m, Owner.Creature, card, false);
    }
}