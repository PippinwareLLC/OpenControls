namespace OpenControls.Controls;

public sealed class UiTableColumn
{
    public UiTableColumn()
    {
    }

    public UiTableColumn(string header, int width = 0, float weight = 1f)
    {
        Header = header;
        Width = width;
        Weight = weight;
    }

    public string Header { get; set; } = string.Empty;
    public int Width { get; set; }
    public int MinWidth { get; set; } = 40;
    public float Weight { get; set; } = 1f;
}

public sealed class UiTableRow
{
    public UiTableRow()
    {
    }

    public UiTableRow(params string[] cells)
    {
        Cells = cells ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Cells { get; set; } = Array.Empty<string>();
    public UiColor? Background { get; set; }
    public UiColor? TextColor { get; set; }
}

public sealed class UiTable : UiElement
{
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
    private readonly int[] _singleSelection = new int[1];
    private UiSelectionModel? _selectionModel;

    public List<UiTableColumn> Columns { get; } = new();
    public IReadOnlyList<UiTableRow> Rows { get; set; } = Array.Empty<UiTableRow>();

    public int RowHeight { get; set; } = 22;
    public int HeaderHeight { get; set; } = 24;
    public int CellPadding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public int HeaderTextScale { get; set; } = 1;
    public bool ShowHeader { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool AlternatingRowBackgrounds { get; set; } = true;
    public bool AllowDeselect { get; set; }

    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor GridColor { get; set; } = new UiColor(60, 70, 90);
    public UiColor HeaderBackground { get; set; } = new UiColor(28, 32, 44);
    public UiColor HeaderTextColor { get; set; } = UiColor.White;
    public UiColor RowBackground { get; set; } = new UiColor(24, 28, 38);
    public UiColor RowAlternateBackground { get; set; } = new UiColor(20, 24, 34);
    public UiColor RowHoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor RowSelectedBackground { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public UiSelectionModel? SelectionModel
    {
        get => _selectionModel;
        set
        {
            if (_selectionModel == value)
            {
                return;
            }

            if (_selectionModel != null)
            {
                _selectionModel.SelectionChanged -= HandleSelectionModelChanged;
            }

            _selectionModel = value;
            if (_selectionModel != null)
            {
                _selectionModel.SelectionChanged += HandleSelectionModelChanged;
                _selectionModel.SetItemCount(Rows.Count);
            }

            HandleSelectionModelChanged();
        }
    }

    public int SelectedIndex
    {
        get => _selectionModel?.PrimaryIndex ?? _selectedIndex;
        set
        {
            if (_selectionModel != null)
            {
                if (value < 0)
                {
                    _selectionModel.Clear();
                }
                else
                {
                    _selectionModel.SelectSingle(value);
                }
            }
            else
            {
                SetSelectedIndex(value);
            }
        }
    }

    public IReadOnlyList<int> SelectedIndices
    {
        get
        {
            if (_selectionModel != null)
            {
                return _selectionModel.SelectedIndices;
            }

            if (_selectedIndex < 0)
            {
                return Array.Empty<int>();
            }

            _singleSelection[0] = _selectedIndex;
            return _singleSelection;
        }
    }

    public event Action<int>? SelectionChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _selectionModel?.SetItemCount(Rows.Count);

        UiInputState input = context.Input;
        bool shift = input.ShiftDown;
        bool ctrl = input.CtrlDown;

        _hoverIndex = GetRowIndexAtPoint(input.MousePosition);

        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < Rows.Count)
            {
                ApplySelection(_hoverIndex, shift, ctrl);
            }
            else if (AllowDeselect)
            {
                if (_selectionModel != null)
                {
                    _selectionModel.Clear();
                }
                else
                {
                    SetSelectedIndex(-1);
                }
            }
        }

        if (_focused)
        {
            if (input.Navigation.MoveUp)
            {
                MoveSelection(-1, shift, ctrl);
            }

            if (input.Navigation.MoveDown)
            {
                MoveSelection(1, shift, ctrl);
            }

            if (input.Navigation.Home)
            {
                ApplySelection(Rows.Count > 0 ? 0 : -1, shift, ctrl);
            }

            if (input.Navigation.End)
            {
                ApplySelection(Rows.Count > 0 ? Rows.Count - 1 : -1, shift, ctrl);
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

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        int columnCount = Columns.Count;
        if (columnCount == 0)
        {
            base.Render(context);
            return;
        }

        int headerHeight = ShowHeader ? GetHeaderHeight() : 0;
        int rowHeight = Math.Max(1, RowHeight);
        int[] columnWidths = BuildColumnWidths();
        int rowAreaHeight = Math.Max(0, Bounds.Height - headerHeight);
        int maxRows = rowHeight > 0 ? rowAreaHeight / rowHeight : 0;
        int rowCount = Math.Min(Rows.Count, maxRows);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int headerTextHeight = context.Renderer.MeasureTextHeight(HeaderTextScale);

        context.Renderer.PushClip(Bounds);

        if (ShowHeader)
        {
            UiRect headerRect = new UiRect(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
            if (HeaderBackground.A > 0)
            {
                context.Renderer.FillRect(headerRect, HeaderBackground);
            }
        }

        int rowY = Bounds.Y + headerHeight;
        for (int row = 0; row < rowCount; row++)
        {
            UiRect rowRect = new UiRect(Bounds.X, rowY, Bounds.Width, rowHeight);
            UiTableRow data = Rows[row];
            UiColor fill = GetRowFill(row, data);
            if (fill.A > 0)
            {
                context.Renderer.FillRect(rowRect, fill);
            }

            rowY += rowHeight;
        }

        if (ShowGrid)
        {
            int x = Bounds.X;
            for (int col = 0; col < columnCount - 1; col++)
            {
                x += columnWidths[col];
                context.Renderer.FillRect(new UiRect(x, Bounds.Y, 1, Bounds.Height), GridColor);
            }

            int lineY = Bounds.Y + headerHeight;
            if (ShowHeader && headerHeight > 0 && lineY < Bounds.Bottom)
            {
                context.Renderer.FillRect(new UiRect(Bounds.X, lineY, Bounds.Width, 1), GridColor);
            }

            for (int row = 0; row < rowCount; row++)
            {
                lineY = Bounds.Y + headerHeight + (row + 1) * rowHeight;
                if (lineY >= Bounds.Bottom)
                {
                    break;
                }

                context.Renderer.FillRect(new UiRect(Bounds.X, lineY, Bounds.Width, 1), GridColor);
            }
        }

        if (ShowHeader)
        {
            int headerY = Bounds.Y + (headerHeight - headerTextHeight) / 2;
            int x = Bounds.X;
            for (int col = 0; col < columnCount; col++)
            {
                UiTableColumn column = Columns[col];
                context.Renderer.DrawText(column.Header, new UiPoint(x + CellPadding, headerY), HeaderTextColor, HeaderTextScale);
                x += columnWidths[col];
            }
        }

        rowY = Bounds.Y + headerHeight;
        for (int row = 0; row < rowCount; row++)
        {
            UiTableRow data = Rows[row];
            int textY = rowY + (rowHeight - textHeight) / 2;
            UiColor rowTextColor = GetRowTextColor(row, data);
            int x = Bounds.X;
            IReadOnlyList<string> cells = data.Cells;

            for (int col = 0; col < columnCount; col++)
            {
                string cellText = col < cells.Count ? cells[col] : string.Empty;
                context.Renderer.DrawText(cellText, new UiPoint(x + CellPadding, textY), rowTextColor, TextScale);
                x += columnWidths[col];
            }

            rowY += rowHeight;
        }

        context.Renderer.PopClip();
        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
    }

    private int GetHeaderHeight()
    {
        return Math.Max(1, HeaderHeight);
    }

    private UiColor GetRowFill(int rowIndex, UiTableRow row)
    {
        if (row.Background.HasValue)
        {
            return row.Background.Value;
        }

        if (IsRowSelected(rowIndex))
        {
            return RowSelectedBackground;
        }

        if (rowIndex == _hoverIndex)
        {
            return RowHoverBackground;
        }

        if (AlternatingRowBackgrounds && rowIndex % 2 == 1)
        {
            return RowAlternateBackground;
        }

        return RowBackground;
    }

    private UiColor GetRowTextColor(int rowIndex, UiTableRow row)
    {
        if (row.TextColor.HasValue)
        {
            return row.TextColor.Value;
        }

        return IsRowSelected(rowIndex) ? SelectedTextColor : TextColor;
    }

    private int[] BuildColumnWidths()
    {
        int columnCount = Columns.Count;
        int[] widths = new int[columnCount];

        int fixedWidth = 0;
        float totalWeight = 0f;
        int flexibleCount = 0;

        for (int i = 0; i < columnCount; i++)
        {
            UiTableColumn column = Columns[i];
            if (column.Width > 0)
            {
                widths[i] = column.Width;
                fixedWidth += column.Width;
            }
            else
            {
                float weight = column.Weight > 0f ? column.Weight : 1f;
                totalWeight += weight;
                flexibleCount++;
            }
        }

        int remaining = Math.Max(0, Bounds.Width - fixedWidth);
        if (flexibleCount > 0)
        {
            for (int i = 0; i < columnCount; i++)
            {
                if (Columns[i].Width > 0)
                {
                    continue;
                }

                UiTableColumn column = Columns[i];
                float weight = column.Weight > 0f ? column.Weight : 1f;
                int width = totalWeight > 0f
                    ? (int)Math.Round(remaining * (weight / totalWeight))
                    : remaining / flexibleCount;
                widths[i] = Math.Max(column.MinWidth, width);
            }
        }

        int total = 0;
        for (int i = 0; i < columnCount; i++)
        {
            total += widths[i];
        }

        if (total < Bounds.Width && columnCount > 0)
        {
            widths[columnCount - 1] += Bounds.Width - total;
        }

        return widths;
    }

    private int GetRowIndexAtPoint(UiPoint point)
    {
        if (!Bounds.Contains(point))
        {
            return -1;
        }

        int headerHeight = ShowHeader ? GetHeaderHeight() : 0;
        int rowHeight = Math.Max(1, RowHeight);
        int localY = point.Y - Bounds.Y - headerHeight;
        if (localY < 0)
        {
            return -1;
        }

        int row = localY / rowHeight;
        return row >= 0 && row < Rows.Count ? row : -1;
    }

    private void MoveSelection(int delta, bool shift, bool ctrl)
    {
        if (Rows.Count == 0)
        {
            if (_selectionModel != null)
            {
                _selectionModel.Clear();
            }
            else
            {
                SetSelectedIndex(-1);
            }
            return;
        }

        int current = SelectedIndex;
        int next = current < 0 ? 0 : current + delta;
        next = Math.Clamp(next, 0, Rows.Count - 1);
        ApplySelection(next, shift, ctrl);
    }

    private void ApplySelection(int index, bool shift, bool ctrl)
    {
        if (_selectionModel != null)
        {
            if (index < 0)
            {
                _selectionModel.Clear();
            }
            else
            {
                _selectionModel.ApplySelection(index, ctrl, shift);
            }

            return;
        }

        SetSelectedIndex(index);
    }

    private bool IsRowSelected(int index)
    {
        if (_selectionModel != null)
        {
            return _selectionModel.IsSelected(index);
        }

        return index == _selectedIndex;
    }

    private void SetSelectedIndex(int index)
    {
        int clamped = index;
        if (Rows.Count == 0)
        {
            clamped = -1;
        }
        else
        {
            clamped = Math.Clamp(index, -1, Rows.Count - 1);
        }

        if (_selectedIndex == clamped)
        {
            return;
        }

        _selectedIndex = clamped;
        SelectionChanged?.Invoke(_selectedIndex);
    }

    private void HandleSelectionModelChanged()
    {
        if (_selectionModel == null)
        {
            return;
        }

        int previous = _selectedIndex;
        _selectedIndex = _selectionModel.PrimaryIndex;
        if (previous != _selectedIndex)
        {
            SelectionChanged?.Invoke(_selectedIndex);
        }
    }
}
