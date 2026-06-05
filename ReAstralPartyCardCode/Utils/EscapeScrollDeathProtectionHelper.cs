using System;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EscapeScrollDeathProtectionHelper
{
    [ThreadStatic] private static int _activeDepth;

    public static bool IsActive => _activeDepth > 0;

    public static IDisposable Enter()
    {
        _activeDepth++;
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
            if (_activeDepth > 0)
                _activeDepth--;
        }
    }
}
