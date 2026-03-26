namespace OpenControls.Controls;

public sealed class UiPicker : UiCombo
{
    private IReadOnlyList<UiPickerItem> _items = Array.Empty<UiPickerItem>();
    private readonly List<UiSelectableRow> _rows = new();
    private readonly List<UiPickerItem> _itemSnapshot = new();
    private int _scrollIndex;

    public UiPicker()
    {
        ShowFilterField = true;
        ListView.ItemSpacing = 2;
        ListView.Padding = 0;
        ListView.Border = UiColor.Transparent;
        ListView.Background = UiColor.Transparent;
        CloseOnSelection = true;
        DisplayTextSelector = row => row.Text;
    }

    public IReadOnlyList<UiPickerItem> Items
    {
        get => _items;
        set => _items = value ?? Array.Empty<UiPickerItem>();
    }

    public new UiPickerItem? SelectedItem
    {
        get
        {
            int index = SelectedIndex;
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }
    }

    public int ItemHeight { get; set; } = 28;
    public int MaxVisibleItems { get; set; } = 8;
    public int ScrollWheelItems { get; set; } = 1;
    public int ScrollIndex
    {
        get => Math.Max(0, _scrollIndex);
        set => _scrollIndex = Math.Max(0, value);
    }

    public bool ShowSecondaryText { get; set; } = true;
    public bool ShowImages { get; set; } = true;
    public int ImageSize { get; set; } = 18;

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
            UiPickerItem item = _items[i] ?? new UiPickerItem();
            _itemSnapshot.Add(item);

            UiSelectableRow row = new()
            {
                Text = item.Text,
                SecondaryText = ShowSecondaryText ? item.SecondaryText : string.Empty,
                SearchText = item.SearchText,
                ImageSource = ShowImages ? item.ImageSource : null,
                ImageSize = ImageSize
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
        ListView.ItemSpacing = 2;
        ListView.ScrollPanel.ScrollWheelStep = Math.Max(1, ScrollWheelItems) * Math.Max(1, ItemHeight);
        DropdownMaxHeight = MaxVisibleItems > 0
            ? Math.Max(48, MaxVisibleItems * Math.Max(1, ItemHeight) + Math.Max(0, PopupPadding) * 2)
            : int.MaxValue;
        ApplyRequestedScrollIndex();
    }

    private void SyncRowStyles()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            UiSelectableRow row = _rows[i];
            row.TextScale = TextScale;
            row.SecondaryTextScale = Math.Max(1, TextScale - 1);
            row.Padding = Padding;
            row.TextColor = TextColor;
            row.SelectedTextColor = TextColor;
            row.SecondaryTextColor = PlaceholderColor;
            row.ImageSize = ImageSize;
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
            if (!ReferenceEquals(_itemSnapshot[i], _items[i]))
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
