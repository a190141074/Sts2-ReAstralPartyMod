using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldStarCoinHammer : AstralPartyRelicModel
{
    private const int EternalStarlightToGrant = 15;
    private const int TriggerEternalStarlightThreshold = 40;
    private const int TriggerGoldThreshold = 100;
    private const int GoldCostPerTrigger = 15;
    private const decimal BonusDamageGoldRatio = 0.10m;
    private const decimal StarLightOnKill = 6m;

    private static int _activeBonusHits;

    [SavedProperty] public bool AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetDisplayedBonusDamage();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralEternalStarlightSetId),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn = false;
        if (Owner != null)
        {
            if (TokenRelicBridgeInitializationContext.ShouldSkipOneTimeObtainRewards)
            {
                InvokeDisplayAmountChanged();
                return;
            }

            await TokenEternalStarlight.GrantStacks(Owner, EternalStarlightToGrant);
        }

        InvokeDisplayAmountChanged();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;

        AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterGoldGained(Player player)
    {
        if (player == Owner)
            InvokeDisplayAmountChanged();

        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource
    )
    {
        if (!CanTriggerBonusDamage(dealer, result, target))
            return;

        AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn = true;
        Flash();

        await PlayerCmd.LoseGold(GoldCostPerTrigger, Owner, GoldLossType.Spent);
        InvokeDisplayAmountChanged();

        var bonusDamage = CalculateBonusDamageAfterCost(Owner!.Gold);
        if (bonusDamage <= 0m)
            return;

        try
        {
            _activeBonusHits++;
            await CreatureCmd.Damage(
                choiceContext,
                target,
                bonusDamage,
                ValueProp.Unpowered,
                Owner.Creature,
                null
            );

            if (!target.IsAlive)
                await PowerCmd.Apply(
                    ModelDb.Power<StarLightPower>().ToMutable(),
                    Owner.Creature,
                    StarLightOnKill,
                    Owner.Creature,
                    null,
                    false
                );
        }
        finally
        {
            _activeBonusHits--;
        }
    }

    private bool CanTriggerBonusDamage(Creature? dealer, DamageResult result, Creature target)
    {
        if (Owner?.Creature == null)
            return false;
        if (_activeBonusHits > 0)
            return false;
        if (AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target.Side == Owner.Creature.Side || !target.IsAlive)
            return false;
        if (result.TotalDamage <= 0)
            return false;

        return Owner.Gold >= TriggerGoldThreshold
               && GetEternalStarlightAmount() >= TriggerEternalStarlightThreshold;
    }

    private static decimal CalculateBonusDamageAfterCost(int goldAfterPayment)
    {
        return Math.Floor(Math.Max(goldAfterPayment, 0) * BonusDamageGoldRatio);
    }

    private int GetDisplayedBonusDamage()
    {
        if (Owner == null || Owner.Gold < TriggerGoldThreshold)
            return 0;
        if (GetEternalStarlightAmount() < TriggerEternalStarlightThreshold)
            return 0;

        return (int)CalculateBonusDamageAfterCost(Owner.Gold - GoldCostPerTrigger);
    }

    private int GetEternalStarlightAmount()
    {
        return Owner?.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
    }

    public void RefreshDisplayedBonusDamage()
    {
        InvokeDisplayAmountChanged();
    }
}
