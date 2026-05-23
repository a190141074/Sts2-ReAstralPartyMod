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
    [SavedProperty] public int AstralParty_JewelryNightSkinPendingExtraTurnCount { get; set; }
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
        AstralParty_JewelryNightSkinPendingExtraTurnCount = 0;
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
        AstralParty_JewelryNightSkinPendingExtraTurnCount++;
        Flash();
        MainFile.Logger.Info($"[JewelryNightSkin] Queued fallback extra turn | owner={Owner.NetId} | pending={AstralParty_JewelryNightSkinPendingExtraTurnCount}");
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_JewelryNightSkinLastProcessedRound = 0;
        await AstralMoveAgainDisplayHelper.Sync(Owner);
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner && AstralParty_JewelryNightSkinPendingExtraTurnCount > 0;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner || AstralParty_JewelryNightSkinPendingExtraTurnCount <= 0)
            return;

        AstralParty_JewelryNightSkinPendingExtraTurnCount--;
        if (AstralParty_JewelryNightSkinPendingExtraTurnCount < 0)
        {
            AstralParty_JewelryNightSkinPendingExtraTurnCount = 0;
            MainFile.Logger.Warn(
                $"[JewelryNightSkin] Pending fallback extra turn count dropped below zero during consumption | owner={Owner?.NetId}");
        }
        await AstralMoveAgainDisplayHelper.Sync(Owner);
        MainFile.Logger.Info($"[JewelryNightSkin] Pending fallback extra turn consumed | owner={Owner?.NetId} | remaining={AstralParty_JewelryNightSkinPendingExtraTurnCount}");
    }

    public int GetPendingExtraTurnCount()
    {
        return AstralParty_JewelryNightSkinPendingExtraTurnCount;
    }
}
