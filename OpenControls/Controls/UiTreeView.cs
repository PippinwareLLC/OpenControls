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
                _selectionModel.SetItemCount(_visibleRows.Count);
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
    public bool AllowDeselect { get; set; }
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor HoverColor { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedColor { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public UiColor ArrowColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public event Action<int>? SelectionChanged;
    public event Action<UiTreeViewItem, bool>? ItemToggled;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        RebuildVisibleRows();
        _selectionModel?.SetItemCount(_visibleRows.Count);

        int itemHeight = Math.Max(1, ItemHeight);
        int viewportHeight = Math.Max(0, Bounds.Height);
        _scrollOffset = UiClipper.ClampScrollOffset(_visibleRows.Count, itemHeight, viewportHeight, _scrollOffset);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);

        UiInputState input = context.Input;
        if (input.ScrollDelta != 0 && Bounds.Contains(input.MousePosition))
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                _scrollOffset -= steps * Math.Max(1, ScrollWheelItems) * itemHeight;
            }
        }

        _scrollOffset = UiClipper.ClampScrollOffset(_visibleRows.Count, itemHeight, viewportHeight, _scrollOffset);
        _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
        _hoverIndex = GetIndexAtPoint(input.MousePosition);

        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < _visibleRows.Count)
            {
                VisibleRow hoveredRow = _visibleRows[_hoverIndex];
                if (hoveredRow.Item.HasChildren && GetArrowBounds(_hoverIndex).Contains(input.MousePosition))
                {
                    hoveredRow.Item.IsOpen = !hoveredRow.Item.IsOpen;
                    ItemToggled?.Invoke(hoveredRow.Item, hoveredRow.Item.IsOpen);
                    RebuildVisibleRows();
                    _selectionModel?.SetItemCount(_visibleRows.Count);
                    _clipRange = UiClipper.FixedHeight(_visibleRows.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
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
            HandleKeyboardNavigation(input);
        }
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

        if (!_clipRange.HasMaterializedItems)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        int itemHeight = Math.Max(1, ItemHeight);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);

        context.Renderer.PushClip(Bounds);
        for (int index = _clipRange.FirstMaterializedIndex; index <= _clipRange.LastMaterializedIndex; index++)
        {
            VisibleRow row = _visibleRows[index];
            int y = Bounds.Y + _clipRange.GetItemStart(index) - _clipRange.ViewportStart;
            UiRect rowRect = new(Bounds.X, y, Bounds.Width, itemHeight);

            if (IsItemSelected(index))
            {
                context.Renderer.FillRect(rowRect, SelectedColor);
            }
            else if (index == _hoverIndex)
            {
                context.Renderer.FillRect(rowRect, HoverColor);
            }

            UiRect arrowBounds = GetArrowBounds(index);
            if (row.Item.HasChildren)
            {
                UiArrowDirection direction = row.Item.IsOpen ? UiArrowDirection.Down : UiArrowDirection.Right;
                UiArrow.DrawTriangle(context.Renderer, arrowBounds, direction, ArrowColor);
            }

            int textX = arrowBounds.Right + Math.Max(0, ArrowPadding);
            int textY = y + (itemHeight - textHeight) / 2;
            UiColor textColor = row.Item.TextColor ?? (IsItemSelected(index) ? SelectedTextColor : TextColor);
            context.Renderer.DrawText(row.Item.Text, new UiPoint(textX, textY), textColor, TextScale, font);
        }

        context.Renderer.PopClip();
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
                    item.IsOpen = !item.IsOpen;
                    ItemToggled?.Invoke(item, item.IsOpen);
                    RebuildVisibleRows();
                    _selectionModel?.SetItemCount(_visibleRows.Count);
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
            row.Item.IsOpen = false;
            ItemToggled?.Invoke(row.Item, false);
            RebuildVisibleRows();
            _selectionModel?.SetItemCount(_visibleRows.Count);
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
            row.Item.IsOpen = true;
            ItemToggled?.Invoke(row.Item, true);
            RebuildVisibleRows();
            _selectionModel?.SetItemCount(_visibleRows.Count);
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
                _selectionModel.Clear();
            }
            else
            {
                _selectionModel.ApplySelection(index, ctrl, shift);
            }

            if (_selectionModel.PrimaryIndex >= 0)
            {
                EnsureVisible(_selectionModel.PrimaryIndex);
            }

            return;
        }

        SetSelectedIndex(index);
    }

    private bool IsItemSelected(int index)
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
        _selectedIndex = _selectionModel.PrimaryIndex;
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
