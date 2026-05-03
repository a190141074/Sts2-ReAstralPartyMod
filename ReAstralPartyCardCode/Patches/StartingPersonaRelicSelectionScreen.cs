using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed partial class StartingPersonaRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private const string BackgroundTexturePath =
        "res://ReAstralPartyMod/images/background/starting_persona_pelic_selection_screen.png";

    private const string TreasureRelicHolderScenePath = "ui/treasure_relic_holder";

    private readonly TaskCompletionSource _completionSource = new();
    private readonly TaskCompletionSource<int> _localChoiceSource = new();
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
    private NHandImageCollection _hands = null!;
    private bool _opened;
    private bool _closed;
    private bool _holdersBuilt;
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

        if (NRun.Instance?.ScreenStateTracker == null)
            throw new InvalidOperationException("Run screen state tracker was not ready for starting persona selection.");

        NRun.Instance.ScreenStateTracker.SetIsInSharedRelicPickingScreen(isInSharedRelicPicking: true);
        _hands.Initialize(_runState);
        AnimateIn();
        _openedTicks = Time.GetTicksMsec();
        _ = RunSelectionFlow();
    }

    public void AfterOverlayClosed()
    {
        NRun.Instance?.ScreenStateTracker?.SetIsInSharedRelicPickingScreen(isInSharedRelicPicking: false);
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

        _hands = new NHandImageCollection
        {
            Name = "HandsContainer",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _hands.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_hands);

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
            Text = "所有玩家共享同一批人格。选中后会显示头像；若多人选中同一人格，则按宝箱房规则猜拳决定归属。",
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
        _holderContainer.OffsetBottom = -60f;
        AddChild(_holderContainer);

        _fightBackstop = new ColorRect
        {
            Name = "FightBackstop",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Color = new Color(0f, 0f, 0f, 0.55f)
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
            MouseFilter = MouseFilterEnum.Ignore
        };
        _fightLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        _fightLabel.OffsetTop = 160f;
        _fightLabel.OffsetLeft = -280f;
        _fightLabel.OffsetRight = 280f;
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
            tween.TweenProperty(holder, "position:y", targetY, 0.5).SetDelay(delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.TweenCallback(Callable.From(delegate
            {
                holder.MouseFilter = MouseFilterEnum.Stop;
                holder.Enable();
            })).SetDelay(delay + 0.5f);
        }

        _hands.AnimateHandsIn();
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

        if (RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.None)
        {
            var selectedIndex = await _localChoiceSource.Task;
            ApplySelection(_orderedPlayers[0], selectedIndex);
            return;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync();
        if (synchronizer == null)
            throw new InvalidOperationException("PlayerChoiceSynchronizer was not ready for starting persona selection.");

        var choiceIds = _orderedPlayers.ToDictionary(player => player.NetId, player => synchronizer.ReserveChoiceId(player));
        var selectionTasks = _orderedPlayers
            .Select(player => CollectSelectionForPlayer(player, synchronizer, choiceIds[player.NetId]))
            .ToList();
        await Task.WhenAll(selectionTasks);
    }

    private async Task CollectSelectionForPlayer(Player player, PlayerChoiceSynchronizer synchronizer, uint choiceId)
    {
        if (LocalContext.IsMe(player))
        {
            var selectedIndex = await _localChoiceSource.Task;
            synchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex(selectedIndex));
            ApplySelection(player, selectedIndex);
            return;
        }

        var selectedIndexRemote = (await synchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
        ApplySelection(player, selectedIndexRemote);
    }

    private async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync()
    {
        for (var frame = 0; frame < 60; frame++)
        {
            if (RunManager.Instance.PlayerChoiceSynchronizer != null)
                return RunManager.Instance.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return RunManager.Instance.PlayerChoiceSynchronizer;
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
                var losingMove = GetLosingMove(distinctMoves[0], distinctMoves[1]);
                for (var i = 0; i < players.Count; i++)
                {
                    if (round.moves[i] == losingMove)
                        contenders.Remove(players[i]);
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

        foreach (var state in _selectionStates.Values)
        {
            if (state.SelectedRelic == null)
                _hands.GetHand(state.Player.NetId)?.SetSkipped();
        }

        _hands.BeforeRelicsAwarded();
        var tasksToWait = new List<Task>();
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

                _hands.BeforeFightStarted(result.fight!.playersInvolved);
                await Cmd.Wait(1f);
                await _hands.DoFight(result, holder);

                var fadeTween = CreateTween();
                fadeTween.TweenProperty(_fightBackstop, "modulate:a", 0f, 0.25f);
                await ToSignal(fadeTween, Tween.SignalName.Finished);
                _fightBackstop.Visible = false;
                holder.ZIndex = 0;
            }
            else if (result.type != RelicPickingResultType.Skipped && result.player != null)
            {
                var hand = _hands.GetHand(result.player.NetId);
                if (hand != null)
                {
                    tasksToWait.Add(TaskHelper.RunSafely(hand.GrabRelic(holder)));
                    await Cmd.Wait(0.25f);
                }
            }

            previousType = result.type;
        }

        if (tasksToWait.Count > 0)
            await Task.WhenAll(tasksToWait);
        else if (_runState.Players.Count > 1)
            await Cmd.Wait(0.7f);
    }

    private async Task AwardRelicsAsync(List<RelicPickingResult> results)
    {
        foreach (var result in results)
        {
            if (result.player == null)
                continue;

            var relic = result.relic.ToMutable();
            SaveManager.Instance.MarkRelicAsSeen(relic);
            await RelicCmd.Obtain(relic, result.player);
        }

        await Cmd.Wait(0.2f);
    }

    private void OnHolderSelected(NTreasureRoomRelicHolder holder)
    {
        if (_localChoiceSource.Task.IsCompleted)
            return;

        if (Time.GetTicksMsec() - _openedTicks <= 200uL)
            return;

        var localPlayer = LocalContext.GetMe(_runState.Players);
        if (localPlayer == null)
            return;

        foreach (var relicHolder in _holdersById.Values)
            relicHolder.Disable();

        ApplySelection(localPlayer, holder.Index);
        _subtitleLabel.Text = "已提交你的选择，等待其他玩家……";
        _localChoiceSource.TrySetResult(holder.Index);
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
        const float holderWidth = 136f;
        const float holderHeight = 136f;
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
            var x = (column - (rowCount - 1) * 0.5f) * horizontalSpacing - holderWidth * 0.5f;
            var y = (row - (rows - 1) * 0.5f) * verticalSpacing - holderHeight * 0.5f - 30f;

            holder.AnchorLeft = 0.5f;
            holder.AnchorRight = 0.5f;
            holder.AnchorTop = 0.5f;
            holder.AnchorBottom = 0.5f;
            holder.OffsetLeft = x;
            holder.OffsetTop = y;
            holder.OffsetRight = x + holderWidth;
            holder.OffsetBottom = y + holderHeight;
            holder.PivotOffset = new Vector2(holderWidth * 0.5f, holderHeight * 0.5f);
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
}
