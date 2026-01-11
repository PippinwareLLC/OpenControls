namespace OpenControls.Controls;

public sealed class UiDragInt4 : UiDragIntVectorBase
{
    public UiDragInt4()
        : base(4, new[] { "X", "Y", "Z", "W" })
    {
    }

    public int ValueX
    {
        get => GetValue(0);
        set => SetValue(0, value);
    }

    public int ValueY
    {
        get => GetValue(1);
        set => SetValue(1, value);
    }

    public int ValueZ
    {
        get => GetValue(2);
        set => SetValue(2, value);
    }

    public int ValueW
    {
        get => GetValue(3);
        set => SetValue(3, value);
    }

    public event Action<int, int, int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ, ValueW);
    }
}
