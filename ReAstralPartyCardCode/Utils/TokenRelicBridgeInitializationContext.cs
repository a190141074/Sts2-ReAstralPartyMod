using System.Threading;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public enum TokenRelicBridgeInitializationMode
{
    None = 0,
    RunAfterObtained = 1,
    RunAfterObtainedSkipOneTimeRewards = 2
}

public static class TokenRelicBridgeInitializationContext
{
    private static readonly AsyncLocal<BridgeInitializationScope?> CurrentScope = new();

    public static TokenRelicBridgeInitializationMode CurrentMode =>
        CurrentScope.Value?.Mode ?? TokenRelicBridgeInitializationMode.None;

    public static ModelId CurrentRelicId =>
        CurrentScope.Value?.RelicId ?? ModelId.none;

    public static bool ShouldSkipOneTimeObtainRewards =>
        CurrentMode == TokenRelicBridgeInitializationMode.RunAfterObtainedSkipOneTimeRewards;

    public static IDisposable Push(TokenRelicBridgeInitializationMode mode, ModelId relicId)
    {
        var priorScope = CurrentScope.Value;
        CurrentScope.Value = new BridgeInitializationScope(mode, relicId, priorScope);
        return new ScopeReset(priorScope);
    }

    private sealed class BridgeInitializationScope(
        TokenRelicBridgeInitializationMode mode,
        ModelId relicId,
        BridgeInitializationScope? parent)
    {
        public TokenRelicBridgeInitializationMode Mode { get; } = mode;
        public ModelId RelicId { get; } = relicId;
        public BridgeInitializationScope? Parent { get; } = parent;
    }

    private sealed class ScopeReset(BridgeInitializationScope? priorScope) : IDisposable
    {
        public void Dispose()
        {
            CurrentScope.Value = priorScope;
        }
    }
}
