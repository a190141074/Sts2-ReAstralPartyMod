using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

[RegisterGoodModifier]
public sealed class FinalArenaDistortionComponentModifier : ModifierModel
{
    private static readonly ConditionalWeakTable<FinalArenaDistortionComponentModifier, Data> RuntimeData = [];

    private const decimal ThundercloudDamage = 2m;
    private const decimal RainstormSelfDamage = 1m;
    private const decimal RainstormTemporaryStrength = 1m;
    private const decimal SeasicknessPoison = 2m;
    private const decimal PirateKingsOrderStacks = 1m;
    private const decimal PirateKingsOrderHeal = 3m;
    private const decimal GaleReversedScales = 1m;
    private const decimal WideAreaMarkStacks = 3m;
    private const decimal RobberyStunTurns = 1m;
    private const decimal RefereeStatLoss = 2m;
    private const decimal FlashOfInsightCopyChance = 33m;
    private const int FlashOfInsightMaxTriggersPerTurn = 3;
    private const int PreciseSelectionCandidateCount = 4;
    private const int PreciseSelectionDrawPenalty = 1;
    private const int RefereeTriggerInterval = 3;
    private const int RainstormSkillInterval = 3;
    private const int MysteriousTreasureInterval = 5;
    private const int ArbitrageFloorInterval = 3;
    private const decimal ArbitragePercent = 0.10m;

    private enum RefereePenaltyStat
    {
        Dexterity,
        Strength
    }

    private static readonly LocString PreciseSelectionPrompt =
        new("relics", "RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT.precise_selection.select_prompt");

    private sealed class TurnPlayerState
    {
        public int SkillCardsPlayed;
        public int CardsPlayed;
        public bool SeasicknessTriggered;
        public bool GaleTriggered;
        public bool CopyLowTriggered;
        public bool CopyHighTriggered;
        public int FlashOfInsightTriggers;
    }

    private sealed class PendingCardFlags
    {
        public bool TriggerRainstorm;
        public bool TriggerSeasickness;
        public bool TriggerGale;
        public bool TriggerCopyLow;
        public bool TriggerCopyHigh;
        public int HandCountBeforePlay;
    }

    private sealed class EnemyTurnPlayerState
    {
        public bool TookRealDamage;
    }

    private sealed class EnergyDisorderEntry
    {
        public required CardModel Card { get; init; }
        public required decimal RandomCost { get; init; }
    }

    private sealed class Data
    {
        public Dictionary<ulong, TurnPlayerState> TurnStates { get; } = [];
        public Dictionary<CardModel, PendingCardFlags> PendingCardFlags { get; } = [];
        public Dictionary<ulong, EnemyTurnPlayerState> EnemyTurnPlayerStates { get; } = [];
        public List<EnergyDisorderEntry> EnergyDisorderEntries { get; } = [];
        public Dictionary<ulong, int> PendingPreciseSelectionDrawCounts { get; } = [];
        public Dictionary<ulong, int> PendingPreciseSelectionTurnSeeds { get; } = [];
        public bool TrackingEnemyTurn;
    }

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeCombatStart()
    {
        var runState = RunState;
        var combatState = GetCombatState();
        if (runState == null || combatState == null)
            return;

        ClearCombatRuntimeState();

        var aliveEnemies = GetAliveEnemies(combatState);
        if (aliveEnemies.Count == 0)
            return;

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.PirateKingsOrder))
            await ApplyPirateKingsOrderAtCombatStart(aliveEnemies, combatState.Encounter?.RoomType);

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.Gale))
            await ApplyToAllCreatures(
                combatState,
                creature => PowerCmd.Apply<ReversedScalesHolographicPower>(creature, GaleReversedScales, creature, null, false));

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.WideAreaMark))
            await ApplyToAllCreatures(
                combatState,
                creature => PowerCmd.Apply<MarkLockPower>(creature, WideAreaMarkStacks, creature, null, false));

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.Robbery) &&
            FinalArenaSeriesHelper.ConsumeRobberyNextCombatStun(runState))
        {
            await ApplyRobberyCombatStart(combatState);
        }

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.MysteriousTreasure) &&
            FinalArenaSeriesHelper.ShouldApplyMysteriousTreasureThisCombat(runState))
        {
            var treasureTarget = GetDeterministicRandomNonBossEnemy(aliveEnemies, "mysterious_treasure", runState.TotalFloor);
            if (treasureTarget != null)
                await PowerCmd.Apply<TreasureBucketPower>(treasureTarget, 1m, treasureTarget, null, false);
        }

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.Baton))
        {
            var firstPlayer = GetStablePlayers()
                .FirstOrDefault(player => player.Creature != null && !player.Creature.IsDead);
            if (firstPlayer?.Creature != null)
                await PowerCmd.Apply<BatonPower>(firstPlayer.Creature, 1m, firstPlayer.Creature, null, false);
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (RunState == null)
            return Task.CompletedTask;

        if (side == CombatSide.Enemy)
        {
            ResetEnemyTurnTracking();
            if (FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.PirateKingsOrder))
                return HealEnemiesIfBossIntentIsNonAttack();
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal amount)
    {
        if (RunState == null ||
            !FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.PreciseSelection))
            return amount;

        var nextTurnCounter = PeekNextGlobalPlayerTurnCounter();
        if (nextTurnCounter <= 0 || nextTurnCounter % RefereeTriggerInterval != 0)
        {
            var data = GetData();
            data.PendingPreciseSelectionDrawCounts.Remove(player.NetId);
            data.PendingPreciseSelectionTurnSeeds.Remove(player.NetId);
            return amount;
        }

        var modified = Math.Max(0m, amount - PreciseSelectionDrawPenalty);
        var pendingDrawCount = (int)Math.Max(0m, decimal.Truncate(modified));
        var runtimeData = GetData();
        runtimeData.PendingPreciseSelectionDrawCounts[player.NetId] = pendingDrawCount;
        runtimeData.PendingPreciseSelectionTurnSeeds[player.NetId] = nextTurnCounter;
        return modified;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        var data = GetData();
        if (!data.PendingPreciseSelectionTurnSeeds.TryGetValue(player.NetId, out var turnSeed))
            return;
        if (data.PendingPreciseSelectionDrawCounts.TryGetValue(player.NetId, out var remainingDraws) &&
            remainingDraws > 0)
            return;

        data.PendingPreciseSelectionDrawCounts.Remove(player.NetId);
        data.PendingPreciseSelectionTurnSeeds.Remove(player.NetId);
        await ResolvePreciseSelection(choiceContext, player, turnSeed);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var runState = RunState;
        if (runState == null || player.Creature == null)
            return;

        ResetPlayerTurnState(player);
        ClearEnergyDisorderEntriesForPlayer(player);

        var globalTurnCounter = FinalArenaSeriesHelper.AdvanceGlobalPlayerTurnCounter(runState);

        if (FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.EnergyDisorder))
            ApplyEnergyDisorderForCurrentHand(player, globalTurnCounter);

        if (globalTurnCounter > 0 && globalTurnCounter % RefereeTriggerInterval == 0)
            await TriggerRefereeAttention(player, globalTurnCounter);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool wasFlushed)
    {
        var owner = card.Owner;
        if (owner == null)
            return;

        var data = GetData();
        if (!data.PendingPreciseSelectionTurnSeeds.TryGetValue(owner.NetId, out var turnSeed))
            return;
        if (!data.PendingPreciseSelectionDrawCounts.TryGetValue(owner.NetId, out var remainingDraws))
            return;

        remainingDraws = Math.Max(0, remainingDraws - 1);
        if (remainingDraws > 0)
        {
            data.PendingPreciseSelectionDrawCounts[owner.NetId] = remainingDraws;
            return;
        }

        data.PendingPreciseSelectionDrawCounts.Remove(owner.NetId);
        data.PendingPreciseSelectionTurnSeeds.Remove(owner.NetId);
        await ResolvePreciseSelection(choiceContext, owner, turnSeed);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        if (player?.Creature == null)
            return Task.CompletedTask;

        var state = GetOrCreateTurnState(player);
        var pending = new PendingCardFlags
        {
            HandCountBeforePlay = player.PlayerCombatState?.Hand?.Cards.Count ?? 0
        };

        state.CardsPlayed++;
        if (!state.SeasicknessTriggered && state.CardsPlayed == 8)
        {
            state.SeasicknessTriggered = true;
            pending.TriggerSeasickness = true;
        }

        if (WarforgeEnchantmentHelper.CountsAsSkill(cardPlay.Card))
        {
            state.SkillCardsPlayed++;
            if (state.SkillCardsPlayed % RainstormSkillInterval == 0)
                pending.TriggerRainstorm = true;

            if (!state.CopyLowTriggered && pending.HandCountBeforePlay <= 3)
            {
                state.CopyLowTriggered = true;
                pending.TriggerCopyLow = true;
            }

            if (!state.CopyHighTriggered && pending.HandCountBeforePlay >= 6)
            {
                state.CopyHighTriggered = true;
                pending.TriggerCopyHigh = true;
            }
        }

        if (!state.GaleTriggered && pending.HandCountBeforePlay >= 10)
        {
            state.GaleTriggered = true;
            pending.TriggerGale = true;
        }

        GetData().PendingCardFlags[cardPlay.Card] = pending;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        if (player?.Creature == null)
            return;

        RemoveEnergyDisorderEntry(cardPlay.Card);
        var pending = ConsumePendingCardFlags(cardPlay.Card);

        if (pending?.TriggerRainstorm == true &&
            RunState != null &&
            FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.Rainstorm))
        {
            await CreatureCmdCompat.Damage(choiceContext, player.Creature, RainstormSelfDamage, ValueProp.Move, player.Creature, null);
            await AstralTemporaryStrengthPower.Apply(player.Creature, RainstormTemporaryStrength, this, player.Creature, null, true);
        }

        if (pending?.TriggerSeasickness == true &&
            RunState != null &&
            FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.Seasickness))
        {
            var poisonTarget = GetDeterministicRandomPlayerOrNull("seasickness", player, GetPlayerTurnSequenceSeed(player));
            if (poisonTarget?.Creature != null)
                await PowerCmd.Apply<PoisonPower>(poisonTarget.Creature, SeasicknessPoison, player.Creature, cardPlay.Card, false);
        }

        if (pending?.TriggerGale == true &&
            RunState != null &&
            FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.Gale))
        {
            PersonRelicHelper.AdvanceCooldownRelics(player, 1);
        }

        if (pending?.TriggerCopyLow == true &&
            RunState != null &&
            FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.CopyCard) &&
            WarforgeEnchantmentHelper.CountsAsSkill(cardPlay.Card))
        {
            await PersonMultiplayerEffectHelper.CopyCardToHandOrRedirectLivingFolioAsync(
                player,
                cardPlay.Card,
                this,
                CardPilePosition.Top);
        }

        if (pending?.TriggerCopyHigh == true &&
            RunState != null &&
            FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.CopyCard))
        {
            await DiscardRandomHandCard(choiceContext, player, cardPlay.Card);
        }
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner == null)
            return;

        if (card.Pile?.Type != PileType.Hand)
        {
            RemoveEnergyDisorderEntry(card);
            return;
        }

        if (RunState == null ||
            !FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.FlashOfInsight))
            return;
        if (!ShouldTriggerFlashOfInsight(card, oldPileType, source))
            return;

        var state = GetOrCreateTurnState(card.Owner);
        if (state.FlashOfInsightTriggers >= FlashOfInsightMaxTriggersPerTurn)
            return;

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            MainFile.ModId,
            "flash_of_insight",
            RunState.Rng.StringSeed,
            card.Owner.NetId,
            (card.CanonicalInstance?.Id ?? card.Id).Entry,
            state.FlashOfInsightTriggers,
            card.Owner.PlayerCombatState?.Hand?.Cards.Count ?? 0);
        if (roll >= FlashOfInsightCopyChance)
            return;

        state.FlashOfInsightTriggers++;
        await PersonMultiplayerEffectHelper.CopyCardToHandOrRedirectLivingFolioAsync(
            card.Owner,
            card,
            this,
            CardPilePosition.Top);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card.Owner == null || card.Pile?.Type != PileType.Hand || card.EnergyCost.CostsX)
            return false;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(card))
            return false;

        var entry = GetData().EnergyDisorderEntries
            .FirstOrDefault(existing => ReferenceEquals(existing.Card, card));
        if (entry == null)
            return false;

        modifiedCost = entry.RandomCost;
        return modifiedCost != originalCost;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var data = GetData();
        if (!data.TrackingEnemyTurn || target.Player == null || target.Side != CombatSide.Player)
            return;

        if (result.UnblockedDamage > 0m)
            GetEnemyTurnPlayerState(target.Player).TookRealDamage = true;

        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        var combatState = GetCombatState();
        if (combatState == null)
            return;

        if (side == CombatSide.Enemy)
        {
            await ResolveDexterityCounter(combatState);
            GetData().TrackingEnemyTurn = false;
        }

        if (RunState != null && FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.Thundercloud))
            await ResolveThundercloud(choiceContext, combatState);

        if (side == CombatSide.Player)
        {
            foreach (var player in participants.Select(static creature => creature.Player).Where(static player => player != null))
                ClearEnergyDisorderEntriesForPlayer(player);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var runState = RunState;
        if (runState == null ||
            !FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.Arbitrage))
            return Task.CompletedTask;

        var baseline = FinalArenaSeriesHelper.GetAndUpdateArbitrageFloorBaseline(runState);
        var floor = Math.Max(0, runState.TotalFloor);
        if (floor - baseline < ArbitrageFloorInterval)
            return Task.CompletedTask;

        var intervals = (floor - baseline) / ArbitrageFloorInterval;
        if (intervals <= 0)
            return Task.CompletedTask;

        FinalArenaSeriesHelper.SetArbitrageFloorBaseline(runState, baseline + intervals * ArbitrageFloorInterval);
        return GrantArbitrageGold(intervals);
    }

    private CombatState? GetCombatState()
    {
        return RunState?.Players.FirstOrDefault(static player => player.Creature?.CombatState != null)?.Creature?.CombatState as CombatState;
    }

    private IReadOnlyList<Player> GetStablePlayers()
    {
        return RunState?.Players
            .OrderBy(static player => player.NetId)
            .ToList() ?? [];
    }

    private Data GetData()
    {
        return RuntimeData.GetOrCreateValue(this);
    }

    private List<Creature> GetAliveEnemies(CombatState combatState)
    {
        return combatState.Enemies
            .Where(static enemy => enemy != null && enemy.IsAlive)
            .OrderBy(BuildCreatureStableKey, StringComparer.Ordinal)
            .ToList();
    }

    private async Task ApplyPirateKingsOrderAtCombatStart(IReadOnlyList<Creature> enemies, RoomType? roomType)
    {
        IEnumerable<Creature> targets = roomType switch
        {
            RoomType.Elite => enemies,
            RoomType.Boss => enemies.Where(IsBossEnemy),
            _ => []
        };

        foreach (var enemy in targets)
            await PowerCmd.Apply<PirateKingsOrderPower>(enemy, PirateKingsOrderStacks, enemy, null, false);
    }

    private async Task ApplyRobberyCombatStart(CombatState combatState)
    {
        foreach (var enemy in GetAliveEnemies(combatState))
            await CreatureCmd.Stun(enemy);

        foreach (var player in GetStablePlayers())
            if (player.Creature != null && !player.Creature.IsDead)
                await CreatureCmd.Stun(player.Creature);
    }

    private async Task ApplyToAllCreatures(CombatState combatState, Func<Creature, Task> action)
    {
        foreach (var player in combatState.Players.Where(static player => player.Creature != null))
            if (player.Creature != null && !player.Creature.IsDead)
                await action(player.Creature);

        foreach (var enemy in GetAliveEnemies(combatState))
            await action(enemy);
    }

    private async Task HealEnemiesIfBossIntentIsNonAttack()
    {
        var combatState = GetCombatState();
        if (combatState?.Encounter?.RoomType != RoomType.Boss)
            return;

        var boss = GetAliveEnemies(combatState).FirstOrDefault(IsBossEnemy);
        if (boss == null || PandaPersonaHelper.HasAttackIntent(boss))
            return;

        foreach (var enemy in GetAliveEnemies(combatState))
            await CreatureCmd.Heal(enemy, PirateKingsOrderHeal, true);
    }

    private TurnPlayerState GetOrCreateTurnState(Player player)
    {
        var states = GetData().TurnStates;
        if (states.TryGetValue(player.NetId, out var existing))
            return existing;

        existing = new TurnPlayerState();
        states[player.NetId] = existing;
        return existing;
    }

    private void ResetPlayerTurnState(Player player)
    {
        GetData().TurnStates[player.NetId] = new TurnPlayerState();
    }

    private void ResetEnemyTurnTracking()
    {
        var data = GetData();
        data.TrackingEnemyTurn = true;
        data.EnemyTurnPlayerStates.Clear();
        foreach (var player in GetStablePlayers())
            data.EnemyTurnPlayerStates[player.NetId] = new EnemyTurnPlayerState();
    }

    private EnemyTurnPlayerState GetEnemyTurnPlayerState(Player player)
    {
        var states = GetData().EnemyTurnPlayerStates;
        if (states.TryGetValue(player.NetId, out var existing))
            return existing;

        existing = new EnemyTurnPlayerState();
        states[player.NetId] = existing;
        return existing;
    }

    private PendingCardFlags? ConsumePendingCardFlags(CardModel card)
    {
        var pending = GetData().PendingCardFlags;
        if (!pending.Remove(card, out var flags))
            return null;

        return flags;
    }

    private void ApplyEnergyDisorderForCurrentHand(Player player, int globalTurnCounter)
    {
        var hand = player.PlayerCombatState?.Hand?.Cards;
        if (hand == null)
            return;

        foreach (var card in hand)
        {
            if (!WarforgeEnchantmentHelper.CountsAsAttack(card) || card.EnergyCost.CostsX)
                continue;

            var randomizedCost = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                4,
                MainFile.ModId,
                "energy_disorder",
                RunState?.Rng.StringSeed,
                player.NetId,
                globalTurnCounter,
                (card.CanonicalInstance?.Id ?? card.Id).Entry,
                FindCardIndex(hand, card));
            TrackEnergyDisorderCard(card, randomizedCost);
        }
    }

    private void TrackEnergyDisorderCard(CardModel card, decimal randomizedCost)
    {
        var entries = GetData().EnergyDisorderEntries;
        entries.RemoveAll(existing => ReferenceEquals(existing.Card, card));
        entries.Add(new EnergyDisorderEntry
        {
            Card = card,
            RandomCost = randomizedCost
        });
        card.InvokeEnergyCostChanged();
    }

    private void ClearEnergyDisorderEntriesForPlayer(Player? player)
    {
        if (player == null)
            return;

        var entries = GetData().EnergyDisorderEntries;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Card.Owner != player)
                continue;

            entries[i].Card.InvokeEnergyCostChanged();
            entries.RemoveAt(i);
        }
    }

    private void RemoveEnergyDisorderEntry(CardModel card)
    {
        var entries = GetData().EnergyDisorderEntries;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(entries[i].Card, card))
                continue;

            card.InvokeEnergyCostChanged();
            entries.RemoveAt(i);
            break;
        }
    }

    private int PeekNextGlobalPlayerTurnCounter()
    {
        if (RunState == null)
            return 0;

        if (!FinalArenaSeriesHelper.TryGetRunDataSnapshot(RunState, out _, out var runData))
            return 1;

        return Math.Max(0, runData.GlobalPlayerTurnCounter) + 1;
    }

    private async Task TriggerRefereeAttention(Player player, int globalTurnCounter)
    {
        if (RunState == null)
            return;

        if (FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.RefereeAttentionOne))
        {
            var randomPlayer = GetDeterministicRandomPlayer("referee_attention_1", player, globalTurnCounter);
            if (randomPlayer?.Creature != null)
                await AstralTemporaryDexterityPower.Apply(randomPlayer.Creature, -RefereeStatLoss, this, null, null, true);
        }

        if (FinalArenaSeriesHelper.HasComponent(RunState, FinalArenaDistortionComponent.RefereeAttentionTwo))
        {
            var combatState = GetCombatState();
            var refereePenaltyStat = RollRefereeAttentionTwoPenaltyStat(player, globalTurnCounter);
            var randomPlayer = GetDeterministicRandomPlayer("referee_attention_2_player", player, globalTurnCounter);
            if (randomPlayer?.Creature != null)
                await ApplyDeterministicTemporaryStatPenalty(
                    randomPlayer.Creature,
                    refereePenaltyStat);

            var enemy = combatState == null
                ? null
                : GetDeterministicRandomNonBossEnemy(GetAliveEnemies(combatState), "referee_attention_2_enemy", globalTurnCounter);
            if (enemy != null)
                await ApplyDeterministicTemporaryStatPenalty(
                    enemy,
                    refereePenaltyStat);
        }
    }

    private RefereePenaltyStat RollRefereeAttentionTwoPenaltyStat(Player player, int globalTurnCounter)
    {
        var useDexterity = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            2,
            MainFile.ModId,
            "referee_attention_2_shared_stat",
            RunState?.Rng.StringSeed,
            player.NetId,
            globalTurnCounter) == 0;
        return useDexterity ? RefereePenaltyStat.Dexterity : RefereePenaltyStat.Strength;
    }

    private async Task ApplyDeterministicTemporaryStatPenalty(
        Creature target,
        RefereePenaltyStat penaltyStat)
    {
        if (penaltyStat == RefereePenaltyStat.Dexterity)
            await AstralTemporaryDexterityPower.Apply(target, -RefereeStatLoss, this, null, null, true);
        else
            await AstralTemporaryStrengthPower.Apply(target, -RefereeStatLoss, this, null, null, true);
    }

    private async Task ResolvePreciseSelection(PlayerChoiceContext choiceContext, Player player, int globalTurnCounter)
    {
        var candidates = BuildPreciseSelectionCandidates(player);
        if (candidates.Count == 0)
            return;

        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            player,
            candidates,
            true,
            $"precise_selection|{globalTurnCounter}");
        if (selected == null)
            return;

        var actualCard = candidates.FirstOrDefault(card =>
            (card.CanonicalInstance?.Id ?? card.Id) == (selected.CanonicalInstance?.Id ?? selected.Id));
        if (actualCard == null)
            return;

        await PersonMultiplayerEffectHelper.MoveCombatCardToHandAndNotify(actualCard.Owner!, actualCard, CardPilePosition.Top, this);
    }

    private List<CardModel> BuildPreciseSelectionCandidates(Player player)
    {
        var candidates = new List<CardModel>(PreciseSelectionCandidateCount);
        var drawPile = PileType.Draw.GetPile(player).Cards;
        candidates.AddRange(DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                drawPile,
                card => BuildCardStableKey(card, "draw"),
                MainFile.ModId,
                "precise_selection_draw",
                RunState?.Rng.StringSeed,
                player.NetId)
            .Take(PreciseSelectionCandidateCount));

        if (candidates.Count < PreciseSelectionCandidateCount)
        {
            var needed = PreciseSelectionCandidateCount - candidates.Count;
            var discardPile = PileType.Discard.GetPile(player).Cards
                .Where(card => !candidates.Contains(card))
                .ToList();
            candidates.AddRange(DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                    discardPile,
                    card => BuildCardStableKey(card, "discard"),
                    MainFile.ModId,
                    "precise_selection_discard",
                    RunState?.Rng.StringSeed,
                    player.NetId)
                .Take(needed));
        }

        return candidates;
    }

    private async Task ResolveThundercloud(PlayerChoiceContext choiceContext, CombatState combatState)
    {
        var targets = combatState.Players
            .Select(static player => player.Creature)
            .Concat(GetAliveEnemies(combatState))
            .Where(static creature => creature != null && creature.IsAlive && creature.Block <= 0m)
            .OrderBy(creature => BuildCreatureStableKey(creature!), StringComparer.Ordinal)
            .ToList();
        foreach (var creature in targets)
            await CreatureCmdCompat.Damage(choiceContext, creature!, ThundercloudDamage, ValueProp.Move, null, null);
    }

    private async Task ResolveDexterityCounter(CombatState combatState)
    {
        foreach (var player in GetStablePlayers())
        {
            var creature = player.Creature;
            if (creature == null || creature.IsDead)
                continue;

            var state = GetEnemyTurnPlayerState(player);
            if (creature.Block <= 0m && state.TookRealDamage)
                continue;

            await PowerCmd.Apply<CounterPower>(creature, 1m, creature, null, false);
        }
    }

    private async Task GrantArbitrageGold(int intervals)
    {
        foreach (var player in GetStablePlayers())
        {
            var currentGold = player.Gold;
            if (currentGold <= 0m)
                continue;

            var goldToGain = Math.Floor(currentGold * ArbitragePercent * intervals);
            if (goldToGain <= 0m)
                continue;

            await PlayerCmd.GainGold(goldToGain, player);
        }
    }

    private async Task DiscardRandomHandCard(PlayerChoiceContext choiceContext, Player player, CardModel cardSource)
    {
        var handCards = player.PlayerCombatState?.Hand?.Cards
            .Where(card => !ReferenceEquals(card, cardSource))
            .ToList();
        if (handCards == null || handCards.Count == 0)
            return;

        var chosen = DeterministicMultiplayerChoiceHelper.PickDeterministically(
            handCards,
            card => BuildCardStableKey(card, "discard"),
            MainFile.ModId,
            "copy_card_discard",
            RunState?.Rng.StringSeed,
            player.NetId,
            handCards.Count);
        if (chosen == null)
            return;

        await CardCmd.Discard(choiceContext, [chosen]);
    }

    private bool ShouldTriggerFlashOfInsight(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner?.Creature?.CombatState == null)
            return false;
        if (card.Type is CardType.Status or CardType.Curse)
            return false;
        if (AstralCardTags.HasPersonSkillTag(card) || PersonRelicHelper.IsPersonSkillCard(card))
            return false;
        if (oldPileType == PileType.Draw)
            return false;
        if (ReferenceEquals(source, this))
            return false;
        if (source is CardModel sourceCard && sourceCard.Owner == card.Owner)
            return false;

        return true;
    }

    private Player? GetDeterministicRandomPlayer(string context, Player actingPlayer, int turnSeed)
    {
        return DeterministicMultiplayerChoiceHelper.PickDeterministically(
            GetStablePlayers().Where(static player => player.Creature != null && !player.Creature.IsDead).ToList(),
            player => player.NetId.ToString(),
            MainFile.ModId,
            context,
            RunState?.Rng.StringSeed,
            actingPlayer.NetId,
            turnSeed);
    }

    private Player? GetDeterministicRandomPlayerOrNull(string context, Player actingPlayer, int turnSeed)
    {
        var candidates = GetStablePlayers()
            .Where(static player => player.Creature != null && !player.Creature.IsDead)
            .OrderBy(static player => player.NetId)
            .Cast<Player?>()
            .ToList();
        candidates.Add(null);

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            context,
            RunState?.Rng.StringSeed,
            actingPlayer.NetId,
            turnSeed);
        return candidates[roll];
    }

    private Creature? GetDeterministicRandomNonBossEnemy(
        IReadOnlyList<Creature> aliveEnemies,
        string context,
        int turnSeed)
    {
        var candidates = aliveEnemies
            .Where(enemy => !IsBossEnemy(enemy))
            .OrderBy(BuildCreatureStableKey, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return null;

        return DeterministicMultiplayerChoiceHelper.PickDeterministically(
            candidates,
            BuildCreatureStableKey,
            MainFile.ModId,
            context,
            RunState?.Rng.StringSeed,
            turnSeed,
            candidates.Count);
    }

    private int GetPlayerTurnSequenceSeed(Player player)
    {
        var state = GetOrCreateTurnState(player);
        return state.CardsPlayed;
    }

    private string BuildCardStableKey(CardModel card, string pileTag)
    {
        var pile = card.Pile;
        var index = pile?.Cards == null ? -1 : FindCardIndex(pile.Cards, card);
        var ownerKey = card.Owner?.NetId.ToString() ?? "no_owner";
        return $"{pileTag}|{ownerKey}|{(card.CanonicalInstance?.Id ?? card.Id).Entry}|{index:D3}";
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
        if (!string.IsNullOrWhiteSpace(bossNodePath) &&
            bossNodePath.Contains(slotName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private void ClearCombatRuntimeState()
    {
        var data = GetData();
        data.TurnStates.Clear();
        data.PendingCardFlags.Clear();
        foreach (var entry in data.EnergyDisorderEntries)
            entry.Card.InvokeEnergyCostChanged();
        data.EnergyDisorderEntries.Clear();
        data.EnemyTurnPlayerStates.Clear();
        data.PendingPreciseSelectionDrawCounts.Clear();
        data.PendingPreciseSelectionTurnSeeds.Clear();
        data.TrackingEnemyTurn = false;
    }

    private static int FindCardIndex(IReadOnlyList<CardModel> cards, CardModel target)
    {
        for (var i = 0; i < cards.Count; i++)
            if (ReferenceEquals(cards[i], target))
                return i;

        return -1;
    }

    private string BuildCreatureStableKey(Creature creature)
    {
        if (creature.Player != null)
            return $"player:{creature.Player.NetId:D20}";

        if (creature.PetOwner != null)
            return $"pet:{creature.PetOwner.NetId:D20}:{creature.ModelId.Entry}";

        var combatState = creature.CombatState as CombatState;
        var enemyIndex = -1;
        if (combatState != null)
            for (var i = 0; i < combatState.Enemies.Count; i++)
                if (ReferenceEquals(combatState.Enemies[i], creature))
                {
                    enemyIndex = i;
                    break;
                }

        var monsterEntry = creature.Monster?.Id.Entry ?? creature.ModelId.Entry;
        return $"enemy:{enemyIndex:D3}:{monsterEntry}:{creature.MaxHp}";
    }
}
