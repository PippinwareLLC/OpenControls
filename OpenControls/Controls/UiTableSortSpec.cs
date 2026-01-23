namespace OpenControls.Controls;

public enum UiTableSortDirection
{
    Ascending,
    Descending
}

public sealed class UiTableSortSpec
{
    public UiTableSortSpec(int columnIndex, int sortOrder, UiTableSortDirection direction, int columnUserId)
    {
        ColumnIndex = columnIndex;
        SortOrder = sortOrder;
        Direction = direction;
        ColumnUserId = columnUserId;
    }

    public int ColumnIndex { get; }
    public int SortOrder { get; internal set; }
    public UiTableSortDirection Direction { get; internal set; }
    public int ColumnUserId { get; internal set; }
}
