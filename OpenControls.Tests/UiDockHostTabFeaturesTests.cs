using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockHostTabFeaturesTests
{
    [Fact]
    public void DockHost_AutoSizeTabsAndOverflowIndicesReflectTitleWidths()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 220, 120),
            AutoSizeTabs = true,
            TabWidth = 72,
            TabMaxWidth = 140
        };

        host.AddWindow(new UiWindow { Title = "One" });
        host.AddWindow(new UiWindow { Title = "A Much Longer Tool Title" });
        host.AddWindow(new UiWindow { Title = "Another Very Long Document Title" });

        UiRect shortTab = host.GetTabBounds(0);
        UiRect longTab = host.GetTabBounds(1);
        IReadOnlyList<int> overflow = host.GetOverflowWindowIndices();

        Assert.True(longTab.Width >= shortTab.Width);
        Assert.NotEmpty(overflow);
    }

    [Fact]
    public void DockHost_CloseOtherWindowsLeavesRequestedTab()
    {
        UiDockHost host = CreateHostWithFourWindows();

        int closed = host.CloseOtherWindows(1);

        Assert.Equal(3, closed);
        Assert.Single(host.Windows);
        Assert.Equal("Window 1", host.Windows[0].Title);
        Assert.Equal(0, host.ActiveIndex);
    }

    [Fact]
    public void DockHost_CloseWindowsToRightOnlyRemovesTabsToTheRight()
    {
        UiDockHost host = CreateHostWithFourWindows();

        int closed = host.CloseWindowsToRight(1);

        Assert.Equal(2, closed);
        Assert.Equal(2, host.Windows.Count);
        Assert.Equal("Window 0", host.Windows[0].Title);
        Assert.Equal("Window 1", host.Windows[1].Title);
    }

    [Fact]
    public void DockHost_CloseAllWindowsRemovesAllClosableTabs()
    {
        UiDockHost host = CreateHostWithFourWindows();

        int closed = host.CloseAllWindows();

        Assert.Equal(4, closed);
        Assert.Empty(host.Windows);
        Assert.Equal(-1, host.ActiveIndex);
    }

    private static UiDockHost CreateHostWithFourWindows()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };

        for (int i = 0; i < 4; i++)
        {
            host.AddWindow(new UiWindow { Title = $"Window {i}" });
        }

        return host;
    }
}
