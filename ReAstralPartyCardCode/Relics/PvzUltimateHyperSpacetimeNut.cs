using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
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
using ReAstralPartyMod.ReAstralPartyCardCode.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(PvzNutSeriesRelicPool))]
public class PvzUltimateHyperSpacetimeNut : AstralPartyRelicModel
{
    [SavedProperty] public decimal AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss { get; set; }
    [SavedProperty] public decimal AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatState();
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss = 0m;
        if (AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound != Owner.Creature.CombatState?.RoundNumber)
            AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound = Owner.Creature.CombatState?.RoundNumber ?? 0;

        if (AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount > 0)
        {
            var pending = AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount;
            AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount = 0;
            Flash();
            await PowerCmd.Apply<IntangiblePower>(Owner.Creature, pending, Owner.Creature, null, false);
            MainFile.Logger.Info($"[PvzUltimateNut] Granted pending intangible | owner={Owner.NetId} | stacks={pending}");
        }

        await PowerCmd.Apply<ArtifactPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        if (AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage >= 15)
        {
            AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage = 0;
            Flash();
            await CreatureCmd.Heal(Owner.Creature, Math.Max(0m, Owner.Creature.MaxHp - Owner.Creature.CurrentHp));
            MainFile.Logger.Info($"[PvzUltimateNut] Triggered full heal after 15 safe turns | owner={Owner.NetId}");
        }
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null || !PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return amount;

        if (PvzNutRelicHelper.IsEnemyAttackSource(ownerCreature, dealer, cardSource))
        {
            AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal++;
            if (PvzNutRelicHelper.ShouldNegateEnemyAttack(Owner!, AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal))
            {
                MainFile.Logger.Info(
                    $"[PvzUltimateNut] Deterministic immunity triggered | owner={Owner?.NetId} | hit={AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal}");
                return 0m;
            }
        }

        var cap = PvzNutRelicHelper.GetTwentyFivePercentOfMaxHp(ownerCreature);
        var remainingCap = Math.Max(0m, cap - AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss);
        return Math.Min(amount, remainingCap);
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return Task.CompletedTask;
        if (!PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return Task.CompletedTask;

        if (result.UnblockedDamage > 0m)
        {
            AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss += result.UnblockedDamage;
            AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken += result.UnblockedDamage;
            AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage = 0;

            var threshold = PvzNutRelicHelper.GetThirtyPercentOfMaxHp(ownerCreature);
            while (AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken >= threshold)
            {
                AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken -= threshold;
                AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount++;
                MainFile.Logger.Info(
                    $"[PvzUltimateNut] Queued intangible from cumulative damage | owner={Owner?.NetId} | pending={AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount}");
            }
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    private void ResetCombatState()
    {
        AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss = 0m;
        AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken = 0m;
        AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound = 0;
    }
}
