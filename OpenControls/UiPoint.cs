namespace OpenControls;

public readonly struct UiPoint
{
    public UiPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
}
