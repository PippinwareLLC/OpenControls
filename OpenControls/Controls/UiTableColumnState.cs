namespace OpenControls.Controls;

public enum UiTableColumnWidthMode
{
    Fixed,
    Stretch
}

public sealed class UiTableColumnState
{
    internal UiTableColumnState(int columnIndex)
    {
        ColumnIndex = columnIndex;
    }

    public int ColumnIndex { get; }
    public int DisplayIndex { get; set; }
    public UiTableColumnWidthMode WidthMode { get; set; } = UiTableColumnWidthMode.Stretch;
    public int Width { get; set; }
    public float StretchWeight { get; set; } = 1f;
    public bool Visible { get; set; } = true;
}
