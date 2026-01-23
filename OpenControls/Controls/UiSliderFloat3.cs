namespace OpenControls.Controls;

public sealed class UiSliderFloat3 : UiSliderVectorBase
{
    public UiSliderFloat3()
        : base(3)
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

    public float ValueZ
    {
        get => GetValue(2);
        set => SetValue(2, value);
    }

    public event Action<float, float, float>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ);
    }
}
