using System.Collections.Concurrent;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StackableDebuffGrowthHelper
{
    private static readonly MethodInfo? SetAmountMethodDefinition = ResolveSetAmountMethodDefinition();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> ClosedSetAmountMethods = new();
    private static readonly ConcurrentDictionary<string, byte> LoggedWarnings = new();

    public static bool IsStackableCounterDebuff(PowerModel power)
    {
        return power.Type == PowerType.Debuff &&
               power.StackType == PowerStackType.Counter;
    }

    public static bool CanIncreaseIncomingStackableDebuff(PowerModel canonicalPower, decimal amount)
    {
        return amount > 0m && IsStackableCounterDebuff(canonicalPower);
    }

    public static bool CanGrowExistingStackableDebuff(PowerModel power)
    {
        return power.Owner != null &&
               IsStackableCounterDebuff(power) &&
               power.Amount > 0m;
    }

    public static async Task<bool> TryGrowExistingStackableDebuffAsync(
        PowerModel power,
        decimal delta,
        CardModel? source = null)
    {
        if (!CanGrowExistingStackableDebuff(power) || delta <= 0m)
            return false;

        var owner = power.Owner;
        if (owner == null)
            return false;

        var closedMethod = GetClosedSetAmountMethod(power.GetType());
        if (closedMethod == null)
        {
            LogWarningOnce(
                $"missing_setamount:{power.GetType().FullName}",
                $"[StackableDebuffGrowth] Skipped growth for {DescribePower(power)} because PowerCmd.SetAmount<TPower> could not be resolved.");
            return false;
        }

        try
        {
            var result = closedMethod.Invoke(null, [owner, power.Amount + delta, power.Applier, source]);
            if (result is not Task task)
            {
                LogWarningOnce(
                    $"invalid_return:{power.GetType().FullName}",
                    $"[StackableDebuffGrowth] Skipped growth for {DescribePower(power)} because reflected SetAmount did not return a Task.");
                return false;
            }

            await task;
            return true;
        }
        catch (TargetInvocationException ex)
        {
            LogWarningOnce(
                $"invoke_failed:{power.GetType().FullName}",
                $"[StackableDebuffGrowth] Skipped growth for {DescribePower(power)} because SetAmount threw: {ex.InnerException ?? ex}");
            return false;
        }
        catch (Exception ex)
        {
            LogWarningOnce(
                $"reflection_failed:{power.GetType().FullName}",
                $"[StackableDebuffGrowth] Skipped growth for {DescribePower(power)} because reflected SetAmount failed: {ex}");
            return false;
        }
    }

    public static async Task<bool> TryApplyOrGrowStackableDebuffAsync<TPower>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? source = null,
        bool isSourceGenerated = false)
        where TPower : PowerModel
    {
        if (amount <= 0m)
            return false;

        var existingPower = target.GetPower<TPower>();
        if (existingPower != null && CanGrowExistingStackableDebuff(existingPower))
            return await TryGrowExistingStackableDebuffAsync(existingPower, amount, source);

        await PowerCmd.Apply<TPower>(target, amount, applier, source, isSourceGenerated);
        return true;
    }

    private static MethodInfo? GetClosedSetAmountMethod(Type powerType)
    {
        return ClosedSetAmountMethods.GetOrAdd(
            powerType,
            static type =>
            {
                if (SetAmountMethodDefinition == null || !typeof(PowerModel).IsAssignableFrom(type))
                    return null;

                try
                {
                    return SetAmountMethodDefinition.MakeGenericMethod(type);
                }
                catch
                {
                    return null;
                }
            });
    }

    private static MethodInfo? ResolveSetAmountMethodDefinition()
    {
        return typeof(PowerCmd)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(static method =>
            {
                if (method.Name != nameof(PowerCmd.SetAmount) ||
                    !method.IsGenericMethodDefinition ||
                    method.GetGenericArguments().Length != 1)
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType == typeof(Creature) &&
                       parameters[1].ParameterType == typeof(decimal) &&
                       parameters[2].ParameterType == typeof(Creature) &&
                       parameters[3].ParameterType == typeof(CardModel);
            });
    }

    private static string DescribePower(PowerModel power)
    {
        return $"{power.Id.Entry}/{power.GetType().Name}";
    }

    private static void LogWarningOnce(string key, string message)
    {
        if (LoggedWarnings.TryAdd(key, 0))
            MainFile.Logger.Warn(message);
    }
}
