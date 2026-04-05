namespace OpenControls.Controls;

public sealed class UiComboBox : UiCombo
{
    private IReadOnlyList<string> _items = Array.Empty<string>();
    private readonly List<UiSelectableRow> _rows = new();
    private readonly List<string> _itemSnapshot = new();
    private int _scrollIndex;
    private string _emptyText = "No items";

    public UiComboBox()
    {
        ShowFilterField = false;
        ListView.ItemSpacing = 0;
        ListView.Padding = 0;
        ListView.Border = UiColor.Transparent;
        ListView.Background = UiColor.Transparent;
        CloseOnSelection = true;
    }

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

            if (SelectedIndex >= _items.Count)
            {
                SelectedIndex = _items.Count - 1;
            }
        }
    }

    public new string EmptyText
    {
        get => _emptyText;
        set => _emptyText = value ?? string.Empty;
    }

    public int ItemHeight { get; set; } = 22;
    public int MaxVisibleItems { get; set; } = 6;

    public int ScrollIndex
    {
        get => Math.Max(0, _scrollIndex);
        set => SetInvalidatingValue(ref _scrollIndex, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public int ScrollWheelItems { get; set; } = 1;
    public bool ShowEmptyText { get; set; } = true;
    public UiColor ItemHover { get; set; } = new UiColor(45, 52, 70);
    public UiColor ItemSelected { get; set; } = new UiColor(70, 80, 100);

    public override void Update(UiUpdateContext context)
    {
        SyncRows();
        SyncSettings();
        base.Update(context);
        SyncScrollIndexFromView();
    }

    private void SyncRows()
    {
        if (!NeedsRowRefresh())
        {
            SyncRowStyles();
            return;
        }

        int previousSelection = SelectedIndex;
        _itemSnapshot.Clear();
        _rows.Clear();
        ClearItems();

        for (int i = 0; i < _items.Count; i++)
        {
            string itemText = _items[i] ?? string.Empty;
            _itemSnapshot.Add(itemText);

            UiSelectableRow row = new()
            {
                Text = itemText
            };

            _rows.Add(row);
            AddItem(row);
        }

        if (previousSelection >= 0 && previousSelection < _rows.Count)
        {
            SelectedIndex = previousSelection;
        }

        SyncRowStyles();
    }

    private void SyncSettings()
    {
        ListView.ItemHeight = Math.Max(1, ItemHeight);
        ListView.ItemSpacing = 0;
        ListView.ScrollPanel.ScrollWheelStep = Math.Max(1, ScrollWheelItems) * Math.Max(1, ItemHeight);
        DropdownMaxHeight = MaxVisibleItems > 0
            ? Math.Max(40, MaxVisibleItems * Math.Max(1, ItemHeight) + Math.Max(0, PopupPadding) * 2)
            : int.MaxValue;
        base.EmptyText = ShowEmptyText ? _emptyText : string.Empty;
        ApplyRequestedScrollIndex();
    }

    private void SyncRowStyles()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            UiSelectableRow row = _rows[i];
            row.TextScale = TextScale;
            row.Padding = Padding;
            row.HoverBackground = ItemHover;
            row.SelectedBackground = ItemSelected;
            row.TextColor = TextColor;
            row.SelectedTextColor = TextColor;
            row.SecondaryTextColor = TextColor;
            row.Bounds = new UiRect(row.Bounds.X, row.Bounds.Y, row.Bounds.Width, Math.Max(1, ItemHeight));
        }
    }

    private bool NeedsRowRefresh()
    {
        if (_itemSnapshot.Count != _items.Count || _rows.Count != _items.Count)
        {
            return true;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            string current = _items[i] ?? string.Empty;
            if (!string.Equals(_itemSnapshot[i], current, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyRequestedScrollIndex()
    {
        int itemStep = Math.Max(1, ItemHeight);
        int targetScrollY = _scrollIndex * itemStep;
        if (ListView.ScrollPanel.ScrollY != targetScrollY)
        {
            ListView.ScrollPanel.ScrollY = targetScrollY;
        }
    }

    private void SyncScrollIndexFromView()
    {
        int itemStep = Math.Max(1, ItemHeight);
        _scrollIndex = Math.Max(0, ListView.ScrollPanel.ScrollY / itemStep);
    }
}
