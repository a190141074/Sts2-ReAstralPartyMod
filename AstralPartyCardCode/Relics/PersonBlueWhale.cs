using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonBlueWhale : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;
    private const int FateWeakImprintKillGold = 3;
    private const int ExactRoundRewardBase = 6;
    private const int ExactRoundRewardBonusPerRepeat = 2;
    private const int ExactRoundTarget = 6;

    [SavedProperty] public int AstralParty_PersonBlueWhaleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonBlueWhalePendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonBlueWhaleExactRound6RewardCount { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFateWeakMprint>(),
        HoverTipFactory.FromPower<FateWeakImprintPower>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonBlueWhaleCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonBlueWhalePendingCombatStartCard = true;
        AstralParty_PersonBlueWhaleExactRound6RewardCount = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonBlueWhalePendingCombatStartCard)
        {
            await GrantFateWeakMprint();
            AstralParty_PersonBlueWhalePendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantFateWeakMprint();
        AstralParty_PersonBlueWhaleCounter = 1;
        AstralParty_PersonBlueWhalePendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner == null || Owner.Creature == null)
            return;

        if (target.Side == Owner.Creature.Side)
            return;

        if (!target.HasPower<FateWeakImprintPower>())
            return;

        Flash();
        await PlayerCmd.GainGold(FateWeakImprintKillGold, Owner);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return;

        if (room.CombatState.RoundNumber == ExactRoundTarget)
        {
            // Each exact-turn clear increases future payouts by 2 gold.
            AstralParty_PersonBlueWhaleExactRound6RewardCount++;
            var goldToGain =
                ExactRoundRewardBase
                + (AstralParty_PersonBlueWhaleExactRound6RewardCount - 1) * ExactRoundRewardBonusPerRepeat;
            Flash();
            await PlayerCmd.GainGold(goldToGain, Owner);
        }

        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonBlueWhaleCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonBlueWhaleCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonBlueWhalePendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonBlueWhaleCounter = 1;
            AstralParty_PersonBlueWhalePendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantFateWeakMprint()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFateWeakMprint>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}
