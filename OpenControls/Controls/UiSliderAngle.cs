namespace OpenControls.Controls;

public sealed class UiSliderAngle : UiElement
{
    private readonly UiSlider _slider;
    private float _valueRadians;
    private float _minDegrees = -360f;
    private float _maxDegrees = 360f;
    private bool _syncing;

    public UiSliderAngle()
    {
        _slider = new UiSlider
        {
            ValueFormat = "0.## deg"
        };
        _slider.ValueChanged += HandleSliderChanged;
        AddChild(_slider);
        SyncSliderRange();
    }

    public float MinDegrees
    {
        get => _minDegrees;
        set
        {
            _minDegrees = value;
            SyncSliderRange();
        }
    }

    public float MaxDegrees
    {
        get => _maxDegrees;
        set
        {
            _maxDegrees = value;
            SyncSliderRange();
        }
    }

    public float Value
    {
        get => _valueRadians;
        set => SetValueRadians(value);
    }

    public float ValueDegrees
    {
        get => RadiansToDegrees(_valueRadians);
        set => SetValueRadians(DegreesToRadians(value));
    }

    public float StepDegrees
    {
        get => _slider.Step;
        set => _slider.Step = value;
    }

    public bool WholeNumbers
    {
        get => _slider.WholeNumbers;
        set => _slider.WholeNumbers = value;
    }

    public bool ShowValue
    {
        get => _slider.ShowValue;
        set => _slider.ShowValue = value;
    }

    public string ValueFormat
    {
        get => _slider.ValueFormat;
        set => _slider.ValueFormat = string.IsNullOrWhiteSpace(value) ? "0.## deg" : value;
    }

    public int TextScale
    {
        get => _slider.TextScale;
        set => _slider.TextScale = value;
    }

    public int Padding
    {
        get => _slider.Padding;
        set => _slider.Padding = value;
    }

    public int TrackHeight
    {
        get => _slider.TrackHeight;
        set => _slider.TrackHeight = value;
    }

    public int ThumbWidth
    {
        get => _slider.ThumbWidth;
        set => _slider.ThumbWidth = value;
    }

    public UiColor TrackColor
    {
        get => _slider.TrackColor;
        set => _slider.TrackColor = value;
    }

    public UiColor FillColor
    {
        get => _slider.FillColor;
        set => _slider.FillColor = value;
    }

    public UiColor ThumbColor
    {
        get => _slider.ThumbColor;
        set => _slider.ThumbColor = value;
    }

    public UiColor ThumbHoverColor
    {
        get => _slider.ThumbHoverColor;
        set => _slider.ThumbHoverColor = value;
    }

    public UiColor Border
    {
        get => _slider.Border;
        set => _slider.Border = value;
    }

    public UiColor ValueTextColor
    {
        get => _slider.ValueTextColor;
        set => _slider.ValueTextColor = value;
    }

    public event Action<float>? ValueChanged;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _slider.Bounds = Bounds;
        _slider.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        _slider.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        _slider.RenderOverlay(context);
    }

    private void HandleSliderChanged(float degrees)
    {
        if (_syncing)
        {
            return;
        }

        float radians = DegreesToRadians(degrees);
        if (Math.Abs(_valueRadians - radians) <= float.Epsilon)
        {
            return;
        }

        _valueRadians = radians;
        ValueChanged?.Invoke(_valueRadians);
    }

    private void SetValueRadians(float radians)
    {
        float degrees = ClampDegrees(RadiansToDegrees(radians));
        float clampedRadians = DegreesToRadians(degrees);
        if (Math.Abs(_valueRadians - clampedRadians) <= float.Epsilon)
        {
            return;
        }

        _syncing = true;
        _valueRadians = clampedRadians;
        _slider.Value = degrees;
        _syncing = false;
        ValueChanged?.Invoke(_valueRadians);
    }

    private void SyncSliderRange()
    {
        _slider.Min = _minDegrees;
        _slider.Max = _maxDegrees;
        SetValueRadians(_valueRadians);
    }

    private float ClampDegrees(float degrees)
    {
        if (_maxDegrees <= _minDegrees)
        {
            return _minDegrees;
        }

        return Math.Clamp(degrees, _minDegrees, _maxDegrees);
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * (MathF.PI / 180f);
    }

    private static float RadiansToDegrees(float radians)
    {
        return radians * (180f / MathF.PI);
    }
}
