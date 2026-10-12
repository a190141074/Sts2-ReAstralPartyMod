using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Audio;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

[RegisterSharedEvent]
public sealed class AstralSamuraiPrawn : AstralPartyEventModel
{
    private const string MusicPath = "res://ReAstralPartyMod/audios/events/astral_samurai_prawn.wav";
    private const string RunDataKey = "astral_samurai_prawn";
    private const int FlowCount = 11;

    private static readonly string[] CorrectOptions =
        ["LOOK_COMBO", "KING_POWER", "HP_4", "USE_SKILL", "GAWU", "MOVE", "ONE", "LIGHTNING", "HP_2", "STUNNED", "HOW"];

    private static RunSavedData<AstralSamuraiPrawnRunData> _runData = null!;
    private static bool _registered;
    private static AstralSamuraiPrawn? _activeMusicEvent;
    private AudioMusicHandle? _musicHandle;

    public static void Register()
    {
        if (_registered)
            return;
        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId).Register(
                RunDataKey, static () => new AstralSamuraiPrawnRunData(),
                new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });
        }

        // BeforeRoomEntered runs before the target room starts its event/music logic.
        // MapRoom skips this hook, so the track survives the event's map selection page.
        RitsuLibFramework.SubscribeLifecycle<RoomEnteringEvent>(_ => StopActiveMusic("next_room"), false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => StopActiveMusic(), false);
        RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ => StopActiveMusic(), false);
        _registered = true;
    }

    private AstralSamuraiPrawnProgress Progress
    {
        get
        {
            var run = (RunState)Owner!.RunState;
            var data = _runData.TryGet(run, out var saved) && saved != null ? saved : new AstralSamuraiPrawnRunData();
            var key = $"{run.CurrentActIndex}:{run.TotalFloor}:{Owner.NetId}";
            if (!data.Players.TryGetValue(key, out var progress))
            {
                progress = new AstralSamuraiPrawnProgress();
                data.Players[key] = progress;
            }

            _runData.Set(run, data);
            return progress;
        }
    }

    protected override string EventId => "astral_samurai_prawn";

    public override bool IsAllowed(IRunState runState)
    {
        Register();
        return runState.CurrentActIndex == 1 && !HasBeenConsumed(runState);
    }

    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        MarkConsumed();
        // Remote event models must not start or stop the local player's music.
        if (isPreFinished || Owner == null || !LocalContext.IsMe(Owner))
            return Task.CompletedTask;

        StopActiveMusic();
        _activeMusicEvent = this;
        _musicHandle = GameAudioService.Shared.PlayMusic(AudioSource.StreamingMusic(MusicPath), new AudioPlaybackOptions
        {
            // Lifetime ends at next room entry or local session cleanup, not event completion.
            Scope = AudioLifecycleScope.Manual,
            AllowFadeOutOnStop = false,
            DebugName = "astral_samurai_prawn.music"
        });
        MainFile.Logger.Info($"[SamuraiPrawnMusic] start owner={Owner.NetId} success={_musicHandle != null}");
        return Task.CompletedTask;
    }

    internal static void StopActiveMusic(string reason = "session_cleanup")
    {
        _activeMusicEvent?.StopMusic(reason);
    }

    private void StopMusic(string reason)
    {
        var handle = _musicHandle;
        _musicHandle = null;
        if (ReferenceEquals(_activeMusicEvent, this))
            _activeMusicEvent = null;
        if (handle == null)
            return;

        // Explicit stop gives diagnostics for file playback; no adaptive plan can restart it.
        var stopped = handle.TryStop(false);
        handle.Dispose();
        MainFile.Logger.Info(
            $"[SamuraiPrawnMusic] stop reason={reason} stopped={stopped} released={handle.IsReleased}");
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return Progress.Flow < FlowCount ? CreateFlowOptions() : CreateRewardOptions();
    }

    protected override void SetInitialEventState(bool isPreFinished)
    {
        var progress = Progress;
        if (progress.FinishPage != null)
            SetEventFinished(PageDescription(progress.FinishPage));
        else if (progress.Flow >= FlowCount)
            SetEventState(PageDescription("REWARD"), CreateRewardOptions());
        else
            SetEventState(PageDescription(progress.Flow == 0 ? "INITIAL" : $"FLOW_{progress.Flow + 1}"),
                GenerateInitialOptions());
    }

    private IReadOnlyList<EventOption> CreateFlowOptions()
    {
        var page = Progress.Flow + 1;
        var optionKeys = page switch
        {
            1 => new[] { "LOOK_COMBO" },
            2 => new[] { "FRENZY", "KING_POWER", "MADNESS" },
            3 => new[] { "HP_3", "HP_4", "HP_5" },
            4 => new[] { "USE_SKILL", "OVERLOAD", "CLIMBING" },
            5 => new[] { "GAWU", "CHARGE", "FULL_STRIKE" },
            6 => new[] { "MOVE", "ASSIST", "DIRECTION" },
            7 => new[] { "ONE", "NINE", "ELEVEN" },
            8 => new[] { "LIGHTNING", "BOMB", "THUNDERSTORM" },
            9 => new[] { "HP_1", "HP_2", "HP_3" },
            10 => new[] { "STUNNED", "CAT_CART", "HOSPITAL" },
            11 => new[] { "HOW", "AMAZING", "TRANSFER" },
            _ => Array.Empty<string>()
        };

        var pageKey = page == 1 ? "INITIAL" : $"FLOW_{page}";
        return optionKeys.Select((key, index) =>
            new EventOption(this, () => ChooseFlowOption(key),
                ModOptionKey(pageKey, $"OPTION_{index + 1}"))).ToList();
    }

    private Task ChooseFlowOption(string key)
    {
        var progress = Progress;
        var page = progress.Flow;
        if (page < 0 || page >= FlowCount)
            return Task.CompletedTask;

        if (!string.Equals(key, CorrectOptions[page], StringComparison.Ordinal))
            progress.Perfect = false;

        progress.Flow++;
        if (progress.Flow < FlowCount)
        {
            SetEventState(PageDescription($"FLOW_{progress.Flow + 1}"), CreateFlowOptions());
            return Task.CompletedTask;
        }

        SetEventState(PageDescription("REWARD"), CreateRewardOptions());
        return Task.CompletedTask;
    }

    private IReadOnlyList<EventOption> CreateRewardOptions()
    {
        return
        [
            new EventOption(this, Progress.Perfect ? GrantCursedSword : null, ModOptionKey("REWARD", "CURSED_SWORD"),
                ModelDb.Relic<TokenExclusiveCursedSword>().HoverTips),
            new EventOption(this, Progress.Perfect ? UpgradeTwoAttacks : null, ModOptionKey("REWARD", "TWO_ATTACKS")),
            new EventOption(this, GrantGeneralReward, ModOptionKey("REWARD", "GENERAL"))
        ];
    }

    private async Task GrantCursedSword()
    {
        if (Owner == null)
            return;
        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, ModelDb.Relic<TokenExclusiveCursedSword>());
        Finish("CURSED_SWORD_FINISH");
    }

    private async Task UpgradeTwoAttacks()
    {
        if (Owner == null)
            return;

        var candidates = EventDeckCardHelper.GetRunDeckCards(Owner)
            .Where(card => card.IsUpgradable && card.Type == CardType.Attack)
            .ToList();
        if (candidates.Count > 0)
        {
            var selected = await CardSelectCmd.FromDeckGeneric(
                Owner,
                new CardSelectorPrefs(
                    new LocString("events", "RE_ASTRAL_PARTY_MOD_EVENT_ASTRAL_SAMURAI_PRAWN.select_attacks"),
                    Math.Min(2, candidates.Count), Math.Min(2, candidates.Count))
                {
                    Cancelable = false,
                    RequireManualConfirmation = true
                },
                card => card.IsUpgradable && card.Type == CardType.Attack,
                null!);
            await EventDeckCardMutationHelper.Upgrade(Owner, selected.Take(2).ToList(),
                "astral_samurai_prawn.two_attacks");
        }

        Finish(candidates.Count == 0 ? "NO_ATTACKS_FINISH" : "TWO_ATTACKS_FINISH");
    }

    private async Task GrantGeneralReward()
    {
        if (Owner == null)
            return;

        await PersonMultiplayerEffectHelper.GainGoldDeterministic(30m, Owner);
        var candidates = EventDeckCardHelper.GetRunDeckCards(Owner)
            .Where(static candidate => candidate.Type == CardType.Attack && candidate.IsUpgradable).ToList();
        var card = candidates.Count == 0
            ? null
            : candidates[AstralStableRandom.Index(
                Owner.RunState, candidates.Count, Id.Entry, Owner.NetId, "general_attack_upgrade")];
        if (card != null)
            await EventDeckCardMutationHelper.UpgradeSingleWithSmithPreview(Owner, card,
                "astral_samurai_prawn.general");
        Finish(card == null ? "GOLD_FINISH" : "GENERAL_FINISH");
    }

    private void Finish(string page)
    {
        Progress.FinishPage = page;
        SetEventFinished(PageDescription(page));
    }

    private static string GetRunKey(IRunState runState)
    {
        return runState.Rng.StringSeed;
    }

    private static bool HasBeenConsumed(IRunState runState)
    {
        var data = _runData.TryGet((RunState)runState, out var saved) && saved != null
            ? saved
            : new AstralSamuraiPrawnRunData();
        return data.ConsumedRunKeys.Contains(GetRunKey(runState));
    }

    private void MarkConsumed()
    {
        if (Owner?.RunState is not RunState runState)
            return;

        var data = _runData.TryGet(runState, out var saved) && saved != null
            ? saved
            : new AstralSamuraiPrawnRunData();
        var key = GetRunKey(runState);
        if (data.ConsumedRunKeys.Contains(key))
            return;

        data.ConsumedRunKeys.Add(key);
        _runData.Set(runState, data);
    }
}

public sealed class AstralSamuraiPrawnRunData
{
    public Dictionary<string, AstralSamuraiPrawnProgress> Players { get; set; } = [];
    public List<string> ConsumedRunKeys { get; set; } = [];
}

public sealed class AstralSamuraiPrawnProgress
{
    public int Flow { get; set; }
    public bool Perfect { get; set; } = true;
    public string? FinishPage { get; set; }
}
