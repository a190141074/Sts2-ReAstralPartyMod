using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CardGainAttribution
{
    private static readonly AsyncLocal<AbstractModel?> CurrentSource = new();

    public static AbstractModel? Source => CurrentSource.Value;

    public static Task RunWithSource(AbstractModel? source, Func<Task> action)
    {
        return source == null ? action() : RunWithSourceCore(source, action);
    }

    public static Task<T> RunWithSource<T>(AbstractModel? source, Func<Task<T>> action)
    {
        return source == null ? action() : RunWithSourceCore(source, action);
    }

    public static bool IsCausedBy(Player? player)
    {
        if (player == null)
            return false;

        return Source switch
        {
            CardModel card => card.Owner == player,
            PowerModel power => power.Owner?.Player == player,
            RelicModel relic => relic.Owner == player,
            PotionModel potion => potion.Owner == player,
            _ => false
        };
    }

    private static async Task RunWithSourceCore(AbstractModel source, Func<Task> action)
    {
        var previous = CurrentSource.Value;
        CurrentSource.Value = source;
        try
        {
            await action();
        }
        finally
        {
            CurrentSource.Value = previous;
        }
    }

    private static async Task<T> RunWithSourceCore<T>(AbstractModel source, Func<Task<T>> action)
    {
        var previous = CurrentSource.Value;
        CurrentSource.Value = source;
        try
        {
            return await action();
        }
        finally
        {
            CurrentSource.Value = previous;
        }
    }
}