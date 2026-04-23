namespace OpenControls.Controls;

public sealed class UiListBox : UiElement
{
    private IReadOnlyList<string> _items = Array.Empty<string>();
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
    private int _scrollOffset;
    private UiClipRange _clipRange;
    private readonly int[] _singleSelection = new int[1];
    private UiSelectionModel? _selectionModel;
    private string _selectionScope = string.Empty;
    private bool _showVerticalScrollbar;
    private bool _draggingVerticalScrollbar;
    private bool _hoverVerticalScrollbarThumb;
    private int _dragStartMouseY;
    private int _dragStartScrollOffset;

    public IReadOnlyList<string> Items
    {
        get => _items;
        set
        {
            IReadOnlyList<string> normalized = value ?? Array.Empty<string>();
            if (!SetInvalidatingValue(ref _items, normalized, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State))
            {
                return;
            }

            _selectionModel?.SetItemCount(_items.Count, SelectionScope);
            if (_selectionModel == null && _selectedIndex >= _items.Count)
            {
                SetSelectedIndex(_items.Count - 1);
            }

            _hoverIndex = Math.Clamp(_hoverIndex, -1, _items.Count - 1);
        }
    }
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
            _selectionModel?.SetItemCount(Items.Count, _selectionScope);
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
                _selectionModel.SetItemCount(Items.Count, SelectionScope);
            }

            HandleSelectionModelChanged();
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

    public int ScrollIndex
    {
        get => Math.Max(0, _scrollOffset / Math.Max(1, ItemHeight));
        set => SetScrollOffsetInternal(Math.Max(0, value) * Math.Max(1, ItemHeight), raiseEvent: true);
    }

    public int ScrollOffset
    {
        get => _scrollOffset;
        set => SetScrollOffsetInternal(Math.Max(0, value), raiseEvent: true);
    }

    public int ItemHeight { get; set; } = 20;
    public int ScrollWheelItems { get; set; } = 1;
    public int OverscanItems { get; set; } = 1;
    public int Padding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public bool AllowDeselect { get; set; }
    public UiScrollbarVisibility VerticalScrollbar { get; set; } = UiScrollbarVisibility.Disabled;
    public int ScrollbarThickness { get; set; } = 12;
    public int ScrollbarPadding { get; set; } = 2;
    public int MinThumbSize { get; set; } = 12;
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor HoverColor { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedColor { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public UiColor ScrollbarTrack { get; set; } = new UiColor(20, 24, 34);
    public UiColor ScrollbarBorder { get; set; } = new UiColor(60, 70, 90);
    public UiColor ScrollbarThumb { get; set; } = new UiColor(70, 80, 100);
    public UiColor ScrollbarThumbHover { get; set; } = new UiColor(90, 110, 140);
    public int CornerRadius { get; set; }
    public int FirstVisibleIndex => _clipRange.FirstVisibleIndex;
    public int LastVisibleIndex => _clipRange.LastVisibleIndex;

    public event Action<int>? SelectionChanged;
    public event Action<int>? ScrollOffsetChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.GetSelfInput(this);
        _selectionModel?.SetItemCount(Items.Count, SelectionScope);
        int itemHeight = Math.Max(1, ItemHeight);
        ResolveVerticalScrollbar();
        SetScrollOffsetInternal(UiClipper.ClampScrollOffset(Items.Count, itemHeight, Math.Max(0, Bounds.Height), _scrollOffset), raiseEvent: false);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        bool shift = input.ShiftDown;
        bool ctrl = input.CtrlDown;
        UiRect viewportBounds = GetViewportBounds();
        UiRect verticalScrollbarBounds = GetVerticalScrollbarBounds();
        bool mouseInScrollbar = _showVerticalScrollbar && verticalScrollbarBounds.Contains(input.MousePosition);

        if (_showVerticalScrollbar)
        {
            UpdateVerticalScrollbar(input, verticalScrollbarBounds);
        }

        if (input.ScrollDelta != 0 && Bounds.Contains(input.MousePosition) && !_draggingVerticalScrollbar)
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                int scrollItems = Math.Max(1, ScrollWheelItems);
                SetScrollOffsetInternal(_scrollOffset - steps * scrollItems * itemHeight, raiseEvent: true);
            }
        }

        SetScrollOffsetInternal(UiClipper.ClampScrollOffset(Items.Count, itemHeight, Math.Max(0, Bounds.Height), _scrollOffset), raiseEvent: false);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        _hoverIndex = GetIndexAtPoint(input.MousePosition);

        if (input.LeftClicked && viewportBounds.Contains(input.MousePosition) && !mouseInScrollbar)
        {
            context.Focus.RequestFocus(this);
            if (_hoverIndex >= 0 && _hoverIndex < Items.Count)
            {
                ApplySelection(_hoverIndex, shift, ctrl);
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
                ApplySelection(Items.Count > 0 ? 0 : -1, shift, ctrl);
            }

            if (input.Navigation.End)
            {
                ApplySelection(Items.Count > 0 ? Items.Count - 1 : -1, shift, ctrl);
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

        ResolveVerticalScrollbar();
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        int itemHeight = Math.Max(1, ItemHeight);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        UiFont font = ResolveFont(context.DefaultFont);
        UiRect viewportBounds = GetViewportBounds();

        context.Renderer.PushClip(viewportBounds);
        if (_clipRange.HasMaterializedItems)
        {
            for (int i = _clipRange.FirstMaterializedIndex; i <= _clipRange.LastMaterializedIndex; i++)
            {
                int y = Bounds.Y + _clipRange.GetItemStart(i) - _clipRange.ViewportStart;
                UiRect itemRect = new UiRect(viewportBounds.X, y, viewportBounds.Width, itemHeight);

                if (IsItemSelected(i))
                {
                    context.Renderer.FillRect(itemRect, SelectedColor);
                }
                else if (i == _hoverIndex)
                {
                    context.Renderer.FillRect(itemRect, HoverColor);
                }

                int textY = UiRenderHelpers.GetVerticallyCenteredTextY(itemRect, Items[i], TextScale, font);
                UiColor textColor = IsItemSelected(i) ? SelectedTextColor : TextColor;
                context.Renderer.DrawText(Items[i], new UiPoint(viewportBounds.X + Padding, textY), textColor, TextScale, font);
            }
        }
        context.Renderer.PopClip();

        DrawVerticalScrollbar(context);

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

    private void MoveSelection(int delta, bool shift, bool ctrl)
    {
        if (Items.Count == 0)
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

        int currentIndex = SelectedIndex;
        int next = currentIndex < 0 ? 0 : currentIndex + delta;
        next = Math.Clamp(next, 0, Items.Count - 1);
        ApplySelection(next, shift, ctrl);
    }

    private void EnsureVisible(int index)
    {
        int itemHeight = Math.Max(1, ItemHeight);
        int viewportHeight = Math.Max(0, Bounds.Height);
        if (viewportHeight <= 0)
        {
            return;
        }

        SetScrollOffsetInternal(UiClipper.EnsureVisible(Items.Count, itemHeight, viewportHeight, _scrollOffset, index), raiseEvent: true);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
    }

    private int GetIndexAtPoint(UiPoint point)
    {
        UiRect viewportBounds = GetViewportBounds();
        if (!viewportBounds.Contains(point))
        {
            return -1;
        }

        int contentY = point.Y - viewportBounds.Y + _scrollOffset;
        return _clipRange.GetIndexAtOffset(contentY);
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
        if (Items.Count == 0)
        {
            clamped = -1;
        }
        else
        {
            clamped = Math.Clamp(index, -1, Items.Count - 1);
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

    private void SetScrollOffsetInternal(int value, bool raiseEvent)
    {
        int clamped = Math.Max(0, value);
        if (_scrollOffset == clamped)
        {
            return;
        }

        _scrollOffset = clamped;
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        if (raiseEvent)
        {
            ScrollOffsetChanged?.Invoke(clamped);
        }
    }

    private void ResolveVerticalScrollbar()
    {
        _showVerticalScrollbar = VerticalScrollbar switch
        {
            UiScrollbarVisibility.Always => true,
            UiScrollbarVisibility.Auto => Items.Count * Math.Max(1, ItemHeight) > Math.Max(0, Bounds.Height),
            _ => false
        };
    }

    private UiRect GetViewportBounds()
    {
        if (!_showVerticalScrollbar)
        {
            return Bounds;
        }

        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.X, Bounds.Y, Math.Max(0, Bounds.Width - thickness), Bounds.Height);
    }

    private UiRect GetVerticalScrollbarBounds()
    {
        if (!_showVerticalScrollbar)
        {
            return new UiRect(0, 0, 0, 0);
        }

        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.Right - thickness, Bounds.Y, thickness, Bounds.Height);
    }

    private UiRect GetVerticalScrollbarThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackHeight = Math.Max(0, bar.Height - padding * 2);
        int trackTop = bar.Y + padding;
        int viewportHeight = Math.Max(0, Bounds.Height);
        int contentHeight = Items.Count * Math.Max(1, ItemHeight);
        int scrollRange = Math.Max(0, contentHeight - viewportHeight);

        int thumbHeight = trackHeight;
        if (scrollRange > 0)
        {
            float ratio = viewportHeight / (float)Math.Max(1, contentHeight);
            thumbHeight = Math.Max(MinThumbSize, (int)Math.Round(trackHeight * ratio));
        }

        int travel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = trackTop;
        if (scrollRange > 0 && travel > 0)
        {
            float t = _scrollOffset / (float)scrollRange;
            thumbY = trackTop + (int)Math.Round(travel * t);
        }

        return new UiRect(bar.X + padding, thumbY, Math.Max(1, bar.Width - padding * 2), thumbHeight);
    }

    private void UpdateVerticalScrollbar(UiInputState input, UiRect bar)
    {
        UiRect thumb = GetVerticalScrollbarThumbBounds(bar);
        _hoverVerticalScrollbarThumb = thumb.Contains(input.MousePosition);

        if (!_draggingVerticalScrollbar && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverVerticalScrollbarThumb)
            {
                _draggingVerticalScrollbar = true;
                _dragStartMouseY = input.MousePosition.Y;
                _dragStartScrollOffset = _scrollOffset;
            }
            else
            {
                PageVertical(input.MousePosition.Y < thumb.Y);
            }
        }

        if (_draggingVerticalScrollbar && input.LeftDown)
        {
            int viewportHeight = Math.Max(0, Bounds.Height);
            int contentHeight = Items.Count * Math.Max(1, ItemHeight);
            int scrollRange = Math.Max(0, contentHeight - viewportHeight);
            int trackHeight = Math.Max(1, bar.Height - Math.Max(0, ScrollbarPadding) * 2);
            int thumbHeight = thumb.Height;
            int travel = Math.Max(1, trackHeight - thumbHeight);
            int delta = input.MousePosition.Y - _dragStartMouseY;
            float scrollDelta = scrollRange > 0 ? delta / (float)travel * scrollRange : 0f;
            SetScrollOffsetInternal(_dragStartScrollOffset + (int)Math.Round(scrollDelta), raiseEvent: true);
        }

        if (_draggingVerticalScrollbar && input.LeftReleased)
        {
            _draggingVerticalScrollbar = false;
        }
    }

    private void PageVertical(bool pageUp)
    {
        int pageSize = Math.Max(Math.Max(1, ItemHeight), Math.Max(0, Bounds.Height) - Math.Max(1, ItemHeight));
        int delta = pageUp ? -pageSize : pageSize;
        SetScrollOffsetInternal(_scrollOffset + delta, raiseEvent: true);
    }

    private void DrawVerticalScrollbar(UiRenderContext context)
    {
        if (!_showVerticalScrollbar)
        {
            return;
        }

        UiRect bar = GetVerticalScrollbarBounds();
        UiRect thumb = GetVerticalScrollbarThumbBounds(bar);
        context.Renderer.FillRect(bar, ScrollbarTrack);
        if (ScrollbarBorder.A > 0)
        {
            context.Renderer.DrawRect(bar, ScrollbarBorder, 1);
        }

        context.Renderer.FillRect(
            thumb,
            _hoverVerticalScrollbarThumb || _draggingVerticalScrollbar ? ScrollbarThumbHover : ScrollbarThumb);
    }
}
