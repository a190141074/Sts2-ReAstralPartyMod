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

public sealed partial class StartingPersonaRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private const string BackgroundTexturePath =
        "res://ReAstralPartyMod/images/background/starting_persona_pelic_selection_screen.png";

    private const string TreasureRelicHolderScenePath = "ui/treasure_relic_holder";
    private const float HolderWidth = 136f;
    private const float HolderHeight = 136f;
    private const int MaxFightTieRounds = 4;
    private const int MaxSynchronizerWaitFrames = 600;
    private const int MaxLocalPlayerResolveWaitFrames = 600;
    private const int SelectionCommitTimeoutMilliseconds = 10 * 60 * 1000;
    private const int SelectionCommitGraceMilliseconds = 5 * 1000;

    private readonly TaskCompletionSource _completionSource = new();
    private readonly TaskCompletionSource<int> _singlePlayerChoiceSource = new();
    private readonly TaskCompletionSource<CommittedSelectionSnapshot> _multiplayerCommitSource = new();
    private readonly RunState _runState;
    private readonly IReadOnlyList<RelicModel> _relicOptions;
    private readonly List<Player> _orderedPlayers;
    private readonly Dictionary<ulong, PlayerSelectionState> _selectionStates = new();
    private readonly Dictionary<ModelId, NTreasureRoomRelicHolder> _holdersById = new();
    private readonly Dictionary<NTreasureRoomRelicHolder, int> _holderOptionIndexes = new();
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
    private readonly string _choiceSessionKey;
    private int _deferredUnresolvedLocalSelectionIndex = -1;

    public NetScreenType ScreenType => NetScreenType.None;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _holdersById.Values.OrderBy(holder => holder.Index).FirstOrDefault();

    private StartingPersonaRelicSelectionScreen(RunState runState, IReadOnlyList<RelicModel> relicOptions)
    {
        _runState = runState;
        _relicOptions = relicOptions;
        _orderedPlayers = runState.Players
            .OrderBy(static player => player.NetId)
            .ToList();
        _localPlayer = ResolveLocalPlayer(_orderedPlayers);
        _authorityPlayer = ResolveAuthorityPlayer(_orderedPlayers, _localPlayer);
        _choiceSessionKey =
            $"starting_persona_selection|{AstralChoiceProtocol.CreateRunScopeKey(runState)}|{_orderedPlayers.Count}";

        foreach (var player in _orderedPlayers)
        {
            _selectionStates[player.NetId] = new PlayerSelectionState(player);
            _selectionSequencesByPlayer[player.NetId] = 0;
            _pendingLocalSelectionIndexes[player.NetId] = -1;
        }

        Name = nameof(StartingPersonaRelicSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        Visible = false;

        BuildStaticUi();
    }

    public static StartingPersonaRelicSelectionScreen Create(RunState runState, IReadOnlyList<RelicModel> relicOptions)
    {
        return new StartingPersonaRelicSelectionScreen(runState, relicOptions);
    }

    public Task RelicPickingFinished()
    {
        return _completionSource.Task;
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
        _closed = true;
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
            Text = BuildSelectionIntroSubtitle(),
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
                player => PlayerSelectedHolder(player, GetHolderOptionIndex(holder)),
                _orderedPlayers);
            holder.Connect(NClickableControl.SignalName.Released,
                Callable.From<NTreasureRoomRelicHolder>(_ => OnHolderSelected(holder)));
            _holdersById[relic.Id] = holder;
            _holderOptionIndexes[holder] = index;
        }

        ApplyHolderLayout(_holdersById.Values.OrderBy(holder => holder.Index).ToList(), _relicOptions.Count);
        ConfigureHolderFocusNeighbors();
        RefreshVotes(false);
    }

    private void AnimateIn()
    {
        foreach (var holder in _holdersById.Values.OrderBy(holder => holder.Index))
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
                holder.MouseFilter = MouseFilterEnum.Stop;
                holder.Enable();
            })).SetDelay(delay + 0.5f);
        }
    }

    private async Task RunSelectionFlow()
    {
        try
        {
            await WaitForSelectionEnvironmentReadyAsync();
            await CollectSelectionsAsync();
            var results = ResolveSelectionResults();
            await AnimateSelectionResultsAsync(results);
            await AwardRelicsAsync(results);
            _completionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
            MainFile.Logger.Error($"Starting persona relic shared selection failed: {ex}");
        }
    }

    private async Task WaitForSelectionEnvironmentReadyAsync()
    {
        for (var frame = 0; frame < 180 && !_closed; frame++)
        {
            if (NOverlayStack.Instance != null &&
                NGame.Instance != null &&
                RunManager.Instance?.PlayerChoiceSynchronizer != null)
                return;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }

    private async Task CollectSelectionsAsync()
    {
        RefreshLocalPlayerIdentity();

        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            SaveAllOptionsAsSeen();
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
        InitializeMultiplayerChoiceStreams(_multiplayerSynchronizer);
        _ = TaskHelper.RunSafely(EnsureLocalPlayerIdentityReadyAsync());
        FlushPendingLocalSelectionIfNeeded();
        UpdatePendingSubtitle();

        foreach (var player in _orderedPlayers)
        {
            if (ShouldSkipRemoteObserverForPlayer(player))
                continue;

            _ = ObserveRemoteSelectionsAsync(player, _multiplayerSynchronizer);
        }

        var timeoutTask = WaitForTimeoutAsync(SelectionCommitTimeoutMilliseconds);
        var completedTask = await Task.WhenAny(_multiplayerCommitSource.Task, timeoutTask);
        if (completedTask != _multiplayerCommitSource.Task)
        {
            MainFile.Logger.Warn(
                "Starting persona relic selection timed out while waiting for the final synchronized lock; applying deterministic timeout fallback.");
            var timeoutSnapshot = await ResolveTimeoutFallbackAsync();
            ApplyCommittedSelections(timeoutSnapshot);
            return;
        }

        var committedSnapshot = await _multiplayerCommitSource.Task;
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
        return _selectionStates.Values.All(static state => state.SelectedRelic != null);
    }

    private List<RelicPickingResult> ResolveSelectionResults()
    {
        if (ReAstralPartyModSettingsManager.GetEnableDuplicatePersonas(_runState))
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

        var results = new List<RelicPickingResult>();
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
                results.Add(new RelicPickingResult
                {
                    type = RelicPickingResultType.OnlyOnePlayerVoted,
                    relic = relic,
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
                results.Add(new RelicPickingResult
                {
                    type = RelicPickingResultType.ConsolationPrize,
                    player = consolationPlayers[i],
                    relic = relic
                });
            else
                results.Add(new RelicPickingResult
                {
                    type = RelicPickingResultType.Skipped,
                    player = null,
                    relic = relic
                });
        }

        return results
            .OrderBy(result => result.type)
            .ThenBy(result => result.relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    private List<RelicPickingResult> ResolveSelectionResultsAllowingDuplicates()
    {
        return _orderedPlayers
            .Select(player => _selectionStates[player.NetId])
            .Where(state => state.SelectedRelic != null)
            .Select(state => new RelicPickingResult
            {
                type = RelicPickingResultType.OnlyOnePlayerVoted,
                relic = state.SelectedRelic!,
                player = state.Player
            })
            .OrderBy(result => result.relic.Id.Entry, StringComparer.Ordinal)
            .ThenBy(result => result.player?.NetId ?? 0)
            .ToList();
    }

    private RelicPickingResult GenerateDeterministicFight(List<Player> players, RelicModel relic)
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

        return new RelicPickingResult
        {
            type = RelicPickingResultType.FoughtOver,
            relic = relic,
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

    private async Task AnimateSelectionResultsAsync(List<RelicPickingResult> results)
    {
        _subtitleLabel.Text = "选择已锁定，开始结算归属。";
        var remainingAnimationsByRelicId = results
            .Where(result => result.type != RelicPickingResultType.Skipped && result.player != null)
            .GroupBy(result => result.relic.Id)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var holder in _holdersById.Values)
        {
            holder.Disable();
            holder.SetFocusMode(FocusModeEnum.None);
        }

        RelicPickingResultType? previousType = null;
        foreach (var result in results.OrderBy(result => result.type))
        {
            if (!_holdersById.TryGetValue(result.relic.Id, out var holder))
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
                if (remainingAnimationsByRelicId.TryGetValue(result.relic.Id, out var remainingCount))
                {
                    hideAfterAnimation = remainingCount <= 1;
                    remainingAnimationsByRelicId[result.relic.Id] = Math.Max(0, remainingCount - 1);
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

    private async Task AwardRelicsAsync(List<RelicPickingResult> results)
    {
        var awardedResults = new List<(Player Player, RelicModel Relic)>();

        foreach (var result in results)
        {
            if (result.player == null)
                continue;

            var relic = result.relic.ToMutable();
            SaveManager.Instance.MarkRelicAsSeen(relic);
            MainFile.Logger.Info(
                $"Starting persona selection awarding relic '{relic.Id.Entry}' to player {result.player.NetId}.");
            await RelicCmd.Obtain(relic, result.player);
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
            MainFile.Logger.Warn("Starting persona selection finished without awarding any relics; skipping run save.");
            return;
        }

        MainFile.Logger.Info(
            "Starting persona selection awarded relics: "
            + string.Join(
                ", ",
                awardedResults.Select(static entry => $"player={entry.Player.NetId}:{entry.Relic.Id.Entry}")));

        MainFile.Logger.Info("Starting persona selection forcing current run save.");
        await SaveManager.Instance.SaveRun(null);
        MainFile.Logger.Info("Starting persona selection current run save completed.");
    }

    private void OnHolderSelected(NTreasureRoomRelicHolder holder)
    {
        var selectedIndex = GetHolderOptionIndex(holder);
        if (_selectionFinalized)
            return;

        if (Time.GetTicksMsec() - _openedTicks <= 200uL)
            return;

        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            ApplySelection(_localPlayer ?? _orderedPlayers[0], selectedIndex);
            _subtitleLabel.Text = "已选择起始人格，开始结算……";
            _singlePlayerChoiceSource.TrySetResult(selectedIndex);
            return;
        }

        RefreshLocalPlayerIdentity();
        if (_localPlayer == null || _multiplayerSynchronizer == null ||
            !_nextChoiceIdsByPlayer.ContainsKey(_localPlayer.NetId))
        {
            _deferredUnresolvedLocalSelectionIndex = selectedIndex;
            if (_localPlayer != null)
            {
                _pendingLocalSelectionIndexes[_localPlayer.NetId] = selectedIndex;
                ApplySelection(_localPlayer, selectedIndex);
                UpdatePendingSubtitle();
            }

            _subtitleLabel.Text = _localPlayer == null
                ? "正在识别当前玩家，请稍候再试。"
                : "联机同步尚未就绪，请稍候再试。";
            return;
        }

        if (_selectionStates[_localPlayer.NetId].SelectedRelic?.Id == _relicOptions[selectedIndex].Id)
        {
            UpdatePendingSubtitle();
            return;
        }

        ApplySelection(_localPlayer, selectedIndex);
        SendLocalSelectionUpdate(selectedIndex);
        UpdatePendingSubtitle();
        MaybeCommitSelections();
    }

    private int GetHolderOptionIndex(NTreasureRoomRelicHolder holder)
    {
        if (_holderOptionIndexes.TryGetValue(holder, out var index))
            return index;

        return holder.Index;
    }

    private void RefreshVotes(bool animate = true)
    {
        foreach (var holder in _holdersById.Values)
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
        var holders = _holdersById.Values.OrderBy(holder => holder.Index).ToList();
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
            $"Starting persona selection synced local update: player={_localPlayer.NetId} choiceId={choiceId} sequence={sequence} index={selectedIndex}.");
    }

    private async Task ObserveRemoteSelectionsAsync(Player player, PlayerChoiceSynchronizer synchronizer)
    {
        try
        {
            while (!_multiplayerCommitSource.Task.IsCompleted && !_closed)
            {
                if (!_nextChoiceIdsByPlayer.TryGetValue(player.NetId, out var choiceId))
                {
                    MainFile.Logger.Warn(
                        $"Starting persona selection remote observer missing choice id for player={player.NetId}; stopping observer.");
                    return;
                }

                var waitTask = DeterministicMultiplayerChoiceHelper.WaitForRemoteIndexedEnvelopeAnyKind(
                    synchronizer,
                    player,
                    choiceId,
                    player.NetId == _authorityPlayer?.NetId
                        ? [AstralChoiceKind.StartingPersonaSelectionUpdate, AstralChoiceKind.StartingPersonaSelectionCommit]
                        : [AstralChoiceKind.StartingPersonaSelectionUpdate],
                    _runState,
                    _choiceSessionKey,
                    "starting persona selection");
                var completedTask = await Task.WhenAny(waitTask, _multiplayerCommitSource.Task);
                if (completedTask != waitTask)
                {
                    ObserveTaskFault(waitTask);
                    return;
                }

                var remoteChoice = await waitTask;
                if (remoteChoice == null)
                {
                    MainFile.Logger.Error(
                        $"Starting persona selection exhausted remote choice stream: player={player.NetId} choiceId={choiceId}.");
                    return;
                }

                if (remoteChoice.Value.Kind == AstralChoiceKind.StartingPersonaSelectionUpdate &&
                    TryDecodeSelectionUpdate(remoteChoice.Value.RawResult, out var sequence, out var selectedIndex))
                {
                    _nextChoiceIdsByPlayer[player.NetId] = synchronizer.ReserveChoiceId(player);
                    ApplyRemoteSelectionUpdate(player, sequence, selectedIndex);
                    MaybeCommitSelections();
                    continue;
                }

                if (remoteChoice.Value.Kind == AstralChoiceKind.StartingPersonaSelectionCommit &&
                    _authorityPlayer != null
                    && player.NetId == _authorityPlayer.NetId
                    && TryDecodeSelectionCommit(remoteChoice.Value.RawResult, out var selectedIndexes))
                {
                    _nextChoiceIdsByPlayer[player.NetId] = synchronizer.ReserveChoiceId(player);
                    MainFile.Logger.Info(
                        $"Starting persona selection received final commit: player={player.NetId} choiceId={choiceId}.");
                    _multiplayerCommitSource.TrySetResult(new CommittedSelectionSnapshot
                    {
                        SelectedIndexes = selectedIndexes
                    });
                    return;
                }

                MainFile.Logger.Warn(
                    $"Starting persona selection ignored mismatched multiplayer choice after envelope wait: player={player.NetId} choiceId={choiceId}.");
            }
        }
        catch (ObjectDisposedException ex)
        {
            MainFile.Logger.Warn(
                $"Starting persona selection remote observer disposed for player {player.NetId}; ignoring late UI update. {ex.Message}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Starting persona selection remote observer failed for player {player.NetId}: {ex}");
            if (!_closed && !_selectionFinalized)
                _multiplayerCommitSource.TrySetException(ex);
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
        MainFile.Logger.Info(
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
        _nextChoiceIdsByPlayer[_authorityPlayer.NetId] = _multiplayerSynchronizer.ReserveChoiceId(_authorityPlayer);
        _multiplayerCommitSent = true;

        MainFile.Logger.Info(
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

        MainFile.Logger.Warn(
            "Starting persona relic selection did not receive an authority final commit during grace period; applying local deterministic timeout fallback.");
        _multiplayerCommitSource.TrySetResult(fallbackSnapshot);
        return fallbackSnapshot;
    }

    private CommittedSelectionSnapshot BuildTimeoutFallbackSnapshot()
    {
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

            MainFile.Logger.Warn(
                $"Starting persona selection timeout fallback assigned index {fallbackIndex} ({_relicOptions[fallbackIndex].Id.Entry}) to player {player.NetId}.");
        }

        MainFile.Logger.Warn(
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
                nameof(StartingPersonaRelicSelectionScreen),
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
            _nextChoiceIdsByPlayer[_authorityPlayer.NetId] = _multiplayerSynchronizer.ReserveChoiceId(_authorityPlayer);
            _multiplayerCommitSent = true;

            MainFile.Logger.Warn(
                $"Starting persona selection sent timeout fallback final commit: player={_authorityPlayer.NetId} choiceId={choiceId} indexes={string.Join(",", selectedIndexes)}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"Starting persona selection failed to broadcast timeout fallback final commit: {ex}");
        }
    }

    private List<int> BuildCommittedSelectionIndexes()
    {
        return _orderedPlayers
            .Select(player =>
            {
                if (!_selectionStates.TryGetValue(player.NetId, out var state) || state.SelectedRelic == null)
                    return -1;

                return IndexOfRelic(_relicOptions, state.SelectedRelic);
            })
            .ToList();
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

        FinalizeSelectionDisplay("所有玩家已锁定选择，开始结算……");
    }

    private void FinalizeSelectionDisplay(string subtitle)
    {
        _selectionFinalized = true;
        foreach (var holder in _holdersById.Values)
        {
            if (!IsInstanceValid(holder))
                continue;

            try
            {
                holder.Disable();
            }
            catch (ObjectDisposedException)
            {
                // Late fallback commits can race with UI teardown; ignore disposed holders.
            }
        }

        if (IsInstanceValid(_subtitleLabel))
            _subtitleLabel.Text = subtitle;
    }

    private void UpdatePendingSubtitle()
    {
        if (_selectionFinalized)
            return;

        var selectedCount = _selectionStates.Values.Count(static state => state.SelectedRelic != null);
        if (_localPlayer == null)
        {
            _subtitleLabel.Text = "正在同步当前玩家身份，请稍候……";
            return;
        }

        if (!_selectionStates.TryGetValue(_localPlayer.NetId, out var localState) ||
            localState.SelectedRelic == null)
        {
            _subtitleLabel.Text = BuildSelectionIntroSubtitle();
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

    private static string BuildSelectionIntroSubtitle()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        return ReAstralPartyModSettingsManager.GetEnableDuplicatePersonas(runState)
            ? "所有玩家共享同一批人格。全员完成前可以改选；多人选择同一人格时，都会直接获得。"
            : "所有玩家共享同一批人格。全员完成前可以改选；若多人选中同一人格，则按稳定猜拳规则决定归属。";
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
                NOverlayStack.Instance?.Remove(this);
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

    private PlayerChoiceResult CreateSelectionUpdateChoiceResult(int sequence, int selectedIndex)
    {
        return AstralChoiceProtocol.CreateIndexedEnvelope(
            AstralChoiceKind.StartingPersonaSelectionUpdate,
            _runState,
            _choiceSessionKey,
            sequence,
            [selectedIndex]);
    }

    private PlayerChoiceResult CreateSelectionCommitChoiceResult(IReadOnlyList<int> selectedIndexes)
    {
        var payload = new List<int>(selectedIndexes.Count + 1)
        {
            selectedIndexes.Count
        };
        payload.AddRange(selectedIndexes);
        return AstralChoiceProtocol.CreateIndexedEnvelope(
            AstralChoiceKind.StartingPersonaSelectionCommit,
            _runState,
            _choiceSessionKey,
            0,
            payload);
    }

    private bool TryDecodeSelectionUpdate(PlayerChoiceResult result, out int sequence, out int selectedIndex)
    {
        sequence = 0;
        selectedIndex = -1;
        if (!AstralChoiceProtocol.TryDecodeIndexedEnvelope(
                result,
                AstralChoiceKind.StartingPersonaSelectionUpdate,
                _runState,
                _choiceSessionKey,
                out sequence,
                out var payload)
            || payload.Count < 1)
            return false;

        selectedIndex = payload[0];
        return true;
    }

    private bool TryDecodeSelectionCommit(PlayerChoiceResult result, out IReadOnlyList<int> selectedIndexes)
    {
        selectedIndexes = [];
        if (!AstralChoiceProtocol.TryDecodeIndexedEnvelope(
                result,
                AstralChoiceKind.StartingPersonaSelectionCommit,
                _runState,
                _choiceSessionKey,
                out _,
                out var payload)
            || payload.Count < 1)
            return false;

        var playerCount = payload[0];
        if (playerCount < 0 || payload.Count < playerCount + 1)
            return false;

        selectedIndexes = payload.Skip(1).Take(playerCount).ToArray();
        return true;
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

    private static Player? ResolveLocalPlayer(IReadOnlyList<Player> orderedPlayers)
    {
        var localPlayer = LocalContext.GetMe(orderedPlayers);
        if (localPlayer != null)
            return localPlayer;

        var localNetId = RunManager.Instance?.NetService?.NetId ?? 0UL;
        if (localNetId == 0UL)
            return null;

        return orderedPlayers.FirstOrDefault(player => player.NetId == localNetId);
    }

    private bool ShouldSkipRemoteObserverForPlayer(Player player)
    {
        if (_localPlayer != null && player.NetId == _localPlayer.NetId)
            return true;

        var localNetId = RunManager.Instance?.NetService?.NetId ?? 0UL;
        return localNetId != 0UL && player.NetId == localNetId;
    }

    private bool RefreshLocalPlayerIdentity(bool logOnAcquire = false)
    {
        var resolvedLocalPlayer = ResolveLocalPlayer(_orderedPlayers);
        if (resolvedLocalPlayer == null)
            return false;

        var previousNetId = _localPlayer?.NetId ?? 0UL;
        var changed = previousNetId != resolvedLocalPlayer.NetId;
        _localPlayer = resolvedLocalPlayer;
        _authorityPlayer = ResolveAuthorityPlayer(_orderedPlayers, _localPlayer);
        if (changed && logOnAcquire)
        {
            MainFile.Logger.Info(
                $"Starting persona selection resolved local player identity: local={_localPlayer.NetId} authority={_authorityPlayer?.NetId ?? 0UL}.");
        }

        return true;
    }

    private async Task EnsureLocalPlayerIdentityReadyAsync()
    {
        if (RefreshLocalPlayerIdentity(logOnAcquire: true))
        {
            FinalizeLocalPlayerResolution();
            return;
        }

        for (var frame = 0; frame < MaxLocalPlayerResolveWaitFrames && !_closed && !_selectionFinalized; frame++)
        {
            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();

            if (!RefreshLocalPlayerIdentity(logOnAcquire: true))
                continue;

            FinalizeLocalPlayerResolution();
            return;
        }

        MainFile.Logger.Warn("Starting persona selection could not resolve local player identity before timeout.");
    }

    private void FinalizeLocalPlayerResolution()
    {
        SaveAllOptionsAsSeen();
        CaptureDeferredSelectionForResolvedLocalPlayer();
        FlushPendingLocalSelectionIfNeeded();
        UpdatePendingSubtitle();
    }

    private void CaptureDeferredSelectionForResolvedLocalPlayer()
    {
        if (_localPlayer == null)
            return;
        if (_deferredUnresolvedLocalSelectionIndex < 0 || _deferredUnresolvedLocalSelectionIndex >= _relicOptions.Count)
            return;

        _pendingLocalSelectionIndexes[_localPlayer.NetId] = _deferredUnresolvedLocalSelectionIndex;
        ApplySelection(_localPlayer, _deferredUnresolvedLocalSelectionIndex);
        _deferredUnresolvedLocalSelectionIndex = -1;
    }

    private void FlushPendingLocalSelectionIfNeeded()
    {
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

    private static void ObserveTaskFault(Task task)
    {
        _ = task.ContinueWith(
            static completedTask => { _ = completedTask.Exception; },
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }
}
