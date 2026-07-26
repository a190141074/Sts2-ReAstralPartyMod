using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

internal static partial class FinalArenaDistortionComponentCardUi
{
    public const string FontPath = "res://ReAstralPartyMod/fonts/荆南波波黑-Bold.ttf";
    public const string CardBackgroundPath = "res://ReAstralPartyMod/images/distortion/distortion_base.png";
    public const string LogoBasePath = "res://ReAstralPartyMod/images/distortion/distortion_logo_base.png";

    private const string HoverTipRoot = "RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT";
    private static readonly Vector2 CardSize = new(596f, 475f);
    private static readonly Vector2 LogoSize = new(147f, 147f);
    private static readonly Vector2 DifficultyIconSize = new(78f, 30f);
    private static readonly Regex WrappedTitlePattern = WrappedTitleRegex();
    private static Font? _displayFont;
    private static bool _fontLoadAttempted;

    public static GridContainer BuildComponentGrid(
        IReadOnlyList<FinalArenaDistortionComponent> components,
        IReadOnlySet<FinalArenaDistortionComponent>? activeComponents = null)
    {
        var grid = new GridContainer
        {
            Columns = 2,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        grid.AddThemeConstantOverride("h_separation", 18);
        grid.AddThemeConstantOverride("v_separation", 18);

        if (activeComponents is { Count: > 0 })
        {
            foreach (var component in components.Where(activeComponents.Contains))
                grid.AddChild(BuildComponentCard(component));

            var inactiveComponents = components.Where(component => !activeComponents.Contains(component)).ToList();
            if (grid.GetChildCount() > 0 && inactiveComponents.Count > 0)
            {
                grid.AddChild(BuildSpacerCell());
                grid.AddChild(BuildSpacerCell());
            }

            foreach (var component in inactiveComponents)
                grid.AddChild(BuildComponentCard(component));

            return grid;
        }

        foreach (var component in components)
            grid.AddChild(BuildComponentCard(component));

        return grid;
    }

    public static Control BuildComponentCard(FinalArenaDistortionComponent component)
    {
        var model = BuildDisplayModel(component);
        var card = new Control
        {
            CustomMinimumSize = CardSize,
            Size = CardSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };

        var background = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(CardBackgroundPath),
            Size = CardSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddChild(background);

        var title = BuildHeaderLabel(model.Name, 42, new Color(0.02f, 0.25f, 0.34f));
        title.Position = new Vector2(48f, 20f);
        title.Size = new Vector2(388f, 70f);
        title.ClipText = true;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.AddThemeColorOverride("font_outline_color", Colors.White);
        title.AddThemeConstantOverride("outline_size", 5);
        card.AddChild(title);

        var difficulty = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(GetDifficultyIconPath(model.Difficulty)),
            Position = new Vector2(482f, 1f),
            Size = DifficultyIconSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddChild(difficulty);

        var description = BuildDescription(model.Description);
        description.Position = new Vector2(24f, 145f);
        description.Size = new Vector2(430f, 190f);
        description.CustomMinimumSize = new Vector2(430f, 190f);
        card.AddChild(description);

        var logoPosition = new Vector2(424f, 303f);
        var logoBase = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(LogoBasePath),
            Position = logoPosition,
            Size = LogoSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddChild(logoBase);

        if (model.IconId.HasValue)
        {
            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(GetIconPath(model.IconId.Value)),
                Position = logoBase.Position,
                Size = logoBase.Size,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            card.AddChild(icon);
        }

        return card;
    }

    private static Control BuildSpacerCell()
    {
        return new Control
        {
            CustomMinimumSize = new Vector2(CardSize.X, 24f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
    }

    public static Label BuildHeaderLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        ApplyFont(label, fontSize, color);
        return label;
    }

    private static MegaRichTextLabel BuildDescription(string description)
    {
        var label = new MegaRichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MaxFontSize = 28,
            MinFontSize = 28
        };
        ApplyMegaTextFont(label, 28, Colors.White);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0.18f, 0.22f, 0.82f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.SetTextAutoSize(ForceDisplayFont(description));
        return label;
    }

    private static ComponentDisplayModel BuildDisplayModel(FinalArenaDistortionComponent component)
    {
        var title = new LocString("relics", $"{HoverTipRoot}.{FinalArenaSeriesHelper.GetLocStem(component)}.title");
        title.Add("Difficulty", FinalArenaSeriesHelper.GetDifficulty(component));
        var fullTitle = title.GetFormattedText();
        var match = WrappedTitlePattern.Match(fullTitle);
        var difficulty = FinalArenaSeriesHelper.GetDifficulty(component);
        return new ComponentDisplayModel(
            match.Success ? match.Groups["name"].Value : fullTitle,
            difficulty,
            new LocString("relics",
                $"{HoverTipRoot}.{FinalArenaSeriesHelper.GetLocStem(component)}.description").GetFormattedText(),
            GetIconId(component));
    }

    private static string GetDifficultyIconPath(int difficulty)
    {
        var fileName = difficulty switch
        {
            0 => "diff_0",
            -1 => "diff_negative_1",
            -2 => "diff_negative_2",
            1 => "diff_positive_1",
            2 => "diff_positive_2",
            _ => "diff_0"
        };
        return $"res://ReAstralPartyMod/images/distortion/{fileName}.png";
    }

    internal static Texture2D? LoadComponentIcon(FinalArenaDistortionComponent component)
    {
        var iconId = GetIconId(component);
        return iconId.HasValue ? ResourceLoader.Load<Texture2D>(GetIconPath(iconId.Value)) : null;
    }

    private static int? GetIconId(FinalArenaDistortionComponent component)
    {
        return component switch
        {
            FinalArenaDistortionComponent.HumanAiDuel => 60112,
            FinalArenaDistortionComponent.RefereeAttentionOne => 60151,
            FinalArenaDistortionComponent.RefereeAttentionTwo => 60152,
            FinalArenaDistortionComponent.PirateKingsOrder => 60015,
            FinalArenaDistortionComponent.Thundercloud => 60013,
            FinalArenaDistortionComponent.Rainstorm => 60007,
            FinalArenaDistortionComponent.Seasickness => 60014,
            FinalArenaDistortionComponent.EnergyDisorder => 60004,
            FinalArenaDistortionComponent.CopyCard => 60155,
            FinalArenaDistortionComponent.WideAreaMark => 60159,
            FinalArenaDistortionComponent.Robbery => 60012,
            FinalArenaDistortionComponent.Gale => 60006,
            FinalArenaDistortionComponent.Dexterity => 60010,
            FinalArenaDistortionComponent.Overload => 60011,
            FinalArenaDistortionComponent.Arbitrage => 60156,
            FinalArenaDistortionComponent.PreciseSelection => 60153,
            FinalArenaDistortionComponent.FlashOfInsight => 60009,
            FinalArenaDistortionComponent.MysteriousTreasure => 60008,
            FinalArenaDistortionComponent.UnexpectedWealth => 60154,
            FinalArenaDistortionComponent.Baton => 60157,
            _ => null
        };
    }

    private static string GetIconPath(int iconId)
    {
        return $"res://ReAstralPartyMod/images/distortion/UT_Mutator_{iconId}.png";
    }

    private static string ForceDisplayFont(string text)
    {
        return $"[font={FontPath}]{text}[/font]";
    }

    private static void ApplyFont(Label label, int fontSize, Color color)
    {
        var font = LoadDisplayFont();
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        label.AddThemeConstantOverride("shadow_offset_y", 0);
    }

    private static void ApplyMegaTextFont(MegaRichTextLabel label, int fontSize, Color color)
    {
        var font = LoadDisplayFont() ?? label.GetThemeDefaultFont();
        if (font != null)
        {
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontOverride("normal_font", font);
            label.AddThemeFontOverride("bold_font", font);
            label.AddThemeFontOverride("italics_font", font);
            label.AddThemeFontOverride("bold_italics_font", font);
            label.AddThemeFontOverride("mono_font", font);
        }

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_font_size", fontSize);
        label.AddThemeFontSizeOverride("italics_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
        label.AddThemeFontSizeOverride("mono_font_size", fontSize);
        label.AddThemeColorOverride("default_color", color);
    }

    private static Font? LoadDisplayFont()
    {
        if (_fontLoadAttempted)
            return _displayFont;

        _fontLoadAttempted = true;
        _displayFont = ResourceLoader.Load<Font>(FontPath);
        if (_displayFont == null)
            MainFile.Logger.Warn($"[FinalArena] Failed to load distortion component font: {FontPath}");
        return _displayFont;
    }

    [GeneratedRegex("^【(?<name>.+?)（(?<difficulty>[+-]?\\d+)）】$")]
    private static partial Regex WrappedTitleRegex();

    private sealed record ComponentDisplayModel(
        string Name,
        int Difficulty,
        string Description,
        int? IconId);
}
