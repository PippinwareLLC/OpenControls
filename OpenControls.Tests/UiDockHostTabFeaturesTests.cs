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
    public void DockHost_CloseButtonCanRemoveLastWindowWhenEnabled()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            AllowClosingLastWindow = true
        };
        host.AddWindow(new UiWindow
        {
            Title = "Window 0",
            AllowClose = true
        });

        UiRect tabBounds = host.GetTabBounds(0);
        UiPoint closePoint = new(tabBounds.Right - 2, tabBounds.Y + tabBounds.Height / 2);

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftClicked = true,
            LeftDown = true
        });

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftReleased = true
        });

        Assert.Empty(host.Windows);
        Assert.Equal(-1, host.ActiveIndex);
    }

    [Fact]
    public void DockHost_LeftCloseButtonPlacement_ClosesFromLeadingEdge()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            AllowClosingLastWindow = true,
            CloseButtonPlacement = UiTabCloseButtonPlacement.Left
        };
        host.AddWindow(new UiWindow
        {
            Title = "Window 0",
            AllowClose = true
        });

        UiRect tabBounds = host.GetTabBounds(0);
        UiPoint closePoint = new(tabBounds.X + 2, tabBounds.Y + tabBounds.Height / 2);

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftClicked = true,
            LeftDown = true
        });

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftReleased = true
        });

        Assert.Empty(host.Windows);
        Assert.Equal(-1, host.ActiveIndex);
    }

    [Fact]
    public void DockHost_CloseButtonKeepsLastWindowWhenDisabled()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            AllowClosingLastWindow = false
        };
        host.AddWindow(new UiWindow
        {
            Title = "Window 0",
            AllowClose = true
        });

        UiRect tabBounds = host.GetTabBounds(0);
        UiPoint closePoint = new(tabBounds.Right - 2, tabBounds.Y + tabBounds.Height / 2);

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftClicked = true,
            LeftDown = true
        });

        Update(host, new UiInputState
        {
            MousePosition = closePoint,
            LeftReleased = true
        });

        Assert.Single(host.Windows);
        Assert.Equal("Window 0", host.Windows[0].Title);
    }

    [Fact]
    public void TabBar_LeftCloseButtonPlacement_UsesLeadingCloseBounds()
    {
        UiTabBar bar = new()
        {
            Bounds = new UiRect(0, 0, 320, 120),
            CloseButtonPlacement = UiTabCloseButtonPlacement.Left
        };
        bar.AddChild(new UiTabItem
        {
            Text = "Overview",
            AllowClose = true
        });

        Update(bar, new UiInputState());
        UiRect tabBounds = bar.GetTabBounds(0);
        UiRect closeBounds = bar.GetTabCloseBounds(0);

        Assert.Equal(tabBounds.X, closeBounds.X);
        Assert.True(closeBounds.Width > 0);
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

    [Fact]
    public void DockHost_ContentBoundsUseFullWindowBoundsWhenDockHostHidesTitleBars()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            HideDockedTitleBars = true
        };

        UiWindow window = new()
        {
            Title = "Window 0",
            ShowTitleBar = true,
            Bounds = new UiRect(10, 20, 200, 100)
        };

        host.AddWindow(window);

        Assert.Equal(window.Bounds, window.ContentBounds);
    }

    [Fact]
    public void DockHost_PanelInsetOffsetsTabsAndDockedContent()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            HideDockedTitleBars = true,
            PanelInset = 4,
            TabBarHeight = 24
        };

        UiWindow window = new()
        {
            Title = "Window 0"
        };
        host.AddWindow(window);

        Update(host, new UiInputState());

        Assert.Equal(new UiRect(4, 4, 120, 24), host.GetTabBounds(0));
        Assert.Equal(new UiRect(4, 28, 312, 128), window.Bounds);
        Assert.Equal(window.Bounds, window.ContentBounds);
    }

    [Fact]
    public void DockHost_EmptyTabBarSpaceQueryAcceptsOnlyUnusedTabBackground()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            TabBarHeight = 24,
            TabWidth = 120
        };
        host.AddWindow(new UiWindow { Title = "Layers" });

        UiRect tab = host.GetTabBounds(0);

        Assert.False(host.IsPointInEmptyTabBarSpace(new UiPoint(tab.X + 20, tab.Y + 12)));
        Assert.True(host.IsPointInEmptyTabBarSpace(new UiPoint(200, 12)));
        Assert.False(host.IsPointInEmptyTabBarSpace(new UiPoint(200, 40)));
        Assert.False(host.IsPointInEmptyTabBarSpace(new UiPoint(321, 12)));
    }

    [Fact]
    public void DockHost_TrailingTabInsetReservesHostChromeWithoutShrinkingContent()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            TabBarHeight = 24,
            TabWidth = 120,
            TabTrailingInset = 24
        };
        UiWindow window = new() { Title = "Layers" };
        host.AddWindow(window);

        Update(host, new UiInputState());

        Assert.True(host.IsPointInEmptyTabBarSpace(new UiPoint(200, 12)));
        Assert.False(host.IsPointInEmptyTabBarSpace(new UiPoint(308, 12)));
        Assert.Equal(new UiRect(0, 24, 320, 136), window.Bounds);
    }

    [Fact]
    public void DockHost_EmptyTabBarSpaceQueryRejectsOverflowAndCollapseControls()
    {
        UiDockHost overflowing = new()
        {
            Bounds = new UiRect(0, 0, 180, 120),
            TabBarHeight = 24,
            TabWidth = 120,
            ScrollButtonWidth = 18,
            OverflowButtonWidth = 18,
            ShowOverflowMenuButton = true
        };
        overflowing.AddWindow(new UiWindow { Title = "Layers" });
        overflowing.AddWindow(new UiWindow { Title = "Channels" });
        overflowing.AddWindow(new UiWindow { Title = "Paths" });

        Assert.NotEmpty(overflowing.GetOverflowWindowIndices());
        Assert.False(overflowing.IsPointInEmptyTabBarSpace(new UiPoint(153, 12)));
        Assert.False(overflowing.IsPointInEmptyTabBarSpace(new UiPoint(171, 12)));

        overflowing.ActivateWindow(2);
        Assert.False(overflowing.IsPointInEmptyTabBarSpace(new UiPoint(9, 12)));

        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 600, 300)
        };
        UiDockHost paletteHost = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Right,
            0.30f);
        workspace.RootHost.DockWindow(new UiWindow { Title = "Document" });
        paletteHost.DockWindow(new UiWindow { Title = "Color" });
        workspace.Arrange();
        UiRect collapse = paletteHost.CollapseToggleBounds;

        Assert.True(collapse.Width > 0);
        Assert.False(paletteHost.IsPointInEmptyTabBarSpace(new UiPoint(
            collapse.X + collapse.Width / 2,
            collapse.Y + collapse.Height / 2)));
    }

    [Fact]
    public void DockHost_DraggingTabOutsideBoundsDetachesImmediatelyUsingScreenCoordinates()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };
        host.AddWindow(new UiWindow { Title = "Window 0" });
        host.AddWindow(new UiWindow { Title = "Window 1" });

        UiRect tabBounds = host.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 10, tabBounds.Y + 12);
        UiPoint screenDetachPoint = new(700, 500);

        UiWindow? detachedWindow = null;
        UiPoint detachedPoint = default;
        host.TabDetached += (window, point) =>
        {
            detachedWindow = window;
            detachedPoint = point;
        };

        Update(host, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = new UiPoint(300, 200),
            LeftClicked = true,
            LeftDown = true
        });

        Update(host, new UiInputState
        {
            MousePosition = new UiPoint(400, 20),
            ScreenMousePosition = screenDetachPoint,
            LeftDown = true
        });

        Assert.NotNull(detachedWindow);
        Assert.Equal("Window 0", detachedWindow!.Title);
        Assert.Equal(new UiPoint(screenDetachPoint.X - 10, screenDetachPoint.Y - 12), detachedPoint);
        Assert.Single(host.Windows);
    }

    [Fact]
    public void DockHost_ContextMenuDetach_UsesScreenCoordinates()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };
        host.AddWindow(new UiWindow { Title = "Window 0" });
        host.AddWindow(new UiWindow { Title = "Window 1" });

        UiRect tabBounds = host.GetTabBounds(0);
        UiPoint rightClickPoint = new(tabBounds.X + 10, tabBounds.Y + 12);
        UiPoint screenDetachPoint = new(1220, 760);

        UiWindow? detachedWindow = null;
        UiPoint detachedPoint = default;
        host.TabDetached += (window, point) =>
        {
            detachedWindow = window;
            detachedPoint = point;
        };

        Update(host, new UiInputState
        {
            MousePosition = rightClickPoint,
            ScreenMousePosition = new UiPoint(480, 320),
            RightClicked = true
        });

        Update(host, new UiInputState
        {
            MousePosition = new UiPoint(rightClickPoint.X, host.TabBarHeight + 1),
            ScreenMousePosition = screenDetachPoint,
            LeftClicked = true
        });

        Assert.NotNull(detachedWindow);
        Assert.Equal("Window 0", detachedWindow!.Title);
        Assert.Equal(screenDetachPoint, detachedPoint);
        Assert.Single(host.Windows);
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
}
