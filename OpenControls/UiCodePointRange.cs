using System.Text;

namespace OpenControls;

public readonly record struct UiCodePointRange
{
    public UiCodePointRange(int start, int end)
    {
        if (start < 0 || end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        Start = start;
        End = end;
    }

    public static UiCodePointRange BasicLatin => new(0x20, 0x7E);
    public static UiCodePointRange Latin1Supplement => new(0x20, 0x00FF);
    public static UiCodePointRange PrivateUseArea => new(0xE000, 0xF8FF);

    public int Start { get; }
    public int End { get; }

    public bool Contains(int value)
    {
        return value >= Start && value <= End;
    }

    public bool Contains(Rune rune)
    {
        return Contains(rune.Value);
    }

    public static UiCodePointRange Inclusive(int start, int end)
    {
        return new UiCodePointRange(start, end);
    }
}
