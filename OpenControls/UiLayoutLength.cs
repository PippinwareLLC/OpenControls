namespace OpenControls;

public enum UiLayoutLengthKind
{
    Auto,
    Fixed,
    Fill,
    Weight,
    Percentage
}

public readonly record struct UiLayoutLength(UiLayoutLengthKind Kind, float Value)
{
    public static UiLayoutLength Auto => new(UiLayoutLengthKind.Auto, 0f);

    public static UiLayoutLength Fixed(int value)
    {
        return new UiLayoutLength(UiLayoutLengthKind.Fixed, Math.Max(0, value));
    }

    public static UiLayoutLength Fill()
    {
        return new UiLayoutLength(UiLayoutLengthKind.Fill, 1f);
    }

    public static UiLayoutLength Weight(float value)
    {
        return new UiLayoutLength(UiLayoutLengthKind.Weight, Math.Max(0.01f, value));
    }

    public static UiLayoutLength Percentage(float value)
    {
        return new UiLayoutLength(UiLayoutLengthKind.Percentage, Math.Clamp(value, 0f, 1f));
    }
}
