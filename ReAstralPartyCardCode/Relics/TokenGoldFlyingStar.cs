using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenGoldFlyingStar : AstralPartyRelicModel
{
    private const decimal EndTurnDamage = 6m;
    private const int StarLightAmount = 1;

    [SavedProperty] public bool AstralParty_TokenGoldFlyingStarDealtEnemyUnblockedDamageByCardThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenGoldFlyingStarDealtEnemyUnblockedDamageByCardThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsTrackedCardDamage(target, result.UnblockedDamage, cardSource))
            return;

        AstralParty_TokenGoldFlyingStarDealtEnemyUnblockedDamageByCardThisTurn = true;
        await Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        var shouldResolve = !AstralParty_TokenGoldFlyingStarDealtEnemyUnblockedDamageByCardThisTurn;
        AstralParty_TokenGoldFlyingStarDealtEnemyUnblockedDamageByCardThisTurn = false;
        if (!shouldResolve)
            return;

        Flash();
        var enemies = EventCombatTargetHelper.GetAliveNonSummonEnemies(Owner.Creature.CombatState, Owner.Creature).ToList();
        var bossExtraDamage = GetBossExtraDamage();
        foreach (var enemy in enemies)
        {
            await CreatureCmdCompat.Damage(
                choiceContext,
                enemy,
                EndTurnDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature,
                null);
            if (bossExtraDamage > 0m && IsBossEnemy(enemy))
                await CreatureCmdCompat.Damage(
                    choiceContext,
                    enemy,
                    bossExtraDamage,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    Owner.Creature,
                    null);
        }

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature, StarLightAmount, Owner.Creature, null, false);
    }

    private decimal GetBossExtraDamage()
    {
        return Math.Max(0, Owner?.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0) / 3m;
    }

    private bool IsTrackedCardDamage(Creature target, decimal unblockedDamage, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (target.Side == Owner.Creature.Side)
            return false;
        if (unblockedDamage <= 0m)
            return false;
        if (cardSource == null || cardSource.Owner != Owner)
            return false;

        return true;
    }

    private bool IsBossEnemy(Creature creature)
    {
        var combatState = creature.CombatState as CombatState;
        if (creature.Monster == null || combatState?.Encounter?.RoomType != RoomType.Boss)
            return false;

        var bossIds = TryGetBossMonsterIds(combatState);
        if (bossIds.Count > 0)
            return bossIds.Contains(creature.Monster.Id);

        return combatState.Enemies.Count == 1 && ReferenceEquals(combatState.Enemies[0], creature);
    }

    private HashSet<ModelId> TryGetBossMonsterIds(CombatState combatState)
    {
        var encounter = combatState.Encounter;
        if (encounter?.RoomType != RoomType.Boss)
            return [];

        try
        {
            var bossNodePath = encounter.BossNodePath ?? string.Empty;
            return encounter.MonstersWithSlots
                .Where(entry => SlotLooksLikeBoss(entry.Item2, bossNodePath))
                .Select(entry => entry.Item1.Id)
                .ToHashSet();
        }
        catch
        {
            return [];
        }
    }

    private static bool SlotLooksLikeBoss(string? slotName, string? bossNodePath)
    {
        if (string.IsNullOrWhiteSpace(slotName))
            return false;
        if (slotName.Contains("boss", StringComparison.OrdinalIgnoreCase))
            return true;
        if (slotName.Contains("leader", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrWhiteSpace(bossNodePath)
            && bossNodePath.Contains(slotName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
