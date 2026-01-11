namespace OpenControls.Controls;

public sealed class UiDragInt3 : UiDragIntVectorBase
{
    public UiDragInt3()
        : base(3, new[] { "X", "Y", "Z" })
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

    public event Action<int, int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ);
    }
}
