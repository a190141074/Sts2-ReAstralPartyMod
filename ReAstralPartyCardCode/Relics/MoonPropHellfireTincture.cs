using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropHellfireTincture : MoonPropStackableRelicBase
{
    private const decimal BaseMaxHpRatio = 0.025m;
    private const decimal AllyRatio = 0.25m;
    private const decimal EnemyMultiplier = 12m;
    private const int SharedActionSchemaVersion = 1;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("SelfDamagePercent", GetSelfDamagePercentText()),
        new StringVar("AllyDamagePercent", GetAllyDamagePercentText()),
        new StringVar("EnemyMultiplier", GetEnemyMultiplierText())
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var ownerCreature = Owner.Creature;
        var stacks = GetStacks();
        var baseDamage = GetRoundedUpDamage(ownerCreature.MaxHp * BaseMaxHpRatio * stacks);
        if (baseDamage <= 0m)
            return;

        var allyDamage = GetRoundedUpDamage(baseDamage * AllyRatio * stacks);
        var enemyDamage = baseDamage * GetEnemyDamageMultiplier(stacks);

        var selfRecipient = ResolveHellfireRecipient(ownerCreature);
        var teammates = CombatTargetSnapshotHelper.GetAliveTeammates(ownerCreature);
        var opponents = CombatTargetSnapshotHelper.GetAliveOpponents(ownerCreature);
        var snapshot = CreateDamageSnapshot(ownerCreature, selfRecipient, teammates, opponents, baseDamage, allyDamage, enemyDamage);

        MainFile.Logger.Info(
            $"[MoonPropHellfireTincture] turn start | owner={Owner.NetId} | stacks={stacks} | baseDamage={baseDamage} | allyDamage={allyDamage} | enemyDamage={enemyDamage} | self={DescribeTarget(ownerCreature)} | selfRecipient={DescribeTarget(selfRecipient)} | teammateCount={teammates.Count} | opponentCount={opponents.Count} | hitCount={snapshot.Entries.Count}");

        if (RunManager.Instance?.ActionQueueSynchronizer == null)
        {
            MainFile.Logger.Warn(
                $"[MoonPropHellfireTincture] shared action unavailable; falling back to local execution | owner={Owner.NetId} | hitCount={snapshot.Entries.Count}");
            await ExecuteSharedDamageAsync(Owner, snapshot);
            return;
        }

        if (!LocalContext.IsMe(Owner))
        {
            MainFile.Logger.Info(
                $"[MoonPropHellfireTincture] skipped local direct execution on non-owner peer | owner={Owner.NetId} | localNetId={LocalContext.NetId?.ToString() ?? "<none>"} | hitCount={snapshot.Entries.Count}");
            return;
        }

        Flash();
        MainFile.Logger.Info(
            $"[MoonPropHellfireTincture] enqueue shared action | owner={Owner.NetId} | hitCount={snapshot.Entries.Count} | targets={string.Join(",", snapshot.Entries.Select(static entry => entry.Descriptor))} | isHost={RunManager.Instance.NetService.Type == NetGameType.Host}");
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new MoonPropHellfireTinctureDamageAction(Owner, snapshot));
    }

    private static decimal GetRoundedUpDamage(decimal rawDamage)
    {
        return rawDamage <= 0m ? 0m : Math.Ceiling(rawDamage);
    }

    private static decimal GetEnemyDamageMultiplier(int stacks)
    {
        return EnemyMultiplier + Math.Max(0, stacks - 1);
    }

    private static Creature ResolveHellfireRecipient(Creature target)
    {
        var osty = target.Player?.Osty;
        return osty is { IsAlive: true } ? osty : target;
    }

    private static HellfireDamageSnapshot CreateDamageSnapshot(
        Creature ownerCreature,
        Creature selfRecipient,
        IReadOnlyList<Creature> teammates,
        IReadOnlyList<Creature> opponents,
        decimal selfDamage,
        decimal allyDamage,
        decimal enemyDamage)
    {
        var entries = new List<HellfireDamageEntry>();

        entries.Add(CreateDamageEntry(selfRecipient, selfDamage));

        if (allyDamage > 0m)
        {
            foreach (var ally in teammates)
                entries.Add(CreateDamageEntry(ResolveHellfireRecipient(ally), allyDamage));
        }

        foreach (var enemy in opponents)
            entries.Add(CreateDamageEntry(enemy, enemyDamage));

        return new HellfireDamageSnapshot(ownerCreature.Player?.NetId ?? 0UL, entries);
    }

    private static HellfireDamageEntry CreateDamageEntry(Creature target, decimal damage)
    {
        return new HellfireDamageEntry(
            target.CombatId ?? 0u,
            target.ModelId.ToString(),
            target.SlotName ?? string.Empty,
            target.Player?.NetId ?? 0UL,
            Decimal.ToInt32(damage),
            DescribeTarget(target));
    }

    private static async Task ExecuteSharedDamageAsync(Player owner, HellfireDamageSnapshot snapshot)
    {
        if (owner.Creature?.CombatState is not { } combatState)
            throw new InvalidOperationException("MoonPropHellfireTincture shared damage requires an active combat state.");

        foreach (var entry in snapshot.Entries)
        {
            var target = ResolveSnapshotTarget(combatState, entry);
            if (target == null)
            {
                MainFile.Logger.Error(
                    $"[MoonPropHellfireTincture] failed to resolve shared target | owner={owner.NetId} | descriptor={entry.Descriptor} | combatId={entry.CombatId} | playerNetId={entry.PlayerNetId}");
                continue;
            }

            MainFile.Logger.Info(
                $"[MoonPropHellfireTincture] shared tick | owner={owner.NetId} | resolved={DescribeTarget(target)} | damage={entry.Damage}");
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                target,
                entry.Damage,
                ValueProp.Unpowered,
                owner.Creature,
                null);
        }
    }

    private static Creature? ResolveSnapshotTarget(ICombatState combatState, HellfireDamageEntry entry)
    {
        if (entry.CombatId != 0u)
        {
            var byCombatId = combatState.Creatures.FirstOrDefault(creature => creature.CombatId == entry.CombatId);
            if (byCombatId != null)
                return byCombatId;
        }

        if (entry.PlayerNetId != 0UL)
        {
            var byPlayer = combatState.Players.FirstOrDefault(player => player.NetId == entry.PlayerNetId)?.Creature;
            if (byPlayer != null)
                return byPlayer;
        }

        return combatState.Creatures.FirstOrDefault(creature =>
            string.Equals(creature.ModelId.ToString(), entry.ModelId, StringComparison.Ordinal)
            && string.Equals(creature.SlotName ?? string.Empty, entry.Slot, StringComparison.Ordinal));
    }

    private static string DescribeTarget(Creature? creature)
    {
        if (creature == null)
            return "<null>";

        var playerNetId = creature.Player?.NetId.ToString() ?? "<none>";
        return
            $"{creature.ModelId}|player={playerNetId}|combatId={creature.CombatId?.ToString() ?? "<none>"}|slot={creature.SlotName ?? "<none>"}|hp={creature.CurrentHp}";
    }

    private string GetSelfDamagePercentText()
    {
        return FormatPercent(BaseMaxHpRatio * GetStacks());
    }

    private string GetAllyDamagePercentText()
    {
        return FormatPercent(AllyRatio * GetStacks());
    }

    private string GetEnemyMultiplierText()
    {
        return FormatValue(GetEnemyDamageMultiplier(GetStacks()));
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("SelfDamagePercent", GetSelfDamagePercentText());
        SetDynamicString("AllyDamagePercent", GetAllyDamagePercentText());
        SetDynamicString("EnemyMultiplier", GetEnemyMultiplierText());
    }

    private sealed class MoonPropHellfireTinctureDamageAction(Player owner, HellfireDamageSnapshot snapshot) : GameAction
    {
        public override ulong OwnerId => owner.NetId;

        public override GameActionType ActionType => GameActionType.Combat;

        protected override async Task ExecuteAction()
        {
            await ExecuteSharedDamageAsync(owner, snapshot);
        }

        public override INetAction ToNetAction()
        {
            return new MoonPropHellfireTinctureDamageNetAction
            {
                Snapshot = snapshot
            };
        }
    }

    private sealed class MoonPropHellfireTinctureDamageNetAction : INetAction
    {
        public HellfireDamageSnapshot Snapshot { get; set; } = HellfireDamageSnapshot.Empty;

        public GameAction ToGameAction(Player player)
        {
            return new MoonPropHellfireTinctureDamageAction(player, Snapshot);
        }

        public void Serialize(PacketWriter writer)
        {
            writer.WriteInt(SharedActionSchemaVersion);
            writer.WriteULong(Snapshot.OwnerNetId);
            writer.WriteInt(Snapshot.Entries.Count);
            foreach (var entry in Snapshot.Entries)
            {
                writer.WriteUInt(entry.CombatId);
                writer.WriteString(entry.ModelId);
                writer.WriteString(entry.Slot);
                writer.WriteULong(entry.PlayerNetId);
                writer.WriteInt(entry.Damage);
                writer.WriteString(entry.Descriptor);
            }
        }

        public void Deserialize(PacketReader reader)
        {
            _ = reader.ReadInt();
            var ownerNetId = reader.ReadULong();
            var count = reader.ReadInt();
            var entries = new List<HellfireDamageEntry>(count);
            for (var i = 0; i < count; i++)
            {
                entries.Add(new HellfireDamageEntry(
                    reader.ReadUInt(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadULong(),
                    reader.ReadInt(),
                    reader.ReadString()));
            }

            Snapshot = new HellfireDamageSnapshot(ownerNetId, entries);
        }
    }

    private readonly record struct HellfireDamageEntry(
        uint CombatId,
        string ModelId,
        string Slot,
        ulong PlayerNetId,
        int Damage,
        string Descriptor);

    private sealed record HellfireDamageSnapshot(
        ulong OwnerNetId,
        IReadOnlyList<HellfireDamageEntry> Entries)
    {
        public static readonly HellfireDamageSnapshot Empty = new(0UL, []);
    }
}
