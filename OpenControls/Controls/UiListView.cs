using OpenControls.State;

namespace OpenControls.Controls;

public sealed class UiListView : UiElement, IUiStatefulElement
{
    private readonly UiChildRegion _region;
    private readonly UiSelectionModel _internalSelectionModel = new();
    private UiSelectionModel? _selectionModel;
    private readonly List<UiSelectableRow> _items = new();
    private int _lastPrimaryIndex = -2;
    private bool _navigationHandledThisFrame;
    private string _selectionScope = string.Empty;

    public UiListView()
    {
        _region = new UiChildRegion();
        AddChild(_region);
        _internalSelectionModel.SelectionChanged += HandleSelectionModelChanged;
    }

    public IReadOnlyList<UiSelectableRow> Items => _items;
    public UiChildRegion Region => _region;
    public UiScrollPanel ScrollPanel => _region.ScrollPanel;

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
            }

            ActiveSelectionModel.SetItemCount(_items.Count, _selectionScope);
            BindItems();
            HandleSelectionModelChanged();
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
            _lastPrimaryIndex = -2;
            ActiveSelectionModel.SetItemCount(_items.Count, _selectionScope);
            BindItems();
            HandleSelectionModelChanged();
        }
    }

    public int SelectedIndex
    {
        get => ActiveSelectionModel.GetPrimaryIndex(_selectionScope);
        set
        {
            if (value < 0)
            {
                ActiveSelectionModel.Clear(_selectionScope);
            }
            else
            {
                ActiveSelectionModel.SelectSingle(value, _selectionScope);
            }
        }
    }

    public IReadOnlyList<int> SelectedIndices => ActiveSelectionModel.GetSelectedIndices(_selectionScope);
    public string FilterText { get; set; } = string.Empty;
    public Func<UiSelectableRow, string, bool>? FilterPredicate { get; set; }
    public int ItemHeight { get; set; } = 28;
    public int ItemSpacing { get; set; } = 2;
    public int Padding { get; set; } = 0;
    public bool AllowDeselect { get; set; }
    public string EmptyText { get; set; } = "No items";
    public int EmptyTextScale { get; set; } = 1;
    public UiColor EmptyTextColor { get; set; } = new UiColor(140, 150, 170);

    public UiColor Background
    {
        get => _region.Background;
        set => _region.Background = value;
    }

    public UiColor Border
    {
        get => _region.Border;
        set => _region.Border = value;
    }

    public int BorderThickness
    {
        get => _region.BorderThickness;
        set => _region.BorderThickness = value;
    }

    public int CornerRadius
    {
        get => _region.CornerRadius;
        set => _region.CornerRadius = value;
    }

    public event Action<int>? SelectionChanged;
    public event Action<UiSelectableRow, int>? ItemInvoked;

    public override bool IsFocusable => true;

    public void CaptureState(UiElementState state)
    {
        state.ScrollY = ScrollPanel.ScrollY;
        state.FilterText = FilterText;
        state.SelectedIndex = SelectedIndex;
        state.SelectedIndices = SelectedIndices.ToList();
    }

    public void ApplyState(UiElementState state)
    {
        if (state.FilterText != null)
        {
            FilterText = state.FilterText;
        }

        if (state.ScrollY.HasValue)
        {
            ScrollPanel.ScrollY = Math.Max(0, state.ScrollY.Value);
        }

        if (state.SelectedIndices.Count > 0)
        {
            ActiveSelectionModel.SetSelection(state.SelectedIndices, state.SelectedIndex ?? -1, state.SelectedIndex ?? -1, _selectionScope);
        }
        else if (state.SelectedIndex.HasValue)
        {
            SelectedIndex = state.SelectedIndex.Value;
        }
    }

    public void AddItem(UiSelectableRow item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_items.Contains(item))
        {
            return;
        }

        _items.Add(item);
        _region.AddContentChild(item);
        item.OwnerListView = this;
        item.Invoked += HandleItemInvoked;
        BindItems();
    }

    public bool RemoveItem(UiSelectableRow item)
    {
        if (!_items.Remove(item))
        {
            return false;
        }

        item.OwnerListView = null;
        item.SelectionModel = null;
        item.Invoked -= HandleItemInvoked;
        _region.RemoveContentChild(item);
        BindItems();
        return true;
    }

    public void ClearItems()
    {
        foreach (UiSelectableRow item in _items)
        {
            item.OwnerListView = null;
            item.SelectionModel = null;
            item.Invoked -= HandleItemInvoked;
            _region.RemoveContentChild(item);
        }

        _items.Clear();
        BindItems();
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _region.Bounds = Bounds;
        ActiveSelectionModel.SetItemCount(_items.Count, _selectionScope);
        LayoutItems();
        _navigationHandledThisFrame = false;

        if (context.Input.LeftClicked && Bounds.Contains(context.Input.MousePosition))
        {
            context.Focus.RequestFocus(this);
            if (AllowDeselect && GetItemAtPoint(context.Input.MousePosition) == null)
            {
                ActiveSelectionModel.Clear(_selectionScope);
            }
        }

        if (context.Focus.Focused == this)
        {
            HandleContainerNavigation(context);
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        base.Render(context);

        if (GetVisibleItemCount() == 0 && !string.IsNullOrEmpty(EmptyText))
        {
            UiFont font = ResolveFont(context.DefaultFont);
            int textHeight = context.Renderer.MeasureTextHeight(EmptyTextScale, font);
            int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
            int textX = Bounds.X + Math.Max(0, Padding) + 6;
            context.Renderer.DrawText(EmptyText, new UiPoint(textX, textY), EmptyTextColor, EmptyTextScale, font);
        }
    }

    internal bool HandleNavigation(UiSelectableRow item, UiUpdateContext context)
    {
        if (_navigationHandledThisFrame)
        {
            return false;
        }

        int currentIndex = _items.IndexOf(item);
        if (currentIndex < 0)
        {
            return false;
        }

        UiInputState input = context.Input;
        if (input.Navigation.MoveUp)
        {
            _navigationHandledThisFrame = true;
            MoveFocus(currentIndex, -1, context.Focus, input);
            return true;
        }

        if (input.Navigation.MoveDown)
        {
            _navigationHandledThisFrame = true;
            MoveFocus(currentIndex, 1, context.Focus, input);
            return true;
        }

        if (input.Navigation.Home)
        {
            _navigationHandledThisFrame = true;
            FocusIndex(FindFirstVisibleIndex(), context.Focus, input);
            return true;
        }

        if (input.Navigation.End)
        {
            _navigationHandledThisFrame = true;
            FocusIndex(FindLastVisibleIndex(), context.Focus, input);
            return true;
        }

        if (input.Navigation.PageUp)
        {
            _navigationHandledThisFrame = true;
            MoveByPage(currentIndex, -1, context.Focus, input);
            return true;
        }

        if (input.Navigation.PageDown)
        {
            _navigationHandledThisFrame = true;
            MoveByPage(currentIndex, 1, context.Focus, input);
            return true;
        }

        return false;
    }

    private UiSelectionModel ActiveSelectionModel => _selectionModel ?? _internalSelectionModel;

    private void BindItems()
    {
        UiSelectionModel model = ActiveSelectionModel;
        model.SetItemCount(_items.Count, _selectionScope);

        for (int i = 0; i < _items.Count; i++)
        {
            UiSelectableRow item = _items[i];
            item.SelectionModel = model;
            item.SelectionIndex = i;
            item.SelectionScope = _selectionScope;
        }
    }

    private void LayoutItems()
    {
        int padding = Math.Max(0, Padding);
        int y = padding;
        int width = Math.Max(0, Bounds.Width - padding * 2);

        for (int i = 0; i < _items.Count; i++)
        {
            UiSelectableRow item = _items[i];
            bool visible = PassesFilter(item);
            item.Visible = visible;
            item.Highlighted = i == ActiveSelectionModel.GetPrimaryIndex(_selectionScope);
            if (!visible)
            {
                continue;
            }

            int itemHeight = item.Bounds.Height > 0 ? item.Bounds.Height : Math.Max(1, ItemHeight);
            item.Bounds = new UiRect(padding, y, width, itemHeight);
            y += itemHeight + Math.Max(0, ItemSpacing);
        }
    }

    private bool PassesFilter(UiSelectableRow item)
    {
        string filter = FilterText?.Trim() ?? string.Empty;
        if (filter.Length == 0)
        {
            return true;
        }

        if (FilterPredicate != null)
        {
            return FilterPredicate(item, filter);
        }

        return item.EffectiveSearchText.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void HandleContainerNavigation(UiUpdateContext context)
    {
        UiInputState input = context.Input;
        if (input.Navigation.MoveUp)
        {
            FocusIndex(FindPreviousVisibleIndex(Math.Max(FindFirstVisibleIndex(), ActiveSelectionModel.GetPrimaryIndex(_selectionScope))), context.Focus, input);
        }
        else if (input.Navigation.MoveDown)
        {
            int current = ActiveSelectionModel.GetPrimaryIndex(_selectionScope);
            if (current < 0)
            {
                FocusIndex(FindFirstVisibleIndex(), context.Focus, input);
            }
            else
            {
                FocusIndex(FindNextVisibleIndex(current), context.Focus, input);
            }
        }
        else if (input.Navigation.Home)
        {
            FocusIndex(FindFirstVisibleIndex(), context.Focus, input);
        }
        else if (input.Navigation.End)
        {
            FocusIndex(FindLastVisibleIndex(), context.Focus, input);
        }
    }

    private void MoveFocus(int currentIndex, int delta, UiFocusManager focus, UiInputState input)
    {
        int target = delta < 0 ? FindPreviousVisibleIndex(currentIndex) : FindNextVisibleIndex(currentIndex);
        FocusIndex(target, focus, input);
    }

    private void MoveByPage(int currentIndex, int direction, UiFocusManager focus, UiInputState input)
    {
        int pageSize = Math.Max(1, Bounds.Height / Math.Max(1, ItemHeight + Math.Max(0, ItemSpacing)));
        int target = currentIndex;
        for (int i = 0; i < pageSize; i++)
        {
            int next = direction < 0 ? FindPreviousVisibleIndex(target) : FindNextVisibleIndex(target);
            if (next == target)
            {
                break;
            }

            target = next;
        }

        FocusIndex(target, focus, input);
    }

    private void FocusIndex(int index, UiFocusManager focus, UiInputState input)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        UiSelectableRow item = _items[index];
        if (!item.Visible)
        {
            return;
        }

        ActiveSelectionModel.ApplySelection(index, input.CtrlDown, input.ShiftDown, _selectionScope);
        EnsureVisible(index);
        focus.RequestFocus(item);
    }

    private int FindFirstVisibleIndex()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Visible)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindLastVisibleIndex()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i].Visible)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindNextVisibleIndex(int index)
    {
        if (index < 0)
        {
            return FindFirstVisibleIndex();
        }

        for (int i = index + 1; i < _items.Count; i++)
        {
            if (_items[i].Visible)
            {
                return i;
            }
        }

        return index;
    }

    private int FindPreviousVisibleIndex(int index)
    {
        if (index < 0)
        {
            return FindFirstVisibleIndex();
        }

        for (int i = index - 1; i >= 0; i--)
        {
            if (_items[i].Visible)
            {
                return i;
            }
        }

        return index;
    }

    private int GetVisibleItemCount()
    {
        int count = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Visible)
            {
                count++;
            }
        }

        return count;
    }

    private UiSelectableRow? GetItemAtPoint(UiPoint point)
    {
        UiPoint local = new UiPoint(point.X - Bounds.X + ScrollPanel.ScrollX, point.Y - Bounds.Y + ScrollPanel.ScrollY);
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            UiSelectableRow item = _items[i];
            if (item.Visible && item.Bounds.Contains(local))
            {
                return item;
            }
        }

        return null;
    }

    private void EnsureVisible(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        UiSelectableRow item = _items[index];
        if (!item.Visible)
        {
            return;
        }

        UiRect viewport = ScrollPanel.ViewportBounds;
        int top = item.Bounds.Y;
        int bottom = item.Bounds.Bottom;
        int currentTop = ScrollPanel.ScrollY;
        int currentBottom = currentTop + viewport.Height;

        if (top < currentTop)
        {
            ScrollPanel.ScrollY = top;
        }
        else if (bottom > currentBottom)
        {
            ScrollPanel.ScrollY = Math.Max(0, bottom - viewport.Height);
        }
    }

    private void HandleItemInvoked(UiSelectableRow item)
    {
        int index = _items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        EnsureVisible(index);
        ItemInvoked?.Invoke(item, index);
    }

    private void HandleSelectionModelChanged()
    {
        int primaryIndex = ActiveSelectionModel.GetPrimaryIndex(_selectionScope);
        if (_lastPrimaryIndex == primaryIndex)
        {
            return;
        }

        _lastPrimaryIndex = primaryIndex;
        if (primaryIndex >= 0)
        {
            EnsureVisible(primaryIndex);
        }

        SelectionChanged?.Invoke(primaryIndex);
    }
}
