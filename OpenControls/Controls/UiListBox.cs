namespace OpenControls.Controls;

public sealed class UiListBox : UiElement
{
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
    private int _scrollOffset;
    private UiClipRange _clipRange;
    private readonly int[] _singleSelection = new int[1];
    private UiSelectionModel? _selectionModel;

    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
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
                _selectionModel.SetItemCount(Items.Count);
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

    public int ScrollIndex
    {
        get => Math.Max(0, _scrollOffset / Math.Max(1, ItemHeight));
        set => _scrollOffset = Math.Max(0, value) * Math.Max(1, ItemHeight);
    }

    public int ScrollOffset
    {
        get => _scrollOffset;
        set => _scrollOffset = Math.Max(0, value);
    }

    public int ItemHeight { get; set; } = 20;
    public int ScrollWheelItems { get; set; } = 1;
    public int OverscanItems { get; set; } = 1;
    public int Padding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public bool AllowDeselect { get; set; }
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor HoverColor { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedColor { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }
    public int FirstVisibleIndex => _clipRange.FirstVisibleIndex;
    public int LastVisibleIndex => _clipRange.LastVisibleIndex;

    public event Action<int>? SelectionChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _selectionModel?.SetItemCount(Items.Count);
        int itemHeight = Math.Max(1, ItemHeight);
        _scrollOffset = UiClipper.ClampScrollOffset(Items.Count, itemHeight, Math.Max(0, Bounds.Height), _scrollOffset);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        bool shift = input.ShiftDown;
        bool ctrl = input.CtrlDown;

        if (input.ScrollDelta != 0 && Bounds.Contains(input.MousePosition))
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                int scrollItems = Math.Max(1, ScrollWheelItems);
                _scrollOffset -= steps * scrollItems * itemHeight;
            }
        }

        _scrollOffset = UiClipper.ClampScrollOffset(Items.Count, itemHeight, Math.Max(0, Bounds.Height), _scrollOffset);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        _hoverIndex = GetIndexAtPoint(input.MousePosition);

        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
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

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        int itemHeight = Math.Max(1, ItemHeight);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, Math.Max(0, Bounds.Height), OverscanItems);
        UiFont font = ResolveFont(context.DefaultFont);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);

        context.Renderer.PushClip(Bounds);
        if (_clipRange.HasMaterializedItems)
        {
            for (int i = _clipRange.FirstMaterializedIndex; i <= _clipRange.LastMaterializedIndex; i++)
            {
                int y = Bounds.Y + _clipRange.GetItemStart(i) - _clipRange.ViewportStart;
                UiRect itemRect = new UiRect(Bounds.X, y, Bounds.Width, itemHeight);

                if (IsItemSelected(i))
                {
                    context.Renderer.FillRect(itemRect, SelectedColor);
                }
                else if (i == _hoverIndex)
                {
                    context.Renderer.FillRect(itemRect, HoverColor);
                }

                int textY = y + (itemHeight - textHeight) / 2;
                UiColor textColor = IsItemSelected(i) ? SelectedTextColor : TextColor;
                context.Renderer.DrawText(Items[i], new UiPoint(Bounds.X + Padding, textY), textColor, TextScale, font);
            }
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

    private void MoveSelection(int delta, bool shift, bool ctrl)
    {
        if (Items.Count == 0)
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

        _scrollOffset = UiClipper.EnsureVisible(Items.Count, itemHeight, viewportHeight, _scrollOffset, index);
        _clipRange = UiClipper.FixedHeight(Items.Count, itemHeight, _scrollOffset, viewportHeight, OverscanItems);
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

            int primary = _selectionModel.PrimaryIndex;
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
            return _selectionModel.IsSelected(index);
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
}
