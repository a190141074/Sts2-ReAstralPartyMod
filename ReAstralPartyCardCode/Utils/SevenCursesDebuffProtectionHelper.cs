using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SevenCursesDebuffProtectionHelper
{
    [ThreadStatic] private static int _debuffDamageDepth;

    public static bool IsInDebuffDamageContext => _debuffDamageDepth > 0;

    public static IDisposable EnterDebuffDamageContext()
    {
        _debuffDamageDepth++;
        return new Scope();
    }

    public static bool IsDebuffDamage(
        Creature? target,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (IsInDebuffDamageContext)
            return true;

        if (target == null)
            return false;
        if (cardSource != null)
            return false;

        if (dealer != null)
        {
            if (dealer == target)
                return false;
            if (dealer.Side != target.Side)
                return false;
        }

        return props.HasFlag(ValueProp.Unblockable) || props.HasFlag(ValueProp.Unpowered) || props.HasFlag(ValueProp.Move);
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_debuffDamageDepth > 0)
                _debuffDamageDepth--;
        }
    }
}
