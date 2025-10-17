using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Circular "vernier" style control that exposes an angle in the range [0, 360).
/// </summary>
[Virtual]
public class AngleKnobControl : global::Robust.Client.UserInterface.Control
{
    private const float PointerLengthPadding = 10f;
    private const float MinorTickLength = 6f;
    private const float MajorTickLength = 10f;
    private const float MinorTickStep = 15f;
    private const float MajorTickStep = 45f;

    private readonly IUserInterfaceManager _uiManager;
    private float _value;
    private bool _dragging;

    /// <summary>
    /// Angle represented by the knob, in degrees.
    /// </summary>
    public float Value
    {
        get => _value;
        set => SetValueInternal(value, true, false);
    }

    /// <summary>
    /// Amount to snap by while dragging with the mouse. Set to 0 to disable snapping.
    /// </summary>
    public float DragSnapIncrement { get; set; } = 5f;

    /// <summary>
    /// Amount to adjust by when scrolling the mouse wheel over the control.
    /// </summary>
    public float ScrollStep { get; set; } = 1f;

    /// <summary>
    /// Raised when the user changes the knob value interactively.
    /// </summary>
    public event Action<float>? ValueChanged;

    public AngleKnobControl()
    {
        _uiManager = IoCManager.Resolve<IUserInterfaceManager>();
        MinSize = new Vector2(96f, 96f);
        MouseFilter = MouseFilterMode.Stop;
    }

    /// <summary>
    /// Sets the value without raising <see cref="ValueChanged"/>.
    /// </summary>
    public void SetValueWithoutEvent(float value)
    {
        SetValueInternal(value, false, false);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var center = size * 0.5f;
        var radius = MathF.Min(size.X, size.Y) * 0.5f - 2f;
        if (radius <= 0f)
            return;

        var circleColor = Color.FromHex("#1f2a39");
        var ringColor = Color.FromHex("#3a5068");
        var pointerColor = Color.CornflowerBlue;
        var tickColor = Color.LightGray;
        var majorTickColor = Color.White;

        handle.DrawCircle(center, radius, circleColor);
        handle.DrawCircle(center, radius, ringColor, false);

        DrawTicks(handle, center, radius, tickColor, majorTickColor);
        DrawPointer(handle, center, radius, pointerColor);

        DrawLabel(handle, center);
    }

    private void DrawTicks(DrawingHandleScreen handle, Vector2 center, float radius, Color minorColor, Color majorColor)
    {
        for (var tick = 0f; tick < 360f; tick += MinorTickStep)
        {
            var isMajor = MathF.Abs(tick % MajorTickStep) <= 0.001f;
            var length = isMajor ? MajorTickLength : MinorTickLength;
            var color = isMajor ? majorColor : minorColor;

            var radians = MathHelper.DegreesToRadians(tick);
            var direction = new Vector2(MathF.Cos(radians), -MathF.Sin(radians));

            var outer = center + direction * (radius - 2f);
            var inner = center + direction * (radius - length);

            handle.DrawLine(inner, outer, color);
        }
    }

    private void DrawPointer(DrawingHandleScreen handle, Vector2 center, float radius, Color color)
    {
        var radians = MathHelper.DegreesToRadians(_value);
        var direction = new Vector2(MathF.Cos(radians), -MathF.Sin(radians));
        var length = radius - PointerLengthPadding;

        var end = center + direction * length;
        handle.DrawCircle(center, 4f, color);
        handle.DrawLine(center, end, color);
        handle.DrawCircle(end, 5f, color, false);
    }

    private void DrawLabel(DrawingHandleScreen handle, Vector2 center)
    {
        var font = TryGetStyleProperty<Font>("font", out var styleFont)
            ? styleFont
            : _uiManager.ThemeDefaults.DefaultFont;

        var text = $"{_value:000}";
        var dims = handle.GetDimensions(font, text, 1f);
        var position = center - dims * 0.5f;
        handle.DrawString(font, position, text, Color.White);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (UpdateFromPointer(args.RelativePosition, applySnap: true))
        {
            _dragging = true;
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (_dragging)
        {
            _dragging = false;
            args.Handle();
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_dragging)
            return;

        if (UpdateFromPointer(args.RelativePosition, applySnap: true))
            args.Handle();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (MathF.Abs(args.Delta.Y) < 0.001f || ScrollStep <= 0f)
            return;

        var delta = args.Delta.Y > 0f ? ScrollStep : -ScrollStep;

        if (SetValueInternal(_value + delta, true, false))
            args.Handle();
    }

    private bool UpdateFromPointer(Vector2 position, bool applySnap)
    {
        var center = PixelSize * 0.5f;
        var offset = position - center;

        if (offset.LengthSquared() <= 1f)
            return false;

        var angle = MathF.Atan2(-offset.Y, offset.X);
        var degrees = MathHelper.RadiansToDegrees(angle);

        return SetValueInternal(degrees, true, applySnap);
    }

    private bool SetValueInternal(float value, bool raiseEvent, bool applySnap)
    {
        var normalized = NormalizeAngle(value);

        if (applySnap && DragSnapIncrement > 0f)
        {
            normalized = MathF.Round(normalized / DragSnapIncrement) * DragSnapIncrement;
            normalized = NormalizeAngle(normalized);
        }

        if (MathF.Abs(normalized - _value) < 0.01f)
            return false;

        _value = normalized;
        if (raiseEvent)
            ValueChanged?.Invoke(_value);

        return true;
    }

    private static float NormalizeAngle(float degrees)
    {
        var normalized = degrees % 360f;
        if (normalized < 0f)
            normalized += 360f;
        return normalized;
    }

    protected override void Deparented()
    {
        if (HasKeyboardFocus())
            ReleaseKeyboardFocus();
        base.Deparented();
    }
}


