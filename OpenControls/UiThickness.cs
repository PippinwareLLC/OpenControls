namespace OpenControls;

public readonly record struct UiThickness(int Left, int Top, int Right, int Bottom)
{
    public static UiThickness Uniform(int value)
    {
        return new UiThickness(value, value, value, value);
    }

    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;
}
