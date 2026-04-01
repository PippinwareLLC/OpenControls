namespace OpenControls.Controls;

public sealed class UiTreeView : UiElement
{
    private readonly struct VisibleRow
    {
        public VisibleRow(UiTreeViewItem item, int depth)
        {
            Item = item;
            Depth = depth;
        }

        public UiTreeViewItem Item { get; }
        public int Depth { get; }
    }

    private readonly List<VisibleRow> _visibleRows = new();
    private readonly int[] _singleSelection = new int[1];
    private UiSelectionModel? _selectionModel;
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
    private int _scrollOffset;
    private UiClipRange _clipRange;
    private string _selectionScope = string.Empty;
    private bool _showVerticalScrollbar;
    private bool _draggingVerticalScrollbar;
    private bool _hoverVerticalScrollbarThumb;
    private int _scrollbarDragStartMouseY;
    private int _scrollbarDragStartScrollOffset;

    public List<UiTreeViewItem> RootItems { get; } = new();

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
                _selectionModel.SetItemCount(_visibleRows.Count, SelectionScope);
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

    public UiTreeViewItem? SelectedItem
    {
        get
        {
            int index = SelectedIndex;
            return index >= 0 && index < _visibleRows.Count ? _visibleRows[index].Item : null;
        }
    }

    public int VisibleItemCount => _visibleRows.Count;
    public int FirstVisibleIndex => _clipRange.FirstVisibleIndex;
    public int LastVisibleIndex => _clipRange.LastVisibleIndex;

    public int RowHitTest(UiPoint point) => GetIndexAtPoint(point);

    public bool TryGetVisibleRowBounds(int index, out UiRect bounds)
    {
        if (index < 0 || index >= _visibleRows.Count)
        {
            bounds = default;
            return false;
        }

        int itemHeight = Math.Max(1, ItemHeight);
        int y = Bounds.Y + _clipRange.GetItemStart(index) - _clipRange.ViewportStart;
        bounds = new UiRect(Bounds.X, y, Bounds.Width, itemHeight);
        return true;
    }

    public bool TryGetVisibleRowBounds(UiTreeViewItem item, out UiRect bounds)
    {
        ArgumentNullException.ThrowIfNull(item);
        return TryGetVisibleRowBounds(IndexOfVisibleItem(item), out bounds);
    }

    public bool TryGetDebugHoveredRowBounds(out UiRect bounds)
    {
        return TryGetVisibleRowBounds(_hoverIndex, out bounds);
    }

    public bool TryGetDebugSelectedRowBounds(out UiRect bounds)
    {
        return TryGetVisibleRowBounds(SelectedIndex, out bounds);
    }

    public int GetVisibleItemDepth(int index)
    {
        if (index < 0 || index >= _visibleRows.Count)
        {
            return -1;
        }

        return _visibleRows[index].Depth;
    }

    public int ScrollOffset
    {
        get => _scrollOffset;
        set => _scrollOffset = Math.Max(0, value);
    }

    public int ItemHeight { get; set; } = 22;
    public int IndentWidth { get; set; } = 16;
    public int ArrowSize { get; set; } = 8;
    public int ArrowPadding { get; set; } = 4;
    public int Padding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public int ScrollWheelItems { get; set; } = 3;
    public int OverscanItems { get; set; } = 1;
    public UiScrollbarVisibility VerticalScrollbar { get; set; } = UiScrollbarVisibility.Auto;
    public int ScrollbarThickness { get; set; } = 12;
    public int ScrollbarPadding { get; set; } = 2;
    public int MinThumbSize { get; set; } = 12;
    public bool ShowHierarchyLines { get; set; } = true;
    public UiColor HierarchyLineColor { get; set; } = new UiColor(76, 88, 112);
    public int HierarchyLineThickness { get; set; } = 1;
    public bool AllowDeselect { get; set; }
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
            RefreshVisibleRows();
            HandleSelectionModelChanged();
        }
    }
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor HoverColor { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedColor { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public UiColor ArrowColor { get; set; } = UiColor.White;
    public UiColor ScrollbarTrack { get; set; } = new UiColor(20, 24, 34);
    public UiColor ScrollbarBorder { get; set; } = new UiColor(60, 70, 90);
    public UiColor ScrollbarThumb { get; set; } = new UiColor(70, 80, 100);
    public UiColor ScrollbarThumbHover { get; set; } = new UiColor(90, 110, 140);
    public int CornerRadius { get; set; }

    public event Action<int>? SelectionChanged;
    public event Action<UiTreeViewItem, bool>? ItemToggled;

    public override bool IsFocusable => true;

    public void ExpandAll()
    {
        if (SetOpenRecursive(RootItems, true))
        {
            RefreshVisibleRows();
        }
    }

    public void CollapseAll()
    {
        if (SetOpenRecursive(RootItems, false))
        {
            RefreshVisibleRows();
        }
    }

    public void SetOpen(UiTreeViewItem item, bool isOpen, bool includeDescendants = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        bool changed = includeDescendants
            ? SetOpenRecursive(item, isOpen)
            : SetItemOpen(item, isOpen);

        if (changed)
        {
            RefreshVisibleRows();
        }
    }

    public void ToggleOpen(UiTreeViewItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        SetOpen(item, !item.IsOpen);
    }

    public bool RevealItem(UiTreeViewItem item, bool select = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        List<UiTreeViewItem> path = new();
        if (!TryFindPath(RootItems, item, path))
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < path.Count - 1; i++)
        {
            changed |= SetItemOpen(path[i], true);
        }

        if (changed)
        {
            RefreshVisibleRows();
        }
        else
        {
            RebuildVisibleRows();
        }

        int index = IndexOfVisibleItem(item);
        if (index < 0)
        {
            return false;
        }

        EnsureVisible(index);
        if (select)
        {
            ApplySelection(index, shift: false, ctrl: false);
        }

        return true;
    }

    public int IndexOfVisibleItem(UiTreeViewItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        for (int i = 0; i < _visibleRows.Count; i++)
        {
            if (ReferenceEquals(_visibleRows[i].Item, item))
            {
                return i;
            }
        }

        return -1;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        RefreshVisibleRows();

        UiInputState input = context.GetInputFor(this);
        ResolveScrollbars();

        int itemHeight = Math.Max(1, ItemHeight);
        int viewportHeight = Math.Max(0, Bounds.Height);
        UiRect viewport = GetViewportBounds();
        bool mouseInViewport = viewport.Contains(input.MousePosition);
        bool mouseInScrollbar = false;
        if (_showVerticalScrollbar)
        {
            UiRect verticalBar = GetVerticalScrollbarBounds();
            mouseInScrollbar = verticalBar.Contains(input.MousePosition);
            UpdateVerticalScrollbar(input, verticalBar);
        }

        if (input.ScrollDelta != 0 && (mouseInViewport || mouseInScrollbar))
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                _scrollOffset -= steps * Math.Max(1, ScrollWheelItems) * itemHeight;
            }
        }

        _scrollOffset = UiClipper.ClampScrollOffset(_visibleRows.Count, itemHeight, viewportHeight, _scrollOffset);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
        _hoverIndex = mouseInScrollbar ? -1 : GetIndexAtPoint(input.MousePosition);

        if (input.LeftClicked && Bounds.Contains(input.MousePosition) && !mouseInScrollbar)
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < _visibleRows.Count)
            {
                VisibleRow hoveredRow = _visibleRows[_hoverIndex];
                if (hoveredRow.Item.HasChildren && GetArrowBounds(_hoverIndex).Contains(input.MousePosition))
                {
                    SetItemOpen(hoveredRow.Item, !hoveredRow.Item.IsOpen);
                    RefreshVisibleRows();
                    EnsureVisible(Math.Min(_hoverIndex, Math.Max(0, _visibleRows.Count - 1)));
                }
                else
                {
                    ApplySelection(_hoverIndex, input.ShiftDown, input.CtrlDown);
                }
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

        if (_focused)
        {
            HandleKeyboardNavigation(input);
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        EnsureRenderState();
        ResolveScrollbars();

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        if (!_clipRange.HasMaterializedItems)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        int itemHeight = Math.Max(1, ItemHeight);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
        UiRect viewport = GetViewportBounds();

        context.Renderer.PushClip(viewport);
        int firstIndex = Math.Max(0, _clipRange.FirstMaterializedIndex);
        int lastIndex = Math.Min(_visibleRows.Count - 1, _clipRange.LastMaterializedIndex);
        for (int index = firstIndex; index <= lastIndex; index++)
        {
            VisibleRow row = _visibleRows[index];
            int y = Bounds.Y + _clipRange.GetItemStart(index) - _clipRange.ViewportStart;
            UiRect rowRect = new(Bounds.X, y, viewport.Width, itemHeight);

            if (IsItemSelected(index))
            {
                context.Renderer.FillRect(rowRect, SelectedColor);
            }
            else if (index == _hoverIndex)
            {
                context.Renderer.FillRect(rowRect, HoverColor);
            }

            if (ShowHierarchyLines)
            {
                DrawHierarchyLines(context, index, rowRect);
            }

            UiRect arrowBounds = GetArrowBounds(index);
            if (row.Item.HasChildren)
            {
                UiArrowDirection direction = row.Item.IsOpen ? UiArrowDirection.Down : UiArrowDirection.Right;
                UiArrow.DrawTriangle(context.Renderer, arrowBounds, direction, ArrowColor);
            }

            int textX = arrowBounds.Right + Math.Max(0, ArrowPadding) + Math.Max(0, row.Item.ExtraTextOffset);
            int textY = y + (itemHeight - textHeight) / 2;
            UiColor textColor = row.Item.TextColor ?? (IsItemSelected(index) ? SelectedTextColor : TextColor);
            context.Renderer.DrawText(row.Item.Text, new UiPoint(textX, textY), textColor, TextScale, font);
        }

        context.Renderer.PopClip();
        DrawScrollbars(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
    }

    public UiTreeViewItem? GetVisibleItem(int index)
    {
        return index >= 0 && index < _visibleRows.Count ? _visibleRows[index].Item : null;
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
            ApplySelection(_visibleRows.Count > 0 ? 0 : -1, shift, ctrl);
        }

        if (input.Navigation.End)
        {
            ApplySelection(_visibleRows.Count > 0 ? _visibleRows.Count - 1 : -1, shift, ctrl);
        }

        if (input.Navigation.PageUp)
        {
            MoveSelectionByPage(-1, shift, ctrl);
        }

        if (input.Navigation.PageDown)
        {
            MoveSelectionByPage(1, shift, ctrl);
        }

        if (input.Navigation.MoveLeft)
        {
            HandleMoveLeft(shift, ctrl);
        }

        if (input.Navigation.MoveRight)
        {
            HandleMoveRight(shift, ctrl);
        }

        if (input.Navigation.Activate)
        {
            int current = SelectedIndex;
            if (current >= 0 && current < _visibleRows.Count)
            {
                UiTreeViewItem item = _visibleRows[current].Item;
                if (item.HasChildren)
                {
                    SetItemOpen(item, !item.IsOpen);
                    RefreshVisibleRows();
                    EnsureVisible(Math.Min(current, Math.Max(0, _visibleRows.Count - 1)));
                }
            }
        }
    }

    private void HandleMoveLeft(bool shift, bool ctrl)
    {
        int current = SelectedIndex;
        if (current < 0 || current >= _visibleRows.Count)
        {
            return;
        }

        VisibleRow row = _visibleRows[current];
        if (row.Item.HasChildren && row.Item.IsOpen)
        {
            SetItemOpen(row.Item, false);
            RefreshVisibleRows();
            EnsureVisible(Math.Min(current, Math.Max(0, _visibleRows.Count - 1)));
            return;
        }

        int parentIndex = FindParentIndex(current);
        if (parentIndex >= 0)
        {
            ApplySelection(parentIndex, shift, ctrl);
        }
    }

    private void HandleMoveRight(bool shift, bool ctrl)
    {
        int current = SelectedIndex;
        if (current < 0 || current >= _visibleRows.Count)
        {
            return;
        }

        VisibleRow row = _visibleRows[current];
        if (!row.Item.HasChildren)
        {
            return;
        }

        if (!row.Item.IsOpen)
        {
            SetItemOpen(row.Item, true);
            RefreshVisibleRows();
            EnsureVisible(current);
            return;
        }

        int childIndex = current + 1;
        if (childIndex < _visibleRows.Count && _visibleRows[childIndex].Depth == row.Depth + 1)
        {
            ApplySelection(childIndex, shift, ctrl);
        }
    }

    private void MoveSelection(int delta, bool shift, bool ctrl)
    {
        if (_visibleRows.Count == 0)
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
        next = Math.Clamp(next, 0, _visibleRows.Count - 1);
        ApplySelection(next, shift, ctrl);
    }

    private void MoveSelectionByPage(int direction, bool shift, bool ctrl)
    {
        if (_visibleRows.Count == 0)
        {
            return;
        }

        int pageSize = Math.Max(1, Math.Max(0, Bounds.Height) / Math.Max(1, ItemHeight));
        int current = SelectedIndex < 0 ? 0 : SelectedIndex;
        int next = current + direction * pageSize;
        next = Math.Clamp(next, 0, _visibleRows.Count - 1);
        ApplySelection(next, shift, ctrl);
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

            int primary = _selectionModel.GetPrimaryIndex(SelectionScope);
            if (primary >= 0)
            {
                EnsureVisible(primary);
            }

            return;
        }

        SetSelectedIndex(index);
    }

    private bool IsItemSelected(int index)
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
        if (_visibleRows.Count == 0)
        {
            clamped = -1;
        }
        else
        {
            clamped = Math.Clamp(index, -1, _visibleRows.Count - 1);
        }

        if (_selectedIndex == clamped)
        {
            return;
        }

        _selectedIndex = clamped;
        if (_selectedIndex >= 0)
        {
            EnsureVisible(_selectedIndex);
        }

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
            SelectionChanged?.Invoke(_selectedIndex);
        }
    }

    private void EnsureVisible(int index)
    {
        int viewportHeight = Math.Max(0, Bounds.Height);
        if (viewportHeight <= 0)
        {
            return;
        }

        _scrollOffset = UiClipper.EnsureVisible(_visibleRows.Count, Math.Max(1, ItemHeight), viewportHeight, _scrollOffset, index);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, Math.Max(1, ItemHeight), _scrollOffset, viewportHeight, OverscanItems);
    }

    private int GetIndexAtPoint(UiPoint point)
    {
        if (!Bounds.Contains(point))
        {
            return -1;
        }

        if (_showVerticalScrollbar && GetVerticalScrollbarBounds().Contains(point))
        {
            return -1;
        }

        int contentY = point.Y - Bounds.Y + _scrollOffset;
        return _clipRange.GetIndexAtOffset(contentY);
    }

    private UiRect GetArrowBounds(int index)
    {
        if (index < 0 || index >= _visibleRows.Count)
        {
            return new UiRect(Bounds.X, Bounds.Y, 0, 0);
        }

        VisibleRow row = _visibleRows[index];
        int itemHeight = Math.Max(1, ItemHeight);
        int size = Math.Max(4, ArrowSize);
        int x = Bounds.X + Math.Max(0, Padding) + row.Depth * Math.Max(1, IndentWidth);
        int y = Bounds.Y + _clipRange.GetItemStart(index) - _clipRange.ViewportStart + (itemHeight - size) / 2;
        return new UiRect(x, y, size, size);
    }

    private int FindParentIndex(int index)
    {
        if (index <= 0 || index >= _visibleRows.Count)
        {
            return -1;
        }

        int depth = _visibleRows[index].Depth;
        for (int i = index - 1; i >= 0; i--)
        {
            if (_visibleRows[i].Depth < depth)
            {
                return i;
            }
        }

        return -1;
    }

    private void RebuildVisibleRows()
    {
        _visibleRows.Clear();
        for (int i = 0; i < RootItems.Count; i++)
        {
            AddVisibleRows(RootItems[i], 0);
        }
    }

    private void RefreshVisibleRows()
    {
        RebuildVisibleRows();
        _selectionModel?.SetItemCount(_visibleRows.Count, SelectionScope);
        int itemHeight = Math.Max(1, ItemHeight);
        int viewportHeight = Math.Max(0, Bounds.Height);
        _scrollOffset = UiClipper.ClampScrollOffset(_visibleRows.Count, itemHeight, viewportHeight, _scrollOffset);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
        ResolveScrollbars();
    }

    private void EnsureRenderState()
    {
        int itemHeight = Math.Max(1, ItemHeight);
        int viewportHeight = Math.Max(0, Bounds.Height);

        if ((_visibleRows.Count == 0 && RootItems.Count > 0) || _clipRange.ItemCount != _visibleRows.Count)
        {
            RefreshVisibleRows();
            return;
        }

        _scrollOffset = UiClipper.ClampScrollOffset(_visibleRows.Count, itemHeight, viewportHeight, _scrollOffset);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
        ResolveScrollbars();
    }

    private void ResolveScrollbars()
    {
        int contentHeight = _visibleRows.Count * Math.Max(1, ItemHeight);
        bool always = VerticalScrollbar == UiScrollbarVisibility.Always;
        bool auto = VerticalScrollbar == UiScrollbarVisibility.Auto;
        _showVerticalScrollbar = always || (auto && contentHeight > Bounds.Height);
        if (!_showVerticalScrollbar)
        {
            _hoverVerticalScrollbarThumb = false;
            _draggingVerticalScrollbar = false;
        }
    }

    private UiRect GetViewportBounds()
    {
        int width = Math.Max(0, Bounds.Width - (_showVerticalScrollbar ? Math.Max(1, ScrollbarThickness) : 0));
        return new UiRect(Bounds.X, Bounds.Y, width, Bounds.Height);
    }

    private UiRect GetVerticalScrollbarBounds()
    {
        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.Right - thickness, Bounds.Y, thickness, Bounds.Height);
    }

    private UiRect GetVerticalThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackTop = bar.Y + padding;
        int trackHeight = Math.Max(1, bar.Height - padding * 2);
        int contentHeight = Math.Max(trackHeight, _visibleRows.Count * Math.Max(1, ItemHeight));
        int thumbHeight = trackHeight;
        if (contentHeight > 0)
        {
            double ratio = trackHeight / (double)contentHeight;
            thumbHeight = Math.Max(MinThumbSize, (int)Math.Round(trackHeight * ratio));
        }

        thumbHeight = Math.Min(trackHeight, thumbHeight);
        int maxScroll = Math.Max(0, contentHeight - Math.Max(1, Bounds.Height));
        int travel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = trackTop;
        if (travel > 0 && maxScroll > 0)
        {
            double t = _scrollOffset / (double)maxScroll;
            thumbY = trackTop + (int)Math.Round(travel * t);
        }

        return new UiRect(bar.X + padding, thumbY, Math.Max(1, bar.Width - padding * 2), thumbHeight);
    }

    private void UpdateVerticalScrollbar(UiInputState input, UiRect bar)
    {
        UiRect thumb = GetVerticalThumbBounds(bar);
        _hoverVerticalScrollbarThumb = thumb.Contains(input.MousePosition);

        if (!_draggingVerticalScrollbar && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverVerticalScrollbarThumb)
            {
                _draggingVerticalScrollbar = true;
                _scrollbarDragStartMouseY = input.MousePosition.Y;
                _scrollbarDragStartScrollOffset = _scrollOffset;
            }
            else
            {
                PageVertical(input.MousePosition.Y < thumb.Y);
            }
        }

        if (_draggingVerticalScrollbar)
        {
            if (input.LeftDown)
            {
                int trackHeight = Math.Max(1, bar.Height - ScrollbarPadding * 2);
                int thumbHeight = Math.Max(1, thumb.Height);
                int travel = Math.Max(1, trackHeight - thumbHeight);
                int maxScroll = Math.Max(0, _visibleRows.Count * Math.Max(1, ItemHeight) - Math.Max(1, Bounds.Height));
                int deltaPixels = input.MousePosition.Y - _scrollbarDragStartMouseY;
                int deltaScroll = maxScroll <= 0 ? 0 : (int)Math.Round(deltaPixels * (maxScroll / (double)travel));
                _scrollOffset = _scrollbarDragStartScrollOffset + deltaScroll;
            }
            else
            {
                _draggingVerticalScrollbar = false;
            }
        }
    }

    private void PageVertical(bool pageUp)
    {
        int page = Math.Max(1, Bounds.Height - Math.Max(1, ItemHeight));
        _scrollOffset += pageUp ? -page : page;
    }

    private void DrawScrollbars(UiRenderContext context)
    {
        if (!_showVerticalScrollbar)
        {
            return;
        }

        UiRect bar = GetVerticalScrollbarBounds();
        context.Renderer.FillRect(bar, ScrollbarTrack);
        context.Renderer.DrawRect(bar, ScrollbarBorder, 1);

        UiRect thumb = GetVerticalThumbBounds(bar);
        UiColor thumbColor = (_hoverVerticalScrollbarThumb || _draggingVerticalScrollbar) ? ScrollbarThumbHover : ScrollbarThumb;
        context.Renderer.FillRect(thumb, thumbColor);
    }

    private static bool TryFindPath(IReadOnlyList<UiTreeViewItem> items, UiTreeViewItem target, List<UiTreeViewItem> path)
    {
        for (int i = 0; i < items.Count; i++)
        {
            UiTreeViewItem item = items[i];
            path.Add(item);
            if (ReferenceEquals(item, target))
            {
                return true;
            }

            if (TryFindPath(item.Children, target, path))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
        }

        return false;
    }

    private bool SetOpenRecursive(IReadOnlyList<UiTreeViewItem> items, bool isOpen)
    {
        bool changed = false;
        for (int i = 0; i < items.Count; i++)
        {
            changed |= SetOpenRecursive(items[i], isOpen);
        }

        return changed;
    }

    private bool SetOpenRecursive(UiTreeViewItem item, bool isOpen)
    {
        bool changed = SetItemOpen(item, isOpen);
        for (int i = 0; i < item.Children.Count; i++)
        {
            changed |= SetOpenRecursive(item.Children[i], isOpen);
        }

        return changed;
    }

    private bool SetItemOpen(UiTreeViewItem item, bool isOpen)
    {
        if (!item.HasChildren || item.IsOpen == isOpen)
        {
            return false;
        }

        item.IsOpen = isOpen;
        ItemToggled?.Invoke(item, isOpen);
        return true;
    }

    private void DrawHierarchyLines(UiRenderContext context, int index, UiRect rowRect)
    {
        if (index < 0 || index >= _visibleRows.Count)
        {
            return;
        }

        VisibleRow row = _visibleRows[index];
        int thickness = Math.Max(1, HierarchyLineThickness);
        int midY = rowRect.Y + rowRect.Height / 2;
        for (int depth = 0; depth < row.Depth; depth++)
        {
            if (HasContinuationAtDepth(index, depth))
            {
                DrawVerticalLine(context, GetConnectorX(depth), rowRect.Y, rowRect.Bottom, thickness);
            }
        }

        if (row.Depth <= 0)
        {
            return;
        }

        int connectorX = GetConnectorX(row.Depth);
        DrawVerticalLine(context, connectorX, rowRect.Y, midY + 1, thickness);
        if (HasContinuationAtDepth(index, row.Depth))
        {
            DrawVerticalLine(context, connectorX, midY, rowRect.Bottom, thickness);
        }

        int textStart = GetArrowBounds(index).Right + Math.Max(0, ArrowPadding);
        int horizontalWidth = Math.Max(thickness, textStart - connectorX);
        context.Renderer.FillRect(new UiRect(connectorX, midY, horizontalWidth, thickness), HierarchyLineColor);
    }

    private void DrawVerticalLine(UiRenderContext context, int x, int top, int bottom, int thickness)
    {
        int height = Math.Max(thickness, bottom - top);
        context.Renderer.FillRect(new UiRect(x, top, thickness, height), HierarchyLineColor);
    }

    private int GetConnectorX(int depth)
    {
        int size = Math.Max(4, ArrowSize);
        return Bounds.X + Math.Max(0, Padding) + depth * Math.Max(1, IndentWidth) + size / 2;
    }

    private bool HasContinuationAtDepth(int index, int depth)
    {
        for (int i = index + 1; i < _visibleRows.Count; i++)
        {
            int nextDepth = _visibleRows[i].Depth;
            if (nextDepth < depth)
            {
                return false;
            }

            if (nextDepth == depth)
            {
                return true;
            }
        }

        return false;
    }

    private void AddVisibleRows(UiTreeViewItem item, int depth)
    {
        _visibleRows.Add(new VisibleRow(item, depth));
        if (!item.IsOpen || !item.HasChildren)
        {
            return;
        }

        for (int i = 0; i < item.Children.Count; i++)
        {
            AddVisibleRows(item.Children[i], depth + 1);
        }
    }
}
