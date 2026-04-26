using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

public sealed partial class StartingPersonaRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private static readonly FieldInfo TreasureRoomRunStateField =
        AccessTools.Field(typeof(NTreasureRoom), "_runState")
        ?? throw new MissingFieldException(typeof(NTreasureRoom).FullName, "_runState");

    private static readonly FieldInfo TreasureRoomRoomField =
        AccessTools.Field(typeof(NTreasureRoom), "_room")
        ?? throw new MissingFieldException(typeof(NTreasureRoom).FullName, "_room");

    private const string TreasureRoomScenePath = "rooms/treasure_room";
    private const string TreasureRelicHolderScenePath = "ui/treasure_relic_holder";
    private const string BackgroundTexturePath =
        "res://AstralPartyMod/images/background/starting_persona_pelic_selection_screen.png";

    private readonly TaskCompletionSource _completionSource = new();
    private readonly IRunState _runState;
    private readonly int _optionCount;
    private readonly NTreasureRoom _treasureRoom;
    private readonly NTreasureRoomRelicCollection _relicCollection;
    private readonly Node2D _fakeChestVisual = new();
    private bool _opened;

    public NetScreenType ScreenType => NetScreenType.Rewards;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _relicCollection.DefaultFocusedControl;

    private StartingPersonaRelicSelectionScreen(IRunState runState, IReadOnlyList<RelicModel> relics)
    {
        _runState = runState;
        _optionCount = relics.Count;
        Name = nameof(StartingPersonaRelicSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        AddBackground();

        _treasureRoom = CreateTreasureRoom(runState, _optionCount);
        _treasureRoom.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _treasureRoom.Visible = false;
        AddChild(_treasureRoom);

        _fakeChestVisual.Name = "FakeChestVisual";
        _fakeChestVisual.Visible = false;
        AddChild(_fakeChestVisual);

        _relicCollection = _treasureRoom.GetNode<NTreasureRoomRelicCollection>("%RelicCollection");
    }

    public static StartingPersonaRelicSelectionScreen Create(IRunState runState, IReadOnlyList<RelicModel> relics)
    {
        return new StartingPersonaRelicSelectionScreen(runState, relics);
    }

    public Task RelicPickingFinished()
    {
        return _completionSource.Task;
    }

    public void Close()
    {
        NOverlayStack.Instance?.Remove(this);
    }

    public void AfterOverlayOpened()
    {
        Visible = true;

        if (_opened)
            return;

        _opened = true;
        HideTreasureRoomChrome();
        _treasureRoom.Visible = true;
        _relicCollection.InitializeRelics();
        _relicCollection.AnimIn(_fakeChestVisual);
        _ = WaitForRelicPickingFinished();
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

    private async Task WaitForRelicPickingFinished()
    {
        try
        {
            await _relicCollection.RelicPickingFinished();
            _relicCollection.AnimOut(_fakeChestVisual);
            await Cmd.Wait(0.35f);
            _completionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
            MainFile.Logger.Error($"Starting persona relic shared selection failed: {ex}");
        }
    }

    private void HideTreasureRoomChrome()
    {
        HideNode("ColorRect");
        HideNode("%Banner");
        HideNode("%Chest");
        HideNode("%ChestVisual");
        HideNode("%GoldExplosion");
        HideNode("%ProceedButton");
        HideNode("%SkipMultiplayerVoteContainer");
    }

    private void AddBackground()
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
    }

    private void HideNode(string path)
    {
        if (_treasureRoom.GetNodeOrNull<CanvasItem>(path) is { } node)
            node.Visible = false;
    }

    private static NTreasureRoom CreateTreasureRoom(IRunState runState, int optionCount)
    {
        var treasureRoom = SceneHelper.Instantiate<NTreasureRoom>(TreasureRoomScenePath);
        TreasureRoomRunStateField.SetValue(treasureRoom, runState);
        TreasureRoomRoomField.SetValue(treasureRoom, new TreasureRoom(0));
        var relicCollection = treasureRoom.GetNode<NTreasureRoomRelicCollection>("%RelicCollection");
        EnsureHolderCapacity(relicCollection, optionCount);
        return treasureRoom;
    }

    private static void EnsureHolderCapacity(NTreasureRoomRelicCollection relicCollection, int optionCount)
    {
        if (optionCount <= 1)
            return;

        var container = relicCollection.GetNode<Control>("Container");
        var currentHolders = new List<NTreasureRoomRelicHolder>();
        foreach (var child in container.GetChildren())
        {
            if (child is NTreasureRoomRelicHolder holder && holder.Name != "SingleplayerRelicHolder")
                currentHolders.Add(holder);
        }

        while (currentHolders.Count < optionCount)
        {
            var holder = SceneHelper.Instantiate<NTreasureRoomRelicHolder>(TreasureRelicHolderScenePath);
            holder.Name = $"StartingPersonaRelicHolder{currentHolders.Count + 1}";
            holder.Visible = false;
            container.AddChild(holder);
            currentHolders.Add(holder);
        }

        ApplyHolderLayout(currentHolders, optionCount);
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
            var y = (row - (rows - 1) * 0.5f) * verticalSpacing - holderHeight * 0.5f - 40f;

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
}
