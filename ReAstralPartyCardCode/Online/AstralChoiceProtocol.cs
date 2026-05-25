using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal enum AstralChoiceKind
{
    RelicSelection = 1,
    CanonicalCardSelection = 2,
    RefreshableRelicSelection = 3,
    DeckCardMutationSelection = 4,
    StartingPersonaSelectionUpdate = 100,
    StartingPersonaSelectionCommit = 101,
    RunSettingsSnapshot = 200
}

internal static class AstralChoiceProtocol
{
    private const int EnvelopeMagic = unchecked((int)0x5241504D);
    private const int EnvelopeSchemaVersion = 1;

    public static void LogStartupDiagnostics()
    {
        MainFile.Logger.Info(
            $"[{MainFile.ModId}] Astral choice protocol ready | magic=0x{EnvelopeMagic:X8} schema={EnvelopeSchemaVersion}");
    }

    public static PlayerChoiceResult CreateIndexedEnvelope(
        AstralChoiceKind kind,
        RunState? runState,
        string sessionKey,
        int sequence,
        IReadOnlyList<int> payload)
    {
        var values = new List<int>(payload.Count + 7)
        {
            EnvelopeMagic,
            EnvelopeSchemaVersion,
            (int)kind,
            ComputeRunScopeHash(runState),
            ComputeStableHash(sessionKey),
            sequence,
            payload.Count
        };
        values.AddRange(payload);
        return PlayerChoiceResult.FromIndexes(values);
    }

    public static bool TryDecodeIndexedEnvelope(
        PlayerChoiceResult result,
        AstralChoiceKind expectedKind,
        RunState? runState,
        string sessionKey,
        out int sequence,
        out IReadOnlyList<int> payload)
    {
        sequence = 0;
        payload = [];

        try
        {
            var values = result.AsIndexes();
            if (values == null || values.Count < 7)
                return false;
            if (values[0] != EnvelopeMagic || values[1] != EnvelopeSchemaVersion || values[2] != (int)expectedKind)
                return false;
            if (values[3] != ComputeRunScopeHash(runState) || values[4] != ComputeStableHash(sessionKey))
                return false;

            var count = values[6];
            if (count < 0 || values.Count < count + 7)
                return false;

            sequence = values[5];
            payload = values.Skip(7).Take(count).ToArray();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool TryDecodeIndexedEnvelopeHeader(
        PlayerChoiceResult result,
        RunState? runState,
        string sessionKey,
        out AstralChoiceKind kind,
        out int sequence,
        out IReadOnlyList<int> payload)
    {
        kind = default;
        sequence = 0;
        payload = [];

        try
        {
            var values = result.AsIndexes();
            if (values == null || values.Count < 7)
                return false;
            if (values[0] != EnvelopeMagic || values[1] != EnvelopeSchemaVersion)
                return false;
            if (values[3] != ComputeRunScopeHash(runState) || values[4] != ComputeStableHash(sessionKey))
                return false;
            if (!Enum.IsDefined(typeof(AstralChoiceKind), values[2]))
                return false;

            var count = values[6];
            if (count < 0 || values.Count < count + 7)
                return false;

            kind = (AstralChoiceKind)values[2];
            sequence = values[5];
            payload = values.Skip(7).Take(count).ToArray();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static string CreateRunScopeKey(RunState? runState)
    {
        if (runState == null)
            return "no_run";

        var orderedPlayers = runState.Players
            .Select(player => player.NetId.ToString())
            .OrderBy(static netId => netId, StringComparer.Ordinal);
        return $"{runState.Rng.StringSeed}|{string.Join(",", orderedPlayers)}";
    }

    public static int ComputeRunScopeHash(RunState? runState)
    {
        return ComputeStableHash(CreateRunScopeKey(runState));
    }

    public static int ComputeStableHash(string value)
    {
        return unchecked(StringHelper.GetDeterministicHashCode(value ?? "<null>"));
    }
}
