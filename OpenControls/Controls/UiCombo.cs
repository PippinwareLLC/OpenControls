using OpenControls.State;

namespace OpenControls.Controls;

public class UiCombo : UiElement, IUiStatefulElement
{
    private readonly UiPopup _popup;
    private readonly UiTextField _filterField;
    private readonly UiListView _listView;
    private bool _hovered;
    private bool _focused;

    public UiCombo()
    {
        _popup = new UiPopup
        {
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100),
            ClampToParent = true
        };

        _filterField = new UiTextField
        {
            Placeholder = "Filter..."
        };

        _listView = new UiListView
        {
            Background = UiColor.Transparent,
            Border = UiColor.Transparent,
            EmptyText = "No items",
            ItemHeight = 28,
            ItemSpacing = 2
        };

        _listView.SelectionChanged += index =>
        {
            Invalidate(UiInvalidationReason.State | UiInvalidationReason.Text | UiInvalidationReason.Paint);
            SelectionChanged?.Invoke(index);
        };
        _listView.ItemInvoked += (_, _) =>
        {
            if (CloseOnSelection)
            {
                Close();
            }
        };

        _popup.Opened += () => Opened?.Invoke();
        _popup.Closed += () => Closed?.Invoke();
        _popup.AddChild(_filterField);
        _popup.AddChild(_listView);
        AddChild(_popup);
    }

    public UiListView ListView => _listView;
    public UiTextField FilterField => _filterField;
    public bool IsOpen => _popup.IsOpen;
    public int SelectedIndex
    {
        get => _listView.SelectedIndex;
        set
        {
            int previous = _listView.SelectedIndex;
            _listView.SelectedIndex = value;
            if (previous != _listView.SelectedIndex)
            {
                Invalidate(UiInvalidationReason.State | UiInvalidationReason.Text | UiInvalidationReason.Paint);
            }
        }
    }

    public UiSelectableRow? SelectedItem
    {
        get
        {
            int index = SelectedIndex;
            return index >= 0 && index < _listView.Items.Count ? _listView.Items[index] : null;
        }
    }

    public string Placeholder { get; set; } = string.Empty;
    public string FilterPlaceholder
    {
        get => _filterField.Placeholder;
        set => _filterField.Placeholder = value;
    }

    public string EmptyText
    {
        get => _listView.EmptyText;
        set => _listView.EmptyText = value;
    }

    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 6;
    public int ArrowSize { get; set; } = 6;
    public int DropdownMaxHeight { get; set; } = 240;
    public int DropdownWidth { get; set; }
    public int PopupPadding { get; set; } = 6;
    public int PopupSpacing { get; set; } = 6;
    public int FilterHeight { get; set; } = 26;
    public bool ShowFilterField { get; set; } = true;
    public bool CloseOnSelection { get; set; } = true;
    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor DropdownBackground
    {
        get => _popup.Background;
        set => _popup.Background = value;
    }

    public UiColor DropdownBorder
    {
        get => _popup.Border;
        set => _popup.Border = value;
    }

    public bool ClampToParent
    {
        get => _popup.ClampToParent;
        set => _popup.ClampToParent = value;
    }

    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor PlaceholderColor { get; set; } = new UiColor(120, 130, 150);
    public UiColor ArrowColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }
    public Func<UiSelectableRow, string>? DisplayTextSelector { get; set; }

    public event Action<int>? SelectionChanged;
    public event Action? Opened;
    public event Action? Closed;

    public override bool IsFocusable => true;

    public void CaptureState(UiElementState state)
    {
        state.SelectedIndex = SelectedIndex;
        state.FilterText = _filterField.Text;
        state.ScrollY = _listView.ScrollPanel.ScrollY;
        state.IsOpen = _popup.IsOpen;
    }

    public void ApplyState(UiElementState state)
    {
        if (state.FilterText != null)
        {
            _filterField.Text = state.FilterText;
            _listView.FilterText = ShowFilterField ? state.FilterText : string.Empty;
        }

        if (state.SelectedIndex.HasValue)
        {
            SelectedIndex = state.SelectedIndex.Value;
        }

        if (state.ScrollY.HasValue)
        {
            _listView.ScrollPanel.ScrollY = Math.Max(0, state.ScrollY.Value);
        }

        if (state.IsOpen == true)
        {
            Open();
        }
        else if (state.IsOpen == false)
        {
            LayoutPopup();
            _popup.Close();
        }
    }

    public void AddItem(UiSelectableRow item)
    {
        _listView.AddItem(item);
    }

    public bool RemoveItem(UiSelectableRow item)
    {
        return _listView.RemoveItem(item);
    }

    public void ClearItems()
    {
        _listView.ClearItems();
    }

    public void Open(UiUpdateContext? context = null)
    {
        LayoutPopup();
        _popup.Open(_popup.Bounds);
        _listView.FilterText = ShowFilterField ? _filterField.Text : string.Empty;
        if (context != null)
        {
            context.Value.Focus.RequestFocus(ShowFilterField ? _filterField : ResolvePopupFocusTarget());
        }
    }

    public void Close()
    {
        _filterField.Text = string.Empty;
        _listView.FilterText = string.Empty;
        _popup.Close();
    }

    public void Toggle(UiUpdateContext? context = null)
    {
        if (_popup.IsOpen)
        {
            Close();
        }
        else
        {
            Open(context);
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _hovered = Bounds.Contains(context.Input.MousePosition);

        if (context.Input.LeftClicked && _hovered)
        {
            context.Focus.RequestFocus(this);
            Toggle(context);
        }
        else if (_focused && !_popup.IsOpen && (context.Input.Navigation.Activate || context.Input.Navigation.MoveDown))
        {
            Open(context);
        }
        else if (_focused && _popup.IsOpen && context.Input.Navigation.Activate)
        {
            Close();
        }

        _listView.FilterText = ShowFilterField ? _filterField.Text : string.Empty;
        LayoutPopup();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor fill = _hovered || _popup.IsOpen ? HoverBackground : Background;
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, fill);
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);

        UiFont font = ResolveFont(context.DefaultFont);
        string text = GetDisplayText();
        UiColor textColor = string.IsNullOrEmpty(text) ? PlaceholderColor : TextColor;
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(text, new UiPoint(Bounds.X + Padding, textY), textColor, TextScale, font);

        DrawArrow(context);
        base.Render(context);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible)
        {
            return null;
        }

        if (_popup.IsOpen)
        {
            UiElement? popupHit = _popup.HitTest(point);
            if (popupHit != null)
            {
                return popupHit;
            }
        }

        return base.HitTest(point);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
    }

    private void LayoutPopup()
    {
        int popupPadding = Math.Max(0, PopupPadding);
        int popupSpacing = Math.Max(0, PopupSpacing);
        int width = DropdownWidth > 0 ? DropdownWidth : Bounds.Width;
        int contentHeight = GetListContentHeight();
        int filterHeight = ShowFilterField ? Math.Max(20, FilterHeight) : 0;
        int popupHeight = popupPadding * 2 + contentHeight + filterHeight;
        if (ShowFilterField && contentHeight > 0)
        {
            popupHeight += popupSpacing;
        }

        popupHeight = Math.Min(Math.Max(filterHeight + popupPadding * 2, popupHeight), Math.Max(40, DropdownMaxHeight));

        UiRect popupBounds = UiPopupLayout.BuildBounds(Bounds, new UiPoint(width, popupHeight), UiPopupPlacement.BottomLeft);
        popupBounds = _popup.ClampToParent ? UiPopupLayout.Clamp(_popup, popupBounds) : popupBounds;
        _popup.Bounds = popupBounds;

        int contentX = popupBounds.X + popupPadding;
        int contentY = popupBounds.Y + popupPadding;
        int contentWidth = Math.Max(0, popupBounds.Width - popupPadding * 2);
        if (ShowFilterField)
        {
            _filterField.Visible = true;
            _filterField.Bounds = new UiRect(contentX, contentY, contentWidth, filterHeight);
            contentY += filterHeight + popupSpacing;
        }
        else
        {
            _filterField.Visible = false;
            _filterField.Bounds = new UiRect(contentX, contentY, contentWidth, 0);
        }

        int listHeight = Math.Max(0, popupBounds.Bottom - popupPadding - contentY);
        _listView.Bounds = new UiRect(contentX, contentY, contentWidth, listHeight);
    }

    private int GetListContentHeight()
    {
        int height = 0;
        int visibleCount = 0;
        foreach (UiSelectableRow item in _listView.Items)
        {
            bool visible = string.IsNullOrWhiteSpace(_listView.FilterText)
                || (_listView.FilterPredicate?.Invoke(item, _listView.FilterText.Trim())
                    ?? item.EffectiveSearchText.Contains(_listView.FilterText.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!visible)
            {
                continue;
            }

            visibleCount++;
            height += (item.Bounds.Height > 0 ? item.Bounds.Height : Math.Max(1, _listView.ItemHeight)) + Math.Max(0, _listView.ItemSpacing);
        }

        if (visibleCount == 0)
        {
            return Math.Max(_listView.ItemHeight, 32);
        }

        return Math.Max(0, height - Math.Max(0, _listView.ItemSpacing));
    }

    private UiElement ResolvePopupFocusTarget()
    {
        if (SelectedItem != null && SelectedItem.Visible)
        {
            return SelectedItem;
        }

        foreach (UiSelectableRow item in _listView.Items)
        {
            if (item.Visible)
            {
                return item;
            }
        }

        return _listView;
    }

    private string GetDisplayText()
    {
        UiSelectableRow? selected = SelectedItem;
        if (selected == null)
        {
            return Placeholder;
        }

        if (DisplayTextSelector != null)
        {
            return DisplayTextSelector(selected);
        }

        return !string.IsNullOrEmpty(selected.Text) ? selected.Text : Placeholder;
    }

    private void DrawArrow(UiRenderContext context)
    {
        int size = Math.Max(4, ArrowSize);
        int x = Bounds.Right - Padding - size;
        int y = Bounds.Y + (Bounds.Height - size) / 2;
        UiArrow.DrawTriangle(context.Renderer, new UiRect(x, y, size, size), UiArrowDirection.Down, ArrowColor);
    }
}
