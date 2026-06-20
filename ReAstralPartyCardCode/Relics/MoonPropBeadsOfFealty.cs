using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropBeadsOfFealty : AstralPartyRelicModel
{
    private const string EliteRollContextId = "moon_prop_beads_of_fealty_elite";
    private const string RewardRollContextId = "moon_prop_beads_of_fealty_reward";

    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect { get; set; }
    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat { get; set; }
    [SavedProperty] public int AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward { get; set; }
    [SavedProperty] public int AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMoonPropId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
            return;

        AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect = true;
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = false;
    }

    public override async Task BeforeCombatStart()
    {
        if (!AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect
            || AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat
            || Owner?.Creature?.CombatState is not { } combatState
            || Owner.RunState is not { } runState
            || Owner.RunState?.CurrentRoom is not CombatRoom)
            return;

        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads pending combat effect accepted | ownerNetId={Owner.NetId} | isLocal={LocalContext.IsMe(Owner)} | isHost={RunManager.Instance?.NetService?.Type == NetGameType.Host} | act={runState.CurrentActIndex} | floor={runState.TotalFloor} | encounter={combatState.Encounter?.Id.Entry ?? "<null_encounter>"}");

        var eliteCandidates = GetEliteEncounterCandidates(Owner).ToList();
        if (eliteCandidates.Count == 0)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] MoonPropBeadsOfFealty could not find any elite encounter candidates for player {Owner.NetId}.");
            return;
        }

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            eliteCandidates.Count,
            MainFile.ModId,
            RelicId,
            EliteRollContextId,
            Owner.RunState?.Rng.StringSeed ?? "<null_seed>",
            Owner.RunState?.CurrentActIndex ?? -1,
            Owner.RunState?.TotalFloor ?? -1,
            Owner.NetId,
            combatState.Encounter?.Id.Entry ?? "<null_encounter>");
        var selectedEncounter = eliteCandidates[selectedIndex].ToMutable();
        selectedEncounter.GenerateMonstersWithSlots(runState);

        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads selected elite encounter | ownerNetId={Owner.NetId} | encounterId={selectedEncounter.Id.Entry} | monsters={selectedEncounter.MonstersWithSlots.Count} | isLocal={LocalContext.IsMe(Owner)}");

        AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward = selectedEncounter.MinGoldReward;
        AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward = selectedEncounter.MaxGoldReward;

        await RequestSynchronizedEliteSpawnAsync(
            combatState as MegaCrit.Sts2.Core.Combat.CombatState
            ?? throw new InvalidOperationException("Expected CombatState for elite encounter injection."),
            selectedEncounter);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat || Owner == null)
            return;

        AddStandardEliteRewards(room, Owner);
        AddSharedBeadsRewards(room);
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads elite rewards granted after shared spawn | ownerNetId={Owner.NetId} | roomType={room.RoomType} | encounter={room.CombatState?.Encounter?.Id.Entry ?? "<null_encounter>"}");

        AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect = false;
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = false;
        AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward = 0;
        AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward = 0;
        await RelicCmd.Remove(this);
    }

    private static IEnumerable<EncounterModel> GetEliteEncounterCandidates(Player owner)
    {
        return owner.RunState?.Act?.AllEliteEncounters
                   .Where(IsBaseGameEliteEncounter)
               ?? [];
    }

    private static bool IsBaseGameEliteEncounter(EncounterModel encounter)
    {
        return encounter.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Encounters";
    }

    private async Task RequestSynchronizedEliteSpawnAsync(CombatState combatState, EncounterModel encounter)
    {
        if (RunManager.Instance?.ActionQueueSynchronizer == null || LocalContext.NetId is not { } localNetId)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] beads shared spawn unavailable | ownerNetId={Owner?.NetId} | encounterId={encounter.Id.Entry}");
            return;
        }

        var hookContext = new HookPlayerChoiceContext(this, localNetId, combatState, GameActionType.Combat);
        var hookAction = RunManager.Instance.ActionQueueSynchronizer.GenerateHookAction(Owner!.NetId, GameActionType.Combat);
        var executionStarted = new TaskCompletionSource();
        _ = hookAction.ExecutionStartedTask.ContinueWith(
            _ => executionStarted.TrySetResult(),
            TaskScheduler.Default);

        var spawnTask = ExecuteSharedEliteSpawnAsync(executionStarted.Task, combatState, encounter);
        var assignTask = hookContext.AssignTaskAndWaitForPauseOrCompletion(spawnTask);
        hookAction.SetChoiceContext(hookContext);

        if (LocalContext.IsMe(Owner))
        {
            Flash();
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] beads enqueue shared elite spawn action | ownerNetId={Owner.NetId} | encounterId={encounter.Id.Entry} | hookId={hookAction.HookId} | isHost={RunManager.Instance.NetService.Type == NetGameType.Host}");
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueueHookAction(hookAction);
        }
        else
        {
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] beads skipped local direct spawn on client | ownerNetId={Owner?.NetId} | encounterId={encounter.Id.Entry} | localNetId={localNetId}");
        }

        await assignTask;
        await hookContext.WaitForCompletion();
    }

    private async Task ExecuteSharedEliteSpawnAsync(Task executionStartedTask, CombatState combatState, EncounterModel encounter)
    {
        await executionStartedTask;
        await AddEliteEncounterGroup(combatState, encounter);
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = true;
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads shared elite spawn action executed | ownerNetId={Owner?.NetId} | encounterId={encounter.Id.Entry} | roomAct={Owner?.RunState?.CurrentActIndex ?? -1} | roomFloor={Owner?.RunState?.TotalFloor ?? -1}");
    }

    private static async Task AddEliteEncounterGroup(CombatState combatState, EncounterModel encounter)
    {
        ValidateEncounterSlots(combatState, encounter);
        var addedMonsters = new List<MonsterModel>();
        foreach (var (monster, slot) in encounter.MonstersWithSlots)
        {
            var creature = combatState.CreateCreature(monster, CombatSide.Enemy, slot);
            combatState.AddCreature(creature);
            CombatManager.Instance.AddCreature(creature);
            NCombatRoom.Instance?.AddCreature(creature);
            await creature.AfterAddedToRoom();
            await Hook.AfterCreatureAddedToCombat(combatState, creature);
            addedMonsters.Add(creature.Monster);
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] beads spawned monster with original slot | encounterId={encounter.Id.Entry} | monsterId={monster.Id.Entry} | slot={slot ?? "<null_slot>"}");
        }

        foreach (var monster in addedMonsters)
        {
            await monster.BeforeCombatStart();
            monster.InvokeExecutionFinished();
        }

        foreach (var monster in addedMonsters)
        {
            await monster.BeforeCombatStartLate();
            monster.InvokeExecutionFinished();
        }
    }

    private static void ValidateEncounterSlots(CombatState combatState, EncounterModel encounter)
    {
        var occupiedSlots = combatState.Enemies
            .Select(enemy => enemy.SlotName)
            .Where(static slot => !string.IsNullOrWhiteSpace(slot))
            .ToHashSet(StringComparer.Ordinal);
        var encounterSlots = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (monster, slot) in encounter.MonstersWithSlots)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                MainFile.Logger.Error(
                    $"[{MainFile.ModId}] beads encountered invalid original slot | encounterId={encounter.Id.Entry} | monsterId={monster.Id.Entry} | reason=empty_slot");
                throw new InvalidOperationException(
                    $"Encounter {encounter.Id.Entry} produced an empty slot for monster {monster.Id.Entry}.");
            }

            if (!encounterSlots.Add(slot))
            {
                MainFile.Logger.Error(
                    $"[{MainFile.ModId}] beads encountered duplicate original slot | encounterId={encounter.Id.Entry} | monsterId={monster.Id.Entry} | slot={slot}");
                throw new InvalidOperationException(
                    $"Encounter {encounter.Id.Entry} produced duplicate slot {slot}.");
            }

            if (occupiedSlots.Contains(slot))
            {
                MainFile.Logger.Error(
                    $"[{MainFile.ModId}] beads encountered occupied original slot | encounterId={encounter.Id.Entry} | monsterId={monster.Id.Entry} | slot={slot}");
                throw new InvalidOperationException(
                    $"Encounter {encounter.Id.Entry} attempted to use occupied slot {slot}.");
            }
        }
    }

    private void AddStandardEliteRewards(CombatRoom room, Player player)
    {
        room.AddExtraReward(player, new GoldReward(
            AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward,
            AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward,
            player));

        if (RunManager.Instance?.AscensionManager is { } ascensionManager
            && player.PlayerOdds.PotionReward.Roll(player, ascensionManager, RoomType.Elite))
        {
            room.AddExtraReward(player, new PotionReward(player));
        }

        room.AddExtraReward(player, new CardReward(CardCreationOptions.ForRoom(player, RoomType.Elite), 3, player));
        room.AddExtraReward(player, new RelicReward(player));
    }

    private void AddSharedBeadsRewards(CombatRoom room)
    {
        if (Owner?.RunState?.Players == null)
            return;

        var excludedRelicIds = new[] { ModelDb.Relic<MoonPropBeadsOfFealty>().Id };
        foreach (var player in Owner.RunState.Players.OrderBy(static player => player.NetId))
        {
            room.AddExtraReward(player, new CardReward(GetCardRewardOptionsForRoom(player, room.RoomType), 3, player));

            if (TryCreateVanillaPotionReward(player, commonOnly: false) is { } nonCommonPotion)
                room.AddExtraReward(player, new PotionReward(nonCommonPotion, player));

            if (TryCreateVanillaPotionReward(player, commonOnly: true) is { } commonPotion)
                room.AddExtraReward(player, new PotionReward(commonPotion, player));

            if (TryCreateNonCommonNonMoonRelic(player) is { } rewardRelic)
                room.AddExtraReward(player, new RelicReward(rewardRelic, player));

            var moonRelic = MoonPropShopExtraRelicsHelper.CreateDeterministicMoonPropRelicExcluding(
                player,
                RewardRollContextId,
                excludedRelicIds,
                Owner.NetId,
                player.NetId,
                room.CombatState?.Encounter?.Id.Entry ?? "<null_encounter>");
            room.AddExtraReward(player, new RelicReward(moonRelic, player));

            var goldAmount = player == Owner ? 300 : 150;
            room.AddExtraReward(player, new GoldReward(goldAmount, player, false));
        }
    }

    private static CardCreationOptions GetCardRewardOptionsForRoom(Player player, RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Monster or RoomType.Elite or RoomType.Boss => CardCreationOptions.ForRoom(player, roomType),
            _ => CardCreationOptions.ForRoom(player, RoomType.Monster)
        };
    }

    private static PotionModel? TryCreateVanillaPotionReward(Player player, bool commonOnly)
    {
        var candidates = PotionFactory.GetPotionOptions(player, [])
            .Where(potion => IsVanillaPotion(potion)
                             && (commonOnly
                                 ? potion.Rarity == PotionRarity.Common
                                 : potion.Rarity is not PotionRarity.Common))
            .ToList();
        if (candidates.Count == 0)
            return null;

        return player.PlayerRng.Rewards.NextItem(candidates).ToMutable();
    }

    private static RelicModel? TryCreateNonCommonNonMoonRelic(Player player)
    {
        var primaryRarity = player.PlayerRng.Rewards.NextFloat() < 0.66f
            ? RelicRarity.Uncommon
            : RelicRarity.Rare;
        return TryPullRelic(player, primaryRarity)
               ?? TryPullRelic(player, primaryRarity == RelicRarity.Uncommon ? RelicRarity.Rare : RelicRarity.Uncommon);
    }

    private static RelicModel? TryPullRelic(Player player, RelicRarity rarity)
    {
        if (player.RunState == null)
            return null;

        var relic = player.RelicGrabBag.PullFromFront(
            rarity,
            static candidate => candidate.Rarity is not RelicRarity.Common
                                && !MoonPropShopExtraRelicsHelper.IsMoonPropRelic(candidate),
            player.RunState);
        if (relic == null)
            return null;

        player.RunState.SharedRelicGrabBag.Remove(relic);
        return relic.ToMutable();
    }

    private static bool IsVanillaPotion(PotionModel potion)
    {
        return potion.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Potions"
               && potion.Rarity is PotionRarity.Common or PotionRarity.Uncommon or PotionRarity.Rare;
    }
}
