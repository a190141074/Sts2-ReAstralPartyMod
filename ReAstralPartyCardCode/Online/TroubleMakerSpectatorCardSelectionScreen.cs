using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed partial class TroubleMakerSpectatorCardSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private const float CardSpacing = 340f;

    private static string CommonBannerScenePath => SceneHelper.GetScenePath("ui/common_banner");

    private static readonly LocString Prompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_TROUBLE_MAKER.spectator_prompt");

    private readonly Player _owner;
    private readonly IReadOnlyList<CardModel> _options;
    private readonly NCommonBanner _banner;
    private readonly Control _cardRow;
    private bool _closed;
    private bool _cardsBuilt;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => null;

    private TroubleMakerSpectatorCardSelectionScreen(Player owner, IReadOnlyList<CardModel> options)
    {
        _owner = owner;
        _options = options;

        Name = nameof(TroubleMakerSpectatorCardSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;

        _banner = PreloadManager.Cache.GetScene(CommonBannerScenePath)
            .Instantiate<NCommonBanner>(PackedScene.GenEditState.Disabled);
        AddChild(_banner);

        _cardRow = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
            FocusMode = FocusModeEnum.None
        };
        _cardRow.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        _cardRow.OffsetTop = 60f;
        _cardRow.OffsetBottom = 60f;
        AddChild(_cardRow);
    }

    public static TroubleMakerSpectatorCardSelectionScreen Create(Player owner, IReadOnlyList<CardModel> options)
    {
        return new TroubleMakerSpectatorCardSelectionScreen(owner, options);
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
        _banner.label.SetTextAutoSize(Prompt.GetFormattedText());
        _banner.AnimateIn();
        BuildCardsIfNeeded();
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

    private void BuildCardsIfNeeded()
    {
        if (_cardsBuilt)
            return;

        _cardsBuilt = true;
        var startPosition = Vector2.Left * ((_options.Count - 1) * CardSpacing * 0.5f);
        var cardTween = CreateTween().SetParallel();
        for (var index = 0; index < _options.Count; index++)
        {
            var displayCard = (_options[index].CanonicalInstance ?? _options[index]).ToMutable();
            displayCard.Owner = _owner;

            var cardNode = NCard.Create(displayCard);
            if (cardNode == null)
                continue;

            cardNode.MouseFilter = MouseFilterEnum.Ignore;
            var holder = NGridCardHolder.Create(cardNode);
            if (holder == null)
                continue;

            holder.MouseFilter = MouseFilterEnum.Ignore;
            holder.FocusMode = FocusModeEnum.None;
            holder.Modulate = Colors.Black;
            _cardRow.AddChild(holder);
            cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            cardNode.ActivateRewardScreenGlow();

            cardTween.TweenProperty(holder, "position", startPosition + Vector2.Right * CardSpacing * index, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            cardTween.TweenProperty(holder, "modulate", Colors.White, 1f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }
}
