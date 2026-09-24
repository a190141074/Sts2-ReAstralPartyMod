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
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenGoldFlyingStar : AstralPartyRelicModel
{
    private const decimal EndTurnDamage = 6m;
    private const int StarLightAmount = 1;
    private const int EnemiesPerEternalStarlight = 3;

    [SavedProperty] public bool AstralParty_TokenGoldFlyingStarPlayedAttackCardThisTurn { get; set; }
    [SavedProperty] public int AstralParty_TokenGoldFlyingStarEnemyHitProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenGoldFlyingStarEnemyHitProgress;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        TokenEternalStarlight.BuildReferenceHoverTip(),
        HoverTipFactory.FromPower<StarLightPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFlyingStarId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldFlyingStarEnemyHitProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenGoldFlyingStarPlayedAttackCardThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        AstralParty_TokenGoldFlyingStarPlayedAttackCardThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        var shouldResolve = !AstralParty_TokenGoldFlyingStarPlayedAttackCardThisTurn;
        AstralParty_TokenGoldFlyingStarPlayedAttackCardThisTurn = false;
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

            await RecordEnemyHit();
        }

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature, StarLightAmount, Owner.Creature, null, false);
    }

    private async Task RecordEnemyHit()
    {
        AstralParty_TokenGoldFlyingStarEnemyHitProgress++;
        var triggerCount = AstralParty_TokenGoldFlyingStarEnemyHitProgress / EnemiesPerEternalStarlight;
        AstralParty_TokenGoldFlyingStarEnemyHitProgress %= EnemiesPerEternalStarlight;
        InvokeDisplayAmountChanged();

        if (triggerCount <= 0 || Owner == null)
            return;

        await TokenEternalStarlight.GrantStacks(Owner, triggerCount);
    }

    private decimal GetBossExtraDamage()
    {
        return Math.Max(0, Owner?.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0) / 3m;
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
