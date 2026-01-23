namespace OpenControls.Controls;

public sealed class UiInputFloat2 : UiNumericVectorBase
{
    public UiInputFloat2()
        : base(2, new[] { "X", "Y" })
    {
    }

    public float ValueX
    {
        get => (float)GetValue(0);
        set => SetValue(0, value);
    }

    public float ValueY
    {
        get => (float)GetValue(1);
        set => SetValue(1, value);
    }

    public event Action<float, float>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY);
    }
}
