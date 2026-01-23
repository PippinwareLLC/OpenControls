namespace OpenControls.Controls;

public sealed class UiSliderFloat2 : UiSliderVectorBase
{
    public UiSliderFloat2()
        : base(2)
    {
    }

    public float ValueX
    {
        get => GetValue(0);
        set => SetValue(0, value);
    }

    public float ValueY
    {
        get => GetValue(1);
        set => SetValue(1, value);
    }

    public event Action<float, float>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY);
    }
}
