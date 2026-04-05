using System.Collections;
using System.Reflection;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiFontLayoutCacheTests
{
    [Fact]
    public void MeasureTextWidth_ReusesBoundedCacheForRepeatedLongText()
    {
        UiFont font = CreateTestFont();
        string text = BuildText('A', 240, "repeat");

        int widthA = font.MeasureTextWidth(text, scale: 1);
        int widthB = font.MeasureTextWidth(text, scale: 1);

        Assert.Equal(widthA, widthB);
        Assert.Equal(1, GetBoundedLayoutCacheCount(font));
    }

    [Fact]
    public void MeasureTextWidth_DoesNotCacheVeryLongText()
    {
        UiFont font = CreateTestFont();
        string text = BuildText('B', 1400, "uncached");

        _ = font.MeasureTextWidth(text, scale: 1);
        _ = font.MeasureTextWidth(text, scale: 1);

        Assert.Equal(0, GetBoundedLayoutCacheCount(font));
    }

    [Fact]
    public void MeasureTextWidth_BoundedCacheTrimsToConfiguredCapacity()
    {
        UiFont font = CreateTestFont();
        int capacity = GetBoundedLayoutCacheCapacity();

        for (int index = 0; index < capacity + 24; index++)
        {
            string text = BuildText((char)('A' + (index % 26)), 260, $"-entry-{index:D3}");
            _ = font.MeasureTextWidth(text, scale: 1);
        }

        Assert.Equal(capacity, GetBoundedLayoutCacheCount(font));
    }

    private static UiFont CreateTestFont()
    {
        return UiFont.FromTinyBitmap(new TinyBitmapFont(), "TinyBitmap-LayoutCacheTest");
    }

    private static string BuildText(char fill, int repeatCount, string suffix)
    {
        return new string(fill, repeatCount) + suffix;
    }

    private static int GetBoundedLayoutCacheCount(UiFont font)
    {
        FieldInfo field = typeof(UiFont).GetField("_boundedLayoutCache", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UiFont bounded layout cache field not found.");
        IDictionary cache = (IDictionary)(field.GetValue(font)
            ?? throw new InvalidOperationException("UiFont bounded layout cache instance not available."));
        return cache.Count;
    }

    private static int GetBoundedLayoutCacheCapacity()
    {
        FieldInfo field = typeof(UiFont).GetField("BoundedLayoutCacheCapacity", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UiFont bounded layout cache capacity field not found.");
        return (int)(field.GetRawConstantValue()
            ?? throw new InvalidOperationException("UiFont bounded layout cache capacity constant not available."));
    }
}
