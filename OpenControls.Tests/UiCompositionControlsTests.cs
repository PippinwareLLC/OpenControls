using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiCompositionControlsTests
{
    [Fact]
    public void PopupHelpers_ClampContextBoundsToParent()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 100, 100)
        };

        UiPopup popup = new();
        root.AddChild(popup);

        popup.OpenContext(new UiPoint(88, 92), new UiPoint(40, 40));

        Assert.Equal(60, popup.Bounds.X);
        Assert.Equal(60, popup.Bounds.Y);
        Assert.Equal(100, popup.Bounds.Right);
        Assert.Equal(100, popup.Bounds.Bottom);
    }

    [Fact]
    public void ListView_RowNavigationMovesSelectionAndFocus()
    {
        UiListView listView = CreateListView();
        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(listView, focus, clipboard, new UiInputState());
        listView.SelectedIndex = 0;
        focus.RequestFocus(listView.Items[0]);

        Update(listView, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                MoveDown = true
            }
        });

        Assert.Equal(1, listView.SelectedIndex);
        Assert.Same(listView.Items[1], focus.Focused);
    }

    [Fact]
    public void Combo_ComposedFilteringAndSelection_WorkEndToEnd()
    {
        UiCombo combo = new()
        {
            Bounds = new UiRect(0, 0, 180, 26),
            ShowFilterField = true
        };

        combo.AddItem(new UiSelectableRow { Text = "Alpha", SecondaryText = "Scene" });
        combo.AddItem(new UiSelectableRow { Text = "Beta", SecondaryText = "Asset" });
        combo.AddItem(new UiSelectableRow { Text = "Gamma", SecondaryText = "Asset" });

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = new UiPoint(8, 8),
            ScreenMousePosition = new UiPoint(8, 8),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(combo.IsOpen);
        Assert.Same(combo.FilterField, focus.Focused);

        Update(combo, focus, clipboard, new UiInputState
        {
            TextInput = new[] { 'g' }
        });

        Update(combo, focus, clipboard, new UiInputState());

        Assert.False(combo.ListView.Items[0].Visible);
        Assert.False(combo.ListView.Items[1].Visible);
        Assert.True(combo.ListView.Items[2].Visible);

        UiSelectableRow gamma = combo.ListView.Items[2];
        UiPoint rowPoint = new UiPoint(combo.ListView.Bounds.X + 8, combo.ListView.Bounds.Y + gamma.Bounds.Y + 8);

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftClicked = true,
            LeftDown = true
        });

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftReleased = true
        });

        Assert.Equal(2, combo.SelectedIndex);
        Assert.Equal("Gamma", combo.SelectedItem?.Text);
        Assert.False(combo.IsOpen);
    }

    private static UiListView CreateListView()
    {
        UiListView listView = new()
        {
            Bounds = new UiRect(0, 0, 200, 120),
            ItemHeight = 28
        };

        listView.AddItem(new UiSelectableRow { Text = "Alpha" });
        listView.AddItem(new UiSelectableRow { Text = "Beta" });
        listView.AddItem(new UiSelectableRow { Text = "Gamma" });
        return listView;
    }

    private static void Update(UiElement element, UiFocusManager focus, IUiClipboard clipboard, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            clipboard));
    }
}
