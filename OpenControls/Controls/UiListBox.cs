namespace OpenControls.Controls;

public sealed class UiListBox : UiElement
{
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private bool _focused;
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

    public int ScrollIndex { get; set; }
    public int ItemHeight { get; set; } = 20;
    public int ScrollWheelItems { get; set; } = 1;
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
        int visibleCount = GetVisibleCount(itemHeight);
        bool shift = input.ShiftDown;
        bool ctrl = input.CtrlDown;

        if (input.ScrollDelta != 0 && Bounds.Contains(input.MousePosition))
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                int scrollItems = Math.Max(1, ScrollWheelItems);
                ScrollIndex = ClampScrollIndex(ScrollIndex - steps * scrollItems, visibleCount);
            }
        }

        ScrollIndex = ClampScrollIndex(ScrollIndex, visibleCount);
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
        int visibleCount = GetVisibleCount(itemHeight);
        int startIndex = ClampScrollIndex(ScrollIndex, visibleCount);
        int endIndex = Math.Min(Items.Count, startIndex + visibleCount);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);

        context.Renderer.PushClip(Bounds);
        for (int i = startIndex; i < endIndex; i++)
        {
            int row = i - startIndex;
            int y = Bounds.Y + row * itemHeight;
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
            context.Renderer.DrawText(Items[i], new UiPoint(Bounds.X + Padding, textY), textColor, TextScale);
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
        int visibleCount = GetVisibleCount(itemHeight);
        if (visibleCount <= 0)
        {
            return;
        }

        if (index < ScrollIndex)
        {
            ScrollIndex = index;
        }
        else if (index >= ScrollIndex + visibleCount)
        {
            ScrollIndex = index - visibleCount + 1;
        }
    }

    private int GetVisibleCount(int itemHeight)
    {
        if (itemHeight <= 0)
        {
            return 0;
        }

        return Bounds.Height / itemHeight;
    }

    private int ClampScrollIndex(int scrollIndex, int visibleCount)
    {
        if (Items.Count == 0 || visibleCount <= 0)
        {
            return 0;
        }

        int maxStart = Math.Max(0, Items.Count - visibleCount);
        return Math.Clamp(scrollIndex, 0, maxStart);
    }

    private int GetIndexAtPoint(UiPoint point)
    {
        if (!Bounds.Contains(point))
        {
            return -1;
        }

        int itemHeight = Math.Max(1, ItemHeight);
        int visibleCount = GetVisibleCount(itemHeight);
        int startIndex = ClampScrollIndex(ScrollIndex, visibleCount);
        int relativeIndex = (point.Y - Bounds.Y) / itemHeight;
        if (relativeIndex < 0 || relativeIndex >= visibleCount)
        {
            return -1;
        }

        int index = startIndex + relativeIndex;
        return index >= 0 && index < Items.Count ? index : -1;
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
