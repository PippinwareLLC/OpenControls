namespace OpenControls;

public readonly record struct UiFontMetrics(int PixelSize, int Ascent, int Descent, int LineHeight)
{
    public int Baseline => Ascent;
}
