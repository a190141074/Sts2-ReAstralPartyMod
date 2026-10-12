using System.IO;
using System.Threading.Tasks;
using Godot;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

namespace ReAstralPartyMod.Preview;

public partial class FinalArenaDistortionComponentLayoutPreview : Control
{
    public override async void _Ready()
    {
        Name = nameof(FinalArenaDistortionComponentLayoutPreview);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        DisplayServer.WindowSetTitle("畸变组件图鉴卡片布局预览");
        DisplayServer.WindowSetSize(new Vector2I(1440, 1240));
        BuildUi();
        await CaptureIfRequestedAsync();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsEcho() && @event.IsActionPressed("ui_cancel"))
        {
            GetTree().Quit();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        base._UnhandledInput(@event);
    }

    private void BuildUi()
    {
        var background = new ColorRect
        {
            Color = new Color(0.04f, 0.07f, 0.09f, 1f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 44);
        margin.AddThemeConstantOverride("margin_top", 30);
        margin.AddThemeConstantOverride("margin_right", 44);
        margin.AddThemeConstantOverride("margin_bottom", 30);
        AddChild(margin);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 18);
        margin.AddChild(root);

        root.AddChild(FinalArenaDistortionComponentCardUi.BuildHeaderLabel("畸变组件图鉴卡片布局预览", 36, Colors.White));
        root.AddChild(FinalArenaDistortionComponentCardUi.BuildHeaderLabel(
            "直接运行这个场景即可检查两列卡片、字体、背景和 UT_Mutator 图标；Esc 可关闭。",
            18,
            new Color(0.76f, 0.86f, 0.9f, 1f)));

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        scroll.AddChild(FinalArenaDistortionComponentCardUi.BuildComponentGrid(
            FinalArenaSeriesHelper.GetAllComponentsInDisplayOrder()));
    }

    private async Task CaptureIfRequestedAsync()
    {
        var capturePath = GetCapturePath();
        if (string.IsNullOrWhiteSpace(capturePath))
            return;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var image = GetViewport().GetTexture().GetImage();
        var directory = Path.GetDirectoryName(capturePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        image.SavePng(capturePath);
        GetTree().Quit();
    }

    private static string? GetCapturePath()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            const string prefix = "capture=";
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return argument[prefix.Length..].Trim('"');
        }

        return null;
    }
}
