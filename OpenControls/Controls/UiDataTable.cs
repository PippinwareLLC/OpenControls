namespace OpenControls.Controls;

public readonly record struct UiDataTableColumn(string Title, int Width);

/// <summary>
/// A sortable data table: fixed-width columns with clickable headers that
/// toggle ascending/descending ordering (numeric-aware, falling back to
/// ordinal strings), string-cell rows, and single-row selection. Sorting is
/// exposed as a pure view over the row list so hosts and tests can consume
/// the ordering without pumping input.
/// </summary>
public sealed class UiDataTable : UiElement
{
    private readonly List<string[]> _rows = [];
    private IReadOnlyList<UiDataTableColumn> _columns = [];
    private int _sortColumnIndex = -1;
    private bool _sortDescending;
    private int _selectedRowIndex = -1;
    private int _textScale = 1;
    private UiColor _headerBackground = new(45, 52, 68);
    private UiColor _headerTextColor = new(235, 225, 180);
    private UiColor _rowTextColor = new(215, 220, 235);
    private UiColor _selectedBackground = new(70, 82, 108);
    private UiColor _alternateBackground = new(28, 33, 44);

    public event Action<int>? RowClicked;

    public event Action<int, bool>? SortChanged;

    public IReadOnlyList<UiDataTableColumn> Columns
    {
        get => _columns;
        set => SetInvalidatingValue(ref _columns, value ?? [], UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int SortColumnIndex => _sortColumnIndex;

    public bool SortDescending => _sortDescending;

    /// <summary>Index into the unsorted row list, or -1.</summary>
    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set => SetInvalidatingValue(ref _selectedRowIndex, value, UiInvalidationReason.Paint);
    }

    public int RowHeight => 10 * _textScale + 4;

    public void SetRows(IEnumerable<string[]> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows ?? []);
        if (_selectedRowIndex >= _rows.Count)
        {
            _selectedRowIndex = _rows.Count - 1;
        }

        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public IReadOnlyList<string[]> Rows => _rows;

    /// <summary>Row indices in display order under the current sort.</summary>
    public IReadOnlyList<int> SortedRowIndices
    {
        get
        {
            var order = Enumerable.Range(0, _rows.Count).ToList();
            if (_sortColumnIndex >= 0 && _sortColumnIndex < _columns.Count)
            {
                order.Sort((left, right) =>
                {
                    int comparison = CompareCells(
                        CellAt(left, _sortColumnIndex),
                        CellAt(right, _sortColumnIndex));
                    return _sortDescending ? -comparison : comparison;
                });
            }

            return order;
        }
    }

    public void SortBy(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        if (_sortColumnIndex == columnIndex)
        {
            _sortDescending = !_sortDescending;
        }
        else
        {
            _sortColumnIndex = columnIndex;
            _sortDescending = false;
        }

        Invalidate(UiInvalidationReason.Paint);
        SortChanged?.Invoke(_sortColumnIndex, _sortDescending);
    }

    private string CellAt(int rowIndex, int columnIndex)
    {
        string[] row = _rows[rowIndex];
        return columnIndex < row.Length ? row[columnIndex] : string.Empty;
    }

    private static int CompareCells(string left, string right)
    {
        bool leftNumeric = double.TryParse(
            left.Replace(",", string.Empty), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double leftValue);
        bool rightNumeric = double.TryParse(
            right.Replace(",", string.Empty), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double rightValue);
        if (leftNumeric && rightNumeric)
        {
            return leftValue.CompareTo(rightValue);
        }

        return string.CompareOrdinal(left, right);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
        {
            int localY = input.MousePosition.Y - Bounds.Y;
            int localX = input.MousePosition.X - Bounds.X;
            if (localY < RowHeight)
            {
                int x = 0;
                for (int columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
                {
                    if (localX >= x && localX < x + _columns[columnIndex].Width)
                    {
                        SortBy(columnIndex);
                        break;
                    }

                    x += _columns[columnIndex].Width;
                }
            }
            else
            {
                int displayRow = localY / RowHeight - 1;
                IReadOnlyList<int> order = SortedRowIndices;
                if (displayRow >= 0 && displayRow < order.Count)
                {
                    SelectedRowIndex = order[displayRow];
                    RowClicked?.Invoke(SelectedRowIndex);
                }
            }
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        int headerX = Bounds.X;
        UiRenderHelpers.FillRectRounded(
            context.Renderer,
            new UiRect(Bounds.X, Bounds.Y, Bounds.Width, RowHeight),
            0,
            _headerBackground);
        for (int columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
        {
            string marker = columnIndex == _sortColumnIndex ? (_sortDescending ? " v" : " ^") : string.Empty;
            context.Renderer.DrawText(
                _columns[columnIndex].Title + marker,
                new UiPoint(headerX + 3, Bounds.Y + 2),
                _headerTextColor,
                _textScale,
                font);
            headerX += _columns[columnIndex].Width;
        }

        IReadOnlyList<int> order = SortedRowIndices;
        int y = Bounds.Y + RowHeight;
        for (int displayRow = 0; displayRow < order.Count; displayRow++)
        {
            if (y + RowHeight > Bounds.Y + Bounds.Height)
            {
                break;
            }

            int rowIndex = order[displayRow];
            if (rowIndex == _selectedRowIndex)
            {
                UiRenderHelpers.FillRectRounded(
                    context.Renderer,
                    new UiRect(Bounds.X, y, Bounds.Width, RowHeight),
                    0,
                    _selectedBackground);
            }
            else if (displayRow % 2 == 1)
            {
                UiRenderHelpers.FillRectRounded(
                    context.Renderer,
                    new UiRect(Bounds.X, y, Bounds.Width, RowHeight),
                    0,
                    _alternateBackground);
            }

            int cellX = Bounds.X;
            string[] row = _rows[rowIndex];
            for (int columnIndex = 0; columnIndex < _columns.Count && columnIndex < row.Length; columnIndex++)
            {
                context.Renderer.DrawText(
                    row[columnIndex],
                    new UiPoint(cellX + 3, y + 2),
                    _rowTextColor,
                    _textScale,
                    font);
                cellX += _columns[columnIndex].Width;
            }

            y += RowHeight;
        }

        base.Render(context);
    }
}
