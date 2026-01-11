namespace OpenControls.Controls;

public sealed class UiDragFloat4 : UiDragFloatVectorBase
{
    public UiDragFloat4()
        : base(4, new[] { "X", "Y", "Z", "W" })
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

    public float ValueW
    {
        get => GetValue(3);
        set => SetValue(3, value);
    }

    public event Action<float, float, float, float>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ, ValueW);
    }
}
