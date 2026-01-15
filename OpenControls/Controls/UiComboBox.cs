namespace OpenControls.Controls;

public sealed class UiComboBox : UiElement
{
    private bool _open;
    private bool _hovered;
    private bool _focused;
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private int _highlightIndex = -1;

    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndex(value);
    }

    public string Placeholder { get; set; } = string.Empty;
    public string EmptyText { get; set; } = "No items";
    public int TextScale { get; set; } = 1;
    public int ItemHeight { get; set; } = 22;
    public int MaxVisibleItems { get; set; } = 6;
    public int Padding { get; set; } = 6;
    public int ArrowSize { get; set; } = 6;
    public int ScrollIndex { get; set; }
    public int ScrollWheelItems { get; set; } = 1;
    public bool ClampToParent { get; set; } = true;
    public bool CloseOnSelection { get; set; } = true;
    public bool ShowEmptyText { get; set; } = true;

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor DropdownBackground { get; set; } = new UiColor(18, 22, 32);
    public UiColor DropdownBorder { get; set; } = new UiColor(70, 80, 100);
    public UiColor ItemHover { get; set; } = new UiColor(45, 52, 70);
    public UiColor ItemSelected { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor PlaceholderColor { get; set; } = new UiColor(120, 130, 150);
    public UiColor ArrowColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public bool IsOpen => _open;

    public event Action<int>? SelectionChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        _hovered = Bounds.Contains(mouse);

        if (input.LeftClicked)
        {
            if (_hovered)
            {
                ToggleOpen(context.Focus);
            }
            else if (_open)
            {
                if (GetDropdownBounds().Contains(mouse))
                {
                    int index = GetItemIndexAt(mouse);
                    if (index >= 0)
                    {
                        SetSelectedIndex(index);
                        if (CloseOnSelection)
                        {
                            _open = false;
                        }
                    }
                }
                else
                {
                    _open = false;
                }
            }
        }

        if (_open)
        {
            UiRect dropdown = GetDropdownBounds();
            if (input.ScrollDelta != 0 && dropdown.Contains(mouse))
            {
                int steps = (int)Math.Round(input.ScrollDelta / 120f);
                if (steps != 0)
                {
                    int scrollItems = Math.Max(1, ScrollWheelItems);
                    int visibleCount = GetVisibleItemCount();
                    ScrollIndex = ClampScrollIndex(ScrollIndex - steps * scrollItems, visibleCount);
                }
            }

            int visibleCountForHover = GetVisibleItemCount();
            ScrollIndex = ClampScrollIndex(ScrollIndex, visibleCountForHover);
            _hoverIndex = GetItemIndexAt(mouse);

            if (_focused)
            {
                if (input.Navigation.MoveDown)
                {
                    MoveHighlight(1);
                }

                if (input.Navigation.MoveUp)
                {
                    MoveHighlight(-1);
                }

                if (input.Navigation.Home)
                {
                    SetHighlight(0);
                }

                if (input.Navigation.End)
                {
                    SetHighlight(Items.Count - 1);
                }

                if (input.Navigation.Activate && _highlightIndex >= 0)
                {
                    SetSelectedIndex(_highlightIndex);
                    if (CloseOnSelection)
                    {
                        _open = false;
                    }
                }

                if (input.Navigation.Escape)
                {
                    _open = false;
                }
            }
        }
        else
        {
            _hoverIndex = -1;
            _highlightIndex = -1;

            if (_focused && input.Navigation.Activate)
            {
                Open(context.Focus);
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

        UiColor fill = _hovered || _open ? HoverBackground : Background;
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, fill);
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);

        string text = GetSelectedText();
        UiColor textColor = _selectedIndex >= 0 ? TextColor : PlaceholderColor;
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(text, new UiPoint(Bounds.X + Padding, textY), textColor, TextScale);

        DrawArrow(context);

        base.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !_open)
        {
            base.RenderOverlay(context);
            return;
        }

        UiRect dropdown = GetDropdownBounds();
        UiRenderHelpers.FillRectRounded(context.Renderer, dropdown, CornerRadius, DropdownBackground);
        UiRenderHelpers.DrawRectRounded(context.Renderer, dropdown, CornerRadius, DropdownBorder, 1);

        int itemHeight = Math.Max(1, ItemHeight);
        int visibleCount = GetVisibleItemCount();
        int startIndex = ClampScrollIndex(ScrollIndex, visibleCount);
        int endIndex = Math.Min(Items.Count, startIndex + visibleCount);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);

        context.Renderer.PushClip(dropdown);
        if (Items.Count == 0 && ShowEmptyText)
        {
            int textY = dropdown.Y + (dropdown.Height - textHeight) / 2;
            context.Renderer.DrawText(EmptyText, new UiPoint(dropdown.X + Padding, textY), PlaceholderColor, TextScale);
        }
        else
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                int row = i - startIndex;
                int y = dropdown.Y + row * itemHeight;
                UiRect itemRect = new UiRect(dropdown.X, y, dropdown.Width, itemHeight);

                bool hovered = i == _hoverIndex;
                bool highlighted = i == _highlightIndex && _focused;
                bool selected = i == _selectedIndex;

                if (selected)
                {
                    context.Renderer.FillRect(itemRect, ItemSelected);
                }
                else if (hovered || highlighted)
                {
                    context.Renderer.FillRect(itemRect, ItemHover);
                }

                int itemTextY = y + (itemHeight - textHeight) / 2;
                context.Renderer.DrawText(Items[i], new UiPoint(dropdown.X + Padding, itemTextY), TextColor, TextScale);
            }
        }

        context.Renderer.PopClip();
        base.RenderOverlay(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _open = false;
        _highlightIndex = -1;
    }

    private void ToggleOpen(UiFocusManager focus)
    {
        if (_open)
        {
            _open = false;
            return;
        }

        Open(focus);
    }

    private void Open(UiFocusManager focus)
    {
        focus.RequestFocus(this);
        _open = true;
        if (Items.Count > 0)
        {
            _highlightIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
            EnsureHighlightVisible();
        }
    }

    private string GetSelectedText()
    {
        if (_selectedIndex >= 0 && _selectedIndex < Items.Count)
        {
            return Items[_selectedIndex];
        }

        return Placeholder;
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

    private UiRect GetDropdownBounds()
    {
        int itemHeight = Math.Max(1, ItemHeight);
        int visibleCount = GetVisibleItemCount();
        int height = visibleCount * itemHeight;
        UiRect bounds = new UiRect(Bounds.X, Bounds.Bottom, Bounds.Width, height);
        return ClampDropdown(bounds);
    }

    private UiRect ClampDropdown(UiRect bounds)
    {
        if (!ClampToParent || !TryGetClampBounds(out UiRect parentBounds))
        {
            return bounds;
        }
        if (parentBounds.Width <= 0 || parentBounds.Height <= 0)
        {
            return bounds;
        }

        int x = bounds.X;
        int y = bounds.Y;
        if (bounds.Right > parentBounds.Right)
        {
            x = parentBounds.Right - bounds.Width;
        }

        if (bounds.Bottom > parentBounds.Bottom)
        {
            y = parentBounds.Bottom - bounds.Height;
        }

        if (x < parentBounds.X)
        {
            x = parentBounds.X;
        }

        if (y < parentBounds.Y)
        {
            y = parentBounds.Y;
        }

        return new UiRect(x, y, bounds.Width, bounds.Height);
    }

    private bool TryGetClampBounds(out UiRect bounds)
    {
        bounds = default;
        if (Parent == null)
        {
            return false;
        }

        int offsetX = 0;
        int offsetY = 0;
        bool hasFallback = false;
        UiRect fallback = default;

        UiElement? parent = Parent;
        while (parent != null)
        {
            if (parent is UiTreeNode tree)
            {
                UiRect content = tree.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (parent is UiCollapsingHeader header)
            {
                UiRect content = header.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (parent is UiScrollPanel scrollPanel)
            {
                offsetX += scrollPanel.Bounds.X - scrollPanel.ScrollX;
                offsetY += scrollPanel.Bounds.Y - scrollPanel.ScrollY;
                UiRect viewport = scrollPanel.ViewportBounds;
                bounds = new UiRect(viewport.X - offsetX, viewport.Y - offsetY, viewport.Width, viewport.Height);
                return true;
            }
            else
            {
                UiRect parentBounds = parent.Bounds;
                fallback = new UiRect(parentBounds.X - offsetX, parentBounds.Y - offsetY, parentBounds.Width, parentBounds.Height);
                hasFallback = true;
            }

            parent = parent.Parent;
        }

        if (hasFallback)
        {
            bounds = fallback;
            return true;
        }

        return false;
    }

    private int GetVisibleItemCount()
    {
        if (Items.Count == 0)
        {
            return ShowEmptyText ? 1 : 0;
        }

        if (MaxVisibleItems <= 0)
        {
            return Items.Count;
        }

        return Math.Min(Items.Count, MaxVisibleItems);
    }

    private void MoveHighlight(int delta)
    {
        if (Items.Count == 0)
        {
            return;
        }

        int start = _highlightIndex;
        if (start < 0)
        {
            start = _selectedIndex >= 0 ? _selectedIndex : 0;
        }

        int next = Math.Clamp(start + delta, 0, Items.Count - 1);
        SetHighlight(next);
    }

    private void SetHighlight(int index)
    {
        if (Items.Count == 0)
        {
            _highlightIndex = -1;
            return;
        }

        _highlightIndex = Math.Clamp(index, 0, Items.Count - 1);
        EnsureHighlightVisible();
    }

    private void EnsureHighlightVisible()
    {
        if (_highlightIndex < 0)
        {
            return;
        }

        EnsureVisible(_highlightIndex);
    }

    private void EnsureVisible(int index)
    {
        int itemHeight = Math.Max(1, ItemHeight);
        int visibleCount = GetVisibleItemCount();
        if (visibleCount <= 0)
        {
            return;
        }

        int startIndex = ClampScrollIndex(ScrollIndex, visibleCount);
        if (index < startIndex)
        {
            ScrollIndex = index;
        }
        else if (index >= startIndex + visibleCount)
        {
            ScrollIndex = index - visibleCount + 1;
        }
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

    private int GetItemIndexAt(UiPoint point)
    {
        if (Items.Count == 0)
        {
            return -1;
        }

        UiRect dropdown = GetDropdownBounds();
        if (!dropdown.Contains(point))
        {
            return -1;
        }

        int itemHeight = Math.Max(1, ItemHeight);
        int visibleCount = GetVisibleItemCount();
        int startIndex = ClampScrollIndex(ScrollIndex, visibleCount);
        int relativeIndex = (point.Y - dropdown.Y) / itemHeight;
        if (relativeIndex < 0 || relativeIndex >= visibleCount)
        {
            return -1;
        }

        int index = startIndex + relativeIndex;
        return index >= 0 && index < Items.Count ? index : -1;
    }

    private void DrawArrow(UiRenderContext context)
    {
        int size = Math.Max(4, ArrowSize);
        int x = Bounds.Right - Padding - size;
        int y = Bounds.Y + (Bounds.Height - size) / 2;
        UiArrow.DrawTriangle(context.Renderer, x, y, size, UiArrowDirection.Down, ArrowColor);
    }
}
