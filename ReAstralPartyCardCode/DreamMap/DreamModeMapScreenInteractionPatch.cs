using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamMap;

internal sealed class DreamModeMapScreenInteractionState
{
    public float Zoom { get; set; } = 1f;
}

internal static class DreamModeMapScreenInteractionHelper
{
    private const float MinZoom = 0.65f;
    private const float MaxZoom = 1.75f;
    private const float OverscrollX = 220f;
    private const float OverscrollY = 140f;

    private static readonly ConditionalWeakTable<NMapScreen, DreamModeMapScreenInteractionState> States = new();
    private static readonly FieldInfo? RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo? MapContainerField = AccessTools.Field(typeof(NMapScreen), "_mapContainer");
    private static readonly FieldInfo? TargetDragPosField = AccessTools.Field(typeof(NMapScreen), "_targetDragPos");
    private static readonly FieldInfo? IsDraggingField = AccessTools.Field(typeof(NMapScreen), "_isDragging");
    private static readonly FieldInfo? DrawingInputField = AccessTools.Field(typeof(NMapScreen), "_drawingInput");
    private static readonly FieldInfo? MapPointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly MethodInfo? CanScrollMethod = AccessTools.Method(typeof(NMapScreen), "CanScroll");

    public static bool TryGetDreamModeMapScreen(NMapScreen screen, out RunState runState, out Control mapContainer)
    {
        runState = null!;
        mapContainer = null!;

        if (RunStateField?.GetValue(screen) is not RunState candidateRunState)
            return false;
        if (MapContainerField?.GetValue(screen) is not Control candidateMapContainer)
            return false;
        if (candidateRunState.Map == null || !LucidDreamMaliceRuntimeHelper.IsDreamModeEnabled(candidateRunState))
            return false;

        runState = candidateRunState;
        mapContainer = candidateMapContainer;
        return true;
    }

    public static void ApplyZoom(NMapScreen screen)
    {
        if (!TryGetDreamModeMapScreen(screen, out _, out var mapContainer))
            return;

        var state = States.GetOrCreateValue(screen);
        mapContainer.Scale = new Vector2(state.Zoom, state.Zoom);
    }

    public static void HandleMouseDragPostfix(NMapScreen screen, InputEvent inputEvent)
    {
        if (!TryGetDreamModeMapScreen(screen, out _, out _))
            return;
        if (DrawingInputField?.GetValue(screen) != null)
            return;
        if (IsDraggingField?.GetValue(screen) is not true)
            return;
        if (inputEvent is not InputEventMouseMotion motion)
            return;
        if (TargetDragPosField?.GetValue(screen) is not Vector2 target)
            return;

        target.X += motion.Relative.X;
        TargetDragPosField?.SetValue(screen, target);
    }

    public static bool TryHandleZoomScrollPrefix(NMapScreen screen, InputEvent inputEvent)
    {
        if (!TryGetDreamModeMapScreen(screen, out _, out var mapContainer))
            return true;
        if (inputEvent is not InputEventMouseButton { Pressed: true, CtrlPressed: true } buttonEvent)
            return true;
        if (buttonEvent.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown))
            return true;
        if (CanScrollMethod?.Invoke(screen, []) is bool canScroll && !canScroll)
            return false;

        var state = States.GetOrCreateValue(screen);
        var previousZoom = state.Zoom;
        var zoomFactor = buttonEvent.ButtonIndex == MouseButton.WheelUp ? 1.1f : 1f / 1.1f;
        var nextZoom = Mathf.Clamp(previousZoom * zoomFactor, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(previousZoom, nextZoom))
            return false;

        var localMousePosition = screen.GetLocalMousePosition();
        var relativeMouseToMap = (localMousePosition - mapContainer.Position) / previousZoom;
        var nextPosition = localMousePosition - (relativeMouseToMap * nextZoom);

        state.Zoom = nextZoom;
        mapContainer.Scale = new Vector2(nextZoom, nextZoom);
        mapContainer.Position = nextPosition;
        TargetDragPosField?.SetValue(screen, nextPosition);
        return false;
    }

    public static void ClampScrollPostfix(NMapScreen screen, double delta)
    {
        if (!TryGetDreamModeMapScreen(screen, out _, out var mapContainer))
            return;
        if (IsDraggingField?.GetValue(screen) is true)
            return;
        if (TargetDragPosField?.GetValue(screen) is not Vector2 target)
            return;

        var clamped = ClampTargetPosition(screen, mapContainer, target);
        if (clamped == target)
            return;

        TargetDragPosField?.SetValue(screen, target.Lerp(clamped, (float)delta * 12f));
    }

    public static void CenterOnOpenPostfix(NMapScreen screen)
    {
        if (!TryGetDreamModeMapScreen(screen, out var runState, out var mapContainer))
            return;
        if (!runState.CurrentMapCoord.HasValue)
            return;
        if (MapPointDictionaryField?.GetValue(screen) is not IDictionary mapPointDictionary)
            return;
        if (mapPointDictionary[runState.CurrentMapCoord.Value] is not NMapPoint currentPoint)
            return;

        var viewportSize = screen.Size;
        var zoom = States.GetOrCreateValue(screen).Zoom;
        mapContainer.Scale = new Vector2(zoom, zoom);
        var target = new Vector2(
            viewportSize.X * 0.5f - currentPoint.Position.X * zoom,
            viewportSize.Y * 0.5f - currentPoint.Position.Y * zoom);
        target = ClampTargetPosition(screen, mapContainer, target);
        mapContainer.Position = target;
        TargetDragPosField?.SetValue(screen, target);
    }

    private static Vector2 ClampTargetPosition(NMapScreen screen, Control mapContainer, Vector2 requestedPosition)
    {
        var viewportSize = screen.Size;
        var contentRect = GetContentRect(mapContainer);
        var zoom = mapContainer.Scale.X;

        var scaledLeft = requestedPosition.X + contentRect.Position.X * zoom;
        var scaledTop = requestedPosition.Y + contentRect.Position.Y * zoom;
        var scaledWidth = contentRect.Size.X * zoom;
        var scaledHeight = contentRect.Size.Y * zoom;

        var result = requestedPosition;
        if (scaledWidth <= viewportSize.X)
        {
            result.X = ((viewportSize.X - scaledWidth) * 0.5f) - contentRect.Position.X * zoom;
        }
        else
        {
            var minX = viewportSize.X - (scaledLeft + scaledWidth) - OverscrollX;
            var maxX = -scaledLeft + OverscrollX;
            result.X = Mathf.Clamp(requestedPosition.X, requestedPosition.X + minX, requestedPosition.X + maxX);
        }

        if (scaledHeight <= viewportSize.Y)
        {
            result.Y = ((viewportSize.Y - scaledHeight) * 0.5f) - contentRect.Position.Y * zoom;
        }
        else
        {
            var minY = viewportSize.Y - (scaledTop + scaledHeight) - OverscrollY;
            var maxY = -scaledTop + OverscrollY;
            result.Y = Mathf.Clamp(requestedPosition.Y, requestedPosition.Y + minY, requestedPosition.Y + maxY);
        }

        return result;
    }

    private static Rect2 GetContentRect(Control mapContainer)
    {
        Rect2? union = null;
        foreach (var child in mapContainer.GetChildren())
        {
            if (child is not Control control)
                continue;

            var size = control.Size;
            if (size == Vector2.Zero && control.CustomMinimumSize != Vector2.Zero)
                size = control.CustomMinimumSize;
            if (size == Vector2.Zero)
                size = new Vector2(
                    Mathf.Max(0f, control.OffsetRight - control.OffsetLeft),
                    Mathf.Max(0f, control.OffsetBottom - control.OffsetTop));

            var rect = new Rect2(control.Position, size);
            union = union.HasValue ? union.Value.Merge(rect) : rect;
        }

        return union ?? new Rect2(Vector2.Zero, mapContainer.Size == Vector2.Zero ? mapContainer.CustomMinimumSize : mapContainer.Size);
    }
}

public sealed class DreamModeMapScreenProcessMousePatch : IPatchMethod
{
    public static string PatchId => "dream_mode_map_screen_process_mouse";
    public static string Description => "Add horizontal dragging for dream-mode map screen";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "ProcessMouseEvent", [typeof(InputEvent)])];
    }

    public static void Postfix(NMapScreen __instance, InputEvent inputEvent)
    {
        DreamModeMapScreenInteractionHelper.HandleMouseDragPostfix(__instance, inputEvent);
    }
}

public sealed class DreamModeMapScreenProcessScrollPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_map_screen_process_scroll";
    public static string Description => "Enable Ctrl+wheel zoom for dream-mode map screen";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "ProcessScrollEvent", [typeof(InputEvent)])];
    }

    public static bool Prefix(NMapScreen __instance, InputEvent inputEvent)
    {
        return DreamModeMapScreenInteractionHelper.TryHandleZoomScrollPrefix(__instance, inputEvent);
    }
}

public sealed class DreamModeMapScreenUpdateScrollPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_map_screen_update_scroll";
    public static string Description => "Clamp dream-mode map scroll after original smoothing";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "UpdateScrollPosition", [typeof(double)])];
    }

    public static void Postfix(NMapScreen __instance, double delta)
    {
        DreamModeMapScreenInteractionHelper.ClampScrollPostfix(__instance, delta);
    }
}

public sealed class DreamModeMapScreenSetMapPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_map_screen_set_map";
    public static string Description => "Reapply dream-mode map zoom after map refresh";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "SetMap", [typeof(ActMap), typeof(uint), typeof(bool)])];
    }

    public static void Postfix(NMapScreen __instance)
    {
        DreamModeMapScreenInteractionHelper.ApplyZoom(__instance);
    }
}

public sealed class DreamModeMapScreenOpenPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_map_screen_open";
    public static string Description => "Center dream-mode map on current node when opening";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "Open", [typeof(bool)])];
    }

    public static void Postfix(NMapScreen __instance)
    {
        DreamModeMapScreenInteractionHelper.CenterOnOpenPostfix(__instance);
    }
}
