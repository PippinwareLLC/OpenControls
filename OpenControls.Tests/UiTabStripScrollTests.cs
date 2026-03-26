using OpenControls.Controls;
using System.Reflection;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTabStripScrollTests
{
    [Fact]
    public void TabBar_RightScrollArrowPersistsManualScroll()
    {
        UiTabBar bar = new()
        {
            Bounds = new UiRect(0, 0, 220, 120),
            AutoSizeTabs = true,
            TabMaxWidth = 140
        };

        bar.AddChild(new UiTabItem { Text = "Overview / Runtime Summary" });
        bar.AddChild(new UiTabItem { Text = "Diagnostics / Build And Errors" });
        bar.AddChild(new UiTabItem { Text = "Automation / Scheduled Jobs" });
        bar.AddChild(new UiTabItem { Text = "Collaboration / Review Notes" });

        Update(bar, new UiInputState());
        UiRect before = bar.GetTabBounds(0);

        Update(bar, new UiInputState
        {
            MousePosition = new UiPoint(210, 12),
            ScreenMousePosition = new UiPoint(210, 12),
            LeftClicked = true
        });

        UiRect afterClick = bar.GetTabBounds(0);
        Assert.True(afterClick.X < before.X, $"before={before.X}, after={afterClick.X}");

        Update(bar, new UiInputState());
        UiRect afterNextFrame = bar.GetTabBounds(0);

        Assert.Equal(afterClick.X, afterNextFrame.X);
    }

    [Fact]
    public void DockHost_RightScrollArrowPersistsManualScroll()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 220, 120),
            AutoSizeTabs = true,
            TabWidth = 72,
            TabMaxWidth = 140
        };

        host.AddWindow(new UiWindow { Title = "Overview / Runtime Summary" });
        host.AddWindow(new UiWindow { Title = "Diagnostics / Build And Errors" });
        host.AddWindow(new UiWindow { Title = "Automation / Scheduled Jobs" });
        host.AddWindow(new UiWindow { Title = "Collaboration / Review Notes" });

        Update(host, new UiInputState());
        UiRect scrollRightBounds = GetPrivateRect(host, "_scrollRightBounds");
        UiRect before = host.GetTabBounds(0);

        Update(host, new UiInputState
        {
            MousePosition = new UiPoint(scrollRightBounds.X + scrollRightBounds.Width / 2, scrollRightBounds.Y + scrollRightBounds.Height / 2),
            ScreenMousePosition = new UiPoint(scrollRightBounds.X + scrollRightBounds.Width / 2, scrollRightBounds.Y + scrollRightBounds.Height / 2),
            LeftClicked = true
        });

        UiRect afterClick = host.GetTabBounds(0);
        Assert.True(afterClick.X < before.X);

        Update(host, new UiInputState());
        UiRect afterNextFrame = host.GetTabBounds(0);

        Assert.Equal(afterClick.X, afterNextFrame.X);
    }

    private static void Update(UiElement element, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }

    private static UiRect GetPrivateRect(object instance, string fieldName)
    {
        FieldInfo? field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<UiRect>(field?.GetValue(instance));
    }

}
