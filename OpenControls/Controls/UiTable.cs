using OpenControls.State;

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
        WidthMode = width > 0 ? UiTableColumnWidthMode.Fixed : UiTableColumnWidthMode.Stretch;
    }

    public string Header { get; set; } = string.Empty;
    public int Width { get; set; }
    public int MinWidth { get; set; } = 40;
    public float Weight { get; set; } = 1f;
    public UiTableColumnWidthMode WidthMode { get; set; } = UiTableColumnWidthMode.Stretch;
    public bool AllowSort { get; set; } = true;
    public bool AllowResize { get; set; } = true;
    public bool AllowReorder { get; set; } = true;
    public bool AllowHide { get; set; } = true;
    public bool VisibleByDefault { get; set; } = true;
    public UiTableSortDirection DefaultSortDirection { get; set; } = UiTableSortDirection.Ascending;
    public int UserId { get; set; } = -1;
    public UiElement? HeaderContent { get; set; }
    public UiColor? HeaderBackground { get; set; }
    public UiColor? HeaderTextColor { get; set; }
}

public sealed class UiTableRow
{
    private IReadOnlyList<string> _cells = Array.Empty<string>();
    private IReadOnlyList<UiTableCell> _cellItems = Array.Empty<UiTableCell>();

    public UiTableRow()
    {
    }

    public UiTableRow(params string[] cells)
    {
        Cells = cells ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Cells
    {
        get => _cells;
        set
        {
            _cells = value ?? Array.Empty<string>();
            UiTableCell[] cellItems = new UiTableCell[_cells.Count];
            for (int i = 0; i < _cells.Count; i++)
            {
                cellItems[i] = new UiTableCell(_cells[i] ?? string.Empty);
            }

            _cellItems = cellItems;
        }
    }

    public IReadOnlyList<UiTableCell> CellItems
    {
        get => _cellItems;
        set
        {
            _cellItems = value ?? Array.Empty<UiTableCell>();
            string[] cells = new string[_cellItems.Count];
            for (int i = 0; i < _cellItems.Count; i++)
            {
                cells[i] = _cellItems[i]?.Text ?? string.Empty;
            }

            _cells = cells;
        }
    }

    public UiColor? Background { get; set; }
    public UiColor? TextColor { get; set; }
    public int Height { get; set; }

    internal UiTableCell? GetCell(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < _cellItems.Count ? _cellItems[columnIndex] : null;
    }

    internal string GetCellText(int columnIndex)
    {
        if (columnIndex >= 0 && columnIndex < _cellItems.Count)
        {
            return _cellItems[columnIndex]?.Text ?? string.Empty;
        }

        return string.Empty;
    }
}

public sealed class UiTable : UiElement, IUiStatefulElement, IUiDebugBoundsResolver
{
    private sealed class ContentPlacement
    {
        public required UiElement Element { get; init; }
        public required UiRect Bounds { get; init; }
        public required UiRect ClipBounds { get; init; }
    }

    private readonly int[] _singleSelection = new int[1];
    private readonly List<UiTableSortSpec> _sortSpecs = new();
    private readonly List<UiTableColumnState> _columnStates = new();
    private readonly List<int> _visibleColumnModelIndices = new();
    private readonly List<int> _visibleColumnWidths = new();
    private readonly List<UiRect> _visibleColumnRects = new();
    private readonly List<int> _rowHeights = new();
    private readonly List<int> _rowTops = new();
    private readonly List<ContentPlacement> _headerPlacements = new();
    private readonly List<ContentPlacement> _cellPlacements = new();
    private readonly UiTableViewState _viewState = new();
    private UiSelectionModel? _selectionModel;
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
    private int _scrollX;
    private int _scrollY;
    private UiPoint _contentSize;
    private UiPoint _viewportSize;
    private UiRect _headerViewport;
    private UiRect _bodyViewport;
    private bool _showVertical;
    private bool _showHorizontal;
    private bool _draggingVertical;
    private bool _draggingHorizontal;
    private bool _hoverVerticalThumb;
    private bool _hoverHorizontalThumb;
    private int _dragStartMouse;
    private int _dragStartScroll;
    private int _pressedHeaderModelIndex = -1;
    private UiPoint _pressedHeaderPoint;
    private bool _pressedHeaderMultiSort;
    private int _resizingColumnModelIndex = -1;
    private int _resizeStartMouseX;
    private int _resizeStartWidth;
    private int _reorderingColumnModelIndex = -1;
    private int _reorderInsertDisplayIndex = -1;
    private int _visibleRowStartIndex = -1;
    private int _visibleRowEndIndex = -1;
    private int _materializedRowStartIndex = -1;
    private int _materializedRowEndIndex = -1;
    private int _uniformRowHeight;
    private UiClipRange _rowClipRange;
    private bool _headerContextMenuOpen;
    private UiRect _headerContextMenuBounds;
    private int _hoverHeaderMenuEntry = -1;
    private bool _suppressHeaderMenuClick;
    private string _selectionScope = string.Empty;
    private IReadOnlyList<UiTableRow> _rows = Array.Empty<UiTableRow>();

    public List<UiTableColumn> Columns { get; } = new();
    public IReadOnlyList<UiTableRow> Rows
    {
        get => _rows;
        set
        {
            IReadOnlyList<UiTableRow> normalized = value ?? Array.Empty<UiTableRow>();
            if (!SetInvalidatingValue(ref _rows, normalized, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State))
            {
                return;
            }

            _selectionModel?.SetItemCount(_rows.Count, SelectionScope);
            if (_selectionModel == null && _selectedIndex >= _rows.Count)
            {
                SetSelectedIndex(_rows.Count - 1);
            }

            _hoverIndex = Math.Clamp(_hoverIndex, -1, _rows.Count - 1);
        }
    }

    public int RowHeight { get; set; } = 22;
    public int OverscanRows { get; set; } = 1;
    public int HeaderHeight { get; set; } = 24;
    public int CellPadding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public int HeaderTextScale { get; set; } = 1;
    public bool HeaderTextBold { get; set; }
    public bool ShowHeader { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool AlternatingRowBackgrounds { get; set; } = true;
    public bool AllowDeselect { get; set; }
    public bool AllowSorting { get; set; } = true;
    public bool AllowMultiSort { get; set; }
    public bool AllowNoSort { get; set; }
    public bool AllowColumnResize { get; set; } = true;
    public bool AllowColumnReorder { get; set; } = true;
    public bool AllowColumnHiding { get; set; } = true;
    public bool EnableHeaderContextMenu { get; set; } = true;
    public int SortIndicatorPadding { get; set; } = 4;
    public bool UseAngledHeaders { get; set; }
    public int AngledHeaderHeight { get; set; } = 48;
    public int AngledHeaderStep { get; set; }
    public UiScrollbarVisibility HorizontalScrollbar { get; set; } = UiScrollbarVisibility.Auto;
    public UiScrollbarVisibility VerticalScrollbar { get; set; } = UiScrollbarVisibility.Auto;
    public int ScrollbarThickness { get; set; } = 12;
    public int ScrollbarPadding { get; set; } = 2;
    public int MinThumbSize { get; set; } = 12;
    public int ScrollWheelStep { get; set; } = 40;
    public int ResizeHandleWidth { get; set; } = 6;
    public int HeaderContextMenuWidth { get; set; } = 180;
    public int HeaderContextMenuItemHeight { get; set; } = 24;
    public int ReorderIndicatorThickness { get; set; } = 2;
    public Func<int, UiTableRow, int>? RowHeightSelector { get; set; }
    public Func<UiTableCellContext, UiColor?>? CellBackgroundSelector { get; set; }
    public Func<UiTableCellContext, UiColor?>? CellTextColorSelector { get; set; }

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
    public UiColor ScrollbarTrack { get; set; } = new UiColor(20, 24, 34);
    public UiColor ScrollbarBorder { get; set; } = new UiColor(60, 70, 90);
    public UiColor ScrollbarThumb { get; set; } = new UiColor(70, 80, 100);
    public UiColor ScrollbarThumbHover { get; set; } = new UiColor(90, 110, 140);
    public UiColor HeaderMenuBackground { get; set; } = new UiColor(24, 28, 38);
    public UiColor HeaderMenuBorder { get; set; } = new UiColor(70, 80, 100);
    public UiColor HeaderMenuHoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor HeaderMenuTextColor { get; set; } = UiColor.White;
    public UiColor HeaderMenuCheckColor { get; set; } = new UiColor(120, 180, 220);
    public UiColor ReorderIndicatorColor { get; set; } = new UiColor(120, 180, 220);
    public int CornerRadius { get; set; }
    public string SelectionScope
    {
        get => _selectionScope;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
            if (string.Equals(_selectionScope, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _selectionScope = normalized;
            _selectionModel?.SetItemCount(Rows.Count, _selectionScope);
            HandleSelectionModelChanged();
        }
    }

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
                _selectionModel.SetItemCount(Rows.Count, SelectionScope);
            }

            HandleSelectionModelChanged();
        }
    }

    public int SelectedIndex
    {
        get => _selectionModel?.GetPrimaryIndex(SelectionScope) ?? _selectedIndex;
        set
        {
            if (_selectionModel != null)
            {
                if (value < 0)
                {
                    _selectionModel.Clear(SelectionScope);
                }
                else
                {
                    _selectionModel.SelectSingle(value, SelectionScope);
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
                return _selectionModel.GetSelectedIndices(SelectionScope);
            }

            if (_selectedIndex < 0)
            {
                return Array.Empty<int>();
            }

            _singleSelection[0] = _selectedIndex;
            return _singleSelection;
        }
    }

    public IReadOnlyList<UiTableSortSpec> SortSpecs => _sortSpecs;
    public IReadOnlyList<UiTableColumnState> ColumnStates
    {
        get
        {
            EnsureColumnStates();
            return _columnStates;
        }
    }

    public UiPoint ContentSize => _contentSize;
    public UiRect ViewportBounds => _bodyViewport;
    public UiTableViewState ViewState => _viewState;
    public int HoveredIndex => _hoverIndex;
    public int ScrollX
    {
        get => _scrollX;
        set => SetInvalidatingValue(ref _scrollX, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public int ScrollY
    {
        get => _scrollY;
        set => SetInvalidatingValue(ref _scrollY, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public UiPoint ScrollOffset
    {
        get => new UiPoint(_scrollX, _scrollY);
        set
        {
            bool changedX = SetInvalidatingValue(ref _scrollX, Math.Max(0, value.X), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
            bool changedY = SetInvalidatingValue(ref _scrollY, Math.Max(0, value.Y), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
            if (!changedX && !changedY)
            {
                return;
            }
        }
    }

    public int FirstVisibleRowIndex => _visibleRowStartIndex;
    public int LastVisibleRowIndex => _visibleRowEndIndex;

    public event Action<int>? SelectionChanged;
    public event Action<int>? RowActivated;
    public event Action? SortSpecsChanged;
    public event Action? ColumnStatesChanged;
    public event Action? ViewStateChanged;

    public override bool IsFocusable => true;
    public override bool CapturesPointerInput => true;

    public UiTableColumnState GetColumnState(int columnIndex)
    {
        EnsureColumnStates();
        if (columnIndex < 0 || columnIndex >= _columnStates.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        return _columnStates[columnIndex];
    }

    public bool TryGetVisibleRowBounds(int index, out UiRect bounds)
    {
        bounds = default;
        if (index < 0
            || index >= Rows.Count
            || index >= _rowTops.Count
            || index >= _rowHeights.Count
            || _bodyViewport.Width <= 0
            || _bodyViewport.Height <= 0)
        {
            return false;
        }

        int rowY = _bodyViewport.Y + _rowTops[index] - _scrollY;
        UiRect rowRect = new(_bodyViewport.X, rowY, _bodyViewport.Width, _rowHeights[index]);
        if (rowRect.Bottom <= _bodyViewport.Y || rowRect.Y >= _bodyViewport.Bottom)
        {
            return false;
        }

        bounds = IntersectRect(rowRect, _bodyViewport);
        return true;
    }

    public bool TryGetResolvedColumnWidth(int columnIndex, out int width)
    {
        width = 0;
        if (columnIndex < 0 || columnIndex >= Columns.Count)
        {
            return false;
        }

        for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
        {
            if (_visibleColumnModelIndices[slot] != columnIndex)
            {
                continue;
            }

            if (slot < 0 || slot >= _visibleColumnWidths.Count)
            {
                return false;
            }

            width = _visibleColumnWidths[slot];
            return width > 0;
        }

        return false;
    }

    public void CaptureState(UiElementState state)
    {
        state.SelectedIndex = SelectedIndex;
        state.SelectedIndices = SelectedIndices.ToList();
        UiTableElementState tableState = new()
        {
            ScrollX = _scrollX,
            ScrollY = _scrollY
        };

        for (int i = 0; i < ColumnStates.Count; i++)
        {
            UiTableColumnState columnState = ColumnStates[i];
            tableState.Columns.Add(new UiTableColumnSnapshot
            {
                ColumnIndex = columnState.ColumnIndex,
                DisplayIndex = columnState.DisplayIndex,
                WidthMode = columnState.WidthMode,
                Width = columnState.Width,
                StretchWeight = columnState.StretchWeight,
                Visible = columnState.Visible
            });
        }

        state.Table = tableState;
    }

    public void ApplyState(UiElementState state)
    {
        if (state.Table != null)
        {
            _scrollX = Math.Max(0, state.Table.ScrollX);
            _scrollY = Math.Max(0, state.Table.ScrollY);

            foreach (UiTableColumnSnapshot snapshot in state.Table.Columns)
            {
                if (snapshot.ColumnIndex < 0 || snapshot.ColumnIndex >= ColumnStates.Count)
                {
                    continue;
                }

                UiTableColumnState columnState = ColumnStates[snapshot.ColumnIndex];
                columnState.DisplayIndex = snapshot.DisplayIndex;
                columnState.WidthMode = snapshot.WidthMode;
                columnState.Width = snapshot.Width;
                columnState.StretchWeight = snapshot.StretchWeight;
                columnState.Visible = snapshot.Visible;
            }
        }

        if (state.SelectedIndices.Count > 0)
        {
            if (SelectionModel != null)
            {
                SelectionModel.SetSelection(state.SelectedIndices, state.SelectedIndex ?? -1, state.SelectedIndex ?? -1, SelectionScope);
            }
            else
            {
                SelectedIndex = state.SelectedIndex ?? state.SelectedIndices[^1];
            }
        }
        else if (state.SelectedIndex.HasValue)
        {
            SelectedIndex = state.SelectedIndex.Value;
        }
    }

    public void ResetColumnStates()
    {
        _columnStates.Clear();
        EnsureColumnStates();
        OnColumnStatesChanged();
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _selectionModel?.SetItemCount(Rows.Count, SelectionScope);
        EnsureColumnStates();
        ValidateSortSpecs();
        RefreshLayout();

        UiInputState input = context.GetSelfInput(this);
        UiElement? contentHit = HitTestPlacedContent(input.MousePosition);
        bool pointerHandledByMenu = UpdateHeaderContextMenu(input);
        bool pointerHandledByScrollbars = UpdateScrollbars(input, pointerHandledByMenu);

        if (!pointerHandledByMenu)
        {
            HandleWheelScrolling(input);
        }

        _hoverIndex = pointerHandledByMenu ? -1 : GetRowIndexAtPoint(input.MousePosition);

        if (!pointerHandledByMenu)
        {
            HandleHeaderInteractions(context, input, contentHit, pointerHandledByScrollbars);
            HandleBodyInteractions(context, input, contentHit, pointerHandledByScrollbars);
        }
        else
        {
            ClearHeaderPressState();
        }

        if (_focused)
        {
            HandleKeyboardNavigation(input);
        }

        RefreshLayout();
        UpdatePlacedContent(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        using IDisposable scope = UiProfiling.Scope($"OpenControls.Table.Render.{GetProfileName()}");

        RefreshLayout();

        UiFont font = ResolveFont(context.DefaultFont);
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        if (Columns.Count == 0 || _visibleColumnModelIndices.Count == 0)
        {
            DrawScrollbars(context);
            return;
        }

        int headerHeight = ShowHeader ? _headerViewport.Height : 0;
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
        int headerTextHeight = context.Renderer.MeasureTextHeight(HeaderTextScale, font);

        context.Renderer.PushClip(Bounds);

        if (ShowHeader && headerHeight > 0)
        {
            UiRect headerRect = new UiRect(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
            if (HeaderBackground.A > 0)
            {
                context.Renderer.FillRect(headerRect, HeaderBackground);
            }

            context.Renderer.PushClip(_headerViewport);
            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                UiTableColumn column = Columns[modelIndex];
                UiRect cellRect = _visibleColumnRects[slot];
                if (column.HeaderBackground is UiColor headerFill && headerFill.A > 0)
                {
                    context.Renderer.FillRect(cellRect, headerFill);
                }

                if (column.HeaderContent == null)
                {
                    DrawHeaderText(context, font, headerTextHeight, column, cellRect);
                }

                UiTableSortSpec? sortSpec = GetSortSpec(modelIndex);
                if (sortSpec != null)
                {
                    DrawSortIndicator(context, cellRect, sortSpec, font);
                }
            }

            RenderPlacedContent(context, _headerPlacements, overlay: false);
            context.Renderer.PopClip();
        }

        if (_bodyViewport.Width > 0 && _bodyViewport.Height > 0)
        {
            context.Renderer.PushClip(_bodyViewport);
            DrawRows(context, font, textHeight);
            if (ShowGrid)
            {
                DrawGridLines(context);
            }

            RenderPlacedContent(context, _cellPlacements, overlay: false);
            context.Renderer.PopClip();
        }

        context.Renderer.PopClip();

        DrawScrollbars(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        using IDisposable scope = UiProfiling.Scope($"OpenControls.Table.RenderOverlay.{GetProfileName()}");

        RefreshLayout();

        if (_headerPlacements.Count > 0)
        {
            RenderPlacedContent(context, _headerPlacements, overlay: true);
        }

        if (_cellPlacements.Count > 0)
        {
            RenderPlacedContent(context, _cellPlacements, overlay: true);
        }

        if (_reorderingColumnModelIndex >= 0)
        {
            DrawReorderIndicator(context);
        }

        if (_headerContextMenuOpen)
        {
            DrawHeaderContextMenu(context);
        }

        if (CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }
    }

    private string GetProfileName()
    {
        if (!string.IsNullOrWhiteSpace(Id))
        {
            return Id;
        }

        return "Table";
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible)
        {
            return null;
        }

        if (_headerContextMenuOpen && _headerContextMenuBounds.Contains(point))
        {
            return this;
        }

        if (!Bounds.Contains(point))
        {
            return null;
        }

        UiElement? contentHit = HitTestPlacedContent(point);
        return contentHit ?? this;
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _draggingVertical = false;
        _draggingHorizontal = false;
        _resizingColumnModelIndex = -1;
        _reorderingColumnModelIndex = -1;
        ClearHeaderPressState();
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (_resizingColumnModelIndex >= 0 || GetResizeHandleColumnIndexAtPoint(input.MousePosition) >= 0)
        {
            cursor = UiMouseCursor.ResizeEW;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (_headerContextMenuOpen || _draggingVertical || _draggingHorizontal || _resizingColumnModelIndex >= 0 || _reorderingColumnModelIndex >= 0)
        {
            status |= UiItemStatusFlags.Active;
        }

        return status;
    }

    private void EnsureColumnStates()
    {
        if (_columnStates.Count > Columns.Count)
        {
            _columnStates.RemoveRange(Columns.Count, _columnStates.Count - Columns.Count);
        }

        for (int i = _columnStates.Count; i < Columns.Count; i++)
        {
            UiTableColumn column = Columns[i];
            UiTableColumnState state = new(i)
            {
                DisplayIndex = i,
                WidthMode = column.WidthMode == UiTableColumnWidthMode.Fixed || column.Width > 0
                    ? UiTableColumnWidthMode.Fixed
                    : UiTableColumnWidthMode.Stretch,
                Width = column.Width > 0 ? Math.Max(column.MinWidth, column.Width) : Math.Max(column.MinWidth, 0),
                StretchWeight = column.Weight > 0f ? column.Weight : 1f,
                Visible = column.VisibleByDefault
            };
            _columnStates.Add(state);
        }

        NormalizeColumnStates();
    }

    private void NormalizeColumnStates()
    {
        if (_columnStates.Count == 0)
        {
            return;
        }

        List<UiTableColumnState> ordered = new(_columnStates);
        ordered.Sort((left, right) =>
        {
            int order = left.DisplayIndex.CompareTo(right.DisplayIndex);
            return order != 0 ? order : left.ColumnIndex.CompareTo(right.ColumnIndex);
        });

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].DisplayIndex = i;
        }
    }

    private void RefreshLayout()
    {
        BuildVisibleColumnLayout();
        BuildRowMetrics();
        ResolveViewportLayout();
        ClampScrollOffset();
        ResolveVisibleRows();
        UpdateViewState();
        BuildContentPlacements();
    }

    private void BuildVisibleColumnLayout()
    {
        _visibleColumnModelIndices.Clear();
        for (int i = 0; i < _columnStates.Count; i++)
        {
            if (_columnStates[i].Visible)
            {
                _visibleColumnModelIndices.Add(i);
            }
        }

        _visibleColumnModelIndices.Sort((left, right) =>
        {
            int order = _columnStates[left].DisplayIndex.CompareTo(_columnStates[right].DisplayIndex);
            return order != 0 ? order : left.CompareTo(right);
        });
    }

    private void BuildRowMetrics()
    {
        _rowHeights.Clear();
        _rowTops.Clear();

        int top = 0;
        int uniformHeight = 0;
        for (int i = 0; i < Rows.Count; i++)
        {
            UiTableRow row = Rows[i];
            int height = row.Height > 0
                ? row.Height
                : RowHeightSelector?.Invoke(i, row) ?? RowHeight;
            height = Math.Max(1, height);
            _rowHeights.Add(height);
            _rowTops.Add(top);
            top += height;

            if (i == 0)
            {
                uniformHeight = height;
            }
            else if (uniformHeight != height)
            {
                uniformHeight = 0;
            }
        }

        _uniformRowHeight = Rows.Count > 0 ? uniformHeight : 0;
    }

    private void ResolveViewportLayout()
    {
        int tableWidth = Math.Max(0, Bounds.Width);
        int tableHeight = Math.Max(0, Bounds.Height);
        int headerHeight = ShowHeader ? GetHeaderHeight() : 0;
        int thickness = Math.Max(1, ScrollbarThickness);
        int contentHeight = _rowTops.Count == 0 ? 0 : _rowTops[^1] + _rowHeights[^1];

        bool showH = HorizontalScrollbar == UiScrollbarVisibility.Always;
        bool showV = VerticalScrollbar == UiScrollbarVisibility.Always;
        bool autoH = HorizontalScrollbar == UiScrollbarVisibility.Auto;
        bool autoV = VerticalScrollbar == UiScrollbarVisibility.Auto;

        int viewportWidth = tableWidth;
        int viewportHeight = Math.Max(0, tableHeight - headerHeight);

        int contentWidth = MeasureContentWidth(viewportWidth);

        if (autoH && contentWidth > viewportWidth)
        {
            showH = true;
        }

        if (showV)
        {
            viewportWidth = Math.Max(0, tableWidth - thickness);
            contentWidth = MeasureContentWidth(viewportWidth);
        }

        if (showH)
        {
            viewportHeight = Math.Max(0, tableHeight - headerHeight - thickness);
        }

        if (autoV && contentHeight > viewportHeight)
        {
            showV = true;
            viewportWidth = Math.Max(0, tableWidth - thickness);
            contentWidth = MeasureContentWidth(viewportWidth);
        }

        if (autoH && !showH && contentWidth > viewportWidth)
        {
            showH = true;
            viewportHeight = Math.Max(0, tableHeight - headerHeight - thickness);
        }

        if (autoV && !showV && contentHeight > viewportHeight)
        {
            showV = true;
            viewportWidth = Math.Max(0, tableWidth - thickness);
            contentWidth = MeasureContentWidth(viewportWidth);
        }

        _showHorizontal = showH;
        _showVertical = showV;
        _viewportSize = new UiPoint(viewportWidth, viewportHeight);
        _contentSize = new UiPoint(contentWidth, Math.Max(0, contentHeight));
        _headerViewport = new UiRect(Bounds.X, Bounds.Y, viewportWidth, headerHeight);
        _bodyViewport = new UiRect(Bounds.X, Bounds.Y + headerHeight, viewportWidth, viewportHeight);

        BuildVisibleColumnWidths(viewportWidth);
    }

    private int MeasureContentWidth(int viewportWidth)
    {
        if (_visibleColumnModelIndices.Count == 0)
        {
            return 0;
        }

        int total = 0;
        int fixedWidth = 0;
        float totalStretchWeight = 0f;
        int stretchCount = 0;

        for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
        {
            int modelIndex = _visibleColumnModelIndices[slot];
            UiTableColumn column = Columns[modelIndex];
            UiTableColumnState state = _columnStates[modelIndex];
            if (state.WidthMode == UiTableColumnWidthMode.Fixed)
            {
                fixedWidth += Math.Max(column.MinWidth, state.Width > 0 ? state.Width : column.Width);
            }
            else
            {
                totalStretchWeight += state.StretchWeight > 0f ? state.StretchWeight : 1f;
                stretchCount++;
            }
        }

        total += fixedWidth;
        if (stretchCount > 0)
        {
            int remaining = Math.Max(0, viewportWidth - fixedWidth);
            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                UiTableColumn column = Columns[modelIndex];
                UiTableColumnState state = _columnStates[modelIndex];
                if (state.WidthMode == UiTableColumnWidthMode.Fixed)
                {
                    continue;
                }

                float weight = state.StretchWeight > 0f ? state.StretchWeight : 1f;
                int width = totalStretchWeight > 0f
                    ? (int)Math.Round(remaining * (weight / totalStretchWeight))
                    : remaining / stretchCount;
                total += Math.Max(column.MinWidth, width);
            }
        }

        return total;
    }

    private void BuildVisibleColumnWidths(int viewportWidth)
    {
        _visibleColumnWidths.Clear();
        _visibleColumnRects.Clear();

        if (_visibleColumnModelIndices.Count == 0)
        {
            return;
        }

        int fixedWidth = 0;
        float totalStretchWeight = 0f;
        int stretchCount = 0;

        for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
        {
            int modelIndex = _visibleColumnModelIndices[slot];
            UiTableColumn column = Columns[modelIndex];
            UiTableColumnState state = _columnStates[modelIndex];
            if (state.WidthMode == UiTableColumnWidthMode.Fixed)
            {
                fixedWidth += Math.Max(column.MinWidth, state.Width > 0 ? state.Width : column.Width);
            }
            else
            {
                totalStretchWeight += state.StretchWeight > 0f ? state.StretchWeight : 1f;
                stretchCount++;
            }
        }

        int remaining = Math.Max(0, viewportWidth - fixedWidth);
        for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
        {
            int modelIndex = _visibleColumnModelIndices[slot];
            UiTableColumn column = Columns[modelIndex];
            UiTableColumnState state = _columnStates[modelIndex];
            int width;
            if (state.WidthMode == UiTableColumnWidthMode.Fixed)
            {
                width = Math.Max(column.MinWidth, state.Width > 0 ? state.Width : column.Width);
            }
            else
            {
                float weight = state.StretchWeight > 0f ? state.StretchWeight : 1f;
                width = totalStretchWeight > 0f
                    ? (int)Math.Round(remaining * (weight / totalStretchWeight))
                    : remaining / Math.Max(1, stretchCount);
                width = Math.Max(column.MinWidth, width);
            }

            _visibleColumnWidths.Add(width);
        }

        int x = _bodyViewport.X - _scrollX;
        for (int slot = 0; slot < _visibleColumnWidths.Count; slot++)
        {
            int width = _visibleColumnWidths[slot];
            _visibleColumnRects.Add(new UiRect(x, Bounds.Y, width, ShowHeader ? _headerViewport.Height : 0));
            x += width;
        }
    }

    private void ClampScrollOffset()
    {
        int maxX = Math.Max(0, _contentSize.X - _viewportSize.X);
        int maxY = Math.Max(0, _contentSize.Y - _viewportSize.Y);
        _scrollX = Math.Clamp(_scrollX, 0, maxX);
        _scrollY = Math.Clamp(_scrollY, 0, maxY);
    }

    private void ResolveVisibleRows()
    {
        _visibleRowStartIndex = -1;
        _visibleRowEndIndex = -1;
        _materializedRowStartIndex = -1;
        _materializedRowEndIndex = -1;
        _rowClipRange = default;

        if (_bodyViewport.Height <= 0 || Rows.Count == 0)
        {
            return;
        }

        if (_uniformRowHeight > 0)
        {
            _rowClipRange = UiClipper.FixedHeight(Rows.Count, _uniformRowHeight, _scrollY, _bodyViewport.Height, OverscanRows);
            _visibleRowStartIndex = _rowClipRange.FirstVisibleIndex;
            _visibleRowEndIndex = _rowClipRange.LastVisibleIndex;
            _materializedRowStartIndex = _rowClipRange.FirstMaterializedIndex;
            _materializedRowEndIndex = _rowClipRange.LastMaterializedIndex;
            return;
        }

        int top = _scrollY;
        int bottom = _scrollY + _bodyViewport.Height;
        int start = 0;
        while (start < Rows.Count && _rowTops[start] + _rowHeights[start] <= top)
        {
            start++;
        }

        if (start >= Rows.Count)
        {
            return;
        }

        int endExclusive = start;
        while (endExclusive < Rows.Count && _rowTops[endExclusive] < bottom)
        {
            endExclusive++;
        }

        _visibleRowStartIndex = start;
        _visibleRowEndIndex = Math.Max(start, endExclusive - 1);
        _materializedRowStartIndex = _visibleRowStartIndex;
        _materializedRowEndIndex = _visibleRowEndIndex;
    }

    private void UpdateViewState()
    {
        UiPoint previousContentSize = _viewState.ContentSize;
        UiPoint previousViewportSize = _viewState.ViewportSize;
        int previousScrollX = _viewState.ScrollX;
        int previousScrollY = _viewState.ScrollY;
        int previousFirst = _viewState.FirstVisibleRowIndex;
        int previousLast = _viewState.LastVisibleRowIndex;

        _viewState.ContentSize = _contentSize;
        _viewState.ViewportSize = _viewportSize;
        _viewState.HeaderViewportBounds = _headerViewport;
        _viewState.BodyViewportBounds = _bodyViewport;
        _viewState.ScrollX = _scrollX;
        _viewState.ScrollY = _scrollY;
        _viewState.FirstVisibleRowIndex = _visibleRowStartIndex;
        _viewState.LastVisibleRowIndex = _visibleRowEndIndex;

        if (previousContentSize.X != _viewState.ContentSize.X
            || previousContentSize.Y != _viewState.ContentSize.Y
            || previousViewportSize.X != _viewState.ViewportSize.X
            || previousViewportSize.Y != _viewState.ViewportSize.Y
            || previousScrollX != _viewState.ScrollX
            || previousScrollY != _viewState.ScrollY
            || previousFirst != _viewState.FirstVisibleRowIndex
            || previousLast != _viewState.LastVisibleRowIndex)
        {
            ViewStateChanged?.Invoke();
        }
    }

    private void BuildContentPlacements()
    {
        _headerPlacements.Clear();
        _cellPlacements.Clear();

        if (ShowHeader && _headerViewport.Width > 0 && _headerViewport.Height > 0)
        {
            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                UiTableColumn column = Columns[modelIndex];
                UiElement? content = column.HeaderContent;
                if (content == null)
                {
                    continue;
                }

                UiRect cellRect = _visibleColumnRects[slot];
                UiRect contentRect = InsetRect(cellRect, Math.Max(0, CellPadding));
                UiRect clip = IntersectRect(contentRect, _headerViewport);
                if (clip.Width <= 0 || clip.Height <= 0)
                {
                    continue;
                }

                content.Bounds = new UiRect(0, 0, Math.Max(0, contentRect.Width), Math.Max(0, contentRect.Height));
                _headerPlacements.Add(new ContentPlacement
                {
                    Element = content,
                    Bounds = contentRect,
                    ClipBounds = clip
                });
            }
        }

        if (_materializedRowStartIndex < 0 || _materializedRowEndIndex < 0)
        {
            return;
        }

        for (int rowIndex = _materializedRowStartIndex; rowIndex <= _materializedRowEndIndex; rowIndex++)
        {
            UiTableRow row = Rows[rowIndex];
            int rowHeight = _rowHeights[rowIndex];
            int rowY = _bodyViewport.Y + _rowTops[rowIndex] - _scrollY;
            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                UiTableCell? cell = row.GetCell(modelIndex);
                UiElement? content = cell?.Content;
                if (content == null)
                {
                    continue;
                }

                UiRect columnRect = _visibleColumnRects[slot];
                UiRect cellRect = new UiRect(columnRect.X, rowY, columnRect.Width, rowHeight);
                int padding = cell?.Padding >= 0 ? cell.Padding : CellPadding;
                UiRect contentRect = InsetRect(cellRect, Math.Max(0, padding));
                UiRect clip = IntersectRect(contentRect, _bodyViewport);
                if (clip.Width <= 0 || clip.Height <= 0)
                {
                    continue;
                }

                content.Bounds = new UiRect(0, 0, Math.Max(0, contentRect.Width), Math.Max(0, contentRect.Height));
                _cellPlacements.Add(new ContentPlacement
                {
                    Element = content,
                    Bounds = contentRect,
                    ClipBounds = clip
                });
            }
        }
    }

    private void UpdatePlacedContent(UiUpdateContext context)
    {
        for (int i = 0; i < _headerPlacements.Count; i++)
        {
            UpdatePlacement(_headerPlacements[i], context);
        }

        for (int i = 0; i < _cellPlacements.Count; i++)
        {
            UpdatePlacement(_cellPlacements[i], context);
        }
    }

    private void UpdatePlacement(ContentPlacement placement, UiUpdateContext context)
    {
        UiInputState childInput = BuildPlacementInput(context.Input, placement.Bounds, placement.ClipBounds);
        placement.Element.Update(context.CreateChildContext(this, placement.Element, childInput));
    }

    private static UiInputState BuildPlacementInput(UiInputState input, UiRect bounds, UiRect clipBounds)
    {
        bool useMouse = clipBounds.Contains(input.MousePosition);
        UiPoint mouse = useMouse
            ? new UiPoint(input.MousePosition.X - bounds.X, input.MousePosition.Y - bounds.Y)
            : new UiPoint(int.MinValue / 4, int.MinValue / 4);

        return new UiInputState
        {
            MousePosition = mouse,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftDoubleClicked = input.LeftDoubleClicked,
            LeftReleased = input.LeftReleased,
            RightDown = input.RightDown,
            RightClicked = input.RightClicked,
            RightDoubleClicked = input.RightDoubleClicked,
            RightReleased = input.RightReleased,
            MiddleDown = input.MiddleDown,
            MiddleClicked = input.MiddleClicked,
            MiddleDoubleClicked = input.MiddleDoubleClicked,
            MiddleReleased = input.MiddleReleased,
            LeftDragOrigin = TranslatePoint(input.LeftDragOrigin, bounds.X, bounds.Y, useMouse),
            RightDragOrigin = TranslatePoint(input.RightDragOrigin, bounds.X, bounds.Y, useMouse),
            MiddleDragOrigin = TranslatePoint(input.MiddleDragOrigin, bounds.X, bounds.Y, useMouse),
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            TextInput = input.TextInput,
            Composition = input.Composition,
            KeysDown = input.KeysDown,
            KeysPressed = input.KeysPressed,
            KeysReleased = input.KeysReleased,
            Navigation = input.Navigation
        };
    }

    private static UiPoint? TranslatePoint(UiPoint? point, int offsetX, int offsetY, bool useMouse)
    {
        if (!useMouse || point is not UiPoint value)
        {
            return null;
        }

        return new UiPoint(value.X - offsetX, value.Y - offsetY);
    }

    private void RenderPlacedContent(UiRenderContext context, List<ContentPlacement> placements, bool overlay)
    {
        for (int i = 0; i < placements.Count; i++)
        {
            ContentPlacement placement = placements[i];
            context.Renderer.PushClip(placement.ClipBounds);
            UiOffsetRenderer offsetRenderer = new(context.Renderer, new UiPoint(placement.Bounds.X, placement.Bounds.Y));
            UiRenderContext childContext = new(offsetRenderer, context.DefaultFont);
            if (overlay)
            {
                placement.Element.RenderOverlay(childContext);
            }
            else
            {
                placement.Element.Render(childContext);
            }

            context.Renderer.PopClip();
        }
    }

    private UiElement? HitTestPlacedContent(UiPoint point)
    {
        for (int i = _cellPlacements.Count - 1; i >= 0; i--)
        {
            UiElement? hit = HitTestPlacement(_cellPlacements[i], point);
            if (hit != null)
            {
                return hit;
            }
        }

        for (int i = _headerPlacements.Count - 1; i >= 0; i--)
        {
            UiElement? hit = HitTestPlacement(_headerPlacements[i], point);
            if (hit != null)
            {
                return hit;
            }
        }

        return null;
    }

    private static UiElement? HitTestPlacement(ContentPlacement placement, UiPoint point)
    {
        if (!placement.ClipBounds.Contains(point))
        {
            return null;
        }

        UiPoint local = new(point.X - placement.Bounds.X, point.Y - placement.Bounds.Y);
        return placement.Element.HitTest(local);
    }

    internal void AppendDebugChildren(List<UiElement> children)
    {
        for (int i = 0; i < _headerPlacements.Count; i++)
        {
            UiElement element = _headerPlacements[i].Element;
            if (!children.Contains(element))
            {
                children.Add(element);
            }
        }

        for (int i = 0; i < _cellPlacements.Count; i++)
        {
            UiElement element = _cellPlacements[i].Element;
            if (!children.Contains(element))
            {
                children.Add(element);
            }
        }
    }

    bool IUiDebugBoundsResolver.TryResolveDebugBounds(UiElement element, out UiRect bounds, out UiRect clipBounds)
    {
        for (int i = 0; i < _cellPlacements.Count; i++)
        {
            ContentPlacement placement = _cellPlacements[i];
            if (TryResolvePlacementDebugBounds(placement, element, out bounds, out clipBounds))
            {
                return true;
            }
        }

        for (int i = 0; i < _headerPlacements.Count; i++)
        {
            ContentPlacement placement = _headerPlacements[i];
            if (TryResolvePlacementDebugBounds(placement, element, out bounds, out clipBounds))
            {
                return true;
            }
        }

        bounds = default;
        clipBounds = default;
        return false;
    }

    private static bool TryResolvePlacementDebugBounds(ContentPlacement placement, UiElement target, out UiRect bounds, out UiRect clipBounds)
    {
        return TryResolveNestedDebugBounds(
            placement.Element,
            target,
            placement.Bounds.X - placement.Element.Bounds.X,
            placement.Bounds.Y - placement.Element.Bounds.Y,
            placement.ClipBounds,
            out bounds,
            out clipBounds);
    }

    private static bool TryResolveNestedDebugBounds(
        UiElement current,
        UiElement target,
        int parentAbsoluteOriginX,
        int parentAbsoluteOriginY,
        UiRect inheritedClipBounds,
        out UiRect bounds,
        out UiRect clipBounds)
    {
        UiRect absoluteBounds = TranslateRect(current.Bounds, parentAbsoluteOriginX, parentAbsoluteOriginY);
        UiRect absoluteClipBounds = TranslateRect(current.ClipBounds, parentAbsoluteOriginX, parentAbsoluteOriginY);
        UiRect resolvedClipBounds = IntersectRect(absoluteClipBounds, inheritedClipBounds);

        if (ReferenceEquals(current, target))
        {
            bounds = absoluteBounds;
            clipBounds = resolvedClipBounds;
            return true;
        }

        if (current is IUiDebugBoundsResolver resolver
            && resolver.TryResolveDebugBounds(target, out UiRect nestedBounds, out UiRect nestedClipBounds))
        {
            bounds = TranslateRect(nestedBounds, parentAbsoluteOriginX, parentAbsoluteOriginY);
            clipBounds = IntersectRect(
                TranslateRect(nestedClipBounds, parentAbsoluteOriginX, parentAbsoluteOriginY),
                inheritedClipBounds);
            return true;
        }

        UiRect childInheritedClipBounds = current.ClipChildren ? resolvedClipBounds : inheritedClipBounds;
        IReadOnlyList<UiElement> children = GetDebugChildren(current);
        int childOriginX = absoluteBounds.X;
        int childOriginY = absoluteBounds.Y;
        for (int i = 0; i < children.Count; i++)
        {
            if (TryResolveNestedDebugBounds(
                children[i],
                target,
                childOriginX,
                childOriginY,
                childInheritedClipBounds,
                out bounds,
                out clipBounds))
            {
                return true;
            }
        }

        bounds = default;
        clipBounds = default;
        return false;
    }

    private static IReadOnlyList<UiElement> GetDebugChildren(UiElement element)
    {
        List<UiElement> children = new(element.Children.Count + 4);
        for (int i = 0; i < element.Children.Count; i++)
        {
            children.Add(element.Children[i]);
        }

        if (element is UiTable table)
        {
            table.AppendDebugChildren(children);
        }

        return children;
    }

    private void DrawRows(UiRenderContext context, UiFont font, int textHeight)
    {
        if (_materializedRowStartIndex < 0 || _materializedRowEndIndex < 0)
        {
            return;
        }

        for (int rowIndex = _materializedRowStartIndex; rowIndex <= _materializedRowEndIndex; rowIndex++)
        {
            UiTableRow row = Rows[rowIndex];
            int rowHeight = _rowHeights[rowIndex];
            int rowY = _bodyViewport.Y + _rowTops[rowIndex] - _scrollY;
            UiRect rowRect = new UiRect(_bodyViewport.X, rowY, _bodyViewport.Width, rowHeight);
            UiColor fill = GetRowFill(rowIndex, row);
            if (fill.A > 0)
            {
                context.Renderer.FillRect(rowRect, fill);
            }

            UiColor rowTextColor = GetRowTextColor(rowIndex, row);
            int textY = rowY + (rowHeight - textHeight) / 2;

            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                UiTableColumn column = Columns[modelIndex];
                UiRect columnRect = _visibleColumnRects[slot];
                UiRect cellRect = new UiRect(columnRect.X, rowY, columnRect.Width, rowHeight);
                UiTableCell? cell = row.GetCell(modelIndex);
                UiTableCell effectiveCell = cell ?? new UiTableCell(row.GetCellText(modelIndex));
                UiTableCellContext cellContext = new(
                    this,
                    row,
                    column,
                    effectiveCell,
                    rowIndex,
                    modelIndex,
                    cellRect,
                    IsRowSelected(rowIndex),
                    rowIndex == _hoverIndex);

                UiColor? cellFill = CellBackgroundSelector?.Invoke(cellContext) ?? cell?.Background;
                if (cellFill.HasValue && cellFill.Value.A > 0)
                {
                    context.Renderer.FillRect(cellRect, cellFill.Value);
                }

                if (cell?.Content != null)
                {
                    continue;
                }

                UiColor textColor = CellTextColorSelector?.Invoke(cellContext) ?? cell?.TextColor ?? rowTextColor;
                int padding = cell?.Padding >= 0 ? cell.Padding : CellPadding;
                string drawText = effectiveCell.GetRenderText();
                bool clipText = !string.IsNullOrEmpty(drawText);
                if (clipText)
                {
                    context.Renderer.PushClip(cellRect);
                }

                context.Renderer.DrawText(drawText, new UiPoint(cellRect.X + padding, textY), textColor, TextScale, font);
                if (clipText)
                {
                    context.Renderer.PopClip();
                }
            }
        }
    }

    private void DrawGridLines(UiRenderContext context)
    {
        for (int slot = 0; slot < _visibleColumnRects.Count - 1; slot++)
        {
            UiRect rect = _visibleColumnRects[slot];
            int x = rect.Right;
            int top = ShowHeader ? Bounds.Y : _bodyViewport.Y;
            int height = ShowHeader ? Bounds.Height - (_showHorizontal ? Math.Max(1, ScrollbarThickness) : 0) : _bodyViewport.Height;
            context.Renderer.FillRect(new UiRect(x, top, 1, Math.Max(0, height)), GridColor);
        }

        if (ShowHeader && _headerViewport.Height > 0)
        {
            int y = _headerViewport.Bottom;
            if (y < Bounds.Bottom)
            {
                context.Renderer.FillRect(new UiRect(Bounds.X, y, _headerViewport.Width, 1), GridColor);
            }
        }

        if (_materializedRowStartIndex < 0 || _materializedRowEndIndex < 0)
        {
            return;
        }

        for (int rowIndex = _materializedRowStartIndex; rowIndex <= _materializedRowEndIndex; rowIndex++)
        {
            int y = _bodyViewport.Y + _rowTops[rowIndex] + _rowHeights[rowIndex] - _scrollY;
            if (y >= _bodyViewport.Bottom)
            {
                break;
            }

            context.Renderer.FillRect(new UiRect(_bodyViewport.X, y, _bodyViewport.Width, 1), GridColor);
        }
    }

    private void DrawHeaderText(UiRenderContext context, UiFont font, int headerTextHeight, UiTableColumn column, UiRect cellRect)
    {
        UiColor textColor = column.HeaderTextColor ?? HeaderTextColor;
        bool clipText = UseAngledHeaders || !string.IsNullOrEmpty(column.Header);
        if (clipText)
        {
            context.Renderer.PushClip(cellRect);
        }

        if (UseAngledHeaders)
        {
            DrawAngledHeaderText(context.Renderer, column.Header, cellRect, textColor, HeaderTextScale, font);
        }
        else
        {
            int headerY = cellRect.Y + (cellRect.Height - headerTextHeight) / 2;
            UiPoint textPoint = new(cellRect.X + CellPadding, headerY);
            if (HeaderTextBold)
            {
                UiRenderHelpers.DrawTextBold(context.Renderer, column.Header, textPoint, textColor, HeaderTextScale, font);
            }
            else
            {
                context.Renderer.DrawText(column.Header, textPoint, textColor, HeaderTextScale, font);
            }
        }

        if (clipText)
        {
            context.Renderer.PopClip();
        }
    }

    private int GetHeaderHeight()
    {
        int height = Math.Max(1, HeaderHeight);
        if (UseAngledHeaders)
        {
            height = Math.Max(height, Math.Max(1, AngledHeaderHeight));
        }

        return height;
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

    private void HandleHeaderInteractions(UiUpdateContext context, UiInputState input, UiElement? contentHit, bool pointerHandledByScrollbars)
    {
        int headerModelIndex = GetHeaderColumnModelIndexAtPoint(input.MousePosition);

        if (input.RightClicked && headerModelIndex >= 0 && EnableHeaderContextMenu)
        {
            OpenHeaderContextMenu(input.MousePosition);
            ClearHeaderPressState();
            return;
        }

        if (_resizingColumnModelIndex >= 0)
        {
            if (input.LeftDown)
            {
                ResizeColumn(input.MousePosition.X);
            }

            if (input.LeftReleased || !input.LeftDown)
            {
                _resizingColumnModelIndex = -1;
            }

            return;
        }

        if (pointerHandledByScrollbars || contentHit != null)
        {
            if (input.LeftReleased)
            {
                ClearHeaderPressState();
            }

            return;
        }

        if (input.LeftClicked && headerModelIndex >= 0)
        {
            int resizeModelIndex = GetResizeHandleColumnIndexAtPoint(input.MousePosition);
            if (resizeModelIndex >= 0)
            {
                BeginResize(resizeModelIndex, input.MousePosition.X);
                ClearHeaderPressState();
                return;
            }

            _pressedHeaderModelIndex = headerModelIndex;
            _pressedHeaderPoint = input.MousePosition;
            _pressedHeaderMultiSort = input.ShiftDown || input.CtrlDown;
            context.Focus.RequestFocus(this);
        }

        if (_pressedHeaderModelIndex >= 0 && input.LeftDown && _reorderingColumnModelIndex < 0)
        {
            UiTableColumn pressedColumn = Columns[_pressedHeaderModelIndex];
            if (AllowColumnReorder && pressedColumn.AllowReorder && HasExceededDragThreshold(_pressedHeaderPoint, input.MousePosition, input.DragThreshold))
            {
                BeginReorder(_pressedHeaderModelIndex, input.MousePosition);
            }
        }

        if (_reorderingColumnModelIndex >= 0)
        {
            if (input.LeftDown)
            {
                _reorderInsertDisplayIndex = GetReorderInsertDisplayIndex(input.MousePosition);
            }

            if (input.LeftReleased || !input.LeftDown)
            {
                CommitReorder();
            }

            return;
        }

        if (_pressedHeaderModelIndex >= 0 && input.LeftReleased)
        {
            if (headerModelIndex == _pressedHeaderModelIndex)
            {
                HandleHeaderClick(_pressedHeaderModelIndex, _pressedHeaderMultiSort);
            }

            ClearHeaderPressState();
        }
    }

    private void HandleBodyInteractions(UiUpdateContext context, UiInputState input, UiElement? contentHit, bool pointerHandledByScrollbars)
    {
        bool clicked = input.LeftClicked && Bounds.Contains(input.MousePosition);
        bool doubleClicked = input.LeftDoubleClicked && Bounds.Contains(input.MousePosition);
        if (clicked && !_headerViewport.Contains(input.MousePosition) && !pointerHandledByScrollbars && contentHit == null)
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < Rows.Count)
            {
                ApplySelection(_hoverIndex, input.ShiftDown, input.CtrlDown);
                EnsureVisible(_hoverIndex);
            }
            else if (AllowDeselect)
            {
                if (_selectionModel != null)
                {
                    _selectionModel.Clear(SelectionScope);
                }
                else
                {
                    SetSelectedIndex(-1);
                }
            }
        }

        if (doubleClicked && !_headerViewport.Contains(input.MousePosition) && !pointerHandledByScrollbars && contentHit == null)
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < Rows.Count)
            {
                ApplySelection(_hoverIndex, input.ShiftDown, input.CtrlDown);
                EnsureVisible(_hoverIndex);
                RowActivated?.Invoke(_hoverIndex);
            }
        }
    }

    private void HandleKeyboardNavigation(UiInputState input)
    {
        bool shift = input.ShiftDown;
        bool ctrl = input.CtrlDown;

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

        if (input.Navigation.PageUp)
        {
            MoveSelectionByPage(-1, shift, ctrl);
        }

        if (input.Navigation.PageDown)
        {
            MoveSelectionByPage(1, shift, ctrl);
        }

        if (input.Navigation.Activate)
        {
            int current = SelectedIndex;
            if (current >= 0 && current < Rows.Count)
            {
                RowActivated?.Invoke(current);
            }
        }
    }

    private void MoveSelection(int delta, bool shift, bool ctrl)
    {
        if (Rows.Count == 0)
        {
            if (_selectionModel != null)
            {
                _selectionModel.Clear(SelectionScope);
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
        EnsureVisible(next);
    }

    private void MoveSelectionByPage(int direction, bool shift, bool ctrl)
    {
        if (Rows.Count == 0)
        {
            return;
        }

        int current = SelectedIndex < 0 ? 0 : SelectedIndex;
        int moved = current;
        int remainingHeight = Math.Max(1, _bodyViewport.Height);

        while (remainingHeight > 0)
        {
            int next = direction < 0 ? moved - 1 : moved + 1;
            if (next < 0 || next >= Rows.Count)
            {
                break;
            }

            moved = next;
            remainingHeight -= _rowHeights[next];
        }

        ApplySelection(moved, shift, ctrl);
        EnsureVisible(moved);
    }

    private void ApplySelection(int index, bool shift, bool ctrl)
    {
        if (_selectionModel != null)
        {
            if (index < 0)
            {
                _selectionModel.Clear(SelectionScope);
            }
            else
            {
                _selectionModel.ApplySelection(index, ctrl, shift, SelectionScope);
            }

            return;
        }

        SetSelectedIndex(index);
    }

    private bool IsRowSelected(int index)
    {
        if (_selectionModel != null)
        {
            return _selectionModel.IsSelected(index, SelectionScope);
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
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint);
        SelectionChanged?.Invoke(_selectedIndex);
    }

    private void HandleSelectionModelChanged()
    {
        if (_selectionModel == null)
        {
            return;
        }

        int previous = _selectedIndex;
        _selectedIndex = _selectionModel.GetPrimaryIndex(SelectionScope);
        if (_selectedIndex >= 0)
        {
            EnsureVisible(_selectedIndex);
        }

        if (previous != _selectedIndex)
        {
            Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint);
            SelectionChanged?.Invoke(_selectedIndex);
        }
    }

    private void HandleHeaderClick(int modelIndex, bool multiSortRequested)
    {
        if (!AllowSorting || modelIndex < 0 || modelIndex >= Columns.Count)
        {
            return;
        }

        UiTableColumn column = Columns[modelIndex];
        if (!column.AllowSort)
        {
            return;
        }

        UpdateSortSpecs(modelIndex, multiSortRequested && AllowMultiSort);
    }

    private void UpdateSortSpecs(int columnIndex, bool multiSort)
    {
        UiTableSortSpec? existing = GetSortSpec(columnIndex);
        if (!multiSort)
        {
            _sortSpecs.Clear();
        }

        if (existing != null)
        {
            bool remove = ToggleSortSpec(existing);
            if (remove)
            {
                _sortSpecs.Remove(existing);
            }
            else if (!_sortSpecs.Contains(existing))
            {
                _sortSpecs.Add(existing);
            }
        }
        else
        {
            UiTableColumn column = Columns[columnIndex];
            UiTableSortSpec spec = new(columnIndex, 0, column.DefaultSortDirection, column.UserId);
            _sortSpecs.Add(spec);
        }

        RefreshSortOrder();
        SortSpecsChanged?.Invoke();
    }

    private bool ToggleSortSpec(UiTableSortSpec spec)
    {
        if (spec.Direction == UiTableSortDirection.Ascending)
        {
            spec.Direction = UiTableSortDirection.Descending;
            return false;
        }

        if (AllowNoSort)
        {
            return true;
        }

        spec.Direction = UiTableSortDirection.Ascending;
        return false;
    }

    private void RefreshSortOrder()
    {
        for (int i = 0; i < _sortSpecs.Count; i++)
        {
            _sortSpecs[i].SortOrder = i;
            int index = _sortSpecs[i].ColumnIndex;
            if (index >= 0 && index < Columns.Count)
            {
                _sortSpecs[i].ColumnUserId = Columns[index].UserId;
            }
        }
    }

    private void ValidateSortSpecs()
    {
        if (_sortSpecs.Count == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = _sortSpecs.Count - 1; i >= 0; i--)
        {
            UiTableSortSpec spec = _sortSpecs[i];
            if (spec.ColumnIndex < 0 || spec.ColumnIndex >= Columns.Count)
            {
                _sortSpecs.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            RefreshSortOrder();
            SortSpecsChanged?.Invoke();
        }
    }

    private UiTableSortSpec? GetSortSpec(int columnIndex)
    {
        for (int i = 0; i < _sortSpecs.Count; i++)
        {
            if (_sortSpecs[i].ColumnIndex == columnIndex)
            {
                return _sortSpecs[i];
            }
        }

        return null;
    }

    private void DrawSortIndicator(UiRenderContext context, UiRect cellRect, UiTableSortSpec spec, UiFont font)
    {
        int padding = Math.Max(0, SortIndicatorPadding);
        int textHeight = context.Renderer.MeasureTextHeight(HeaderTextScale, font);
        int size = Math.Min(cellRect.Width, cellRect.Height);
        size = Math.Min(size, Math.Max(4, textHeight));
        if (size <= 0)
        {
            return;
        }

        UiRect arrowRect = new(
            cellRect.Right - padding - size,
            cellRect.Y + (cellRect.Height - size) / 2,
            size,
            size);
        UiArrowDirection direction = spec.Direction == UiTableSortDirection.Ascending ? UiArrowDirection.Up : UiArrowDirection.Down;
        UiArrow.DrawTriangle(context.Renderer, arrowRect, direction, HeaderTextColor);
    }

    private void DrawAngledHeaderText(IUiRenderer renderer, string text, UiRect cellRect, UiColor color, int scale, UiFont font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int safeScale = Math.Max(1, scale);
        int glyphWidth = renderer.MeasureTextWidth("W", safeScale, font);
        int glyphHeight = renderer.MeasureTextHeight(safeScale, font);
        int step = AngledHeaderStep > 0 ? AngledHeaderStep : Math.Max(1, Math.Max(1, glyphWidth) / 2);

        int padding = Math.Max(0, CellPadding);
        int x = cellRect.X + padding;
        int y = cellRect.Bottom - padding - glyphHeight;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (!char.IsWhiteSpace(ch))
            {
                if (HeaderTextBold)
                {
                    UiRenderHelpers.DrawTextBold(renderer, ch.ToString(), new UiPoint(x, y), color, safeScale, font);
                }
                else
                {
                    renderer.DrawText(ch.ToString(), new UiPoint(x, y), color, safeScale, font);
                }
            }

            x += step;
            y -= step;
        }
    }

    private int GetRowIndexAtPoint(UiPoint point)
    {
        if (!_bodyViewport.Contains(point) || _bodyViewport.Height <= 0)
        {
            return -1;
        }

        int localY = point.Y - _bodyViewport.Y + _scrollY;
        if (_uniformRowHeight > 0)
        {
            return _rowClipRange.GetIndexAtOffset(localY);
        }

        for (int i = _visibleRowStartIndex >= 0 ? _visibleRowStartIndex : 0; i < Rows.Count; i++)
        {
            int top = _rowTops[i];
            int bottom = top + _rowHeights[i];
            if (localY >= top && localY < bottom)
            {
                return i;
            }

            if (top > localY)
            {
                break;
            }
        }

        return -1;
    }

    private int GetHeaderColumnModelIndexAtPoint(UiPoint point)
    {
        if (!ShowHeader || !_headerViewport.Contains(point))
        {
            return -1;
        }

        for (int slot = 0; slot < _visibleColumnRects.Count; slot++)
        {
            if (_visibleColumnRects[slot].Contains(point))
            {
                return _visibleColumnModelIndices[slot];
            }
        }

        return -1;
    }

    private int GetResizeHandleColumnIndexAtPoint(UiPoint point)
    {
        if (!AllowColumnResize || !ShowHeader || !_headerViewport.Contains(point))
        {
            return -1;
        }

        int gripWidth = Math.Max(2, ResizeHandleWidth);
        for (int slot = 0; slot < _visibleColumnRects.Count; slot++)
        {
            int modelIndex = _visibleColumnModelIndices[slot];
            UiTableColumn column = Columns[modelIndex];
            if (!column.AllowResize)
            {
                continue;
            }

            UiRect rect = _visibleColumnRects[slot];
            if (point.X >= rect.Right - gripWidth && point.X < rect.Right + gripWidth && point.Y >= rect.Y && point.Y < rect.Bottom)
            {
                return modelIndex;
            }
        }

        return -1;
    }

    private void BeginResize(int modelIndex, int mouseX)
    {
        UiTableColumn column = Columns[modelIndex];
        UiTableColumnState state = _columnStates[modelIndex];
        _resizingColumnModelIndex = modelIndex;
        _resizeStartMouseX = mouseX;
        _resizeStartWidth = Math.Max(column.MinWidth, state.Width > 0 ? state.Width : column.Width);
    }

    private void ResizeColumn(int mouseX)
    {
        if (_resizingColumnModelIndex < 0 || _resizingColumnModelIndex >= Columns.Count)
        {
            return;
        }

        UiTableColumn column = Columns[_resizingColumnModelIndex];
        UiTableColumnState state = _columnStates[_resizingColumnModelIndex];
        int delta = mouseX - _resizeStartMouseX;
        int width = Math.Max(column.MinWidth, _resizeStartWidth + delta);
        if (state.Width == width && state.WidthMode == UiTableColumnWidthMode.Fixed)
        {
            return;
        }

        state.WidthMode = UiTableColumnWidthMode.Fixed;
        state.Width = width;
        OnColumnStatesChanged();
    }

    private void BeginReorder(int modelIndex, UiPoint point)
    {
        _reorderingColumnModelIndex = modelIndex;
        _reorderInsertDisplayIndex = GetReorderInsertDisplayIndex(point);
    }

    private int GetReorderInsertDisplayIndex(UiPoint point)
    {
        if (_visibleColumnModelIndices.Count == 0)
        {
            return 0;
        }

        int contentX = point.X - _headerViewport.X + _scrollX;
        int running = 0;
        for (int slot = 0; slot < _visibleColumnWidths.Count; slot++)
        {
            int width = _visibleColumnWidths[slot];
            if (contentX < running + width / 2)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                return _columnStates[modelIndex].DisplayIndex;
            }

            running += width;
        }

        return _columnStates[_visibleColumnModelIndices[^1]].DisplayIndex + 1;
    }

    private void CommitReorder()
    {
        if (_reorderingColumnModelIndex < 0)
        {
            return;
        }

        UiTableColumnState state = _columnStates[_reorderingColumnModelIndex];
        int sourceDisplayIndex = state.DisplayIndex;
        int targetDisplayIndex = _reorderInsertDisplayIndex;
        if (targetDisplayIndex > sourceDisplayIndex)
        {
            targetDisplayIndex--;
        }

        targetDisplayIndex = Math.Clamp(targetDisplayIndex, 0, Math.Max(0, _columnStates.Count - 1));
        if (targetDisplayIndex != sourceDisplayIndex)
        {
            for (int i = 0; i < _columnStates.Count; i++)
            {
                if (i == _reorderingColumnModelIndex)
                {
                    continue;
                }

                UiTableColumnState other = _columnStates[i];
                if (targetDisplayIndex < sourceDisplayIndex)
                {
                    if (other.DisplayIndex >= targetDisplayIndex && other.DisplayIndex < sourceDisplayIndex)
                    {
                        other.DisplayIndex++;
                    }
                }
                else if (other.DisplayIndex <= targetDisplayIndex && other.DisplayIndex > sourceDisplayIndex)
                {
                    other.DisplayIndex--;
                }
            }

            state.DisplayIndex = targetDisplayIndex;
            NormalizeColumnStates();
            OnColumnStatesChanged();
        }

        _reorderingColumnModelIndex = -1;
        _reorderInsertDisplayIndex = -1;
        ClearHeaderPressState();
    }

    private void ClearHeaderPressState()
    {
        _pressedHeaderModelIndex = -1;
        _pressedHeaderPoint = default;
        _pressedHeaderMultiSort = false;
    }

    private void EnsureVisible(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count || _bodyViewport.Height <= 0)
        {
            return;
        }

        if (_uniformRowHeight > 0)
        {
            _scrollY = UiClipper.EnsureVisible(Rows.Count, _uniformRowHeight, _bodyViewport.Height, _scrollY, rowIndex);
            return;
        }

        int top = _rowTops[rowIndex];
        int bottom = top + _rowHeights[rowIndex];
        int viewTop = _scrollY;
        int viewBottom = _scrollY + _bodyViewport.Height;

        if (top < viewTop)
        {
            _scrollY = top;
        }
        else if (bottom > viewBottom)
        {
            _scrollY = Math.Max(0, bottom - _bodyViewport.Height);
        }
    }

    private void HandleWheelScrolling(UiInputState input)
    {
        bool mouseInBody = _bodyViewport.Contains(input.MousePosition);
        bool mouseInHeader = _headerViewport.Contains(input.MousePosition);
        if (!mouseInBody && !mouseInHeader)
        {
            return;
        }

        if (input.ScrollDelta != 0)
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                if (_showVertical)
                {
                    _scrollY -= steps * ScrollWheelStep;
                }
                else if (_showHorizontal)
                {
                    _scrollX -= steps * ScrollWheelStep;
                }
            }
        }

        if (input.ScrollDeltaX != 0 && _showHorizontal)
        {
            int steps = (int)Math.Round(input.ScrollDeltaX / 120f);
            if (steps != 0)
            {
                _scrollX -= steps * ScrollWheelStep;
            }
        }
    }

    private bool UpdateScrollbars(UiInputState input, bool pointerBlockedByMenu)
    {
        bool handled = false;
        bool mouseInScrollbar = false;

        if (_showVertical)
        {
            UiRect verticalBar = GetVerticalScrollbarBounds();
            mouseInScrollbar |= verticalBar.Contains(input.MousePosition);
            handled |= UpdateVerticalScrollbar(input, verticalBar, pointerBlockedByMenu);
        }

        if (_showHorizontal)
        {
            UiRect horizontalBar = GetHorizontalScrollbarBounds();
            mouseInScrollbar |= horizontalBar.Contains(input.MousePosition);
            handled |= UpdateHorizontalScrollbar(input, horizontalBar, pointerBlockedByMenu);
        }

        return handled || mouseInScrollbar;
    }

    private UiRect GetVerticalScrollbarBounds()
    {
        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.Right - thickness, _bodyViewport.Y, thickness, _bodyViewport.Height);
    }

    private UiRect GetHorizontalScrollbarBounds()
    {
        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.X, Bounds.Bottom - thickness, _bodyViewport.Width, thickness);
    }

    private UiRect GetVerticalThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackHeight = Math.Max(0, bar.Height - padding * 2);
        int trackTop = bar.Y + padding;
        int scrollRange = Math.Max(0, _contentSize.Y - _viewportSize.Y);

        int thumbHeight = trackHeight;
        if (scrollRange > 0 && _contentSize.Y > 0)
        {
            float ratio = _viewportSize.Y / (float)_contentSize.Y;
            thumbHeight = Math.Max(MinThumbSize, (int)Math.Round(trackHeight * ratio));
        }

        int travel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = trackTop;
        if (scrollRange > 0 && travel > 0)
        {
            float t = _scrollY / (float)scrollRange;
            thumbY = trackTop + (int)Math.Round(travel * t);
        }

        return new UiRect(bar.X + padding, thumbY, Math.Max(1, bar.Width - padding * 2), thumbHeight);
    }

    private UiRect GetHorizontalThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackWidth = Math.Max(0, bar.Width - padding * 2);
        int trackLeft = bar.X + padding;
        int scrollRange = Math.Max(0, _contentSize.X - _viewportSize.X);

        int thumbWidth = trackWidth;
        if (scrollRange > 0 && _contentSize.X > 0)
        {
            float ratio = _viewportSize.X / (float)_contentSize.X;
            thumbWidth = Math.Max(MinThumbSize, (int)Math.Round(trackWidth * ratio));
        }

        int travel = Math.Max(0, trackWidth - thumbWidth);
        int thumbX = trackLeft;
        if (scrollRange > 0 && travel > 0)
        {
            float t = _scrollX / (float)scrollRange;
            thumbX = trackLeft + (int)Math.Round(travel * t);
        }

        return new UiRect(thumbX, bar.Y + padding, thumbWidth, Math.Max(1, bar.Height - padding * 2));
    }

    private bool UpdateVerticalScrollbar(UiInputState input, UiRect bar, bool pointerBlockedByMenu)
    {
        UiRect thumb = GetVerticalThumbBounds(bar);
        _hoverVerticalThumb = thumb.Contains(input.MousePosition);

        if (pointerBlockedByMenu)
        {
            return false;
        }

        if (!_draggingVertical && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverVerticalThumb)
            {
                _draggingVertical = true;
                _dragStartMouse = input.MousePosition.Y;
                _dragStartScroll = _scrollY;
            }
            else
            {
                PageVertical(input.MousePosition.Y < thumb.Y);
            }

            return true;
        }

        if (_draggingVertical && input.LeftDown)
        {
            int trackHeight = Math.Max(1, bar.Height - ScrollbarPadding * 2);
            int scrollRange = Math.Max(0, _contentSize.Y - _viewportSize.Y);
            int thumbHeight = Math.Max(1, thumb.Height);
            int travel = Math.Max(1, trackHeight - thumbHeight);
            int delta = input.MousePosition.Y - _dragStartMouse;
            float ratio = delta / (float)travel;
            _scrollY = _dragStartScroll + (int)Math.Round(scrollRange * ratio);
            return true;
        }

        if (_draggingVertical && (input.LeftReleased || !input.LeftDown))
        {
            _draggingVertical = false;
            return true;
        }

        return bar.Contains(input.MousePosition);
    }

    private bool UpdateHorizontalScrollbar(UiInputState input, UiRect bar, bool pointerBlockedByMenu)
    {
        UiRect thumb = GetHorizontalThumbBounds(bar);
        _hoverHorizontalThumb = thumb.Contains(input.MousePosition);

        if (pointerBlockedByMenu)
        {
            return false;
        }

        if (!_draggingHorizontal && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverHorizontalThumb)
            {
                _draggingHorizontal = true;
                _dragStartMouse = input.MousePosition.X;
                _dragStartScroll = _scrollX;
            }
            else
            {
                PageHorizontal(input.MousePosition.X < thumb.X);
            }

            return true;
        }

        if (_draggingHorizontal && input.LeftDown)
        {
            int trackWidth = Math.Max(1, bar.Width - ScrollbarPadding * 2);
            int scrollRange = Math.Max(0, _contentSize.X - _viewportSize.X);
            int thumbWidth = Math.Max(1, thumb.Width);
            int travel = Math.Max(1, trackWidth - thumbWidth);
            int delta = input.MousePosition.X - _dragStartMouse;
            float ratio = delta / (float)travel;
            _scrollX = _dragStartScroll + (int)Math.Round(scrollRange * ratio);
            return true;
        }

        if (_draggingHorizontal && (input.LeftReleased || !input.LeftDown))
        {
            _draggingHorizontal = false;
            return true;
        }

        return bar.Contains(input.MousePosition);
    }

    private void PageVertical(bool up)
    {
        if (_viewportSize.Y <= 0)
        {
            return;
        }

        _scrollY += up ? -_viewportSize.Y : _viewportSize.Y;
    }

    private void PageHorizontal(bool left)
    {
        if (_viewportSize.X <= 0)
        {
            return;
        }

        _scrollX += left ? -_viewportSize.X : _viewportSize.X;
    }

    private void DrawScrollbars(UiRenderContext context)
    {
        if (_showVertical)
        {
            UiRect bar = GetVerticalScrollbarBounds();
            context.Renderer.FillRect(bar, ScrollbarTrack);
            context.Renderer.DrawRect(bar, ScrollbarBorder, 1);

            UiRect thumb = GetVerticalThumbBounds(bar);
            UiColor thumbColor = (_hoverVerticalThumb || _draggingVertical) ? ScrollbarThumbHover : ScrollbarThumb;
            context.Renderer.FillRect(thumb, thumbColor);
        }

        if (_showHorizontal)
        {
            UiRect bar = GetHorizontalScrollbarBounds();
            context.Renderer.FillRect(bar, ScrollbarTrack);
            context.Renderer.DrawRect(bar, ScrollbarBorder, 1);

            UiRect thumb = GetHorizontalThumbBounds(bar);
            UiColor thumbColor = (_hoverHorizontalThumb || _draggingHorizontal) ? ScrollbarThumbHover : ScrollbarThumb;
            context.Renderer.FillRect(thumb, thumbColor);
        }

        if (_showVertical && _showHorizontal)
        {
            int thickness = Math.Max(1, ScrollbarThickness);
            UiRect corner = new UiRect(Bounds.Right - thickness, Bounds.Bottom - thickness, thickness, thickness);
            context.Renderer.FillRect(corner, ScrollbarTrack);
            context.Renderer.DrawRect(corner, ScrollbarBorder, 1);
        }
    }

    private void OpenHeaderContextMenu(UiPoint point)
    {
        int itemCount = Columns.Count + 1;
        int width = Math.Max(120, HeaderContextMenuWidth);
        int height = Math.Max(0, itemCount * Math.Max(1, HeaderContextMenuItemHeight));
        UiRect bounds = UiPopupLayout.BuildContextBounds(point, new UiPoint(width, height));
        _headerContextMenuBounds = UiPopupLayout.Clamp(this, bounds);
        _headerContextMenuOpen = true;
        _suppressHeaderMenuClick = true;
        _hoverHeaderMenuEntry = -1;
    }

    private bool UpdateHeaderContextMenu(UiInputState input)
    {
        if (!_headerContextMenuOpen)
        {
            return false;
        }

        if (input.Navigation.Escape)
        {
            _headerContextMenuOpen = false;
            return true;
        }

        if (_suppressHeaderMenuClick)
        {
            _suppressHeaderMenuClick = false;
            return _headerContextMenuBounds.Contains(input.MousePosition);
        }

        _hoverHeaderMenuEntry = GetHeaderMenuEntryAtPoint(input.MousePosition);
        bool mouseInside = _headerContextMenuBounds.Contains(input.MousePosition);

        if (input.LeftClicked)
        {
            if (_hoverHeaderMenuEntry == 0)
            {
                ResetColumnStates();
            }
            else if (_hoverHeaderMenuEntry > 0)
            {
                ToggleHeaderMenuColumn(_hoverHeaderMenuEntry - 1);
            }

            _headerContextMenuOpen = false;
            return true;
        }

        if (input.RightClicked && !mouseInside)
        {
            _headerContextMenuOpen = false;
            return true;
        }

        return mouseInside;
    }

    private void ToggleHeaderMenuColumn(int orderedColumnIndex)
    {
        int[] orderedColumns = GetAllColumnIndicesByDisplayOrder();
        if (orderedColumnIndex < 0 || orderedColumnIndex >= orderedColumns.Length)
        {
            return;
        }

        int modelIndex = orderedColumns[orderedColumnIndex];
        UiTableColumn column = Columns[modelIndex];
        if (!AllowColumnHiding || !column.AllowHide)
        {
            return;
        }

        int visibleCount = 0;
        for (int i = 0; i < _columnStates.Count; i++)
        {
            if (_columnStates[i].Visible)
            {
                visibleCount++;
            }
        }

        UiTableColumnState state = _columnStates[modelIndex];
        if (state.Visible && visibleCount <= 1)
        {
            return;
        }

        state.Visible = !state.Visible;
        OnColumnStatesChanged();
    }

    private int GetHeaderMenuEntryAtPoint(UiPoint point)
    {
        if (!_headerContextMenuBounds.Contains(point))
        {
            return -1;
        }

        int itemHeight = Math.Max(1, HeaderContextMenuItemHeight);
        int localY = point.Y - _headerContextMenuBounds.Y;
        int index = localY / itemHeight;
        return index >= 0 && index < Columns.Count + 1 ? index : -1;
    }

    private void DrawHeaderContextMenu(UiRenderContext context)
    {
        context.Renderer.FillRect(_headerContextMenuBounds, HeaderMenuBackground);
        context.Renderer.DrawRect(_headerContextMenuBounds, HeaderMenuBorder, 1);

        int itemHeight = Math.Max(1, HeaderContextMenuItemHeight);
        int[] orderedColumns = GetAllColumnIndicesByDisplayOrder();
        UiFont font = ResolveFont(context.DefaultFont);
        int textHeight = context.Renderer.MeasureTextHeight(HeaderTextScale, font);

        for (int entry = 0; entry < orderedColumns.Length + 1; entry++)
        {
            UiRect itemRect = new(
                _headerContextMenuBounds.X,
                _headerContextMenuBounds.Y + entry * itemHeight,
                _headerContextMenuBounds.Width,
                itemHeight);
            if (entry == _hoverHeaderMenuEntry)
            {
                context.Renderer.FillRect(itemRect, HeaderMenuHoverBackground);
            }

            string text;
            bool isChecked = false;
            if (entry == 0)
            {
                text = "Reset Columns";
            }
            else
            {
                int modelIndex = orderedColumns[entry - 1];
                text = Columns[modelIndex].Header;
                isChecked = _columnStates[modelIndex].Visible;
            }

            int textY = itemRect.Y + (itemRect.Height - textHeight) / 2;
            int textX = itemRect.X + 10;
            if (entry > 0)
            {
                UiRect checkRect = new(itemRect.X + 10, itemRect.Y + (itemRect.Height - 12) / 2, 12, 12);
                context.Renderer.DrawRect(checkRect, HeaderMenuBorder, 1);
                if (isChecked)
                {
                    UiRect fill = new(checkRect.X + 2, checkRect.Y + 2, Math.Max(0, checkRect.Width - 4), Math.Max(0, checkRect.Height - 4));
                    context.Renderer.FillRect(fill, HeaderMenuCheckColor);
                }

                textX = checkRect.Right + 8;
            }

            context.Renderer.DrawText(text, new UiPoint(textX, textY), HeaderMenuTextColor, HeaderTextScale, font);
        }
    }

    private int[] GetAllColumnIndicesByDisplayOrder()
    {
        int[] ordered = new int[_columnStates.Count];
        for (int i = 0; i < ordered.Length; i++)
        {
            ordered[i] = i;
        }

        Array.Sort(ordered, (left, right) =>
        {
            int order = _columnStates[left].DisplayIndex.CompareTo(_columnStates[right].DisplayIndex);
            return order != 0 ? order : left.CompareTo(right);
        });
        return ordered;
    }

    private void DrawReorderIndicator(UiRenderContext context)
    {
        int x;
        if (_visibleColumnRects.Count == 0)
        {
            x = _headerViewport.X;
        }
        else
        {
            x = _headerViewport.X;
            bool placed = false;
            for (int slot = 0; slot < _visibleColumnModelIndices.Count; slot++)
            {
                int modelIndex = _visibleColumnModelIndices[slot];
                if (_columnStates[modelIndex].DisplayIndex >= _reorderInsertDisplayIndex)
                {
                    x = _visibleColumnRects[slot].X;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                x = _visibleColumnRects[^1].Right;
            }
        }

        int thickness = Math.Max(1, ReorderIndicatorThickness);
        context.Renderer.FillRect(new UiRect(x - thickness / 2, Bounds.Y, thickness, Math.Max(0, _headerViewport.Height + _bodyViewport.Height)), ReorderIndicatorColor);
    }

    private void OnColumnStatesChanged()
    {
        NormalizeColumnStates();
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State | UiInvalidationReason.Clip);
        ColumnStatesChanged?.Invoke();
    }

    private static UiRect InsetRect(UiRect rect, int amount)
    {
        int inset = Math.Max(0, amount);
        return new UiRect(
            rect.X + inset,
            rect.Y + inset,
            Math.Max(0, rect.Width - inset * 2),
            Math.Max(0, rect.Height - inset * 2));
    }

    private static UiRect IntersectRect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        return right <= left || bottom <= top
            ? new UiRect(0, 0, 0, 0)
            : new UiRect(left, top, right - left, bottom - top);
    }

    private static UiRect TranslateRect(UiRect rect, int offsetX, int offsetY)
    {
        return new UiRect(
            rect.X + offsetX,
            rect.Y + offsetY,
            rect.Width,
            rect.Height);
    }

    private static bool HasExceededDragThreshold(UiPoint start, UiPoint current, int threshold)
    {
        int dx = current.X - start.X;
        int dy = current.Y - start.Y;
        int safeThreshold = Math.Max(0, threshold);
        return dx * dx + dy * dy >= safeThreshold * safeThreshold;
    }
}
