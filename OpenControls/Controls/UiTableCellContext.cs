namespace OpenControls.Controls;

public readonly struct UiTableCellContext
{
    public UiTableCellContext(
        UiTable table,
        UiTableRow row,
        UiTableColumn column,
        UiTableCell cell,
        int rowIndex,
        int columnIndex,
        UiRect bounds,
        bool selected,
        bool hovered)
    {
        Table = table;
        Row = row;
        Column = column;
        Cell = cell;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Bounds = bounds;
        Selected = selected;
        Hovered = hovered;
    }

    public UiTable Table { get; }
    public UiTableRow Row { get; }
    public UiTableColumn Column { get; }
    public UiTableCell Cell { get; }
    public int RowIndex { get; }
    public int ColumnIndex { get; }
    public UiRect Bounds { get; }
    public bool Selected { get; }
    public bool Hovered { get; }
}
