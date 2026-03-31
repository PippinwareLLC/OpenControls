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

    [Fact]
    public void DockHost_TryDetachWindow_RespectsDetachPredicate()
    {
        UiDockHost host = CreateHostWithFourWindows();
        host.CanDetachWindowPredicate = window => window.Title == "Window 1";

        UiWindow? detachedWindow = null;
        host.TabDetached += (window, _) => detachedWindow = window;

        bool firstDetached = host.TryDetachWindow(0, new UiPoint(400, 20));
        bool secondDetached = host.TryDetachWindow(1, new UiPoint(420, 20));

        Assert.False(firstDetached);
        Assert.True(secondDetached);
        Assert.NotNull(detachedWindow);
        Assert.Equal("Window 1", detachedWindow!.Title);
        Assert.DoesNotContain(host.Windows, window => window.Title == "Window 1");
    }

    [Fact]
    public void DockHost_UpdateChildren_AllowsWindowRemovalDuringChildUpdate()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };

        UiWindow victim = new() { Title = "Victim" };
        UiWindow remover = new() { Title = "Remover" };
        remover.AddContentChild(new RemovingElement(host, victim));
        host.AddWindow(remover);
        host.AddWindow(victim);

        host.Update(new UiUpdateContext(
            new UiInputState(),
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));

        Assert.Single(host.Windows);
        Assert.Equal("Remover", host.Windows[0].Title);
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

    private sealed class RemovingElement(UiDockHost host, UiWindow target) : UiElement
    {
        private readonly UiDockHost _host = host;
        private readonly UiWindow _target = target;
        private bool _removed;

        public override void Update(UiUpdateContext context)
        {
            if (!_removed)
            {
                _removed = true;
                _host.RemoveWindow(_target);
            }
        }
    }
}
