using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class HkHeavenlyKingFallIncidentHelper
{
    private const string RunDataKey = "hk_heavenly_king_fall_incident";
    private static RunSavedData<FallIncidentRunData> _runData = null!;
    private static bool _registered;

    private static readonly FallIncidentCandidate[] Candidates =
    [
        new(static () => ModelDb.Relic<PersonWeirdEgg>(), static () => ModelDb.Relic<VariantPersonWeirdEgg>()),
        new(static () => ModelDb.Relic<PersonSamuraiPrawn>()),
        new(static () => ModelDb.Relic<PersonNinja>()),
        new(static () => ModelDb.Relic<PersonSocialFearNun>()),
        new(static () => ModelDb.Relic<PersonSupermanMegas>()),
        new(static () => ModelDb.Relic<PersonMidnightFlash>()),
        new(static () => ModelDb.Relic<PersonUnclePederman>())
    ];

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId).Register(
                RunDataKey,
                static () => new FallIncidentRunData(),
                new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });
        }

        RitsuLibFramework.GetContentRegistry(MainFile.ModId).RegisterSingleton<FallIncidentLifecycle>();
        _registered = true;
    }

    public static bool CanPlay(Player? owner)
    {
        if (owner?.RunState is not RunState runState)
            return false;

        return BuildAvailableCandidates(runState, Get(runState).PendingRewards).Count > 0;
    }

    public static async Task PlayAsync(Player owner)
    {
        if (owner.RunState is not RunState runState)
            return;

        var data = Get(runState);
        var availableCandidates = BuildAvailableCandidates(runState, data.PendingRewards);
        if (availableCandidates.Count == 0)
            return;

        var options = availableCandidates
            .Select(candidate => candidate.Factory().ToMutable())
            .ToList();
        var selected = await DeterministicMultiplayerChoiceHelper.SelectOverlayRelicForPlayer(
            owner,
            options,
            new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_HK_HEAVENLY_KING_FALL_INCIDENT.selection_header").GetRawText(),
            new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_HK_HEAVENLY_KING_FALL_INCIDENT.select_prompt").GetRawText(),
            "hk_heavenly_king_fall_incident");
        if (selected == null)
            return;

        var selectedId = (selected.CanonicalInstance ?? selected).Id.Entry;
        var candidate = availableCandidates.FirstOrDefault(item => item.NormalRelicId == selectedId);
        if (candidate == null)
            return;

        data.PendingRewards.Add(new FallIncidentPendingReward
        {
            OwnerNetId = owner.NetId,
            RelicId = candidate.NormalRelicId
        });
        _runData.Set(runState, data);
        MainFile.Logger.Info(
            $"[HkFallIncident] reward queued | owner={owner.NetId} | relic={candidate.NormalRelicId} | candidates={options.Count}.");
    }

    private static List<FallIncidentCandidate> BuildAvailableCandidates(
        RunState runState,
        IReadOnlyCollection<FallIncidentPendingReward> pendingRewards)
    {
        var reservedRelicIds = pendingRewards
            .Select(reward => reward.RelicId)
            .ToHashSet(StringComparer.Ordinal);
        return Candidates
            .Where(candidate => !reservedRelicIds.Contains(candidate.NormalRelicId))
            .Where(candidate => runState.Players.All(player => player.Relics.All(relic =>
                !candidate.EquivalentRelicIds.Contains((relic.CanonicalInstance ?? relic).Id.Entry))))
            .ToList();
    }

    public static Task HandleCombatEnd(CombatRoom room, RunState? runState)
    {
        if (runState == null)
            return Task.CompletedTask;

        var data = Get(runState);
        var changed = false;
        foreach (var pending in data.PendingRewards.ToList())
        {
            var owner = runState.Players.FirstOrDefault(player => player.NetId == pending.OwnerNetId);
            var candidate = Candidates.FirstOrDefault(item => item.NormalRelicId == pending.RelicId);
            if (owner == null || candidate == null)
            {
                data.PendingRewards.Remove(pending);
                changed = true;
                MainFile.Logger.Warn(
                    $"[HkFallIncident] discarded invalid pending reward | owner={pending.OwnerNetId} | relic={pending.RelicId}.");
                continue;
            }

            if (owner.Relics.Any(relic => (relic.CanonicalInstance ?? relic).Id.Entry == pending.RelicId))
            {
                data.PendingRewards.Remove(pending);
                changed = true;
                MainFile.Logger.Info(
                    $"[HkFallIncident] skipped already-owned reward | owner={owner.NetId} | relic={pending.RelicId}.");
                continue;
            }

            try
            {
                room.AddExtraReward(owner, new RelicReward(candidate.Factory().ToMutable(), owner));
                data.PendingRewards.Remove(pending);
                changed = true;
                MainFile.Logger.Info(
                    $"[HkFallIncident] reward added to combat rewards | owner={owner.NetId} | relic={pending.RelicId}.");
            }
            catch (Exception exception)
            {
                MainFile.Logger.Error(
                    $"[HkFallIncident] reward retained | owner={owner.NetId} | relic={pending.RelicId} | error={exception}");
            }
        }

        if (changed)
            _runData.Set(runState, data);

        return Task.CompletedTask;
    }

    private static FallIncidentRunData Get(RunState runState)
    {
        if (!_registered)
            Register();
        return _runData.TryGet(runState, out var data) && data != null ? data : new FallIncidentRunData();
    }

    private sealed record FallIncidentCandidate(Func<RelicModel> Factory, params Func<RelicModel>[] VariantFactories)
    {
        public string NormalRelicId => (Factory().CanonicalInstance ?? Factory()).Id.Entry;

        public IReadOnlySet<string> EquivalentRelicIds =>
            VariantFactories
                .Select(factory => (factory().CanonicalInstance ?? factory()).Id.Entry)
                .Append(NormalRelicId.Replace("_RELIC_PERSON_", "_RELIC_VARIANT_PERSON_", StringComparison.Ordinal))
                .Append(NormalRelicId)
                .ToHashSet(StringComparer.Ordinal);
    }
}

internal sealed class FallIncidentLifecycle : HookedSingletonModel
{
    public FallIncidentLifecycle() : base(HookType.Combat)
    {
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return HkHeavenlyKingFallIncidentHelper.HandleCombatEnd(room, CurrentRunState as RunState);
    }
}

internal sealed class FallIncidentRunData
{
    public List<FallIncidentPendingReward> PendingRewards { get; set; } = [];
}

internal sealed class FallIncidentPendingReward
{
    public ulong OwnerNetId { get; set; }
    public string RelicId { get; set; } = string.Empty;
}
