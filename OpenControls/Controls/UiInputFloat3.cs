namespace OpenControls.Controls;

public sealed class UiInputFloat3 : UiNumericVectorBase
{
    public UiInputFloat3()
        : base(3, new[] { "X", "Y", "Z" })
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

    public float ValueZ
    {
        get => (float)GetValue(2);
        set => SetValue(2, value);
    }

    public event Action<float, float, float>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ);
    }
}
