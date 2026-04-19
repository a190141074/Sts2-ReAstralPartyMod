using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldStarCoinHammer : AstralPartyRelicModel
{
    private const int TriggerGoldThreshold = 150;
    private const int GoldCostPerTrigger = 20;
    private const decimal BonusDamageGoldRatio = 0.075m;

    private static int _activeBonusHits;

    [SavedProperty] public bool AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetDisplayedBonusDamage();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_RelicGoldStarCoinHammerTriggeredThisTurn = false;
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

        var bonusDamage = CalculateBonusDamage(Owner!.Gold);
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

        return Owner.Gold >= TriggerGoldThreshold;
    }

    private static decimal CalculateBonusDamage(int currentGold)
    {
        return Math.Floor(currentGold * BonusDamageGoldRatio);
    }

    private int GetDisplayedBonusDamage()
    {
        if (Owner == null || Owner.Gold < TriggerGoldThreshold)
            return 0;

        return (int)CalculateBonusDamage(Owner.Gold);
    }

    public void RefreshDisplayedBonusDamage()
    {
        InvokeDisplayAmountChanged();
    }
}