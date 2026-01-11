namespace OpenControls.Controls;

public sealed class UiDragInt2 : UiDragIntVectorBase
{
    public UiDragInt2()
        : base(2, new[] { "X", "Y" })
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

    public event Action<int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY);
    }
}
