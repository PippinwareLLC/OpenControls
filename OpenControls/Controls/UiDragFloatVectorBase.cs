using System.Globalization;

namespace OpenControls.Controls;

public abstract class UiDragFloatVectorBase : UiElement
{
    private readonly float[] _values;
    private bool _dragging;
    private bool _focused;
    private int _dragStartX;
    private float _dragStartValue;
    private int _activeIndex = -1;
    private int _hoverIndex = -1;
    private float _min;
    private float _max = 1f;

    protected UiDragFloatVectorBase(int components, string[] defaultLabels)
    {
        if (components <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(components));
        }

        _values = new float[components];
        ComponentLabels = defaultLabels ?? Array.Empty<string>();
    }

    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            ClampAll();
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            ClampAll();
        }
    }

    public float Speed { get; set; } = 0.1f;
    public float Step { get; set; }
    public bool WholeNumbers { get; set; }
    public UiDragFlags Flags { get; set; } = UiDragFlags.Clamp;
    public float SlowSpeedMultiplier { get; set; } = 0.1f;
    public float FastSpeedMultiplier { get; set; } = 10f;
    public string ValueFormat { get; set; } = "0.##";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;
    public int Gap { get; set; } = 6;
    public string[] ComponentLabels { get; set; }

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor ActiveBackground { get; set; } = new UiColor(50, 58, 76);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public IReadOnlyList<float> Values => _values;

    public override bool IsFocusable => true;

    protected float GetValue(int index)
    {
        return _values[index];
    }

    protected void SetValue(int index, float value)
    {
        SetValueInternal(index, value);
    }

    protected virtual void OnValueChanged()
    {
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hoverIndex = GetIndexAt(input.MousePosition);

        if (input.LeftClicked && _hoverIndex >= 0)
        {
            _activeIndex = _hoverIndex;
            _dragging = true;
            _dragStartX = input.MousePosition.X;
            _dragStartValue = _values[_activeIndex];
            context.Focus.RequestFocus(this);
        }

        if (_dragging && input.LeftDown && _activeIndex >= 0)
        {
            float delta = input.MousePosition.X - _dragStartX;
            float speed = GetDragSpeed(input);
            float next = ApplyDrag(_dragStartValue, delta, speed);
            SetValueInternal(_activeIndex, next);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        if (_focused)
        {
            int index = _activeIndex >= 0 ? _activeIndex : 0;
            float step = GetStepValue();

            if (input.Navigation.MoveLeft)
            {
                AdjustValue(index, -step);
            }

            if (input.Navigation.MoveRight)
            {
                AdjustValue(index, step);
            }

            if (input.Navigation.Home && HasRange())
            {
                SetValueInternal(index, _min);
            }

            if (input.Navigation.End && HasRange())
            {
                SetValueInternal(index, _max);
            }
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        for (int i = 0; i < _values.Length; i++)
        {
            UiRect rect = GetPartRect(i);
            UiColor fill = Background;
            if (_dragging && _activeIndex == i)
            {
                fill = ActiveBackground;
            }
            else if (_hoverIndex == i)
            {
                fill = HoverBackground;
            }

            UiRenderHelpers.FillRectRounded(context.Renderer, rect, CornerRadius, fill);
            if (Border.A > 0)
            {
                UiRenderHelpers.DrawRectRounded(context.Renderer, rect, CornerRadius, Border, 1);
            }

            string text = FormatComponentText(i, _values[i]);
            int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
            int textHeight = context.Renderer.MeasureTextHeight(TextScale);
            int textX = rect.X + (rect.Width - textWidth) / 2;
            int textY = rect.Y + (rect.Height - textHeight) / 2;
            context.Renderer.DrawText(text, new UiPoint(textX, textY), TextColor, TextScale);
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _dragging = false;
    }

    private void ClampAll()
    {
        for (int i = 0; i < _values.Length; i++)
        {
            SetValueInternal(i, _values[i]);
        }
    }

    private void AdjustValue(int index, float delta)
    {
        if (delta == 0f)
        {
            return;
        }

        SetValueInternal(index, _values[index] + delta);
    }

    private bool HasRange()
    {
        return _max > _min;
    }

    private bool CanUseLogScale()
    {
        return Flags.HasFlag(UiDragFlags.Logarithmic) && HasRange() && _min > 0f && _max > 0f;
    }

    private float ApplyDrag(float startValue, float delta, float speed)
    {
        if (!CanUseLogScale())
        {
            return startValue + delta * speed;
        }

        float min = MathF.Max(_min, 0.000001f);
        float max = MathF.Max(_max, min + 0.000001f);
        float logMin = MathF.Log(min);
        float logMax = MathF.Log(max);
        float logRange = logMax - logMin;
        if (logRange <= 0f)
        {
            return startValue;
        }

        float clamped = Math.Clamp(startValue, _min, _max);
        float logValue = MathF.Log(MathF.Max(clamped, min));
        float normalized = (logValue - logMin) / logRange;
        float normalizedDelta = delta * speed / 100f;
        normalized = Math.Clamp(normalized + normalizedDelta, 0f, 1f);
        return MathF.Exp(logMin + normalized * logRange);
    }

    private float GetDragSpeed(UiInputState input)
    {
        float speed = Speed;
        if (speed <= 0f)
        {
            float range = MathF.Abs(_max - _min);
            speed = range > 0f ? range / 200f : 0.1f;
        }

        if (!Flags.HasFlag(UiDragFlags.NoSlowFast))
        {
            if (input.CtrlDown)
            {
                speed *= SlowSpeedMultiplier;
            }

            if (input.ShiftDown)
            {
                speed *= FastSpeedMultiplier;
            }
        }

        return speed;
    }

    private float GetStepValue()
    {
        if (Step > 0f)
        {
            return Step;
        }

        if (WholeNumbers)
        {
            return 1f;
        }

        if (Speed > 0f)
        {
            return Speed;
        }

        float range = MathF.Abs(_max - _min);
        return range > 0f ? range / 200f : 0.1f;
    }

    private UiRect GetPartRect(int index)
    {
        int count = _values.Length;
        int padding = Math.Max(0, Padding);
        int gap = Math.Max(0, Gap);
        int height = Math.Max(0, Bounds.Height - padding * 2);
        int width = Math.Max(0, Bounds.Width - padding * 2 - gap * (count - 1));
        int partWidth = count > 0 ? width / count : 0;
        int remainder = width - partWidth * count;

        int x = Bounds.X + padding + index * (partWidth + gap);
        int y = Bounds.Y + padding;
        int actualWidth = partWidth + (index == count - 1 ? remainder : 0);
        return new UiRect(x, y, Math.Max(0, actualWidth), height);
    }

    private int GetIndexAt(UiPoint point)
    {
        for (int i = 0; i < _values.Length; i++)
        {
            if (GetPartRect(i).Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetValueInternal(int index, float value)
    {
        float next = ApplyConstraints(value);
        if (Math.Abs(_values[index] - next) <= float.Epsilon)
        {
            return;
        }

        _values[index] = next;
        OnValueChanged();
    }

    private float ApplyConstraints(float value)
    {
        float next = value;
        if (Flags.HasFlag(UiDragFlags.Clamp) && HasRange())
        {
            next = Math.Clamp(next, _min, _max);
        }

        if (WholeNumbers)
        {
            next = MathF.Round(next);
        }

        if (Step > 0f)
        {
            next = SnapToStep(next);
        }

        return next;
    }

    private float SnapToStep(float value)
    {
        if (Step <= 0f)
        {
            return value;
        }

        float steps = MathF.Round((value - _min) / Step);
        return _min + steps * Step;
    }

    private string FormatComponentText(int index, float value)
    {
        string label = string.Empty;
        if (ComponentLabels != null && index < ComponentLabels.Length)
        {
            label = ComponentLabels[index] ?? string.Empty;
        }

        string formatted = value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(label))
        {
            return formatted;
        }

        return $"{label}: {formatted}";
    }
}
