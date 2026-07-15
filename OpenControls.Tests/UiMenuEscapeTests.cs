using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiMenuEscapeTests
{
    [Fact]
    public void EscapeClosesOnlyDeepestSubmenuWhenConfigured()
    {
        UiMenuBar menu = CreateNestedMenu(closeOneLevel: true);
        UiFocusManager focus = new();
        UiPoint parentItem = OpenNestedMenu(menu, focus);

        Update(menu, focus, Escape(parentItem));

        Assert.True(menu.HasOpenMenu);
        Assert.Single(menu.GetDebugOpenLayoutBounds());
        Assert.True(menu.TryGetDebugHighlightedItemBounds(out UiRect highlighted));
        Assert.Contains(highlighted, menu.GetDebugOpenItemBounds());

        Update(menu, focus, Hover(parentItem));
        Assert.True(menu.HasOpenMenu);
        Assert.Single(menu.GetDebugOpenLayoutBounds());

        Update(menu, focus, Escape(parentItem));

        Assert.False(menu.HasOpenMenu);
        Assert.Empty(menu.GetDebugOpenLayoutBounds());
    }

    [Fact]
    public void EscapeClosesEntireMenuTreeByDefault()
    {
        UiMenuBar menu = CreateNestedMenu(closeOneLevel: false);
        UiFocusManager focus = new();
        UiPoint parentItem = OpenNestedMenu(menu, focus);

        Update(menu, focus, Escape(parentItem));

        Assert.False(menu.HasOpenMenu);
        Assert.Empty(menu.GetDebugOpenLayoutBounds());
    }

    [Fact]
    public void EscapeClosesOnlyDeepestPopupSubmenuWhenConfigured()
    {
        UiMenuBar menu = CreateNestedPopup(closeOneLevel: true);
        UiFocusManager focus = new();
        menu.OpenPopup();
        Update(menu, focus, Hover(new UiPoint(-1, -1)));
        UiRect parent = Assert.Single(menu.GetDebugOpenItemBounds());
        UiPoint parentItem = new(parent.X + parent.Width / 2, parent.Y + parent.Height / 2);
        Update(menu, focus, Hover(parentItem));
        Assert.Equal(2, menu.GetDebugOpenLayoutBounds().Count);

        Update(menu, focus, Escape(parentItem));

        Assert.True(menu.IsPopupOpen);
        Assert.Single(menu.GetDebugOpenLayoutBounds());

        Update(menu, focus, Hover(parentItem));
        Assert.Single(menu.GetDebugOpenLayoutBounds());

        Update(menu, focus, Escape(parentItem));

        Assert.False(menu.IsPopupOpen);
        Assert.Empty(menu.GetDebugOpenLayoutBounds());
    }

    private static UiMenuBar CreateNestedMenu(bool closeOneLevel)
    {
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 320, 24),
            CloseOneLevelOnEscape = closeOneLevel
        };
        UiMenuBar.MenuItem recent = new() { Text = "Recent" };
        recent.Items.Add(new UiMenuBar.MenuItem { Text = "Example" });
        UiMenuBar.MenuItem file = new() { Text = "File" };
        file.Items.Add(recent);
        menu.Items.Add(file);
        return menu;
    }

    private static UiMenuBar CreateNestedPopup(bool closeOneLevel)
    {
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 200, 0),
            DisplayMode = UiMenuDisplayMode.Popup,
            CloseOneLevelOnEscape = closeOneLevel
        };
        UiMenuBar.MenuItem recent = new() { Text = "Recent" };
        recent.Items.Add(new UiMenuBar.MenuItem { Text = "Example" });
        menu.Items.Add(recent);
        return menu;
    }

    private static UiPoint OpenNestedMenu(UiMenuBar menu, UiFocusManager focus)
    {
        Update(menu, focus, Click(new UiPoint(10, 12)));
        UiRect parent = Assert.Single(menu.GetDebugOpenItemBounds());
        UiPoint parentItem = new(parent.X + parent.Width / 2, parent.Y + parent.Height / 2);
        Update(menu, focus, Hover(parentItem));
        Assert.Equal(2, menu.GetDebugOpenLayoutBounds().Count);
        return parentItem;
    }

    private static void Update(UiMenuBar menu, UiFocusManager focus, UiInputState input)
    {
        menu.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }

    private static UiInputState Click(UiPoint point) => new()
    {
        MousePosition = point,
        ScreenMousePosition = point,
        LeftClicked = true,
        LeftDown = true
    };

    private static UiInputState Hover(UiPoint point) => new()
    {
        MousePosition = point,
        ScreenMousePosition = point
    };

    private static UiInputState Escape(UiPoint point) => new()
    {
        MousePosition = point,
        ScreenMousePosition = point,
        KeysPressed = [UiKey.Escape],
        Navigation = new UiNavigationInput { Escape = true }
    };
}
