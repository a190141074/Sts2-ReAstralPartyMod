using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class DerivedHealResolutionHelper
{
    private const int MaxFlushPasses = 16;
    private const int MaxResolvedEntries = 256;
    private static readonly AsyncLocal<ResolutionState?> CurrentState = new();

    private sealed class PendingHalfLifeHeal
    {
        public decimal Amount;
        public Creature? Applier;
        public CardModel? Source;
    }

    private sealed class PendingWarm
    {
        public int Amount;
        public CardModel? Source;
    }

    private sealed class ResolutionState
    {
        public int Depth;
        public bool IsFlushing;
        public int ResolvedEntries;
        public readonly Dictionary<Creature, PendingHalfLifeHeal> PendingHalfLifeHeals = [];
        public readonly Dictionary<Player, PendingWarm> PendingWarms = [];
    }

    public static async Task EnqueueHalfLifeHealAndFlush(
        Creature? target,
        decimal amount,
        Creature? applier,
        CardModel? source,
        string reason)
    {
        await RunBatchedAsync(() => EnqueueHalfLifeHeal(target, amount, applier, source), reason);
    }

    public static async Task EnqueueWarmAndFlush(Player? player, int amount, CardModel? source, string reason)
    {
        await RunBatchedAsync(() => EnqueueWarm(player, amount, source), reason);
    }

    public static async Task RunBatchedAsync(Action enqueueAction, string reason)
    {
        ArgumentNullException.ThrowIfNull(enqueueAction);

        var state = CurrentState.Value ??= new ResolutionState();
        state.Depth++;
        try
        {
            enqueueAction();
        }
        finally
        {
            state.Depth = Math.Max(0, state.Depth - 1);
        }

        if (state.Depth == 0 && !state.IsFlushing)
            await FlushAsync(state, reason);
    }

    public static void EnqueueHalfLifeHeal(Creature? target, decimal amount, Creature? applier, CardModel? source)
    {
        if (target == null || amount <= 0m)
            return;

        var state = CurrentState.Value ??= new ResolutionState();
        if (!state.PendingHalfLifeHeals.TryGetValue(target, out var pending))
        {
            pending = new PendingHalfLifeHeal();
            state.PendingHalfLifeHeals[target] = pending;
        }

        pending.Amount += amount;
        pending.Applier ??= applier;
        pending.Source ??= source;
    }

    public static void EnqueueWarm(Player? player, int amount, CardModel? source)
    {
        if (player == null || amount <= 0)
            return;

        var state = CurrentState.Value ??= new ResolutionState();
        if (!state.PendingWarms.TryGetValue(player, out var pending))
        {
            pending = new PendingWarm();
            state.PendingWarms[player] = pending;
        }

        pending.Amount += amount;
        pending.Source ??= source;
    }

    private static async Task FlushAsync(ResolutionState state, string reason)
    {
        state.IsFlushing = true;
        state.ResolvedEntries = 0;
        try
        {
            var pass = 0;
            while (HasPending(state))
            {
                pass++;
                var currentEntryCount = state.PendingHalfLifeHeals.Count + state.PendingWarms.Count;
                if (pass > MaxFlushPasses || state.ResolvedEntries + currentEntryCount > MaxResolvedEntries)
                {
                    MainFile.Logger.Warn(
                        $"[{MainFile.ModId}] [DerivedHealResolution] Aborting derived heal flush | reason={reason} | pass={pass} | currentEntries={currentEntryCount} | resolvedEntries={state.ResolvedEntries}");
                    state.PendingHalfLifeHeals.Clear();
                    state.PendingWarms.Clear();
                    break;
                }

                var halfLifeEntries = state.PendingHalfLifeHeals.ToArray();
                var warmEntries = state.PendingWarms.ToArray();
                state.PendingHalfLifeHeals.Clear();
                state.PendingWarms.Clear();

                if (pass == 1 || currentEntryCount >= 4)
                {
                    MainFile.Logger.Info(
                        $"[{MainFile.ModId}] [DerivedHealResolution] Flushing derived heal queue | reason={reason} | pass={pass} | halfLifeTargets={halfLifeEntries.Length} | warmTargets={warmEntries.Length}");
                }

                await PersonaMultiplayerEffectHelper.RunAsDerivedSupportPower(async () =>
                {
                    foreach (var (target, pending) in halfLifeEntries)
                    {
                        if (target == null || !target.IsAlive || pending.Amount <= 0m)
                            continue;

                        await PowerCmd.Apply<HalfLifeHealPower>(target, pending.Amount, pending.Applier, pending.Source,
                            false);
                    }

                    foreach (var (player, pending) in warmEntries)
                    {
                        if (player?.Creature == null || !player.Creature.IsAlive || pending.Amount <= 0)
                            continue;

                        await PersonDorothyHaze.ApplyWarmFromDerivedSupport(player, pending.Amount, pending.Source);
                    }
                });

                state.ResolvedEntries += currentEntryCount;
            }
        }
        finally
        {
            state.IsFlushing = false;
            state.ResolvedEntries = 0;
            if (!HasPending(state) && state.Depth == 0)
                CurrentState.Value = null;
        }
    }

    private static bool HasPending(ResolutionState state)
    {
        return state.PendingHalfLifeHeals.Count > 0 || state.PendingWarms.Count > 0;
    }
}
