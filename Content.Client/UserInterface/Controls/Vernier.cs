using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    /// A vernier knob control that allows for precise value selection by dragging the mouse.
    /// </summary>
    public sealed class Vernier : Control
    {
        private float _value;
        private float _minValue;
        private float _maxValue = 100;
        private bool _isDragging;
        private float _dragValueStart;
        private float _dragMouseStartY;

        private const float AngleRangeDegrees = 270f;
        private const float StartAngleDegrees = -AngleRangeDegrees / 2f - 90f;
        private const float DragSensitivity = 0.5f;

        public event Action<float>? OnValueChanged;

        public float Value
        {
            get => _value;
            set
            {
                var clamped = Math.Clamp(value, MinValue, MaxValue);
                if (MathHelper.CloseTo(_value, clamped))
                    return;

                _value = clamped;
                OnValueChanged?.Invoke(_value);
                InvalidateMeasure();
            }
        }

        public float MinValue
        {
            get => _minValue;
            set
            {
                if (MathHelper.CloseTo(_minValue, value))
                    return;

                _minValue = value;
                Value = Math.Max(Value, _minValue);
                InvalidateMeasure();
            }
        }

        public float MaxValue
        {
            get => _maxValue;
            set
            {
                if (MathHelper.CloseTo(_maxValue, value))
                    return;

                _maxValue = value;
                Value = Math.Min(Value, _maxValue);
                InvalidateMeasure();
            }
        }

        public Vernier()
        {
            MinSize = new Vector2(64, 64);
            CanFocus = true;
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var size = Math.Min(PixelWidth, PixelHeight);
            var center = PixelSize / 2;
            var radius = size / 2f - 4f;

            // Draw background
            var backgroundColor = new Color(30, 30, 34);
            handle.DrawCircle(center, radius, backgroundColor);

            // Draw outline
            var outlineColor = Color.Black;
            handle.DrawCircle(center, radius, outlineColor, false);

            // Draw indicator
            var valuePercent = (MaxValue - MinValue) == 0 ? 0 : (Value - MinValue) / (MaxValue - MinValue);
            if (float.IsNaN(valuePercent) || float.IsInfinity(valuePercent))
                valuePercent = 0;

            var angleDegrees = StartAngleDegrees + AngleRangeDegrees * valuePercent;
            var angle = Angle.FromDegrees(angleDegrees);
            var direction = angle.ToVec();

            var startPoint = center + direction * (radius * 0.2f);
            var endPoint = center + direction * (radius * 0.9f);
            var indicatorColor = HasFocus || _isDragging ? Color.Cyan : Color.LightGray;
            handle.DrawLine(startPoint, endPoint, indicatorColor);
        }

        // NOTE: This code contains compilation errors as I was unable to find the correct event types.
        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _isDragging = true;
                _dragValueStart = Value;
                _dragMouseStartY = args.RelativePosition.Y;
                args.Handle();
            }
        }

        // NOTE: This code contains compilation errors as I was unable to find the correct event types.
        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _isDragging = false;
                args.Handle();
            }
        }

        // NOTE: This code contains compilation errors as I was unable to find the correct event types.
        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (!_isDragging)
                return;

            var deltaY = _dragMouseStartY - args.RelativePosition.Y;
            var valueRange = MaxValue - MinValue;

            // Adjust sensitivity based on the range. A larger range needs less sensitivity.
            var sensitivity = DragSensitivity * (100f / Math.Max(1, valueRange));
            var valueChange = deltaY * sensitivity;

            Value = _dragValueStart + valueChange;
        }
    }
}