namespace OpenControls.Controls;

public abstract class UiSliderVectorBase : UiElement
{
    private readonly UiSlider[] _sliders;
    private float _min;
    private float _max = 1f;

    protected UiSliderVectorBase(int components)
    {
        if (components <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(components));
        }

        _sliders = new UiSlider[components];
        for (int i = 0; i < components; i++)
        {
            UiSlider slider = new UiSlider();
            slider.ValueChanged += _ => HandleSliderChanged();
            AddChild(slider);
            _sliders[i] = slider;
        }
    }

    public IReadOnlyList<UiSlider> Sliders => _sliders;

    public int Padding { get; set; } = 4;
    public int Gap { get; set; } = 6;
    public int SliderPadding { get; set; } = 4;
    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            ApplySliderOptions();
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            ApplySliderOptions();
        }
    }

    public float Step { get; set; }
    public bool WholeNumbers { get; set; }
    public bool ShowValue { get; set; } = true;
    public string ValueFormat { get; set; } = "0.##";
    public int TextScale { get; set; } = 1;

    protected float GetValue(int index)
    {
        return _sliders[index].Value;
    }

    protected void SetValue(int index, float value)
    {
        _sliders[index].Value = value;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutSliders();
        ApplySliderOptions();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutSliders();
        base.Render(context);
    }

    protected virtual void OnValueChanged()
    {
    }

    private void HandleSliderChanged()
    {
        OnValueChanged();
    }

    private void ApplySliderOptions()
    {
        for (int i = 0; i < _sliders.Length; i++)
        {
            UiSlider slider = _sliders[i];
            slider.Min = _min;
            slider.Max = _max;
            slider.Step = Step;
            slider.WholeNumbers = WholeNumbers;
            slider.ShowValue = ShowValue;
            slider.ValueFormat = ValueFormat;
            slider.TextScale = TextScale;
            slider.Padding = SliderPadding;
        }
    }

    private void LayoutSliders()
    {
        int count = _sliders.Length;
        if (count == 0)
        {
            return;
        }

        int padding = Math.Max(0, Padding);
        int gap = Math.Max(0, Gap);
        int height = Math.Max(0, Bounds.Height - padding * 2);
        int width = Math.Max(0, Bounds.Width - padding * 2 - gap * (count - 1));
        int partWidth = width / count;
        int remainder = width - partWidth * count;

        int x = Bounds.X + padding;
        int y = Bounds.Y + padding;

        for (int i = 0; i < count; i++)
        {
            int actualWidth = partWidth + (i == count - 1 ? remainder : 0);
            UiSlider slider = _sliders[i];
            slider.Bounds = new UiRect(x, y, Math.Max(0, actualWidth), height);
            x += actualWidth + gap;
        }
    }
}
