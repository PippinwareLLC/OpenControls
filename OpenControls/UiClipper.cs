namespace OpenControls;

public static class UiClipper
{
    public static UiClipRange FixedHeight(int itemCount, int itemExtent, int viewportStart, int viewportExtent, int overscanItems = 1)
    {
        int safeCount = Math.Max(0, itemCount);
        int safeExtent = Math.Max(1, itemExtent);
        int safeViewportExtent = Math.Max(0, viewportExtent);
        int safeOverscan = Math.Max(0, overscanItems);
        int contentExtent = CalculateContentExtent(safeCount, safeExtent);
        int clampedViewportStart = ClampScrollOffset(safeCount, safeExtent, safeViewportExtent, viewportStart);

        if (safeCount == 0 || safeViewportExtent <= 0)
        {
            return new UiClipRange(
                safeCount,
                safeExtent,
                clampedViewportStart,
                safeViewportExtent,
                contentExtent,
                -1,
                -1,
                -1,
                -1);
        }

        int firstVisible = Math.Clamp(clampedViewportStart / safeExtent, 0, safeCount - 1);
        int lastVisible = Math.Clamp((clampedViewportStart + Math.Max(0, safeViewportExtent - 1)) / safeExtent, firstVisible, safeCount - 1);
        int firstMaterialized = Math.Max(0, firstVisible - safeOverscan);
        int lastMaterialized = Math.Min(safeCount - 1, lastVisible + safeOverscan);

        return new UiClipRange(
            safeCount,
            safeExtent,
            clampedViewportStart,
            safeViewportExtent,
            contentExtent,
            firstVisible,
            lastVisible,
            firstMaterialized,
            lastMaterialized);
    }

    public static int ClampScrollOffset(int itemCount, int itemExtent, int viewportExtent, int scrollOffset)
    {
        int safeCount = Math.Max(0, itemCount);
        int safeExtent = Math.Max(1, itemExtent);
        int safeViewportExtent = Math.Max(0, viewportExtent);
        int contentExtent = CalculateContentExtent(safeCount, safeExtent);
        int maxScroll = Math.Max(0, contentExtent - safeViewportExtent);
        return Math.Clamp(scrollOffset, 0, maxScroll);
    }

    public static int EnsureVisible(int itemCount, int itemExtent, int viewportExtent, int scrollOffset, int index)
    {
        int safeCount = Math.Max(0, itemCount);
        int safeExtent = Math.Max(1, itemExtent);
        int safeViewportExtent = Math.Max(0, viewportExtent);
        if (safeCount == 0 || index < 0 || index >= safeCount)
        {
            return ClampScrollOffset(safeCount, safeExtent, safeViewportExtent, scrollOffset);
        }

        int clampedScrollOffset = ClampScrollOffset(safeCount, safeExtent, safeViewportExtent, scrollOffset);
        int itemStart = CalculateContentExtent(index, safeExtent);
        int itemEnd = CalculateContentExtent(index + 1, safeExtent);
        int viewEnd = clampedScrollOffset + safeViewportExtent;

        if (itemStart < clampedScrollOffset)
        {
            clampedScrollOffset = itemStart;
        }
        else if (itemEnd > viewEnd)
        {
            clampedScrollOffset = itemEnd - safeViewportExtent;
        }

        return ClampScrollOffset(safeCount, safeExtent, safeViewportExtent, clampedScrollOffset);
    }

    private static int CalculateContentExtent(int itemCount, int itemExtent)
    {
        long extent = (long)Math.Max(0, itemCount) * Math.Max(1, itemExtent);
        return extent > int.MaxValue ? int.MaxValue : (int)extent;
    }
}
