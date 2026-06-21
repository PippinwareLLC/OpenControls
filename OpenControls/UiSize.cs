namespace OpenControls;

public readonly struct UiSize
{
    public UiSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
}
