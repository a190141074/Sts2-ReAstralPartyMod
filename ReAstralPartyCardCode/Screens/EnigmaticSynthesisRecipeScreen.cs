using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Screens;

public sealed partial class EnigmaticSynthesisRecipeScreen : Control, ICapstoneScreen, IScreenContext
{
    private const string SettingsLocTable = "settings_ui";
    private const string LocPrefix = "RE_ASTRAL_PARTY_MOD_SETTINGS.enigmatic_synthesis_formula";
    private static readonly Color BackgroundColor = new(0.04f, 0.05f, 0.06f, 0.94f);
    private static readonly Color PanelColor = new(0.16f, 0.17f, 0.19f, 0.98f);
    private static readonly Color PanelBorderColor = new(0.36f, 0.38f, 0.42f, 1f);
    private static readonly Color SlotColor = new(0.24f, 0.25f, 0.28f, 1f);
    private static readonly Color EmptySlotColor = new(0.17f, 0.18f, 0.2f, 0.85f);
    private static readonly Color SufficientSlotAccent = new(0.66f, 0.76f, 0.84f, 0.9f);
    private static readonly Color InsufficientSlotAccent = new(0.82f, 0.32f, 0.28f, 0.95f);
    private static readonly Color CraftableColor = new(0.9f, 0.82f, 0.36f, 1f);
    private static readonly Color MissingColor = new(0.83f, 0.55f, 0.5f, 1f);
    private static EnigmaticSynthesisRecipeScreen? _instance;

    private Player? _player;
    private GridContainer _recipeList = null!;
    private Button _closeButton = null!;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _closeButton;

    private EnigmaticSynthesisRecipeScreen()
    {
        Name = nameof(EnigmaticSynthesisRecipeScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        Visible = false;
        BuildUi();
    }

    public static EnigmaticSynthesisRecipeScreen GetOrCreate(Player player)
    {
        if (_instance == null || !GodotObject.IsInstanceValid(_instance))
            _instance = new EnigmaticSynthesisRecipeScreen();

        _instance.Bind(player);
        return _instance;
    }

    private void Bind(Player player)
    {
        _player = player;
        RefreshRecipes();
    }

    private void BuildUi()
    {
        var backdrop = new ColorRect
        {
            Color = BackgroundColor,
            MouseFilter = MouseFilterEnum.Ignore
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var frame = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(1380f, 860f)
        };
        frame.AddThemeStyleboxOverride("panel", CreateFrameStyle());
        center.AddChild(frame);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 18);
        frame.AddChild(root);

        var header = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        header.AddThemeConstantOverride("separation", 16);
        root.AddChild(header);

        var titleColumn = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleColumn.AddThemeConstantOverride("separation", 4);
        header.AddChild(titleColumn);

        titleColumn.AddChild(CreateHeaderLabel($"{LocPrefix}.title", 34, new Color(0.95f, 0.95f, 0.93f, 1f)));
        titleColumn.AddChild(CreateHeaderLabel($"{LocPrefix}.subtitle", 18, new Color(0.78f, 0.8f, 0.83f, 1f)));

        _closeButton = new Button
        {
            Text = Loc($"{LocPrefix}.close"),
            CustomMinimumSize = new Vector2(120f, 52f),
            FocusMode = FocusModeEnum.All
        };
        _closeButton.Pressed += static () => STS2RitsuLib.Screens.ModScreenService.Close();
        _closeButton.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.25f, 0.26f, 0.29f, 1f)));
        _closeButton.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.32f, 0.33f, 0.37f, 1f)));
        _closeButton.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.21f, 0.22f, 0.25f, 1f)));
        _closeButton.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.32f, 0.33f, 0.37f, 1f)));
        header.AddChild(_closeButton);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0f, 700f)
        };
        root.AddChild(scroll);

        _recipeList = new GridContainer
        {
            Columns = 2,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _recipeList.AddThemeConstantOverride("h_separation", 18);
        _recipeList.AddThemeConstantOverride("v_separation", 18);
        scroll.AddChild(_recipeList);
    }

    private void RefreshRecipes()
    {
        if (_recipeList == null)
            return;

        foreach (var child in _recipeList.GetChildren())
            child.QueueFree();

        foreach (var recipe in EnigmaticSynthesisRestSiteHelper.AllRecipes)
            _recipeList.AddChild(CreateRecipeCard(recipe));
    }

    private Control CreateRecipeCard(EnigmaticSynthesisRecipeView recipe)
    {
        var craftable = EnigmaticSynthesisRestSiteHelper.CanCraft(_player, recipe);
        var card = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(620f, 360f)
        };
        card.AddThemeStyleboxOverride("panel", CreateRecipeCardStyle());

        var cardRoot = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        cardRoot.AddThemeConstantOverride("separation", 16);
        card.AddChild(cardRoot);

        var topRow = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        topRow.AddThemeConstantOverride("separation", 12);
        cardRoot.AddChild(topRow);

        var title = new Label
        {
            Text = recipe.Result.DisplayRelic.Title.GetFormattedText(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", Colors.White);
        topRow.AddChild(title);

        var badge = new Label
        {
            Text = Loc(craftable ? $"{LocPrefix}.craftable" : $"{LocPrefix}.missing"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        badge.AddThemeFontSizeOverride("font_size", 18);
        badge.AddThemeColorOverride("font_color", craftable ? CraftableColor : MissingColor);
        topRow.AddChild(badge);

        var layout = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        layout.AddThemeConstantOverride("separation", 28);
        cardRoot.AddChild(layout);

        layout.AddChild(CreateIngredientGrid(recipe));

        var arrow = new Label
        {
            Text = "=>",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(72f, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        arrow.AddThemeFontSizeOverride("font_size", 30);
        arrow.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.78f, 1f));
        layout.AddChild(arrow);

        layout.AddChild(CreateResultPanel(recipe.Result));
        return card;
    }

    private Control CreateIngredientGrid(EnigmaticSynthesisRecipeView recipe)
    {
        var wrapper = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        wrapper.AddThemeConstantOverride("separation", 10);

        var title = new Label
        {
            Text = Loc($"{LocPrefix}.materials"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.82f, 0.83f, 0.86f, 1f));
        wrapper.AddChild(title);

        var gridPanel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        gridPanel.AddThemeStyleboxOverride("panel", CreateGridFrameStyle());
        wrapper.AddChild(gridPanel);

        var grid = new GridContainer
        {
            Columns = 3,
            MouseFilter = MouseFilterEnum.Ignore
        };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 10);
        gridPanel.AddChild(grid);

        var slotMap = BuildSlotMap(recipe);
        for (var i = 0; i < 9; i++)
        {
            if (i < slotMap.Count)
                grid.AddChild(CreateIngredientSlot(slotMap[i].Kind, slotMap[i].RequiredIndex));
            else
                grid.AddChild(CreateEmptySlot());
        }

        return wrapper;
    }

    private List<ExpandedIngredientSlot> BuildSlotMap(EnigmaticSynthesisRecipeView recipe)
    {
        var slots = new List<ExpandedIngredientSlot>();
        foreach (var cost in recipe.Costs)
        {
            for (var i = 1; i <= cost.Amount; i++)
                slots.Add(new ExpandedIngredientSlot(cost.Kind, i));
        }

        return slots;
    }

    private Control CreateIngredientSlot(EnigmaticUniqueMaterialKind kind, int requiredIndex)
    {
        var config = EnigmaticRewardRegistry.GetConfig(kind);
        var relic = config.Relic;
        var ownedAmount = EnigmaticSynthesisRestSiteHelper.GetOwnedMaterialStacks(_player, kind);
        var sufficient = ownedAmount >= requiredIndex;

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(96f, 96f),
            MouseFilter = MouseFilterEnum.Stop
        };
        panel.AddThemeStyleboxOverride("panel", CreateSlotStyle(SlotColor, sufficient ? SufficientSlotAccent : InsufficientSlotAccent));

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.AddThemeConstantOverride("separation", 4);
        panel.AddChild(root);

        var icon = new TextureRect
        {
            Texture = relic.BigIcon ?? relic.Icon,
            CustomMinimumSize = new Vector2(48f, 48f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddChild(icon);

        var amount = new Label
        {
            Text = $"{requiredIndex}",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        amount.AddThemeFontSizeOverride("font_size", 16);
        amount.AddThemeColorOverride("font_color", sufficient ? Colors.White : new Color(1f, 0.8f, 0.78f, 1f));
        root.AddChild(amount);

        var name = new Label
        {
            Text = relic.Title.GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        name.AddThemeFontSizeOverride("font_size", 13);
        name.Visible = false;
        name.AddThemeColorOverride("font_color", new Color(0.86f, 0.88f, 0.9f, 1f));
        root.AddChild(name);

        panel.MouseEntered += () => ShowRelicHover(panel, relic);
        panel.MouseExited += () => NHoverTipSet.Remove(panel);
        return panel;
    }

    private readonly record struct ExpandedIngredientSlot(
        EnigmaticUniqueMaterialKind Kind,
        int RequiredIndex);

    private static Control CreateEmptySlot()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(96f, 96f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", CreateSlotStyle(EmptySlotColor, new Color(0.24f, 0.25f, 0.27f, 0.8f)));
        return panel;
    }

    private Control CreateResultPanel(EnigmaticSynthesisRecipeResult result)
    {
        var relic = result.DisplayRelic;
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220f, 220f),
            MouseFilter = MouseFilterEnum.Stop
        };
        panel.AddThemeStyleboxOverride("panel", CreateResultStyle());

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var title = new Label
        {
            Text = Loc($"{LocPrefix}.result"),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.82f, 0.83f, 0.86f, 1f));
        root.AddChild(title);

        var icon = new TextureRect
        {
            Texture = relic.BigIcon ?? relic.Icon,
            CustomMinimumSize = new Vector2(84f, 84f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddChild(icon);

        var name = new Label
        {
            Text = relic.Title.GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        name.AddThemeFontSizeOverride("font_size", 18);
        name.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(name);

        if (result.ResultAmount > 1)
        {
            var amount = new Label
            {
                Text = $"x{result.ResultAmount}",
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            amount.AddThemeFontSizeOverride("font_size", 18);
            amount.AddThemeColorOverride("font_color", new Color(0.92f, 0.82f, 0.46f, 1f));
            root.AddChild(amount);
        }

        panel.MouseEntered += () => ShowRelicHover(panel, relic);
        panel.MouseExited += () => NHoverTipSet.Remove(panel);
        return panel;
    }

    private static void ShowRelicHover(Control owner, RelicModel relic)
    {
        NHoverTipSet.CreateAndShow(owner, relic.HoverTips, HoverTip.GetHoverTipAlignment(owner));
    }

    private static StyleBoxFlat CreateFrameStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = PanelColor,
            BorderColor = PanelBorderColor,
            ContentMarginLeft = 28f,
            ContentMarginTop = 24f,
            ContentMarginRight = 28f,
            ContentMarginBottom = 24f
        };
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(8);
        return style;
    }

    private static StyleBoxFlat CreateRecipeCardStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.21f, 0.22f, 0.25f, 1f),
            BorderColor = new Color(0.41f, 0.43f, 0.47f, 1f),
            ContentMarginLeft = 18f,
            ContentMarginTop = 18f,
            ContentMarginRight = 18f,
            ContentMarginBottom = 18f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        return style;
    }

    private static StyleBoxFlat CreateGridFrameStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.14f, 0.9f),
            BorderColor = new Color(0.33f, 0.34f, 0.37f, 1f),
            ContentMarginLeft = 12f,
            ContentMarginTop = 12f,
            ContentMarginRight = 12f,
            ContentMarginBottom = 12f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        return style;
    }

    private static StyleBoxFlat CreateResultStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.16f, 0.18f, 1f),
            BorderColor = new Color(0.56f, 0.52f, 0.36f, 1f),
            ContentMarginLeft = 16f,
            ContentMarginTop = 14f,
            ContentMarginRight = 16f,
            ContentMarginBottom = 14f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        return style;
    }

    private static StyleBoxFlat CreateSlotStyle(Color backgroundColor, Color borderColor)
    {
        var style = new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = borderColor,
            ContentMarginLeft = 8f,
            ContentMarginTop = 8f,
            ContentMarginRight = 8f,
            ContentMarginBottom = 8f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        return style;
    }

    private static StyleBoxFlat CreateButtonStyle(Color backgroundColor)
    {
        var style = new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = new Color(0.54f, 0.55f, 0.58f, 1f),
            ContentMarginLeft = 14f,
            ContentMarginTop = 8f,
            ContentMarginRight = 14f,
            ContentMarginBottom = 8f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        return style;
    }

    private static Label CreateHeaderLabel(string locKey, int fontSize, Color fontColor)
    {
        var label = new Label
        {
            Text = Loc(locKey),
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", fontColor);
        return label;
    }

    private static string Loc(string key)
    {
        return new LocString(SettingsLocTable, key).GetRawText();
    }

    public void AfterCapstoneOpened()
    {
        Visible = true;
        RefreshRecipes();
        _closeButton.GrabFocus();
    }

    public void AfterCapstoneClosed()
    {
        Visible = false;
        if (ReferenceEquals(_instance, this))
            _instance = null;
        QueueFree();
    }
}
