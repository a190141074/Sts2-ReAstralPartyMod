using System.Threading;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class DebuffDamageContextHelper
{
    private static readonly AsyncLocal<int> Depth = new();

    public static bool IsActive => Depth.Value > 0;

    public static IDisposable Enter()
    {
        Depth.Value++;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Depth.Value = Math.Max(0, Depth.Value - 1);
        }
    }
}
