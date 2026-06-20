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
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropBeadsOfFealty : AstralPartyRelicModel
{
    private const string EliteRollContextId = "moon_prop_beads_of_fealty_elite";
    private const string RewardRollContextId = "moon_prop_beads_of_fealty_reward";
    private const int SpawnActionSchemaVersion = 1;

    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect { get; set; }
    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat { get; set; }
    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyRewardsGrantedThisCombat { get; set; }
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
        AstralParty_MoonPropBeadsOfFealtyRewardsGrantedThisCombat = false;
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
        AstralParty_MoonPropBeadsOfFealtyRewardsGrantedThisCombat = false;

        var snapshot = CreateEncounterSnapshot(Owner, selectedEncounter);
        await RequestSynchronizedEliteSpawnAsync(snapshot);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat || Owner == null)
            return;

        AddStandardEliteRewards(room, Owner);
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads elite rewards granted | ownerNetId={Owner.NetId} | encounter={room.CombatState?.Encounter?.Id.Entry ?? "<null_encounter>"} | minGold={AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward} | maxGold={AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward}");
        AddSharedBeadsRewards(room);
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads shared moon rewards granted | ownerNetId={Owner.NetId} | roomType={room.RoomType} | encounter={room.CombatState?.Encounter?.Id.Entry ?? "<null_encounter>"}");

        AstralParty_MoonPropBeadsOfFealtyRewardsGrantedThisCombat = true;
        AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect = false;
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = false;
        AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward = 0;
        AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward = 0;
        await RelicCmd.Remove(this);
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads relic removed after resolved combat | ownerNetId={Owner.NetId}");
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

    private Task RequestSynchronizedEliteSpawnAsync(BeadsEliteEncounterSnapshot snapshot)
    {
        if (RunManager.Instance?.ActionQueueSynchronizer == null || Owner == null)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] beads shared spawn unavailable | ownerNetId={Owner?.NetId} | encounterId={snapshot.EncounterId}");
            return Task.CompletedTask;
        }

        if (LocalContext.IsMe(Owner))
        {
            Flash();
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] beads enqueue shared elite spawn action | ownerNetId={Owner.NetId} | encounterId={snapshot.EncounterId} | monsterCount={snapshot.Monsters.Count} | slots={string.Join(",", snapshot.Monsters.Select(static monster => monster.Slot))} | isHost={RunManager.Instance.NetService.Type == NetGameType.Host}");
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
                new MoonPropBeadsOfFealtySpawnAction(Owner, snapshot));
        }
        else
        {
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] beads skipped local direct spawn on client | ownerNetId={Owner.NetId} | encounterId={snapshot.EncounterId} | localNetId={LocalContext.NetId}");
        }

        return Task.CompletedTask;
    }

    private static async Task<bool> AddEliteEncounterGroup(CombatState combatState, BeadsEliteEncounterSnapshot snapshot)
    {
        var addedMonsters = new List<MonsterModel>();
        foreach (var monsterEntry in snapshot.Monsters)
        {
            try
            {
                var canonicalMonster = ModelDb.GetById<MonsterModel>(ModelId.Deserialize(monsterEntry.MonsterId));
                var mutableMonster = canonicalMonster.ToMutable();
                var resolvedSlot = ResolveSpawnSlot(combatState, snapshot.EncounterId, monsterEntry);
                var creature = await CreatureCmdCompat.Add(mutableMonster, combatState, CombatSide.Enemy, resolvedSlot);
                addedMonsters.Add(creature.Monster);
                MainFile.Logger.Info(
                    $"[{MainFile.ModId}] beads spawned monster with original slot | encounterId={snapshot.EncounterId} | monsterId={monsterEntry.MonsterId} | slot={resolvedSlot ?? "<auto_slot>"}");
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error(
                    $"[{MainFile.ModId}] beads failed to add elite monster; aborting encounter group | encounterId={snapshot.EncounterId} | monsterId={monsterEntry.MonsterId} | message={ex.Message}");
                return false;
            }
        }

        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads spawned full elite group | encounterId={snapshot.EncounterId} | monsterCount={snapshot.Monsters.Count} | slots={string.Join(",", snapshot.Monsters.Select(static monster => monster.Slot))}");
        return true;
    }

    private static string? ResolveSpawnSlot(CombatState combatState, string encounterId, BeadsEliteMonsterEntry monsterEntry)
    {
        var occupiedSlots = combatState.Enemies
            .Select(enemy => enemy.SlotName)
            .Where(static slot => !string.IsNullOrWhiteSpace(slot))
            .ToHashSet(StringComparer.Ordinal);
        var originalSlot = string.IsNullOrWhiteSpace(monsterEntry.Slot) ? null : monsterEntry.Slot;
        if (!string.IsNullOrWhiteSpace(originalSlot)
            && !occupiedSlots.Contains(originalSlot)
            && CanUseOriginalEncounterSlot(originalSlot))
            return originalSlot;

        if (!string.IsNullOrWhiteSpace(originalSlot))
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] beads original slot unavailable in current combat room | encounterId={encounterId} | monsterId={monsterEntry.MonsterId} | originalSlot={originalSlot} | hasEncounterSlots={HasEncounterSlots()}");
        }

        var fallbackSlot = combatState.Encounter?.GetNextSlot(combatState);
        if (!string.IsNullOrWhiteSpace(fallbackSlot))
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] beads using safe fallback slot | encounterId={encounterId} | monsterId={monsterEntry.MonsterId} | resolvedSlot={fallbackSlot}");
            return fallbackSlot;
        }

        MainFile.Logger.Warn(
            $"[{MainFile.ModId}] beads fallback slot unresolved; using auto layout | encounterId={encounterId} | monsterId={monsterEntry.MonsterId} | originalSlot={originalSlot ?? "<null_slot>"}");
        return null;
    }

    private static bool CanUseOriginalEncounterSlot(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot) || NCombatRoom.Instance == null)
            return false;

        var encounterSlots = TryGetEncounterSlots();
        if (encounterSlots == null)
            return false;

        if (encounterSlots is System.Collections.IDictionary dictionary)
            return dictionary.Contains(slot);

        return encounterSlots is IEnumerable<object> enumerable
               && enumerable.Any(entry => string.Equals(entry?.ToString(), slot, StringComparison.Ordinal));
    }

    private static bool HasEncounterSlots()
    {
        return TryGetEncounterSlots() != null;
    }

    private static object? TryGetEncounterSlots()
    {
        return typeof(NCombatRoom).GetProperty("EncounterSlots")?.GetValue(NCombatRoom.Instance);
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

    private static BeadsEliteEncounterSnapshot CreateEncounterSnapshot(Player owner, EncounterModel encounter)
    {
        var monsters = encounter.MonstersWithSlots
            .Select(static entry => new BeadsEliteMonsterEntry(
                entry.Item1.Id.ToString(),
                entry.Item2 ?? string.Empty))
            .ToList();
        var snapshot = new BeadsEliteEncounterSnapshot(
            owner.NetId,
            encounter.Id.Entry,
            encounter.MinGoldReward,
            encounter.MaxGoldReward,
            monsters);
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads resolved elite encounter snapshot | ownerNetId={owner.NetId} | encounterId={snapshot.EncounterId} | monsterCount={snapshot.Monsters.Count} | slots={string.Join(",", snapshot.Monsters.Select(static monster => monster.Slot))}");
        return snapshot;
    }

    private static async Task ExecuteSharedEliteSpawnAsync(Player owner, BeadsEliteEncounterSnapshot snapshot)
    {
        if (owner.Creature?.CombatState is not CombatState combatState || owner.RunState == null)
            throw new InvalidOperationException("MoonPropBeadsOfFealty shared spawn requires an active combat state.");

        var spawnSucceeded = await AddEliteEncounterGroup(combatState, snapshot);
        if (!spawnSucceeded)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] beads shared elite spawn action aborted before completion | ownerNetId={owner.NetId} | encounterId={snapshot.EncounterId}");
            return;
        }

        var relic = owner.GetRelic<MoonPropBeadsOfFealty>();
        if (relic != null)
        {
            relic.AstralParty_MoonPropBeadsOfFealtyEliteMinGoldReward = snapshot.EliteMinGoldReward;
            relic.AstralParty_MoonPropBeadsOfFealtyEliteMaxGoldReward = snapshot.EliteMaxGoldReward;
            relic.AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = true;
        }

        MainFile.Logger.Info(
            $"[{MainFile.ModId}] beads shared elite spawn action executed | ownerNetId={owner.NetId} | encounterId={snapshot.EncounterId} | monsterCount={snapshot.Monsters.Count} | roomAct={owner.RunState.CurrentActIndex} | roomFloor={owner.RunState.TotalFloor}");
    }

    private sealed class MoonPropBeadsOfFealtySpawnAction(Player owner, BeadsEliteEncounterSnapshot snapshot) : GameAction
    {
        public Player Owner { get; } = owner;

        public BeadsEliteEncounterSnapshot Snapshot { get; } = snapshot;

        public override ulong OwnerId => Owner.NetId;

        public override GameActionType ActionType => GameActionType.Combat;

        protected override async Task ExecuteAction()
        {
            await ExecuteSharedEliteSpawnAsync(Owner, Snapshot);
        }

        public override INetAction ToNetAction()
        {
            return new MoonPropBeadsOfFealtySpawnNetAction
            {
                Snapshot = Snapshot
            };
        }
    }

    private sealed class MoonPropBeadsOfFealtySpawnNetAction : INetAction
    {
        public BeadsEliteEncounterSnapshot Snapshot { get; set; } = BeadsEliteEncounterSnapshot.Empty;

        public GameAction ToGameAction(Player player)
        {
            return new MoonPropBeadsOfFealtySpawnAction(player, Snapshot);
        }

        public void Serialize(PacketWriter writer)
        {
            writer.WriteInt(SpawnActionSchemaVersion);
            writer.WriteULong(Snapshot.OwnerNetId);
            writer.WriteString(Snapshot.EncounterId);
            writer.WriteInt(Snapshot.EliteMinGoldReward);
            writer.WriteInt(Snapshot.EliteMaxGoldReward);
            writer.WriteInt(Snapshot.Monsters.Count);
            foreach (var monster in Snapshot.Monsters)
            {
                writer.WriteString(monster.MonsterId);
                writer.WriteString(monster.Slot);
            }
        }

        public void Deserialize(PacketReader reader)
        {
            _ = reader.ReadInt();
            var ownerNetId = reader.ReadULong();
            var encounterId = reader.ReadString();
            var minGoldReward = reader.ReadInt();
            var maxGoldReward = reader.ReadInt();
            var monsterCount = reader.ReadInt();
            var monsters = new List<BeadsEliteMonsterEntry>(monsterCount);
            for (var i = 0; i < monsterCount; i++)
                monsters.Add(new BeadsEliteMonsterEntry(reader.ReadString(), reader.ReadString()));

            Snapshot = new BeadsEliteEncounterSnapshot(ownerNetId, encounterId, minGoldReward, maxGoldReward, monsters);
        }
    }

    private readonly record struct BeadsEliteMonsterEntry(string MonsterId, string Slot);

    private sealed record BeadsEliteEncounterSnapshot(
        ulong OwnerNetId,
        string EncounterId,
        int EliteMinGoldReward,
        int EliteMaxGoldReward,
        IReadOnlyList<BeadsEliteMonsterEntry> Monsters)
    {
        public static readonly BeadsEliteEncounterSnapshot Empty = new(
            0UL,
            string.Empty,
            0,
            0,
            []);
    }
}
