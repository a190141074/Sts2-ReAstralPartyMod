using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class JewelryNightSkin : AstralPartyRelicModel
{
    private const int FallbackTurnThreshold = 9;

    [SavedProperty] public int AstralParty_JewelryNightSkinTurnCounter { get; set; }
    [SavedProperty] public int AstralParty_JewelryNightSkinLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MoveAgainPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_JewelryNightSkinTurnCounter = 0;
        AstralParty_JewelryNightSkinLastProcessedRound = 0;
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_JewelryNightSkinLastProcessedRound = 0;
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;
        if (Owner.GetRelic<VariantPersonSara>() != null)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (AstralParty_JewelryNightSkinLastProcessedRound == roundNumber)
            return;

        AstralParty_JewelryNightSkinLastProcessedRound = roundNumber;
        AstralParty_JewelryNightSkinTurnCounter++;
        if (AstralParty_JewelryNightSkinTurnCounter < FallbackTurnThreshold)
            return;

        AstralParty_JewelryNightSkinTurnCounter = 0;
        Flash();
        await PendingExtraTurnQueuePower.EnqueueNightSkinExtraTurn(Owner);
        MainFile.Logger.Info(
            $"[JewelryNightSkin] Queued fallback extra turn | owner={Owner.NetId} | pending={PendingExtraTurnQueuePower.GetPendingCount(Owner)}");
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_JewelryNightSkinLastProcessedRound = 0;
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

}
