namespace OpenControls;

public readonly struct UiColor
{
    public UiColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public static UiColor Black => new(0, 0, 0);
    public static UiColor White => new(255, 255, 255);
    public static UiColor Transparent => new(0, 0, 0, 0);
}
