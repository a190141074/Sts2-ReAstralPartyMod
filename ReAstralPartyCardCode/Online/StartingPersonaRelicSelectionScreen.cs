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

    private readonly TaskCompletionSource _completionSource = new();
    private readonly RunState _runState;
    private readonly IReadOnlyList<RelicModel> _relicOptions;
    private readonly List<Player> _orderedPlayers;
    private readonly Dictionary<ulong, PlayerSelectionState> _selectionStates = new();
    private readonly Dictionary<ModelId, NTreasureRoomRelicHolder> _holdersById = new();

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

    public NetScreenType ScreenType => NetScreenType.None;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _holdersById.Values.OrderBy(holder => holder.Index).FirstOrDefault();

    private StartingPersonaRelicSelectionScreen(RunState runState, IReadOnlyList<RelicModel> relicOptions)
    {
        _runState = runState;
        _relicOptions = relicOptions;
        _orderedPlayers = runState.Players.OrderBy(player => player.NetId).ToList();

        foreach (var player in _orderedPlayers)
            _selectionStates[player.NetId] = new PlayerSelectionState(player);

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
        if (_closed)
            return;

        _closed = true;
        NOverlayStack.Instance?.Remove(this);
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
            Text = "所有玩家共享同一批人格。选中后会显示选择状态；若多人选中同一人格，则按宝箱房规则猜拳决定归属。",
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
                _runState.Players);
            holder.Connect(NClickableControl.SignalName.Released, Callable.From<NTreasureRoomRelicHolder>(_ => OnHolderSelected(holder)));
            _holdersById[relic.Id] = holder;
        }

        ApplyHolderLayout(_holdersById.Values.OrderBy(holder => holder.Index).ToList(), _relicOptions.Count);
        ConfigureHolderFocusNeighbors();
        RefreshVotes(animate: false);
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

    private async Task CollectSelectionsAsync()
    {
        SaveAllOptionsAsSeen();

        var relicSynchronizer = RunManager.Instance.TreasureRoomRelicSynchronizer
            ?? throw new InvalidOperationException("TreasureRoomRelicSynchronizer was not ready for starting persona selection.");

        if (!TreasureRoomRelicSessionHelper.TryBeginSession(relicSynchronizer, _runState, _relicOptions))
            throw new InvalidOperationException("Treasure-room relic session was already active for starting persona selection.");

        try
        {
            for (var frame = 0; frame < 60 * 60 * 5; frame++)
            {
                SyncSelectionStatesFromVotes(relicSynchronizer);
                if (AreAllSelectionsSubmitted())
                {
                    _selectionFinalized = true;
                    foreach (var holder in _holdersById.Values)
                        holder.Disable();

                    _subtitleLabel.Text = "所有玩家已锁定选择，开始结算……";
                    return;
                }

                await Task.Yield();
            }

            throw new TimeoutException("Starting persona relic selection timed out while waiting for votes.");
        }
        finally
        {
            TreasureRoomRelicSessionHelper.EndSessionSafely(relicSynchronizer);
        }
    }

    private void ApplySelection(Player player, int selectedIndex)
    {
        RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < _relicOptions.Count
            ? _relicOptions[selectedIndex]
            : null;

        var state = _selectionStates[player.NetId];
        state.SelectedRelic = selectedRelic;
        state.SelectionResolved = true;
        RefreshVotes();
    }

    private void SaveAllOptionsAsSeen()
    {
        if (_runState.Players.FirstOrDefault(LocalContext.IsMe) == null)
            return;

        foreach (var relic in _relicOptions)
            SaveManager.Instance.MarkRelicAsSeen(relic);
    }

    private void SyncSelectionStatesFromVotes(TreasureRoomRelicSynchronizer synchronizer)
    {
        foreach (var player in _orderedPlayers)
        {
            var vote = synchronizer.GetPlayerVote(player);
            var selectedRelic = vote.voteReceived
                && vote.index.HasValue
                && vote.index.Value >= 0
                && vote.index.Value < _relicOptions.Count
                ? _relicOptions[vote.index.Value]
                : null;

            var state = _selectionStates[player.NetId];
            if (state.SelectionResolved == vote.voteReceived
                && state.SelectedRelic?.Id == selectedRelic?.Id)
            {
                continue;
            }

            state.SelectionResolved = vote.voteReceived;
            state.SelectedRelic = selectedRelic;
        }

        RefreshVotes();
    }

    private bool AreAllSelectionsSubmitted()
    {
        return _selectionStates.Values.All(static state => state.SelectionResolved);
    }

    private List<RelicPickingResult> ResolveSelectionResults()
    {
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

        var assignedPlayers = results.Where(result => result.player != null).Select(result => result.player!).ToHashSet();
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
            {
                results.Add(new RelicPickingResult
                {
                    type = RelicPickingResultType.ConsolationPrize,
                    player = consolationPlayers[i],
                    relic = relic
                });
            }
            else
            {
                results.Add(new RelicPickingResult
                {
                    type = RelicPickingResultType.Skipped,
                    player = null,
                    relic = relic
                });
            }
        }

        return results
            .OrderBy(result => result.type)
            .ThenBy(result => result.relic.Id.Entry, StringComparer.Ordinal)
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
                {
                    if (round.moves[i] == losingMove)
                        contenders.Remove(players[i]);
                }
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

    private Player BreakFightTieDeterministically(IReadOnlyCollection<Player> contenders, RelicModel relic, int roundIndex)
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

                await AnimateAwardResultAsync(holder, result.player, "猜拳胜出");

                var fadeTween = CreateTween();
                fadeTween.TweenProperty(_fightBackstop, "modulate:a", 0f, 0.25f);
                await ToSignal(fadeTween, Tween.SignalName.Finished);
                _fightBackstop.Visible = false;
                holder.ZIndex = 0;
            }
            else if (result.type != RelicPickingResultType.Skipped && result.player != null)
            {
                await AnimateAwardResultAsync(holder, result.player, result.type == RelicPickingResultType.ConsolationPrize
                    ? "补发获得"
                    : "直接获得");
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

    private async Task AnimateAwardResultAsync(NTreasureRoomRelicHolder holder, Player player, string reason)
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

        holder.Visible = false;
        holder.Scale = Vector2.One;
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

    private async Task PersistStartingPersonaSelectionAsync(IReadOnlyCollection<(Player Player, RelicModel Relic)> awardedResults)
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
        await SaveManager.Instance.SaveRun(preFinishedRoom: null);
        MainFile.Logger.Info("Starting persona selection current run save completed.");
    }

    private void OnHolderSelected(NTreasureRoomRelicHolder holder)
    {
        if (_selectionFinalized)
            return;

        if (Time.GetTicksMsec() - _openedTicks <= 200uL)
            return;

        var relicSynchronizer = RunManager.Instance.TreasureRoomRelicSynchronizer;
        if (relicSynchronizer == null)
            return;

        relicSynchronizer.PickRelicLocally(holder.Index);
        SyncSelectionStatesFromVotes(relicSynchronizer);
        _subtitleLabel.Text = "已更新你的选择，等待其他玩家锁定……";
    }

    private void RefreshVotes(bool animate = true)
    {
        foreach (var holder in _holdersById.Values)
            holder.VoteContainer.RefreshPlayerVotes(animate);
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

        var columns = Math.Min(4, Math.Max(1, optionCount));
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
}
