using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed partial class StartingPersonRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private const string BackgroundTexturePath =
        "res://ReAstralPartyMod/images/background/starting_persona_pelic_selection_screen.png";

    private const string TreasureRelicHolderScenePath = "ui/treasure_relic_holder";
    private const float HolderWidth = 136f;
    private const float HolderHeight = 136f;
    private const int MaxFightTieRounds = 4;
    private const int MultiplayerChoiceMagic = unchecked((int)0x52415053);
    private const int ChoiceKindSelectionUpdate = 1;
    private const int ChoiceKindSelectionCommit = 2;
    private const int MaxSynchronizerWaitFrames = 600;
    private const int SelectionCommitTimeoutMilliseconds = 10 * 60 * 1000;
    private const int SelectionCommitGraceMilliseconds = 5 * 1000;
    private const int AutomaticSelectionMinimumCountdownMilliseconds = 250;

    private readonly TaskCompletionSource _completionSource = new();
    private readonly TaskCompletionSource _overlayClosedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<int> _singlePlayerChoiceSource = new();
    private readonly TaskCompletionSource<CommittedSelectionSnapshot> _multiplayerCommitSource = new();
    private readonly RunState _runState;
    private readonly IReadOnlyList<RelicModel> _relicOptions;
    private readonly StartingPersonDisplayMode _displayMode;
    private readonly StartingPersonAssignmentMode _assignmentMode;
    private readonly bool _allowDuplicates;
    private readonly int _automaticSelectionCountdownSeconds;
    private readonly List<Player> _orderedPlayers;
    private readonly Dictionary<ulong, PlayerSelectionState> _selectionStates = new();
    private readonly Dictionary<int, NTreasureRoomRelicHolder> _holdersByIndex = new();
    private readonly Dictionary<ulong, uint> _nextChoiceIdsByPlayer = new();
    private readonly Dictionary<ulong, int> _selectionSequencesByPlayer = new();
    private readonly Dictionary<ulong, int> _pendingLocalSelectionIndexes = new();

    private Control _holderContainer = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private ColorRect _fightBackstop = null!;
    private Label _fightLabel = null!;
    private bool _opened;
    private bool _closed;
    private bool _holdersBuilt;
    private bool _selectionFinalized;
    private ulong _openedTicks;
    private PlayerChoiceSynchronizer? _multiplayerSynchronizer;
    private Player? _localPlayer;
    private Player? _authorityPlayer;
    private bool _multiplayerCommitSent;
    private bool _closeQueued;
    private bool _automaticCommitTriggered;

    public NetScreenType ScreenType => NetScreenType.None;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _holdersByIndex.Values.OrderBy(holder => holder.Index).FirstOrDefault();

    private StartingPersonRelicSelectionScreen(
        RunState runState,
        IReadOnlyList<RelicModel> relicOptions,
        StartingPersonDisplayMode displayMode,
        StartingPersonAssignmentMode assignmentMode,
        int automaticSelectionCountdownSeconds)
    {
        _runState = runState;
        _relicOptions = relicOptions;
        _displayMode = displayMode;
        _assignmentMode = assignmentMode;
        _allowDuplicates = ReAstralPartyModSettingsManager.ResolveAllowDuplicates(
            ReAstralPartyModSettingsManager.GetStartingPersonaMode(runState));
        _automaticSelectionCountdownSeconds = automaticSelectionCountdownSeconds;
        _orderedPlayers = runState.Players
            .OrderBy(static player => player.NetId)
            .ToList();
        _localPlayer = LocalContext.GetMe(_orderedPlayers) ?? _orderedPlayers.FirstOrDefault();
        _authorityPlayer = ResolveAuthorityPlayer(_orderedPlayers, _localPlayer);

        foreach (var player in _orderedPlayers)
        {
            _selectionStates[player.NetId] = new PlayerSelectionState(player);
            _selectionSequencesByPlayer[player.NetId] = 0;
            _pendingLocalSelectionIndexes[player.NetId] = -1;
        }

        Name = nameof(StartingPersonRelicSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        Visible = false;

        BuildStaticUi();
    }

    public static StartingPersonRelicSelectionScreen Create(
        RunState runState,
        IReadOnlyList<RelicModel> relicOptions,
        StartingPersonDisplayMode displayMode,
        StartingPersonAssignmentMode assignmentMode,
        int automaticSelectionCountdownSeconds)
    {
        return new StartingPersonRelicSelectionScreen(
            runState,
            relicOptions,
            displayMode,
            assignmentMode,
            automaticSelectionCountdownSeconds);
    }

    public Task RelicPickingFinished()
    {
        return _completionSource.Task;
    }

    public Task WaitUntilClosedAsync()
    {
        return _overlayClosedSource.Task;
    }

    public void Close()
    {
        if (_closed || _closeQueued)
            return;

        _closeQueued = true;
        _ = TaskHelper.RunSafely(CloseAfterInputReleasedAsync());
    }

    public void AfterOverlayOpened()
    {
        Visible = true;

        if (_opened)
            return;

        _opened = true;
        BuildRelicHoldersIfNeeded();
        AnimateIn();
        _openedTicks = Time.GetTicksMsec();
        _ = RunSelectionFlow();
    }

    public void AfterOverlayClosed()
    {
        if (_closed)
            return;

        _closed = true;
        _overlayClosedSource.TrySetResult();
        QueueFree();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
    }

    private void BuildStaticUi()
    {
        var background = new TextureRect
        {
            Name = "Background",
            Texture = GD.Load<Texture2D>(BackgroundTexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var titlePanel = new VBoxContainer
        {
            Name = "TitlePanel",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titlePanel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        titlePanel.OffsetTop = 36f;
        titlePanel.OffsetLeft = 90f;
        titlePanel.OffsetRight = -90f;
        titlePanel.AddThemeConstantOverride("separation", 6);
        AddChild(titlePanel);

        _titleLabel = new Label
        {
            Text = "选择起始人格",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderLarge"
        };
        titlePanel.AddChild(_titleLabel);

        _subtitleLabel = new Label
        {
            Text = BuildSelectionIntroSubtitle(_runState),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        titlePanel.AddChild(_subtitleLabel);

        _holderContainer = new Control
        {
            Name = "HolderContainer",
            MouseFilter = MouseFilterEnum.Pass
        };
        _holderContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _holderContainer.OffsetTop = 140f;
        _holderContainer.OffsetBottom = -170f;
        AddChild(_holderContainer);

        _fightBackstop = new ColorRect
        {
            Name = "FightBackstop",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Color = new Color(0f, 0f, 0f, 0.6f)
        };
        _fightBackstop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_fightBackstop);

        _fightLabel = new Label
        {
            Name = "FightLabel",
            Text = "猜拳中……",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ThemeTypeVariation = "HeaderMedium",
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _fightLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        _fightLabel.OffsetTop = 132f;
        _fightLabel.OffsetLeft = -320f;
        _fightLabel.OffsetRight = 320f;
        _fightLabel.OffsetBottom = 220f;
        _fightBackstop.AddChild(_fightLabel);
    }

    private void BuildRelicHoldersIfNeeded()
    {
        if (_holdersBuilt)
            return;

        _holdersBuilt = true;

        for (var index = 0; index < _relicOptions.Count; index++)
        {
            var relic = _relicOptions[index];
            var holder = SceneHelper.Instantiate<NTreasureRoomRelicHolder>(TreasureRelicHolderScenePath);
            holder.Name = $"StartingPersonaRelicHolder_{index}";
            holder.Index = index;
            holder.Visible = true;
            holder.Modulate = Colors.Black;
            holder.MouseFilter = MouseFilterEnum.Ignore;
            holder.Disable();
            _holderContainer.AddChild(holder);
            holder.Initialize(relic, _runState);
            holder.VoteContainer.Initialize(
                player => PlayerSelectedHolder(player, holder.Index),
                _orderedPlayers);
            holder.Connect(NClickableControl.SignalName.Released,
                Callable.From<NTreasureRoomRelicHolder>(_ => OnHolderSelected(holder)));
            _holdersByIndex[index] = holder;
        }

        ApplyHolderLayout(_holdersByIndex.OrderBy(entry => entry.Key).Select(entry => entry.Value).ToList(), _relicOptions.Count);
        ConfigureHolderFocusNeighbors();
        RefreshVotes(false);
    }

    private void AnimateIn()
    {
        foreach (var holder in _holdersByIndex.Values.OrderBy(holder => holder.Index))
        {
            holder.MouseFilter = MouseFilterEnum.Ignore;
            var delay = 0.15f + 0.03f * holder.Index;
            var targetY = holder.Position.Y;
            holder.Position = new Vector2(holder.Position.X, targetY + 70f);

            var tween = holder.CreateTween().SetParallel();
            tween.TweenProperty(holder, "modulate", Colors.White, 0.22).SetDelay(delay);
            tween.TweenProperty(holder, "position:y", targetY, 0.5f).SetDelay(delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.TweenCallback(Callable.From(delegate
            {
                if (_displayMode == StartingPersonDisplayMode.Automatic)
                {
                    holder.MouseFilter = MouseFilterEnum.Ignore;
                    holder.Disable();
                    return;
                }

                if (IsDestinedCloneMode() && !IsLocalAuthorityPlayer())
                {
                    holder.MouseFilter = MouseFilterEnum.Ignore;
                    holder.Disable();
                    return;
                }

                holder.MouseFilter = MouseFilterEnum.Stop;
                holder.Enable();
            })).SetDelay(delay + 0.5f);
        }
    }

    private async Task RunSelectionFlow()
    {
        try
        {
            LogInfo("P101", "Starting persona selection flow started.");
            await CollectSelectionsAsync();
            var results = ResolveSelectionResults();
            LogInfo("P108", $"Starting persona selection resolved {results.Count} results.");
            await AnimateSelectionResultsAsync(results);
            await AwardRelicsAsync(results);
            LogInfo("P110", "Starting persona selection awarding completed.");
            _completionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
            MainFile.Logger.Error($"[P199] Starting persona relic shared selection failed: {ex}");
            AstralNotificationService.ShowDiagnosticError(
                AstralNotificationModule.Multiplayer,
                AstralNotificationArea.PersonaSelection,
                199,
                "开局人格选择整体流程失败，请把日志和编号发给作者。",
                "总流程");
        }
    }

    private async Task CollectSelectionsAsync()
    {
        SaveAllOptionsAsSeen();

        if (_displayMode == StartingPersonDisplayMode.Automatic)
        {
            await CollectAutomaticSelectionsAsync();
            return;
        }

        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            var selectedIndex = await _singlePlayerChoiceSource.Task;
            ApplySelection(_localPlayer ?? _orderedPlayers[0], selectedIndex);
            AstralTelemetry.RecordPersonaChoice(
                _runState,
                _relicOptions,
                new Dictionary<ulong, int>
                {
                    [(_localPlayer ?? _orderedPlayers[0]).NetId] = selectedIndex
                });
            FinalizeSelectionDisplay("选择已锁定，开始结算……");
            return;
        }

        _multiplayerSynchronizer = await WaitForPlayerChoiceSynchronizerAsync()
                                   ?? throw new InvalidOperationException(
                                       "PlayerChoiceSynchronizer was not ready for starting persona selection.");
        LogInfo("P102", "Starting persona selection acquired PlayerChoiceSynchronizer.");
        InitializeMultiplayerChoiceStreams(_multiplayerSynchronizer);
        LogInfo("P103", $"Starting persona selection initialized choice streams for {_orderedPlayers.Count} players.");
        FlushPendingLocalSelectionIfNeeded();
        UpdatePendingSubtitle();

        foreach (var player in _orderedPlayers)
        {
            if (_localPlayer != null && player.NetId == _localPlayer.NetId)
                continue;

            _ = ObserveRemoteSelectionsAsync(player, _multiplayerSynchronizer);
        }

        var timeoutTask = WaitForTimeoutAsync(SelectionCommitTimeoutMilliseconds);
        var completedTask = await Task.WhenAny(_multiplayerCommitSource.Task, timeoutTask);
        if (completedTask != _multiplayerCommitSource.Task)
        {
            LogWarn("P104",
                "Starting persona relic selection timed out while waiting for the final synchronized lock; applying deterministic timeout fallback.");
            AstralNotificationService.ShowDiagnosticWarning(
                AstralNotificationModule.Multiplayer,
                AstralNotificationArea.PersonaSelection,
                104,
                "全员选完后未能及时收到最终锁定，正在尝试自动补救。",
                "等待最终提交");
            var timeoutSnapshot = await ResolveTimeoutFallbackAsync();
            ApplyCommittedSelections(timeoutSnapshot);
            return;
        }

        var committedSnapshot = await _multiplayerCommitSource.Task;
        LogInfo("P105",
            $"Starting persona selection received committed snapshot with {committedSnapshot.SelectedIndexes.Count} players.");
        ApplyCommittedSelections(committedSnapshot);
    }

    private async Task CollectAutomaticSelectionsAsync()
    {
        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            await RunAutomaticCountdownAsync();
            ApplyCommittedSelections(new CommittedSelectionSnapshot
            {
                SelectedIndexes = BuildAutomaticCommittedSelectionIndexes()
            });
            return;
        }

        _multiplayerSynchronizer = await WaitForPlayerChoiceSynchronizerAsync()
                                   ?? throw new InvalidOperationException(
                                       "PlayerChoiceSynchronizer was not ready for automatic starting persona selection.");
        LogInfo("P102", "Starting persona automatic selection acquired PlayerChoiceSynchronizer.");
        InitializeMultiplayerChoiceStreams(_multiplayerSynchronizer);

        foreach (var player in _orderedPlayers)
        {
            if (_localPlayer != null && player.NetId == _localPlayer.NetId)
                continue;

            _ = ObserveRemoteSelectionsAsync(player, _multiplayerSynchronizer);
        }

        var countdownTask = RunAutomaticCountdownAsync();
        var timeoutTask = WaitForTimeoutAsync(SelectionCommitTimeoutMilliseconds);
        var completedTask = await Task.WhenAny(_multiplayerCommitSource.Task, timeoutTask, countdownTask);

        if (completedTask == timeoutTask)
        {
            LogWarn("P104",
                "Starting persona automatic selection timed out while waiting for the final synchronized lock; applying deterministic timeout fallback.");
            var timeoutSnapshot = await ResolveTimeoutFallbackAsync();
            ApplyCommittedSelections(timeoutSnapshot);
            return;
        }

        if (completedTask == countdownTask && !_multiplayerCommitSource.Task.IsCompleted)
        {
            var automaticSnapshot = await ResolveAutomaticCommitAfterCountdownAsync();
            ApplyCommittedSelections(automaticSnapshot);
            return;
        }

        var committedSnapshot = await _multiplayerCommitSource.Task;
        LogInfo("P105",
            $"Starting persona automatic selection received committed snapshot with {committedSnapshot.SelectedIndexes.Count} players.");
        ApplyCommittedSelections(committedSnapshot);
    }

    private void ApplySelection(Player player, int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _relicOptions.Count)
        {
            MainFile.Logger.Warn(
                $"Starting persona selection ignored invalid selection index: player={player.NetId} index={selectedIndex} options={_relicOptions.Count}.");
            return;
        }

        var selectedRelic = selectedIndex >= 0 && selectedIndex < _relicOptions.Count
            ? _relicOptions[selectedIndex]
            : null;

        var state = _selectionStates[player.NetId];
        state.SelectedRelic = selectedRelic;
        state.SelectionResolved = true;
        RefreshVotes();
    }

    private void SaveAllOptionsAsSeen()
    {
        if (_localPlayer == null)
            return;

        foreach (var relic in _relicOptions)
            SaveManager.Instance.MarkRelicAsSeen(relic);
    }

    private void InitializeMultiplayerChoiceStreams(PlayerChoiceSynchronizer synchronizer)
    {
        foreach (var player in _orderedPlayers)
            _nextChoiceIdsByPlayer[player.NetId] = synchronizer.ReserveChoiceId(player);
    }

    private bool AreAllSelectionsSubmitted()
    {
        if (IsDestinedCloneMode())
            return TryGetAuthoritySelectedIndex(out _);

        return _selectionStates.Values.All(static state => state.SelectedRelic != null);
    }

    private List<StartingPersonSelectionResult> ResolveSelectionResults()
    {
        if (_displayMode == StartingPersonDisplayMode.Automatic)
            return ResolveAutomaticSelectionResults();

        if (_assignmentMode == StartingPersonAssignmentMode.Clone)
            return ResolveCloneSelectionResults();

        if (_allowDuplicates)
            return ResolveSelectionResultsAllowingDuplicates();

        var votesByRelic = _relicOptions.ToDictionary(relic => relic.Id, _ => new List<Player>());
        var skippedPlayers = new List<Player>();

        foreach (var state in _selectionStates.Values)
        {
            if (state.SelectedRelic == null)
            {
                skippedPlayers.Add(state.Player);
                continue;
            }

            votesByRelic[state.SelectedRelic.Id].Add(state.Player);
        }

        var results = new List<StartingPersonSelectionResult>();
        var unclaimedRelics = new List<RelicModel>();

        foreach (var relic in _relicOptions)
        {
            var voters = votesByRelic[relic.Id];
            if (voters.Count == 0)
            {
                unclaimedRelics.Add(relic);
                continue;
            }

            if (voters.Count == 1)
            {
                results.Add(new StartingPersonSelectionResult
                {
                    type = RelicPickingResultType.OnlyOnePlayerVoted,
                    relic = relic,
                    optionIndex = IndexOfRelic(_relicOptions, relic),
                    player = voters[0]
                });
                continue;
            }

            results.Add(GenerateDeterministicFight(voters, relic));
        }

        var assignedPlayers =
            results.Where(result => result.player != null).Select(result => result.player!).ToHashSet();
        var consolationPlayers = _orderedPlayers
            .Where(player => !assignedPlayers.Contains(player) && !skippedPlayers.Contains(player))
            .ToList();
        var orderedUnclaimedRelics = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            unclaimedRelics,
            relic => relic.Id.Entry,
            MainFile.ModId,
            "starting_persona_consolation_relics",
            _runState.Rng.StringSeed,
            _orderedPlayers.Count);

        for (var i = 0; i < orderedUnclaimedRelics.Count; i++)
        {
            var relic = orderedUnclaimedRelics[i];
            if (i < consolationPlayers.Count)
                results.Add(new StartingPersonSelectionResult
                {
                    type = RelicPickingResultType.ConsolationPrize,
                    player = consolationPlayers[i],
                    relic = relic,
                    optionIndex = IndexOfRelic(_relicOptions, relic)
                });
            else
                results.Add(new StartingPersonSelectionResult
                {
                    type = RelicPickingResultType.Skipped,
                    player = null,
                    relic = relic,
                    optionIndex = IndexOfRelic(_relicOptions, relic)
                });
        }

        return results
            .OrderBy(result => result.type)
            .ThenBy(result => result.relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    private List<StartingPersonSelectionResult> ResolveSelectionResultsAllowingDuplicates()
    {
        return _orderedPlayers
            .Select(player => _selectionStates[player.NetId])
            .Where(state => state.SelectedRelic != null)
            .Select(state => new StartingPersonSelectionResult
            {
                type = RelicPickingResultType.OnlyOnePlayerVoted,
                relic = state.SelectedRelic!,
                optionIndex = IndexOfRelic(_relicOptions, state.SelectedRelic!),
                player = state.Player
            })
            .OrderBy(result => result.relic.Id.Entry, StringComparer.Ordinal)
            .ThenBy(result => result.player?.NetId ?? 0)
            .ToList();
    }

    private List<StartingPersonSelectionResult> ResolveCloneSelectionResults()
    {
        if (IsDestinedCloneMode())
        {
            if (!TryGetAuthoritySelectedIndex(out var authoritySelectedIndex))
                return [];

            var authorityRelic = _relicOptions[authoritySelectedIndex];
            return _orderedPlayers
                .Select(player => new StartingPersonSelectionResult
                {
                    type = RelicPickingResultType.OnlyOnePlayerVoted,
                    relic = authorityRelic,
                    optionIndex = authoritySelectedIndex,
                    player = player
                })
                .ToList();
        }

        var selectedIndexes = BuildCommittedSelectionIndexes()
            .Where(index => index >= 0 && index < _relicOptions.Count)
            .ToList();
        if (selectedIndexes.Count == 0)
            return [];

        var chosenListIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            selectedIndexes.Count,
            MainFile.ModId,
            "starting_persona_manual_clone_final_pick",
            _runState.Rng.StringSeed,
            _orderedPlayers.Count,
            string.Join(",", selectedIndexes));
        var selectedIndex = selectedIndexes[chosenListIndex];
        var sharedRelic = _relicOptions[selectedIndex];

        return _orderedPlayers
            .Select(player => new StartingPersonSelectionResult
            {
                type = RelicPickingResultType.OnlyOnePlayerVoted,
                relic = sharedRelic,
                optionIndex = selectedIndex,
                player = player
            })
            .ToList();
    }

    private List<StartingPersonSelectionResult> ResolveAutomaticSelectionResults()
    {
        var selectedIndexes = BuildCommittedSelectionIndexes();
        var results = new List<StartingPersonSelectionResult>(_orderedPlayers.Count);
        for (var i = 0; i < _orderedPlayers.Count; i++)
        {
            var selectedIndex = selectedIndexes[i];
            results.Add(new StartingPersonSelectionResult
            {
                type = RelicPickingResultType.OnlyOnePlayerVoted,
                relic = _relicOptions[selectedIndex],
                optionIndex = selectedIndex,
                player = _orderedPlayers[i]
            });
        }

        return results;
    }

    private StartingPersonSelectionResult GenerateDeterministicFight(List<Player> players, RelicModel relic)
    {
        var fight = new RelicPickingFight();
        fight.playersInvolved.AddRange(players);

        var contenders = players.ToHashSet();
        var roundIndex = 0;
        var tieRounds = 0;
        while (contenders.Count > 1)
        {
            var round = new RelicPickingFightRound();
            foreach (var player in players)
            {
                if (!contenders.Contains(player))
                {
                    round.moves.Add(null);
                    continue;
                }

                var move = (RelicPickingFightMove)DeterministicMultiplayerChoiceHelper.RollDeterministically(
                    0,
                    3,
                    MainFile.ModId,
                    "starting_persona_fight_move",
                    _runState.Rng.StringSeed,
                    relic.Id.Entry,
                    roundIndex,
                    player.NetId);
                round.moves.Add(move);
            }

            fight.rounds.Add(round);
            var distinctMoves = round.moves.OfType<RelicPickingFightMove>().Distinct().ToList();
            if (distinctMoves.Count == 2)
            {
                tieRounds = 0;
                var losingMove = GetLosingMove(distinctMoves[0], distinctMoves[1]);
                for (var i = 0; i < players.Count; i++)
                    if (round.moves[i] == losingMove)
                        contenders.Remove(players[i]);
            }
            else
            {
                tieRounds++;
                if (tieRounds >= MaxFightTieRounds)
                {
                    var resolvedWinner = BreakFightTieDeterministically(contenders, relic, roundIndex);
                    contenders.Clear();
                    contenders.Add(resolvedWinner);
                    MainFile.Logger.Info(
                        $"Starting persona selection fight tie-break resolved for relic '{relic.Id.Entry}' after {tieRounds} tied rounds: player {resolvedWinner.NetId}.");
                }
            }

            roundIndex++;
        }

        return new StartingPersonSelectionResult
        {
            type = RelicPickingResultType.FoughtOver,
            relic = relic,
            optionIndex = IndexOfRelic(_relicOptions, relic),
            player = contenders.First(),
            fight = fight
        };
    }

    private Player BreakFightTieDeterministically(IReadOnlyCollection<Player> contenders, RelicModel relic,
        int roundIndex)
    {
        return DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                contenders.ToList(),
                player => player.NetId.ToString(),
                MainFile.ModId,
                "starting_persona_fight_tiebreak",
                _runState.Rng.StringSeed,
                relic.Id.Entry,
                roundIndex)
            .First();
    }

    private static RelicPickingFightMove GetLosingMove(RelicPickingFightMove move1, RelicPickingFightMove move2)
    {
        return (int)(move1 + 1) % 3 == (int)move2 ? move1 : move2;
    }

    private async Task AnimateSelectionResultsAsync(List<StartingPersonSelectionResult> results)
    {
        _subtitleLabel.Text = IsDestinedCloneMode()
            ? "房主人格已锁定，开始统一结算。"
            : _assignmentMode == StartingPersonAssignmentMode.Clone
                ? "最终人格已锁定，开始结算归属。"
                : "选择已锁定，开始结算归属。";
        var remainingAnimationsByOptionIndex = results
            .Where(result => result.type != RelicPickingResultType.Skipped && result.player != null)
            .GroupBy(result => result.optionIndex)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var holder in _holdersByIndex.Values)
        {
            holder.Disable();
            holder.SetFocusMode(FocusModeEnum.None);
        }

        RelicPickingResultType? previousType = null;
        foreach (var result in results.OrderBy(result => result.type))
        {
            if (!_holdersByIndex.TryGetValue(result.optionIndex, out var holder))
                continue;

            holder.AnimateAwayVotes();
            if (previousType.HasValue && previousType != result.type)
                await Cmd.Wait(0.45f);

            if (result.type == RelicPickingResultType.FoughtOver)
            {
                holder.ZIndex = 1;
                _fightBackstop.Visible = true;
                _fightLabel.Text = $"【{result.relic.Title.GetFormattedText()}】发生冲突，开始猜拳";

                var tween = CreateTween().SetParallel();
                tween.TweenProperty(_fightBackstop, "modulate:a", 1f, 0.25f);
                tween.TweenProperty(holder, "global_position", (GetViewportRect().Size - holder.Size) * 0.5f, 0.25f)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.In);
                await ToSignal(tween, Tween.SignalName.Finished);

                if (result.fight != null)
                    await AnimateFightSummaryAsync(result.fight, result.relic);

                await AnimateAwardResultAsync(holder, result.player!, "猜拳胜出", true);

                var fadeTween = CreateTween();
                fadeTween.TweenProperty(_fightBackstop, "modulate:a", 0f, 0.25f);
                await ToSignal(fadeTween, Tween.SignalName.Finished);
                _fightBackstop.Visible = false;
                holder.ZIndex = 0;
            }
            else if (result.type != RelicPickingResultType.Skipped && result.player != null)
            {
                var hideAfterAnimation = true;
                if (remainingAnimationsByOptionIndex.TryGetValue(result.optionIndex, out var remainingCount))
                {
                    hideAfterAnimation = remainingCount <= 1;
                    remainingAnimationsByOptionIndex[result.optionIndex] = Math.Max(0, remainingCount - 1);
                }

                await AnimateAwardResultAsync(holder, result.player,
                    result.type == RelicPickingResultType.ConsolationPrize
                        ? "补发获得"
                        : "直接获得",
                    hideAfterAnimation);
            }

            previousType = result.type;
        }

        if (_runState.Players.Count > 1)
            await Cmd.Wait(0.7f);
    }

    private async Task AnimateFightSummaryAsync(RelicPickingFight fight, RelicModel relic)
    {
        _fightLabel.Text = $"【{relic.Title.GetFormattedText()}】发生冲突，正在稳定判定归属";
        await Cmd.Wait(0.8f);

        for (var roundIndex = 0; roundIndex < fight.rounds.Count; roundIndex++)
        {
            var round = fight.rounds[roundIndex];
            var activeMoves = round.moves.OfType<RelicPickingFightMove>().Distinct().ToList();
            if (activeMoves.Count == 2)
            {
                var losingMove = GetLosingMove(activeMoves[0], activeMoves[1]);
                _fightLabel.Text = $"第 {roundIndex + 1} 轮结束：{FormatMove(losingMove)} 方落败";
            }
            else
            {
                _fightLabel.Text = roundIndex + 1 >= MaxFightTieRounds
                    ? $"第 {roundIndex + 1} 轮结束：平局过多，改为稳定判定归属"
                    : $"第 {roundIndex + 1} 轮结束：平局，继续";
            }

            await Cmd.Wait(0.55f);
        }
    }

    private async Task AnimateAwardResultAsync(
        NTreasureRoomRelicHolder holder,
        Player player,
        string reason,
        bool hideAfterAnimation)
    {
        _fightLabel.Text = $"玩家 {player.NetId} {reason}";

        var targetPosition = ResolvePlayerAnchorPosition(player, holder.Size);
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(holder, "global_position", targetPosition, 0.35f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(holder, "scale", new Vector2(0.85f, 0.85f), 0.35f);
        await ToSignal(tween, Tween.SignalName.Finished);

        var flashTween = CreateTween().SetParallel();
        flashTween.TweenProperty(holder, "modulate", new Color(1f, 1f, 1f, 0.35f), 0.12f);
        flashTween.TweenProperty(holder, "modulate", Colors.White, 0.18f).SetDelay(0.12f);
        await ToSignal(flashTween, Tween.SignalName.Finished);

        holder.Scale = Vector2.One;

        if (hideAfterAnimation)
            holder.Visible = false;
    }

    private Vector2 ResolvePlayerAnchorPosition(Player player, Vector2 holderSize)
    {
        var playerIndex = _orderedPlayers.FindIndex(entry => entry.NetId == player.NetId);
        if (playerIndex < 0)
            playerIndex = 0;

        var viewport = GetViewportRect().Size;
        var spacing = Math.Max(220f, viewport.X / Math.Max(2, _orderedPlayers.Count + 1));
        var startX = viewport.X * 0.5f - spacing * (_orderedPlayers.Count - 1) * 0.5f;
        var x = startX + spacing * playerIndex - holderSize.X * 0.5f;
        var y = viewport.Y - holderSize.Y - 72f;
        return new Vector2(x, y);
    }

    private async Task AwardRelicsAsync(List<StartingPersonSelectionResult> results)
    {
        var awardedResults = new List<(Player Player, RelicModel Relic)>();

        foreach (var result in results)
        {
            if (result.player == null)
                continue;

            var relic = result.relic.ToMutable();
            if (PersonMultiplayerEffectHelper.IsRelicBannedForOwner(result.player, relic))
            {
                MainFile.Logger.Warn(
                    $"[P108] Starting persona selection skipped banned relic '{relic.Id.Entry}' for player {result.player.NetId}.");
                continue;
            }

            SaveManager.Instance.MarkRelicAsSeen(relic);
            MainFile.Logger.Info(
                $"[P109] Starting persona selection awarding relic '{relic.Id.Entry}' to player {result.player.NetId}.");
            try
            {
                await RelicCmd.Obtain(relic, result.player);
            }
            catch (Exception ex)
            {
                AstralRelicDiagnosticHelper.ShowObtainFailure(result.player, relic, "人格发放", 109, ex);
                throw;
            }
            awardedResults.Add((result.player, relic));
        }

        await PersistStartingPersonaSelectionAsync(awardedResults);
        await Cmd.Wait(0.2f);
    }

    private async Task PersistStartingPersonaSelectionAsync(
        IReadOnlyCollection<(Player Player, RelicModel Relic)> awardedResults)
    {
        if (awardedResults.Count == 0)
        {
            LogWarn("P111", "Starting persona selection finished without awarding any relics; skipping run save.");
            return;
        }

        LogInfo("P112",
            "Starting persona selection awarded relics: "
            + string.Join(
                ", ",
                awardedResults.Select(static entry => $"player={entry.Player.NetId}:{entry.Relic.Id.Entry}")));

        LogInfo("P113", "Starting persona selection forcing current run save.");
        await SaveManager.Instance.SaveRun(null);
        LogInfo("P114", "Starting persona selection current run save completed.");
    }

    private void OnHolderSelected(NTreasureRoomRelicHolder holder)
    {
        if (_selectionFinalized)
            return;

        if (_displayMode == StartingPersonDisplayMode.Automatic)
            return;

        if (IsDestinedCloneMode() && !IsLocalAuthorityPlayer())
            return;

        if (Time.GetTicksMsec() - _openedTicks <= 200uL)
            return;

        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            ApplySelection(_localPlayer ?? _orderedPlayers[0], holder.Index);
            _subtitleLabel.Text = "已选择起始人格，开始结算……";
            _singlePlayerChoiceSource.TrySetResult(holder.Index);
            return;
        }

        if (_localPlayer == null || _multiplayerSynchronizer == null ||
            !_nextChoiceIdsByPlayer.ContainsKey(_localPlayer.NetId))
        {
            if (_localPlayer != null)
            {
                _pendingLocalSelectionIndexes[_localPlayer.NetId] = holder.Index;
                ApplySelection(_localPlayer, holder.Index);
                UpdatePendingSubtitle();
            }

            _subtitleLabel.Text = "联机同步尚未就绪，请稍候再试。";
            LogWarn("P115", "Starting persona selection ignored local click because multiplayer sync was not ready.");
            return;
        }

        if (_selectionStates[_localPlayer.NetId].SelectedRelic?.Id == _relicOptions[holder.Index].Id)
        {
            UpdatePendingSubtitle();
            return;
        }

        ApplySelection(_localPlayer, holder.Index);
        SendLocalSelectionUpdate(holder.Index);
        UpdatePendingSubtitle();
        MaybeCommitSelections();
    }

    private void RefreshVotes(bool animate = true)
    {
        foreach (var holder in _holdersByIndex.Values)
        {
            if (!IsInstanceValid(holder) || !IsInstanceValid(holder.VoteContainer))
                continue;

            holder.VoteContainer.RefreshPlayerVotes(animate);
        }
    }

    private bool PlayerSelectedHolder(Player player, int holderIndex)
    {
        if (!_selectionStates.TryGetValue(player.NetId, out var state))
            return false;

        if (state.SelectedRelic == null)
            return false;

        if (holderIndex < 0 || holderIndex >= _relicOptions.Count)
            return false;

        return state.SelectedRelic.Id == _relicOptions[holderIndex].Id;
    }

    private void ConfigureHolderFocusNeighbors()
    {
        var holders = _holdersByIndex.Values.OrderBy(holder => holder.Index).ToList();
        for (var i = 0; i < holders.Count; i++)
        {
            holders[i].SetFocusMode(FocusModeEnum.All);
            holders[i].FocusNeighborTop = holders[i].GetPath();
            holders[i].FocusNeighborBottom = holders[i].GetPath();
            holders[i].FocusNeighborLeft = holders[(i - 1 + holders.Count) % holders.Count].GetPath();
            holders[i].FocusNeighborRight = holders[(i + 1) % holders.Count].GetPath();
        }
    }

    private static void ApplyHolderLayout(IReadOnlyList<NTreasureRoomRelicHolder> holders, int optionCount)
    {
        const float horizontalSpacing = 176f;
        const float verticalSpacing = 190f;

        var columns = Math.Min(8, Math.Max(1, optionCount));
        var rows = (int)Math.Ceiling(optionCount / (double)columns);

        for (var index = 0; index < holders.Count; index++)
        {
            var holder = holders[index];
            var row = index / columns;
            var column = index % columns;
            var rowCount = row == rows - 1 ? optionCount - row * columns : columns;
            var x = (column - (rowCount - 1) * 0.5f) * horizontalSpacing - HolderWidth * 0.5f;
            var y = (row - (rows - 1) * 0.5f) * verticalSpacing - HolderHeight * 0.5f - 30f;

            holder.AnchorLeft = 0.5f;
            holder.AnchorRight = 0.5f;
            holder.AnchorTop = 0.5f;
            holder.AnchorBottom = 0.5f;
            holder.OffsetLeft = x;
            holder.OffsetTop = y;
            holder.OffsetRight = x + HolderWidth;
            holder.OffsetBottom = y + HolderHeight;
            holder.PivotOffset = new Vector2(HolderWidth * 0.5f, HolderHeight * 0.5f);
        }
    }

    private sealed class PlayerSelectionState
    {
        public PlayerSelectionState(Player player)
        {
            Player = player;
        }

        public Player Player { get; }

        public RelicModel? SelectedRelic { get; set; }

        public bool SelectionResolved { get; set; }
    }

    private sealed class CommittedSelectionSnapshot
    {
        public required IReadOnlyList<int> SelectedIndexes { get; init; }
    }

    private sealed class StartingPersonSelectionResult
    {
        public required RelicPickingResultType type { get; init; }

        public required RelicModel relic { get; init; }

        public required int optionIndex { get; init; }

        public Player? player { get; init; }

        public RelicPickingFight? fight { get; init; }
    }

    private static string FormatMove(RelicPickingFightMove move)
    {
        return move switch
        {
            RelicPickingFightMove.Rock => "石头",
            RelicPickingFightMove.Paper => "布",
            RelicPickingFightMove.Scissors => "剪刀",
            _ => move.ToString()
        };
    }

    private void SendLocalSelectionUpdate(int selectedIndex)
    {
        if (_localPlayer == null || _multiplayerSynchronizer == null)
            return;

        var sequence = _selectionSequencesByPlayer[_localPlayer.NetId] + 1;
        _selectionSequencesByPlayer[_localPlayer.NetId] = sequence;

        var choiceId = _nextChoiceIdsByPlayer[_localPlayer.NetId];
        _multiplayerSynchronizer.SyncLocalChoice(
            _localPlayer,
            choiceId,
            CreateSelectionUpdateChoiceResult(sequence, selectedIndex));
        _nextChoiceIdsByPlayer[_localPlayer.NetId] = _multiplayerSynchronizer.ReserveChoiceId(_localPlayer);

        MainFile.Logger.Info(
            $"[P116] Starting persona selection synced local update: player={_localPlayer.NetId} choiceId={choiceId} sequence={sequence} index={selectedIndex}.");
    }

    private async Task ObserveRemoteSelectionsAsync(Player player, PlayerChoiceSynchronizer synchronizer)
    {
        try
        {
            while (!_multiplayerCommitSource.Task.IsCompleted && !_closed)
            {
                var choiceId = _nextChoiceIdsByPlayer[player.NetId];
                var waitTask = synchronizer.WaitForRemoteChoice(player, choiceId);
                var completedTask = await Task.WhenAny(waitTask, _multiplayerCommitSource.Task);
                if (completedTask != waitTask)
                {
                    ObserveTaskFault(waitTask);
                    return;
                }

                var remoteChoice = await waitTask;
                if (TryDecodeSelectionUpdate(remoteChoice, out var sequence, out var selectedIndex))
                {
                    _nextChoiceIdsByPlayer[player.NetId] = synchronizer.ReserveChoiceId(player);
                    ApplyRemoteSelectionUpdate(player, sequence, selectedIndex);
                    MaybeCommitSelections();
                    continue;
                }

                if (_authorityPlayer != null
                    && player.NetId == _authorityPlayer.NetId
                    && TryDecodeSelectionCommit(remoteChoice, out var selectedIndexes))
                {
                    LogInfo("P117",
                        $"Starting persona selection received final commit: player={player.NetId} choiceId={choiceId}.");
                    _multiplayerCommitSource.TrySetResult(new CommittedSelectionSnapshot
                    {
                        SelectedIndexes = selectedIndexes
                    });
                    return;
                }

                LogWarn("P118",
                    $"Starting persona selection skipped foreign multiplayer choice: player={player.NetId} choiceId={choiceId} result={remoteChoice}.");
                _nextChoiceIdsByPlayer[player.NetId] = synchronizer.ReserveChoiceId(player);
            }
        }
        catch (ObjectDisposedException ex)
        {
            LogWarn("P119",
                $"Starting persona selection remote observer disposed for player {player.NetId}; ignoring late UI update. {ex.Message}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[P120] Starting persona selection remote observer failed for player {player.NetId}: {ex}");
            if (!_closed && !_selectionFinalized)
            {
                AstralNotificationService.ShowDiagnosticWarning(
                    AstralNotificationModule.Multiplayer,
                    AstralNotificationArea.PersonaSelection,
                    120,
                    $"远端玩家 {player.NetId} 的人格选择观察线程失败，后续可能出现穿过选择页或未正常同步。",
                    "远端观察");
                _multiplayerCommitSource.TrySetException(ex);
            }
        }
    }

    private void ApplyRemoteSelectionUpdate(Player player, int sequence, int selectedIndex)
    {
        if (!_selectionSequencesByPlayer.TryGetValue(player.NetId, out var currentSequence))
            currentSequence = 0;

        if (sequence < currentSequence)
        {
            MainFile.Logger.Warn(
                $"Starting persona selection ignored stale remote update: player={player.NetId} sequence={sequence} current={currentSequence}.");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= _relicOptions.Count)
        {
            MainFile.Logger.Warn(
                $"Starting persona selection ignored out-of-range remote update: player={player.NetId} sequence={sequence} index={selectedIndex} options={_relicOptions.Count}.");
            return;
        }

        _selectionSequencesByPlayer[player.NetId] = sequence;
        ApplySelection(player, selectedIndex);
        LogInfo("P121",
            $"Starting persona selection applied remote update: player={player.NetId} sequence={sequence} index={selectedIndex}.");
        UpdatePendingSubtitle();
    }

    private void MaybeCommitSelections()
    {
        if (_selectionFinalized || _multiplayerCommitSent || _multiplayerSynchronizer == null ||
            _authorityPlayer == null)
            return;

        if (RunManager.Instance.NetService.Type != NetGameType.Host)
            return;

        if (_localPlayer == null || _localPlayer.NetId != _authorityPlayer.NetId)
            return;

        if (!AreAllSelectionsSubmitted())
            return;

        var selectedIndexes = BuildCommittedSelectionIndexes();
        if (selectedIndexes.Any(index => index < 0 || index >= _relicOptions.Count))
        {
            MainFile.Logger.Warn(
                $"Starting persona selection deferred final commit because some indexes were invalid: {string.Join(',', selectedIndexes)}.");
            return;
        }

        var choiceId = _nextChoiceIdsByPlayer[_authorityPlayer.NetId];
        _multiplayerSynchronizer.SyncLocalChoice(
            _authorityPlayer,
            choiceId,
            CreateSelectionCommitChoiceResult(selectedIndexes));
        _multiplayerCommitSent = true;

        LogInfo("P122",
            $"Starting persona selection sent final commit: player={_authorityPlayer.NetId} choiceId={choiceId} indexes={string.Join(",", selectedIndexes)}.");

        _multiplayerCommitSource.TrySetResult(new CommittedSelectionSnapshot
        {
            SelectedIndexes = selectedIndexes
        });
    }

    private async Task<CommittedSelectionSnapshot> ResolveTimeoutFallbackAsync()
    {
        if (_multiplayerCommitSource.Task.IsCompleted)
            return await _multiplayerCommitSource.Task;

        var fallbackSnapshot = BuildTimeoutFallbackSnapshot();

        if (RunManager.Instance.NetService.Type == NetGameType.Host
            && _localPlayer != null
            && _authorityPlayer != null
            && _localPlayer.NetId == _authorityPlayer.NetId
            && _multiplayerSynchronizer != null
            && !_multiplayerCommitSent)
        {
            TryBroadcastTimeoutFallbackCommit(fallbackSnapshot.SelectedIndexes);
            _multiplayerCommitSource.TrySetResult(fallbackSnapshot);
            return fallbackSnapshot;
        }

        var graceTask = WaitForTimeoutAsync(SelectionCommitGraceMilliseconds);
        var completedTask = await Task.WhenAny(_multiplayerCommitSource.Task, graceTask);
        if (completedTask == _multiplayerCommitSource.Task)
            return await _multiplayerCommitSource.Task;

        LogWarn("P123",
            "Starting persona relic selection did not receive an authority final commit during grace period; applying local deterministic timeout fallback.");
        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.PersonaSelection,
            123,
            "宽限期内仍未收到房主最终提交，已改用本地确定性补救。",
            "宽限期最终提交");
        _multiplayerCommitSource.TrySetResult(fallbackSnapshot);
        return fallbackSnapshot;
    }

    private async Task<CommittedSelectionSnapshot> ResolveAutomaticCommitAfterCountdownAsync()
    {
        if (_multiplayerCommitSource.Task.IsCompleted)
            return await _multiplayerCommitSource.Task;

        var snapshot = new CommittedSelectionSnapshot
        {
            SelectedIndexes = BuildAutomaticCommittedSelectionIndexes()
        };

        if (RunManager.Instance.NetService.Type == NetGameType.Host
            && _localPlayer != null
            && _authorityPlayer != null
            && _localPlayer.NetId == _authorityPlayer.NetId
            && _multiplayerSynchronizer != null
            && !_multiplayerCommitSent)
        {
            TryBroadcastTimeoutFallbackCommit(snapshot.SelectedIndexes);
            _multiplayerCommitSource.TrySetResult(snapshot);
            return snapshot;
        }

        return await ResolveTimeoutFallbackAsync();
    }

    private CommittedSelectionSnapshot BuildTimeoutFallbackSnapshot()
    {
        if (_displayMode == StartingPersonDisplayMode.Automatic)
        {
            return new CommittedSelectionSnapshot
            {
                SelectedIndexes = BuildAutomaticCommittedSelectionIndexes()
            };
        }

        if (IsDestinedCloneMode())
        {
            var authorityFallbackIndex = ResolveAuthorityTimeoutFallbackIndex();
            var destinedCloneIndexes = Enumerable.Repeat(authorityFallbackIndex, _orderedPlayers.Count).ToList();
            LogWarn("P125",
                $"Starting persona selection timeout fallback snapshot: {string.Join(",", destinedCloneIndexes)}.");
            return new CommittedSelectionSnapshot
            {
                SelectedIndexes = destinedCloneIndexes
            };
        }

        var selectedIndexes = new List<int>(_orderedPlayers.Count);
        var reservedIndexes = new HashSet<int>();

        for (var i = 0; i < _orderedPlayers.Count; i++)
        {
            var player = _orderedPlayers[i];
            if (_selectionStates.TryGetValue(player.NetId, out var state) && state.SelectedRelic != null)
            {
                var selectedIndex = IndexOfRelic(_relicOptions, state.SelectedRelic);
                if (selectedIndex >= 0 && selectedIndex < _relicOptions.Count)
                {
                    selectedIndexes.Add(selectedIndex);
                    continue;
                }
            }

            var fallbackIndex = SelectDeterministicTimeoutFallbackIndex(player, reservedIndexes);
            selectedIndexes.Add(fallbackIndex);
            reservedIndexes.Add(fallbackIndex);

            LogWarn("P124",
                $"Starting persona selection timeout fallback assigned index {fallbackIndex} ({_relicOptions[fallbackIndex].Id.Entry}) to player {player.NetId}.");
        }

        LogWarn("P125",
            $"Starting persona selection timeout fallback snapshot: {string.Join(",", selectedIndexes)}.");

        return new CommittedSelectionSnapshot
        {
            SelectedIndexes = selectedIndexes
        };
    }

    private int SelectDeterministicTimeoutFallbackIndex(Player player, IReadOnlySet<int> reservedIndexes)
    {
        var orderedIndexes = Enumerable.Range(0, _relicOptions.Count)
            .OrderBy(index => DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                int.MaxValue,
                MainFile.ModId,
                nameof(StartingPersonRelicSelectionScreen),
                "timeout_fallback",
                _runState.Rng.StringSeed,
                _orderedPlayers.Count,
                player.NetId,
                index))
            .ThenBy(index => _relicOptions[index].Id.Entry, StringComparer.Ordinal)
            .ToList();

        foreach (var index in orderedIndexes)
        {
            if (reservedIndexes.Contains(index))
                continue;

            return index;
        }

        return 0;
    }

    private void TryBroadcastTimeoutFallbackCommit(IReadOnlyList<int> selectedIndexes)
    {
        if (_authorityPlayer == null || _multiplayerSynchronizer == null || _multiplayerCommitSent)
            return;

        try
        {
            var choiceId = _nextChoiceIdsByPlayer[_authorityPlayer.NetId];
            _multiplayerSynchronizer.SyncLocalChoice(
                _authorityPlayer,
                choiceId,
                CreateSelectionCommitChoiceResult(selectedIndexes));
            _multiplayerCommitSent = true;

            LogWarn("P126",
                $"Starting persona selection sent timeout fallback final commit: player={_authorityPlayer.NetId} choiceId={choiceId} indexes={string.Join(",", selectedIndexes)}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"[P127] Starting persona selection failed to broadcast timeout fallback final commit: {ex}");
            AstralNotificationService.ShowDiagnosticError(
                AstralNotificationModule.Multiplayer,
                AstralNotificationArea.PersonaSelection,
                127,
                "超时补救结果未能广播给其他玩家，请把日志和编号发给作者。",
                "超时补救广播");
        }
    }

    private List<int> BuildCommittedSelectionIndexes()
    {
        if (IsDestinedCloneMode())
        {
            if (!TryGetAuthoritySelectedIndex(out var authoritySelectedIndex))
                return Enumerable.Repeat(-1, _orderedPlayers.Count).ToList();

            return Enumerable.Repeat(authoritySelectedIndex, _orderedPlayers.Count).ToList();
        }

        return _orderedPlayers
            .Select(player =>
            {
                if (!_selectionStates.TryGetValue(player.NetId, out var state) || state.SelectedRelic == null)
                    return -1;

                return IndexOfRelic(_relicOptions, state.SelectedRelic);
            })
            .ToList();
    }

    private List<int> BuildAutomaticCommittedSelectionIndexes()
    {
        if (_assignmentMode == StartingPersonAssignmentMode.Clone)
        {
            var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                _relicOptions.Count,
                MainFile.ModId,
                "starting_persona_automatic_clone",
                _runState.Rng.StringSeed,
                _orderedPlayers.Count);
            return Enumerable.Repeat(selectedIndex, _orderedPlayers.Count).ToList();
        }

        var orderedIndexes = Enumerable.Range(0, _relicOptions.Count)
            .OrderBy(index => DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                int.MaxValue,
                MainFile.ModId,
                "starting_persona_automatic_independent",
                _runState.Rng.StringSeed,
                _orderedPlayers.Count,
                index))
            .ThenBy(index => _relicOptions[index].Id.Entry, StringComparer.Ordinal)
            .ToList();

        var selectedIndexes = new List<int>(_orderedPlayers.Count);
        for (var i = 0; i < _orderedPlayers.Count; i++)
            selectedIndexes.Add(orderedIndexes[i % orderedIndexes.Count]);

        return selectedIndexes;
    }

    private void ApplyCommittedSelections(CommittedSelectionSnapshot snapshot)
    {
        if (snapshot.SelectedIndexes.Count != _orderedPlayers.Count)
            throw new InvalidOperationException(
                $"Starting persona selection commit player count mismatch: expected {_orderedPlayers.Count}, got {snapshot.SelectedIndexes.Count}.");

        for (var i = 0; i < _orderedPlayers.Count; i++)
        {
            if (snapshot.SelectedIndexes[i] < 0 || snapshot.SelectedIndexes[i] >= _relicOptions.Count)
                throw new InvalidOperationException(
                    $"Starting persona selection commit index out of range for player {_orderedPlayers[i].NetId}: {snapshot.SelectedIndexes[i]} / {_relicOptions.Count}.");

            ApplySelection(_orderedPlayers[i], snapshot.SelectedIndexes[i]);
        }

        AstralTelemetry.RecordPersonaChoice(
            _runState,
            _relicOptions,
            _orderedPlayers
                .Select((player, index) => new KeyValuePair<ulong, int>(player.NetId, snapshot.SelectedIndexes[index]))
                .ToDictionary());

        LogInfo("P128",
            $"Starting persona selection applied committed snapshot: {string.Join(",", snapshot.SelectedIndexes)}.");
        FinalizeSelectionDisplay(IsDestinedCloneMode()
            ? "房主人格已锁定，开始统一结算……"
            : _assignmentMode == StartingPersonAssignmentMode.Clone
                ? "最终人格已锁定，开始结算……"
                : "所有玩家已锁定选择，开始结算……");
    }

    private void FinalizeSelectionDisplay(string subtitle)
    {
        _selectionFinalized = true;
        foreach (var holder in _holdersByIndex.Values)
            holder.Disable();

        _subtitleLabel.Text = subtitle;
    }

    private void UpdatePendingSubtitle()
    {
        if (_selectionFinalized)
            return;

        if (_displayMode == StartingPersonDisplayMode.Automatic)
            return;

        var selectedCount = _selectionStates.Values.Count(static state => state.SelectedRelic != null);
        if (_localPlayer == null || !_selectionStates.TryGetValue(_localPlayer.NetId, out var localState) ||
            localState.SelectedRelic == null)
        {
            if (IsDestinedCloneMode() && !IsLocalAuthorityPlayer())
            {
                _subtitleLabel.Text = "当前为天命克隆模式：仅房主可以选择人格，锁定后会统一发给所有玩家。";
                return;
            }

            _subtitleLabel.Text = BuildSelectionIntroSubtitle(_runState);
            return;
        }

        if (IsDestinedCloneMode())
        {
            _subtitleLabel.Text = RunManager.Instance.NetService.Type == NetGameType.Host
                ? "已更新房主人格选择；锁定前可以继续改选。"
                : "房主已更新人格选择，等待房主锁定……";
            return;
        }

        if (selectedCount < _orderedPlayers.Count)
        {
            _subtitleLabel.Text = $"已更新你的选择（{selectedCount}/{_orderedPlayers.Count} 已选）；全员完成前可以继续改选。";
            return;
        }

        _subtitleLabel.Text = RunManager.Instance.NetService.Type == NetGameType.Host
            ? "所有玩家已选择，正在锁定……"
            : "所有玩家已选择，等待同步锁定……";
    }

    private static string BuildSelectionIntroSubtitle(IRunState? runState)
    {
        var mode = ReAstralPartyModSettingsManager.GetStartingPersonaMode(runState);

        if (mode == StartingPersonMode.RandomAssign)
            return "当前为随机分配模式：展示等于玩家数量的人格候选，5 秒后自动为每名玩家分配结果。";

        if (mode == StartingPersonMode.RandomClone)
            return "当前为随机克隆模式：展示等于玩家数量的人格候选，5 秒后自动命中 1 个并为所有玩家统一发放。";

        if (mode == StartingPersonMode.Clone)
            return "当前为克隆模式：所有玩家先正常选择，锁定后会从已提交选择中稳定随机命中 1 个，并为所有玩家统一发放。";

        if (mode == StartingPersonMode.DestinedClone)
            return "当前为天命克隆模式：仅房主选择的人格会生效，锁定后统一发给所有玩家；其他玩家无法选择。";

        if (mode == StartingPersonMode.StandardDuplicate)
        {
            return "当前为标准可重复模式：所有玩家共享同一批人格。全员完成前可以改选；多人选择同一人格时，都会直接获得。";
        }

        return "当前为标准模式：所有玩家共享同一批人格。全员完成前可以改选；若多人选中同一人格，则按稳定猜拳规则决定归属。";
    }

    private async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync()
    {
        var runManager = RunManager.Instance;
        for (var frame = 0; frame < MaxSynchronizerWaitFrames; frame++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }

        LogWarn("P129",
            $"Starting persona selection did not acquire PlayerChoiceSynchronizer within {MaxSynchronizerWaitFrames} frames.");
        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.PersonaSelection,
            129,
            "联机同步器长时间未就绪，请把日志和编号发给作者。",
            "等待同步器");
        return runManager.PlayerChoiceSynchronizer;
    }

    private async Task WaitForFramesAsync(int frameCount)
    {
        for (var frame = 0; frame < frameCount && !_closed && !_multiplayerCommitSource.Task.IsCompleted; frame++)
            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
    }

    private async Task WaitForTimeoutAsync(int timeoutMilliseconds)
    {
        var startedAt = Time.GetTicksMsec();
        while (!_closed && !_multiplayerCommitSource.Task.IsCompleted)
        {
            if (Time.GetTicksMsec() - startedAt >= (ulong)timeoutMilliseconds)
                return;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }

    private async Task CloseAfterInputReleasedAsync()
    {
        try
        {
            await WaitForMouseReleaseAsync();
        }
        finally
        {
            if (!_closed)
            {
                if (NOverlayStack.Instance != null && IsInsideTree())
                    NOverlayStack.Instance.Remove(this);
                else
                {
                    _closed = true;
                    _overlayClosedSource.TrySetResult();
                    QueueFree();
                }
            }
        }
    }

    private async Task WaitForMouseReleaseAsync()
    {
        if (!await AwaitProcessFrameIfInsideTreeAsync())
            return;

        while (Input.IsMouseButtonPressed(MouseButton.Left))
            if (!await AwaitProcessFrameIfInsideTreeAsync())
                return;

        await AwaitProcessFrameIfInsideTreeAsync();
    }

    private async Task<bool> AwaitProcessFrameIfInsideTreeAsync()
    {
        if (!IsInsideTree())
            return false;

        var tree = GetTree();
        if (tree == null)
            return false;

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return IsInsideTree();
    }

    private static PlayerChoiceResult CreateSelectionUpdateChoiceResult(int sequence, int selectedIndex)
    {
        return PlayerChoiceResult.FromIndexes([
            MultiplayerChoiceMagic, ChoiceKindSelectionUpdate, sequence, selectedIndex
        ]);
    }

    private static PlayerChoiceResult CreateSelectionCommitChoiceResult(IReadOnlyList<int> selectedIndexes)
    {
        var payload = new List<int>(selectedIndexes.Count + 3)
        {
            MultiplayerChoiceMagic,
            ChoiceKindSelectionCommit,
            selectedIndexes.Count
        };
        payload.AddRange(selectedIndexes);
        return PlayerChoiceResult.FromIndexes(payload);
    }

    private static bool TryDecodeSelectionUpdate(PlayerChoiceResult result, out int sequence, out int selectedIndex)
    {
        sequence = 0;
        selectedIndex = -1;
        if (!TryGetIndexPayload(result, out var payload)
            || payload.Count < 4
            || payload[0] != MultiplayerChoiceMagic
            || payload[1] != ChoiceKindSelectionUpdate)
            return false;

        sequence = payload[2];
        selectedIndex = payload[3];
        return true;
    }

    private static bool TryDecodeSelectionCommit(PlayerChoiceResult result, out IReadOnlyList<int> selectedIndexes)
    {
        selectedIndexes = [];
        if (!TryGetIndexPayload(result, out var payload)
            || payload.Count < 3
            || payload[0] != MultiplayerChoiceMagic
            || payload[1] != ChoiceKindSelectionCommit)
            return false;

        var playerCount = payload[2];
        if (playerCount < 0 || payload.Count < playerCount + 3)
            return false;

        selectedIndexes = payload.Skip(3).Take(playerCount).ToArray();
        return true;
    }

    private static bool TryGetIndexPayload(PlayerChoiceResult result, out List<int> payload)
    {
        payload = [];
        try
        {
            var indexes = result.AsIndexes();
            if (indexes == null)
                return false;

            payload = indexes;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static int IndexOfRelic(IReadOnlyList<RelicModel> relics, RelicModel relic)
    {
        for (var i = 0; i < relics.Count; i++)
        {
            var left = relics[i].CanonicalInstance?.Id ?? relics[i].Id;
            var right = relic.CanonicalInstance?.Id ?? relic.Id;
            if (left == right)
                return i;
        }

        return -1;
    }

    private static Player? ResolveAuthorityPlayer(IReadOnlyList<Player> orderedPlayers, Player? localPlayer)
    {
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null)
            return localPlayer ?? orderedPlayers.FirstOrDefault();

        var authorityNetId = netService.Type == NetGameType.Host
            ? netService.NetId
            : netService is INetClientGameService clientService
                ? clientService.NetClient?.HostNetId ?? 0UL
                : 0UL;

        if (authorityNetId != 0UL)
        {
            var authorityPlayer = orderedPlayers.FirstOrDefault(player => player.NetId == authorityNetId);
            if (authorityPlayer != null)
                return authorityPlayer;
        }

        return localPlayer ?? orderedPlayers.FirstOrDefault();
    }

    private void FlushPendingLocalSelectionIfNeeded()
    {
        if (_displayMode == StartingPersonDisplayMode.Automatic)
            return;

        if (_localPlayer == null || _multiplayerSynchronizer == null)
            return;
        if (!_pendingLocalSelectionIndexes.TryGetValue(_localPlayer.NetId, out var pendingIndex))
            return;
        if (pendingIndex < 0 || pendingIndex >= _relicOptions.Count)
            return;

        if (_selectionStates[_localPlayer.NetId].SelectedRelic?.Id != _relicOptions[pendingIndex].Id)
            ApplySelection(_localPlayer, pendingIndex);

        SendLocalSelectionUpdate(pendingIndex);
        _pendingLocalSelectionIndexes[_localPlayer.NetId] = -1;
    }

    private async Task RunAutomaticCountdownAsync()
    {
        if (_automaticCommitTriggered)
            return;

        _automaticCommitTriggered = true;
        var deadline = Time.GetTicksMsec() + (ulong)Math.Max(
            AutomaticSelectionMinimumCountdownMilliseconds,
            _automaticSelectionCountdownSeconds * 1000);

        while (!_closed && !_multiplayerCommitSource.Task.IsCompleted)
        {
            var now = Time.GetTicksMsec();
            var remainingMilliseconds = deadline > now ? deadline - now : 0UL;
            var remainingSeconds = (int)Math.Ceiling(remainingMilliseconds / 1000d);
            _subtitleLabel.Text = _assignmentMode == StartingPersonAssignmentMode.Clone
                ? $"随机克隆模式：{Math.Max(0, remainingSeconds)} 秒后自动统一命中 1 个人格。"
                : $"随机模式：{Math.Max(0, remainingSeconds)} 秒后自动分配人格。";

            if (remainingMilliseconds == 0UL)
                break;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }

    private bool IsDestinedCloneMode()
    {
        return ReAstralPartyModSettingsManager.GetStartingPersonaMode(_runState) == StartingPersonMode.DestinedClone;
    }

    private bool IsLocalAuthorityPlayer()
    {
        return _localPlayer != null && _authorityPlayer != null && _localPlayer.NetId == _authorityPlayer.NetId;
    }

    private bool TryGetAuthoritySelectedIndex(out int selectedIndex)
    {
        selectedIndex = -1;
        if (_authorityPlayer == null)
            return false;

        if (!_selectionStates.TryGetValue(_authorityPlayer.NetId, out var authorityState) || authorityState.SelectedRelic == null)
            return false;

        selectedIndex = IndexOfRelic(_relicOptions, authorityState.SelectedRelic);
        return selectedIndex >= 0 && selectedIndex < _relicOptions.Count;
    }

    private int ResolveAuthorityTimeoutFallbackIndex()
    {
        if (_authorityPlayer == null)
            return 0;

        if (TryGetAuthoritySelectedIndex(out var selectedIndex))
            return selectedIndex;

        var fallbackIndex = SelectDeterministicTimeoutFallbackIndex(_authorityPlayer, new HashSet<int>());
        LogWarn("P124",
            $"Starting persona selection timeout fallback assigned index {fallbackIndex} ({_relicOptions[fallbackIndex].Id.Entry}) to authority player {_authorityPlayer.NetId}.");
        return fallbackIndex;
    }

    private static void ObserveTaskFault(Task task)
    {
        _ = task.ContinueWith(
            static completedTask => { _ = completedTask.Exception; },
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    private static void LogInfo(string code, string message)
    {
        MainFile.Logger.Info($"[{code}] {message}");
    }

    private static void LogWarn(string code, string message)
    {
        MainFile.Logger.Warn($"[{code}] {message}");
    }

}
