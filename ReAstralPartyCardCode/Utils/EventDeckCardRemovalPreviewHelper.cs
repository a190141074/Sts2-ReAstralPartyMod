using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EventDeckCardRemovalPreviewHelper
{
    public static async Task PlayShatterPreviewAsync(Player owner, CardModel removedCard)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(removedCard);

        if (TestMode.IsOn)
            return;

        if (!ShouldShowPreviewForLocalPlayer(owner))
            return;

        var previewContainer = NRun.Instance?.GlobalUi.CardPreviewContainer
                               ?? NGame.Instance?.GetTree()?.CurrentScene as Control;
        if (previewContainer == null)
            return;

        try
        {
            var preview = EventDeckCardRemovalPreviewVfx.Create(removedCard);
            previewContainer.AddChildSafely(preview);
            await preview.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            Log.Warn($"[{MainFile.ModId}] Failed to play event deck card shatter preview.\n{ex}");
        }
    }

    private static bool ShouldShowPreviewForLocalPlayer(Player owner)
    {
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (runManager == null || netService == null)
            return true;

        if (netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return true;

        if (owner.NetId == 0UL || netService.NetId == 0UL)
            return true;

        return owner.NetId == netService.NetId;
    }
}

internal sealed partial class EventDeckCardRemovalPreviewVfx : Control
{
    private const float PreviewWidth = 480f;
    private const float PreviewHeight = 700f;
    private const float CardScale = 1.04f;
    private const float AnimationDurationScale = 1.15f;
    private static readonly Vector2[] ShardDirections =
    [
        new(-0.95f, -0.65f),
        new(-0.55f, -1.05f),
        new(0.15f, -1.12f),
        new(0.82f, -0.72f),
        new(1.02f, 0.12f),
        new(0.7f, 0.95f),
        new(-0.18f, 1.08f),
        new(-0.96f, 0.58f)
    ];

    private readonly TaskCompletionSource _completionSource = new();
    private readonly CardModel _removedCard;
    private readonly ColorRect[] _shards = new ColorRect[ShardDirections.Length];

    private Control _cardStage = null!;
    private ColorRect _flashOverlay = null!;
    private NCard? _cardNode;

    private EventDeckCardRemovalPreviewVfx(CardModel removedCard)
    {
        _removedCard = removedCard;

        Name = nameof(EventDeckCardRemovalPreviewVfx);
        MouseFilter = MouseFilterEnum.Ignore;
        FocusMode = FocusModeEnum.None;
        ZIndex = 110;
        Modulate = Colors.White;

        BuildUi();
    }

    public static EventDeckCardRemovalPreviewVfx Create(CardModel removedCard)
    {
        return new EventDeckCardRemovalPreviewVfx(removedCard);
    }

    public Task WaitForCompletionAsync()
    {
        return _completionSource.Task;
    }

    public override void _Ready()
    {
        if (GetParent() is Control parentControl)
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Size = parentControl.Size;
            Position = Vector2.Zero;
        }

        TaskHelper.RunSafely(PlayAnimationAsync());
    }

    private void BuildUi()
    {
        _cardStage = new Control
        {
            Name = "CardStage",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _cardStage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_cardStage);

        _flashOverlay = new ColorRect
        {
            Name = "FlashOverlay",
            Color = new Color(1f, 0.94f, 0.9f, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        _flashOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_flashOverlay);

        for (var i = 0; i < _shards.Length; i++)
        {
            var shard = new ColorRect
            {
                Name = $"Shard{i}",
                Color = new Color(0.96f, 0.35f, 0.28f, 0f),
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(56f, 96f),
                Size = new Vector2(56f, 96f),
                PivotOffset = new Vector2(28f, 48f)
            };
            shard.RotationDegrees = i * 19f - 24f;
            shard.Scale = new Vector2(0.2f, 0.2f);
            _cardStage.AddChild(shard);
            _shards[i] = shard;
        }
    }

    private async Task PlayAnimationAsync()
    {
        try
        {
            _cardNode = NCard.Create(_removedCard);
            _cardStage.AddChildSafely(_cardNode);

            var tree = GetTree();
            if (tree == null)
                return;

            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (_cardNode == null)
                return;

            _cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            _cardNode.PivotOffset = new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.5f);
            _cardNode.Position = GetCenteredCardPosition();
            _cardNode.Scale = Vector2.One * 0.08f;
            _cardNode.Modulate = new Color(1f, 1f, 1f, 0f);
            UpdateShardOrigins();

            var intro = CreateTween().SetParallel();
            intro.TweenProperty(_cardNode, "scale", Vector2.One * CardScale, ScaleDuration(0.18f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            intro.TweenProperty(_cardNode, "modulate:a", 1f, ScaleDuration(0.15f));
            await ToSignal(intro, Tween.SignalName.Finished);

            await WaitSecondsAsync(0.12f);

            var compress = CreateTween().SetParallel();
            compress.TweenProperty(_cardNode, "scale", new Vector2(CardScale * 1.08f, CardScale * 0.84f), ScaleDuration(0.1f))
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Back);
            compress.TweenProperty(_cardNode, "rotation_degrees", -4f, ScaleDuration(0.08f))
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            compress.TweenProperty(_flashOverlay, "color:a", 0.26f, ScaleDuration(0.08f));
            await ToSignal(compress, Tween.SignalName.Finished);

            PlayShatterBurst();

            var shatter = CreateTween().SetParallel();
            shatter.TweenProperty(_cardNode, "scale", new Vector2(CardScale * 1.26f, CardScale * 0.42f), ScaleDuration(0.12f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            shatter.TweenProperty(_cardNode, "rotation_degrees", 8f, ScaleDuration(0.12f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            shatter.TweenProperty(_cardNode, "modulate:a", 0f, ScaleDuration(0.14f));
            shatter.TweenProperty(_flashOverlay, "color:a", 0.48f, ScaleDuration(0.06f));
            await ToSignal(shatter, Tween.SignalName.Finished);

            var outro = CreateTween().SetParallel();
            outro.TweenProperty(_flashOverlay, "color:a", 0f, ScaleDuration(0.18f));
            outro.TweenProperty(this, "modulate:a", 0f, ScaleDuration(0.18f));
            await ToSignal(outro, Tween.SignalName.Finished);
        }
        finally
        {
            _completionSource.TrySetResult();
            QueueFree();
        }
    }

    private Vector2 GetCenteredCardPosition()
    {
        return new Vector2(
            (Size.X - PreviewWidth) * 0.5f,
            (Size.Y - PreviewHeight) * 0.5f);
    }

    private void UpdateShardOrigins()
    {
        var center = GetCenteredCardPosition() + new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.48f);
        for (var i = 0; i < _shards.Length; i++)
            _shards[i].Position = center - (_shards[i].Size * 0.5f);
    }

    private void PlayShatterBurst()
    {
        var origin = GetCenteredCardPosition() + new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.48f);
        for (var i = 0; i < _shards.Length; i++)
        {
            var shard = _shards[i];
            var direction = ShardDirections[i];
            var target = origin + (direction * 235f) - (shard.Size * 0.5f);
            shard.Color = i % 2 == 0
                ? new Color(1f, 0.84f, 0.42f, 0.92f)
                : new Color(0.93f, 0.28f, 0.28f, 0.88f);
            shard.Scale = new Vector2(0.28f, 0.28f);

            var tween = shard.CreateTween().SetParallel();
            tween.TweenProperty(shard, "position", target, ScaleDuration(0.24f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(shard, "rotation_degrees", shard.RotationDegrees + (direction.X * 42f), ScaleDuration(0.24f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(shard, "scale", new Vector2(0.95f, 1.18f), ScaleDuration(0.18f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.TweenProperty(shard, "color:a", 0f, ScaleDuration(0.26f));
        }
    }

    private float ScaleDuration(float duration)
    {
        return duration * AnimationDurationScale;
    }

    private async Task WaitSecondsAsync(float seconds)
    {
        await ToSignal(GetTree().CreateTimer(ScaleDuration(seconds)), SceneTreeTimer.SignalName.Timeout);
    }
}
