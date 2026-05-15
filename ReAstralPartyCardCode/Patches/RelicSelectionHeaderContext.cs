namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

// Tracks temporary header overrides while the persona relic selection UI is open.
public static class RelicSelectionHeaderContext
{
    private static string? _currentHeaderText;

    public static string? CurrentHeaderText => _currentHeaderText;

    public static IDisposable Push(string headerText)
    {
        var previousHeaderText = _currentHeaderText;
        _currentHeaderText = headerText;
        return new HeaderScope(previousHeaderText);
    }

    private sealed class HeaderScope(string? previousHeaderText) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _currentHeaderText = previousHeaderText;
            _disposed = true;
        }
    }
}
