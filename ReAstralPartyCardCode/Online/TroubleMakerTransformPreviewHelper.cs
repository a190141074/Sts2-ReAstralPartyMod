using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public static class TroubleMakerTransformPreviewHelper
{
    public static async Task PlayTroubleMakerTransformAsync(Player owner, CardModel sourceCard,
        CardModel selectedEventCard)
    {
        await PlayTransformPreviewAsync(owner, sourceCard, selectedEventCard, TroubleMakerPreviewMode.Transform);
    }

    public static async Task PlayResultCardPreviewAsync(Player owner, CardModel selectedEventCard)
    {
        await PlayTransformPreviewAsync(owner, selectedEventCard, selectedEventCard,
            TroubleMakerPreviewMode.RevealResult);
    }

    public static async Task PlayTransformPreviewAsync(Player owner, CardModel sourceCard, CardModel selectedEventCard,
        TroubleMakerPreviewMode mode = TroubleMakerPreviewMode.Transform)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(sourceCard);
        ArgumentNullException.ThrowIfNull(selectedEventCard);

        if (TestMode.IsOn)
            return;

        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return;

        var previewContainer = NCombatRoom.Instance?.Ui.PlayContainer
                               ?? NCombatRoom.Instance?.Ui.CardPreviewContainer
                               ?? NRun.Instance?.GlobalUi.CardPreviewContainer;
        if (previewContainer == null)
            return;

        try
        {
            var sourceCanonical = sourceCard.CanonicalInstance ?? sourceCard;
            var selectedCanonical = selectedEventCard.CanonicalInstance ?? selectedEventCard;
            var previewSource = combatState.CreateCard(sourceCanonical, owner);
            var previewTarget = combatState.CreateCard(selectedCanonical, owner);

            var preview = TroubleMakerTransformPreviewVfx.Create(previewSource, previewTarget, mode);
            previewContainer.AddChildSafely(preview);
            await preview.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            Log.Warn(
                $"[{MainFile.ModId}] Failed to play Trouble Maker transform preview. Falling back to normal autoplay.\n{ex}");
        }
    }
}

internal sealed partial class TroubleMakerTransformPreviewVfx : Control
{
    private const float PreviewWidth = 480f;
    private const float PreviewHeight = 700f;
    private const float CardScale = 1.08f;
    private const float AnimationDurationScale = 1.5f;
    private readonly TaskCompletionSource _completionSource = new();
    private readonly CardModel _startCard;
    private readonly CardModel _endCard;
    private readonly TroubleMakerPreviewMode _mode;
    private readonly ColorRect[] _burstStreaks = new ColorRect[8];

    private Control _cardStage = null!;
    private ColorRect _flashOverlay = null!;
    private NCard? _cardNode;

    private TroubleMakerTransformPreviewVfx(CardModel startCard, CardModel endCard, TroubleMakerPreviewMode mode)
    {
        _startCard = startCard;
        _endCard = endCard;
        _mode = mode;

        Name = nameof(TroubleMakerTransformPreviewVfx);
        MouseFilter = MouseFilterEnum.Ignore;
        FocusMode = FocusModeEnum.None;
        ZIndex = 100;
        CustomMinimumSize = new Vector2(PreviewWidth, PreviewHeight);
        PivotOffset = new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.5f);

        BuildUi();
    }

    public static TroubleMakerTransformPreviewVfx Create(CardModel startCard, CardModel endCard,
        TroubleMakerPreviewMode mode)
    {
        return new TroubleMakerTransformPreviewVfx(startCard, endCard, mode);
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
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(PreviewWidth, PreviewHeight)
        };
        _cardStage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_cardStage);

        _flashOverlay = new ColorRect
        {
            Name = "FlashOverlay",
            Color = new Color(1f, 0.95f, 0.72f, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        _flashOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_flashOverlay);

        for (var i = 0; i < _burstStreaks.Length; i++)
        {
            var streak = new ColorRect
            {
                Name = $"BurstStreak{i}",
                Color = new Color(1f, 0.86f, 0.34f, 0f),
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(14f, 128f),
                PivotOffset = new Vector2(7f, 128f)
            };
            streak.RotationDegrees = i * (360f / _burstStreaks.Length);
            streak.Scale = new Vector2(0.15f, 0.15f);
            _cardStage.AddChild(streak);
            _burstStreaks[i] = streak;
        }
    }

    private async Task PlayAnimationAsync()
    {
        try
        {
            var initialCard = _mode == TroubleMakerPreviewMode.RevealResult ? _endCard : _startCard;
            _cardNode = NCard.Create(initialCard);
            _cardStage.AddChildSafely(_cardNode);
            var tree = GetTree();
            if (tree == null)
                return;

            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (_cardNode == null)
                return;

            _cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            _cardNode.Position = PileType.Play.GetTargetPosition(_cardNode);
            _cardNode.Scale = Vector2.One * 0.05f;
            _cardNode.Modulate = new Color(1f, 1f, 1f, 0f);
            UpdateBurstLayout(_cardNode.Position);

            SfxCmd.Play("event:/sfx/ui/cards/card_transform");

            var intro = CreateTween().SetParallel();
            intro.TweenProperty(_cardNode, "scale", Vector2.One * CardScale, ScaleDuration(0.24f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            intro.TweenProperty(_cardNode, "modulate:a", 1f, ScaleDuration(0.18f));
            await ToSignal(intro, Tween.SignalName.Finished);

            await WaitSecondsAsync(0.16f);

            if (_mode == TroubleMakerPreviewMode.RevealResult)
            {
                PlayBurst();

                var reveal = CreateTween().SetParallel();
                reveal.TweenProperty(_cardNode!, "scale", new Vector2(CardScale * 0.96f, CardScale * 1.08f),
                        ScaleDuration(0.12f))
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Back);
                reveal.TweenProperty(_cardNode!, "rotation_degrees", -1.5f, ScaleDuration(0.12f))
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Quad);
                await ToSignal(reveal, Tween.SignalName.Finished);

                var revealSettle = CreateTween().SetParallel();
                revealSettle.TweenProperty(_cardNode!, "scale", Vector2.One * CardScale, ScaleDuration(0.22f))
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Back);
                revealSettle.TweenProperty(_cardNode!, "rotation_degrees", 0f, ScaleDuration(0.22f))
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                await ToSignal(revealSettle, Tween.SignalName.Finished);

                await WaitSecondsAsync(0.34f);

                var revealOutro = CreateTween().SetParallel();
                revealOutro.TweenProperty(_cardNode!, "scale", Vector2.One * (CardScale * 0.9f),
                        ScaleDuration(0.18f))
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Cubic);
                revealOutro.TweenProperty(_cardNode!, "modulate:a", 0f, ScaleDuration(0.18f));
                revealOutro.TweenProperty(this, "modulate:a", 0f, ScaleDuration(0.18f));
                await ToSignal(revealOutro, Tween.SignalName.Finished);
                return;
            }

            var charge = CreateTween().SetParallel();
            charge.TweenProperty(_cardNode!, "scale", new Vector2(CardScale * 1.12f, CardScale * 0.9f),
                    ScaleDuration(0.16f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            charge.TweenProperty(_cardNode!, "rotation_degrees", 2f, ScaleDuration(0.12f))
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            charge.TweenProperty(_flashOverlay, "color:a", 0.18f, ScaleDuration(0.12f));
            await ToSignal(charge, Tween.SignalName.Finished);

            _cardNode!.Model = _endCard;
            _cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            PlayBurst();

            var swap = CreateTween().SetParallel();
            swap.TweenProperty(_cardNode!, "scale", new Vector2(CardScale * 0.96f, CardScale * 1.08f),
                    ScaleDuration(0.12f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            swap.TweenProperty(_cardNode!, "rotation_degrees", -1.5f, ScaleDuration(0.12f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            swap.TweenProperty(_flashOverlay, "color:a", 0.34f, ScaleDuration(0.08f));
            await ToSignal(swap, Tween.SignalName.Finished);

            var settle = CreateTween().SetParallel();
            settle.TweenProperty(_cardNode!, "scale", Vector2.One * CardScale, ScaleDuration(0.22f))
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Back);
            settle.TweenProperty(_cardNode!, "rotation_degrees", 0f, ScaleDuration(0.22f))
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            settle.TweenProperty(_flashOverlay, "color:a", 0f, ScaleDuration(0.18f));
            await ToSignal(settle, Tween.SignalName.Finished);

            await WaitSecondsAsync(0.34f);

            var outro = CreateTween().SetParallel();
            outro.TweenProperty(_cardNode!, "scale", Vector2.One * (CardScale * 0.9f), ScaleDuration(0.18f))
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            outro.TweenProperty(_cardNode!, "modulate:a", 0f, ScaleDuration(0.18f));
            outro.TweenProperty(this, "modulate:a", 0f, ScaleDuration(0.18f));
            await ToSignal(outro, Tween.SignalName.Finished);
        }
        finally
        {
            _completionSource.TrySetResult();
            QueueFree();
        }
    }

    private void PlayBurst()
    {
        foreach (var streak in _burstStreaks)
        {
            streak.Color = new Color(1f, 0.86f, 0.34f, 0.65f);
            streak.Scale = new Vector2(0.15f, 0.15f);
            var tween = streak.CreateTween().SetParallel();
            tween.TweenProperty(streak, "scale", new Vector2(1.25f, 1.05f), ScaleDuration(0.16f))
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(streak, "color:a", 0f, ScaleDuration(0.24f));
        }
    }

    private float ScaleDuration(float duration)
    {
        return duration * AnimationDurationScale;
    }

    private void UpdateBurstLayout(Vector2 cardTopLeft)
    {
        var burstCenter = cardTopLeft + new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.46f);
        foreach (var streak in _burstStreaks) streak.Position = new Vector2(burstCenter.X - 7f, burstCenter.Y - 128f);
    }

    private async Task WaitSecondsAsync(float seconds)
    {
        await ToSignal(GetTree().CreateTimer(ScaleDuration(seconds)), SceneTreeTimer.SignalName.Timeout);
    }
}

public enum TroubleMakerPreviewMode
{
    Transform,
    RevealResult
}
