namespace OpenControls;

public readonly struct UiClipRange
{
    internal UiClipRange(
        int itemCount,
        int itemExtent,
        int viewportStart,
        int viewportExtent,
        int contentExtent,
        int firstVisibleIndex,
        int lastVisibleIndex,
        int firstMaterializedIndex,
        int lastMaterializedIndex)
    {
        ItemCount = itemCount;
        ItemExtent = itemExtent;
        ViewportStart = viewportStart;
        ViewportExtent = viewportExtent;
        ContentExtent = contentExtent;
        FirstVisibleIndex = firstVisibleIndex;
        LastVisibleIndex = lastVisibleIndex;
        FirstMaterializedIndex = firstMaterializedIndex;
        LastMaterializedIndex = lastMaterializedIndex;
    }

    public int ItemCount { get; }
    public int ItemExtent { get; }
    public int ViewportStart { get; }
    public int ViewportExtent { get; }
    public int ContentExtent { get; }
    public int FirstVisibleIndex { get; }
    public int LastVisibleIndex { get; }
    public int FirstMaterializedIndex { get; }
    public int LastMaterializedIndex { get; }

    public bool HasVisibleItems =>
        ItemCount > 0
        && FirstVisibleIndex >= 0
        && LastVisibleIndex >= FirstVisibleIndex
        && LastVisibleIndex < ItemCount;

    public bool HasMaterializedItems =>
        ItemCount > 0
        && FirstMaterializedIndex >= 0
        && LastMaterializedIndex >= FirstMaterializedIndex
        && LastMaterializedIndex < ItemCount;

    public int GetItemStart(int index)
    {
        if (ItemExtent <= 0 || index < 0)
        {
            return 0;
        }

        long start = (long)index * ItemExtent;
        return start > int.MaxValue ? int.MaxValue : (int)start;
    }

    public int GetItemEnd(int index)
    {
        if (ItemExtent <= 0 || index < 0)
        {
            return 0;
        }

        long end = ((long)index + 1L) * ItemExtent;
        return end > int.MaxValue ? int.MaxValue : (int)end;
    }

    public int GetIndexAtOffset(int contentOffset)
    {
        if (ItemCount <= 0 || ItemExtent <= 0 || contentOffset < 0)
        {
            return -1;
        }

        int index = contentOffset / ItemExtent;
        return index >= 0 && index < ItemCount ? index : -1;
    }
}
