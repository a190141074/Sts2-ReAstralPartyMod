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
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzUltimateHyperSpacetimeNut : AstralPartyRelicModel
{
    private const decimal ImmunityHealAmount = 1m;
    private const decimal ImmunityStarLightAmount = 2m;
    private decimal _turnHpLoss;
    private decimal _combatUnblockedDamageTaken;

    [SavedProperty]
    private string AstralParty_PvzUltimateHyperSpacetimeNutTurnHpLoss
    {
        get => PvzNutRelicHelper.SerializeDecimal(_turnHpLoss);
        set => _turnHpLoss = PvzNutRelicHelper.DeserializeDecimal(value);
    }

    [SavedProperty]
    private string AstralParty_PvzUltimateHyperSpacetimeNutCombatUnblockedDamageTaken
    {
        get => PvzNutRelicHelper.SerializeDecimal(_combatUnblockedDamageTaken);
        set => _combatUnblockedDamageTaken = PvzNutRelicHelper.DeserializeDecimal(value);
    }

    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal { get; set; }
    [SavedProperty] public int AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

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

        _turnHpLoss = 0m;
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
                AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount++;
                MainFile.Logger.Info(
                    $"[PvzUltimateNut] Deterministic immunity triggered | owner={Owner?.NetId} | hit={AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal}");
                return 0m;
            }
        }

        var cap = PvzNutRelicHelper.GetTwentyPercentOfMaxHp(ownerCreature);
        var remainingCap = Math.Max(0m, cap - _turnHpLoss);
        return Math.Min(amount, remainingCap);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return;
        if (!PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return;

        if (AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount > 0 &&
            PvzNutRelicHelper.IsEnemyAttackSource(ownerCreature, dealer, cardSource))
        {
            var rewardCount = AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount;
            AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount = 0;
            Flash();

            for (var i = 0; i < rewardCount; i++)
            {
                await PowerCmd.Apply<HalfLifeHealPower>(ownerCreature, ImmunityHealAmount, ownerCreature, null, false);
                await PowerCmd.Apply(
                    ModelDb.Power<StarLightPower>().ToMutable(),
                    ownerCreature,
                    ImmunityStarLightAmount,
                    ownerCreature,
                    null,
                    false
                );
            }

            MainFile.Logger.Info(
                $"[PvzUltimateNut] Granted immunity rewards | owner={Owner?.NetId} | count={rewardCount}");
        }

        if (result.UnblockedDamage > 0m)
        {
            _turnHpLoss += result.UnblockedDamage;
            _combatUnblockedDamageTaken += result.UnblockedDamage;
            AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage = 0;

            var threshold = PvzNutRelicHelper.GetThirtyPercentOfMaxHp(ownerCreature);
            while (_combatUnblockedDamageTaken >= threshold)
            {
                _combatUnblockedDamageTaken -= threshold;
                AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount++;
                MainFile.Logger.Info(
                    $"[PvzUltimateNut] Queued intangible from cumulative damage | owner={Owner?.NetId} | pending={AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount}");
            }
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        return Task.CompletedTask;
    }

    private void ResetCombatState()
    {
        _turnHpLoss = 0m;
        _combatUnblockedDamageTaken = 0m;
        AstralParty_PvzUltimateHyperSpacetimeNutPendingIntangibleCount = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutPendingImmunityRewardCount = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutTurnsWithoutUnblockedDamage = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutDamageEventOrdinal = 0;
        AstralParty_PvzUltimateHyperSpacetimeNutLastProcessedRound = 0;
    }
}
