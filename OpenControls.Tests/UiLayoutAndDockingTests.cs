using System;
using OpenControls.Controls;
using OpenControls.State;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiLayoutAndDockingTests
{
    [Fact]
    public void Stack_HorizontalLayoutSupportsFixedPercentageAndFill()
    {
        UiStack stack = new()
        {
            Bounds = new UiRect(0, 0, 300, 40),
            Orientation = UiLayoutOrientation.Horizontal,
            Padding = UiThickness.Uniform(4),
            Gap = 8
        };

        UiPanel fixedPanel = new() { Bounds = new UiRect(0, 0, 10, 10) };
        UiPanel percentPanel = new() { Bounds = new UiRect(0, 0, 10, 10) };
        UiPanel fillPanel = new() { Bounds = new UiRect(0, 0, 10, 10) };

        stack.AddChild(fixedPanel);
        stack.SetLayout(fixedPanel, new UiStackItemLayout
        {
            PrimaryLength = UiLayoutLength.Fixed(84),
            CrossLength = UiLayoutLength.Fill()
        });

        stack.AddChild(percentPanel);
        stack.SetLayout(percentPanel, new UiStackItemLayout
        {
            PrimaryLength = UiLayoutLength.Percentage(0.25f),
            CrossLength = UiLayoutLength.Fill()
        });

        stack.AddChild(fillPanel);
        stack.SetLayout(fillPanel, new UiStackItemLayout
        {
            PrimaryLength = UiLayoutLength.Fill(),
            CrossLength = UiLayoutLength.Fill()
        });

        Update(stack, new UiInputState());

        Assert.Equal(new UiRect(4, 4, 84, 32), fixedPanel.Bounds);
        Assert.Equal(new UiRect(96, 4, 69, 32), percentPanel.Bounds);
        Assert.Equal(new UiRect(173, 4, 123, 32), fillPanel.Bounds);
    }

    [Fact]
    public void Stack_BaselineAlignmentUsesExplicitOffsets()
    {
        UiStack stack = new()
        {
            Bounds = new UiRect(0, 0, 120, 40),
            Orientation = UiLayoutOrientation.Horizontal,
            CrossAlignment = UiStackAlignment.Baseline,
            Gap = 4
        };

        UiPanel tall = new() { Bounds = new UiRect(0, 0, 24, 24) };
        UiPanel shortPanel = new() { Bounds = new UiRect(0, 0, 24, 16) };

        stack.AddChild(tall);
        stack.SetLayout(tall, new UiStackItemLayout
        {
            CrossLength = UiLayoutLength.Fixed(24),
            BaselineOffset = 18
        });

        stack.AddChild(shortPanel);
        stack.SetLayout(shortPanel, new UiStackItemLayout
        {
            CrossLength = UiLayoutLength.Fixed(16),
            BaselineOffset = 10
        });

        Update(stack, new UiInputState());

        Assert.Equal(tall.Bounds.Y + 18, shortPanel.Bounds.Y + 10);
    }

    [Fact]
    public void WrapPanel_WrapsChildrenAcrossLines()
    {
        UiWrapPanel panel = new()
        {
            Bounds = new UiRect(0, 0, 120, 100),
            Padding = UiThickness.Uniform(4),
            ItemSpacing = 4,
            LineSpacing = 6
        };

        UiPanel first = new() { Bounds = new UiRect(0, 0, 50, 20) };
        UiPanel second = new() { Bounds = new UiRect(0, 0, 50, 20) };
        UiPanel third = new() { Bounds = new UiRect(0, 0, 50, 20) };

        panel.AddChild(first);
        panel.AddChild(second);
        panel.AddChild(third);

        Update(panel, new UiInputState());

        Assert.Equal(new UiRect(4, 4, 50, 20), first.Bounds);
        Assert.Equal(new UiRect(58, 4, 50, 20), second.Bounds);
        Assert.Equal(new UiRect(4, 30, 50, 20), third.Bounds);
    }

    [Fact]
    public void Group_IsHoveredWhenChildIsHovered()
    {
        UiGroup group = new()
        {
            Bounds = new UiRect(0, 0, 120, 40)
        };

        UiButton child = new()
        {
            Bounds = new UiRect(8, 8, 60, 24),
            Text = "Inside"
        };

        group.AddChild(child);

        UiContext context = new(group);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(16, 16),
            ScreenMousePosition = new UiPoint(16, 16)
        });

        Assert.Same(child, context.Hovered);
        Assert.True(context.IsHovered(group));
    }

    [Fact]
    public void DockWorkspace_DraggingSplitterUpdatesSplitRatioAndPersistsState()
    {
        UiDockWorkspace workspace = CreateWorkspace();

        Update(workspace, new UiInputState());
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(150, 80),
            ScreenMousePosition = new UiPoint(150, 80),
            LeftClicked = true,
            LeftDown = true
        });
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(220, 80),
            ScreenMousePosition = new UiPoint(220, 80),
            LeftDown = true
        });
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(220, 80),
            ScreenMousePosition = new UiPoint(220, 80),
            LeftReleased = true
        });

        var state = workspace.CaptureState();
        Assert.NotNull(state.Root);
        Assert.True(state.Root!.SplitRatio > 0.5f);
    }

    [Fact]
    public void DockWorkspace_DraggingSplitterRespectsMinimumPaneSize()
    {
        UiDockWorkspace workspace = CreateWorkspace();

        Update(workspace, new UiInputState());
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(150, 80),
            ScreenMousePosition = new UiPoint(150, 80),
            LeftClicked = true,
            LeftDown = true
        });
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(12, 80),
            ScreenMousePosition = new UiPoint(12, 80),
            LeftDown = true
        });

        Assert.Equal(workspace.MinPaneSize, workspace.RootHost.Bounds.Width);
    }

    [Fact]
    public void DockWorkspace_NestedSplitResizePreservesNestedMinimumWidths()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 500, 180),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiDockHost nestedRoot = workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right);
        UiDockHost nestedRight = workspace.SplitHost(nestedRoot, UiDockWorkspace.DockTarget.Right);

        Update(workspace, new UiInputState());
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(250, 80),
            ScreenMousePosition = new UiPoint(250, 80),
            LeftClicked = true,
            LeftDown = true
        });
        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(490, 80),
            ScreenMousePosition = new UiPoint(490, 80),
            LeftDown = true
        });

        int nestedMinimumWidth = workspace.MinPaneSize * 2 + workspace.SplitterThickness;
        int nestedWidth = nestedRoot.Bounds.Width + workspace.SplitterThickness + nestedRight.Bounds.Width;

        Assert.True(nestedWidth >= nestedMinimumWidth);
        Assert.True(workspace.RootHost.Bounds.Width <= 500 - workspace.SplitterThickness - nestedMinimumWidth);
    }

    [Fact]
    public void DockWorkspace_SplitHostCopiesDockHostIslandStyle()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 320, 180)
        };

        workspace.RootHost.PanelInset = 4;
        workspace.RootHost.CornerRadius = 7;
        workspace.RootHost.ClipChildren = true;

        UiDockHost rightHost = workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right);

        Assert.Equal(4, rightHost.PanelInset);
        Assert.Equal(7, rightHost.CornerRadius);
        Assert.True(rightHost.ClipChildren);
    }

    [Fact]
    public void DockWorkspace_ApplyState_ReparentsWindowsFromRemovedHosts()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 300, 180),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiDockHost rightHost = workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right);
        UiWindow window = new()
        {
            Id = "window-a",
            Title = "Window A"
        };
        rightHost.DockWindow(window);

        UiDockWorkspaceState captured = workspace.CaptureState();
        string rootHostId = workspace.RootHost.Id;
        UiDockWorkspaceState restored = new()
        {
            Id = captured.Id,
            Root = new UiDockNodeState
            {
                HostId = rootHostId
            },
            Hosts =
            {
                new UiDockHostState
                {
                    HostId = rootHostId,
                    WindowIds = { window.Id },
                    ActiveIndex = 0
                }
            }
        };

        workspace.ApplyState(restored, new Dictionary<string, UiWindow>(StringComparer.Ordinal)
        {
            [window.Id] = window
        });

        Assert.Same(workspace.RootHost, window.Parent);
        Assert.Single(workspace.RootHost.Windows);
        Assert.Same(window, workspace.RootHost.Windows[0]);
    }

    [Fact]
    public void DockWorkspace_ApplyState_PreservesRatiosAcrossZeroSizedLayoutPass()
    {
        UiDockWorkspace workspace = new()
        {
            Id = "workspace",
            Bounds = new UiRect(0, 0, 0, 0),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiDockHost leftHost = workspace.RootHost;
        UiDockHost bottomHost = workspace.SplitHost(leftHost, UiDockWorkspace.DockTarget.Bottom);
        UiDockHost centerHost = workspace.SplitHost(leftHost, UiDockWorkspace.DockTarget.Right);
        UiDockHost rightHost = workspace.SplitHost(centerHost, UiDockWorkspace.DockTarget.Right);

        UiWindow leftWindow = new() { Id = "left", Title = "Left" };
        UiWindow bottomWindow = new() { Id = "bottom", Title = "Bottom" };
        UiWindow centerWindow = new() { Id = "center", Title = "Center" };
        UiWindow rightWindow = new() { Id = "right", Title = "Right" };

        UiDockWorkspaceState restored = new()
        {
            Id = workspace.Id,
            Root = new UiDockNodeState
            {
                First = new UiDockNodeState
                {
                    First = new UiDockNodeState
                    {
                        HostId = leftHost.Id
                    },
                    Second = new UiDockNodeState
                    {
                        First = new UiDockNodeState
                        {
                            HostId = centerHost.Id
                        },
                        Second = new UiDockNodeState
                        {
                            HostId = rightHost.Id
                        },
                        SplitHorizontal = false,
                        SplitRatio = 0.741f
                    },
                    SplitHorizontal = false,
                    SplitRatio = 0.172f
                },
                Second = new UiDockNodeState
                {
                    HostId = bottomHost.Id
                },
                SplitHorizontal = true,
                SplitRatio = 0.721f
            },
            Hosts =
            {
                new UiDockHostState
                {
                    HostId = leftHost.Id,
                    WindowIds = { leftWindow.Id },
                    ActiveIndex = 0
                },
                new UiDockHostState
                {
                    HostId = bottomHost.Id,
                    WindowIds = { bottomWindow.Id },
                    ActiveIndex = 0
                },
                new UiDockHostState
                {
                    HostId = centerHost.Id,
                    WindowIds = { centerWindow.Id },
                    ActiveIndex = 0
                },
                new UiDockHostState
                {
                    HostId = rightHost.Id,
                    WindowIds = { rightWindow.Id },
                    ActiveIndex = 0
                }
            }
        };

        workspace.ApplyState(restored, new Dictionary<string, UiWindow>(StringComparer.Ordinal)
        {
            [leftWindow.Id] = leftWindow,
            [bottomWindow.Id] = bottomWindow,
            [centerWindow.Id] = centerWindow,
            [rightWindow.Id] = rightWindow
        });

        Update(workspace, new UiInputState());
        workspace.Bounds = new UiRect(0, 0, 1680, 918);
        Update(workspace, new UiInputState());

        UiDockWorkspaceState captured = workspace.CaptureState();
        Assert.NotNull(captured.Root);
        Assert.InRange(captured.Root!.SplitRatio, 0.720f, 0.722f);
        Assert.NotNull(captured.Root.First);
        Assert.InRange(captured.Root.First!.SplitRatio, 0.171f, 0.173f);
        Assert.NotNull(captured.Root.First.Second);
        Assert.InRange(captured.Root.First.Second!.SplitRatio, 0.740f, 0.742f);
    }

    [Fact]
    public void DockWorkspace_DraggingTabOutsideBoundsDetachesImmediatelyUsingScreenCoordinates()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 320, 180)
        };
        workspace.RootHost.AllowDetach = true;

        UiWindow first = new() { Title = "First" };
        UiWindow second = new() { Title = "Second" };
        workspace.RootHost.DockWindow(first);
        workspace.RootHost.DockWindow(second);
        workspace.RootHost.ActivateWindow(0);

        UiRect tabBounds = workspace.RootHost.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint screenDetachPoint = new(840, 640);

        UiWindow? detachedWindow = null;
        UiPoint detachedPoint = default;
        workspace.TabDetached += (window, point) =>
        {
            detachedWindow = window;
            detachedPoint = point;
        };

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = new UiPoint(320, 220),
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(380, 24),
            ScreenMousePosition = screenDetachPoint,
            LeftDown = true
        });

        Assert.NotNull(detachedWindow);
        Assert.Equal("First", detachedWindow!.Title);
        Assert.Equal(new UiPoint(screenDetachPoint.X - 14, screenDetachPoint.Y - 10), detachedPoint);
        Assert.Single(workspace.RootHost.Windows);
        Assert.DoesNotContain(first, workspace.RootHost.Windows);
    }

    [Fact]
    public void DockWorkspace_CommitExternalDock_DocksWindowIntoHoveredHost()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        workspace.RootHost.DockWindow(new UiWindow { Title = "Root" });
        UiDockHost targetHost = workspace.DockHosts[1];
        targetHost.DockWindow(new UiWindow { Title = "Right" });
        UiWindow external = new()
        {
            Title = "External"
        };

        Update(workspace, new UiInputState());

        UiRect bounds = targetHost.Bounds;
        UiPoint centerTarget = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        workspace.PreviewExternalDock(external, centerTarget, new UiRect(20, 20, 120, 80));

        bool committed = workspace.CommitExternalDock(external);

        Assert.True(committed);
        Assert.Same(targetHost, external.Parent);
        Assert.Contains(external, targetHost.Windows);
    }

    [Fact]
    public void DockWorkspace_CommitExternalDock_SplitsHoveredHostForEdgeTargets()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        workspace.RootHost.DockWindow(new UiWindow { Title = "Root" });
        UiWindow external = new()
        {
            Title = "External"
        };

        Update(workspace, new UiInputState());

        int hostCountBefore = workspace.DockHosts.Count;
        UiRect bounds = workspace.RootHost.Bounds;
        int size = workspace.DropTargetSize;
        int centerX = bounds.X + bounds.Width / 2;
        int centerY = bounds.Y + bounds.Height / 2;
        UiPoint leftTarget = new(centerX - size * 2 + size / 2, centerY);

        workspace.PreviewExternalDock(external, leftTarget, new UiRect(20, 20, 120, 80));

        bool committed = workspace.CommitExternalDock(external);

        Assert.True(committed);
        Assert.True(workspace.DockHosts.Count >= hostCountBefore);
        Assert.DoesNotContain(external, workspace.RootHost.Windows);
        Assert.Contains(workspace.DockHosts, host => host.Windows.Contains(external));
    }

    [Fact]
    public void DockWorkspace_ClearExternalDockPreview_CancelsPendingDock()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiWindow external = new()
        {
            Title = "External"
        };

        Update(workspace, new UiInputState());

        UiRect bounds = workspace.RootHost.Bounds;
        UiPoint centerTarget = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        workspace.PreviewExternalDock(external, centerTarget, new UiRect(20, 20, 120, 80));
        workspace.ClearExternalDockPreview(external);

        bool committed = workspace.CommitExternalDock(external);

        Assert.False(committed);
        Assert.Null(external.Parent);
    }

    [Fact]
    public void DockWorkspace_ExternalDockDebugState_ReportsHoveredTarget()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        workspace.RootHost.DockWindow(new UiWindow { Title = "Root" });
        UiDockHost targetHost = workspace.DockHosts[1];
        targetHost.DockWindow(new UiWindow { Title = "Right" });
        UiWindow external = new()
        {
            Id = "external-window",
            Title = "External"
        };

        Update(workspace, new UiInputState());

        UiRect bounds = targetHost.Bounds;
        UiPoint hoverPoint = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        UiRect previewWindowBounds = new(20, 20, 120, 80);

        workspace.PreviewExternalDock(external, hoverPoint, previewWindowBounds);

        UiDockWorkspace.ExternalDockDebugState state = workspace.GetExternalDockDebugState();

        Assert.True(state.ExternalPreviewActive);
        Assert.Equal("external-window", state.ExternalPreviewWindowId);
        Assert.Equal("External", state.ExternalPreviewWindowTitle);
        Assert.Equal(hoverPoint, state.HoverPoint);
        Assert.Equal(targetHost.Id, state.HoverHostId);
        Assert.Equal(UiDockWorkspace.DockTarget.Center, state.HoverTarget);
        Assert.Equal(targetHost.Bounds, state.HoverHostBounds);
        Assert.Equal(previewWindowBounds, state.PreviewWindowBounds);
        Assert.True(state.PreviewBounds.Width > 0);
        Assert.True(state.PreviewBounds.Height > 0);
    }

    [Fact]
    public void DockWorkspace_ExternalDockPreview_RequiresExplicitTargetInsteadOfDefaultingToCenter()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        workspace.RootHost.DockWindow(new UiWindow { Title = "Root" });
        UiDockHost targetHost = workspace.DockHosts[1];
        targetHost.DockWindow(new UiWindow { Title = "Right" });
        UiWindow external = new()
        {
            Id = "external-window",
            Title = "External"
        };

        Update(workspace, new UiInputState());

        UiRect hostBounds = targetHost.Bounds;
        UiRect previewWindowBounds = new(hostBounds.X + 24, hostBounds.Y + 18, 120, 80);
        UiPoint nonTargetPoint = new(hostBounds.X + 12, hostBounds.Y + 12);

        workspace.PreviewExternalDock(external, nonTargetPoint, previewWindowBounds);

        UiDockWorkspace.ExternalDockDebugState state = workspace.GetExternalDockDebugState();
        bool committed = workspace.CommitExternalDock(external);

        Assert.True(state.ExternalPreviewActive);
        Assert.Equal(targetHost.Id, state.HoverHostId);
        Assert.Equal(UiDockWorkspace.DockTarget.None, state.HoverTarget);
        Assert.Equal(previewWindowBounds, state.PreviewBounds);
        Assert.False(committed);
        Assert.Null(external.Parent);
    }

    [Fact]
    public void DockWorkspace_CommitExternalDockGroup_DocksAllWindowsToResolvedHost()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiWindow root = new()
        {
            Id = "root-window",
            Title = "Root"
        };
        workspace.RootHost.DockWindow(root);

        UiWindow first = new()
        {
            Id = "external-a",
            Title = "External A"
        };
        UiWindow second = new()
        {
            Id = "external-b",
            Title = "External B"
        };

        Update(workspace, new UiInputState());

        UiRect hostBounds = workspace.RootHost.Bounds;
        UiPoint centerPoint = new(hostBounds.X + hostBounds.Width / 2, hostBounds.Y + hostBounds.Height / 2);
        UiRect previewWindowBounds = new(hostBounds.X + 12, hostBounds.Y + 10, 180, 120);

        workspace.PreviewExternalDock(second, centerPoint, previewWindowBounds);

        bool committed = workspace.CommitExternalDockGroup(new[] { first, second }, second, second);

        Assert.True(committed);
        Assert.Same(workspace.RootHost, first.Parent);
        Assert.Same(workspace.RootHost, second.Parent);
        Assert.Equal(3, workspace.RootHost.Windows.Count);
        Assert.Same(first, workspace.RootHost.Windows[1]);
        Assert.Same(second, workspace.RootHost.Windows[2]);
        Assert.Same(second, workspace.RootHost.ActiveWindow);
    }

    [Fact]
    public void DockWorkspace_SplitHost_InheritsExternalDetachBehavior()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 320, 180)
        };
        workspace.RootHost.AllowDetach = true;
        workspace.RootHost.CanDetachWindowPredicate = window => string.Equals(window.Title, "Detachable", StringComparison.Ordinal);

        UiDockHost splitHost = workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right);
        UiWindow window = new()
        {
            Id = "detachable-window",
            Title = "Detachable"
        };
        splitHost.DockWindow(window);
        splitHost.ActivateWindow(0);

        Update(workspace, new UiInputState());

        UiRect tabBounds = splitHost.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint screenDetachPoint = new(840, 640);

        UiWindow? detachedWindow = null;
        UiPoint detachedPoint = default;
        workspace.TabDetached += (detached, point) =>
        {
            detachedWindow = detached;
            detachedPoint = point;
        };

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = new UiPoint(320, 220),
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = new UiPoint(380, 24),
            ScreenMousePosition = screenDetachPoint,
            LeftDown = true
        });

        Assert.True(splitHost.AllowDetach);
        Assert.NotNull(splitHost.CanDetachWindowPredicate);
        Assert.Same(window, detachedWindow);
        Assert.Equal(new UiPoint(screenDetachPoint.X - 14, screenDetachPoint.Y - 10), detachedPoint);
        Assert.DoesNotContain(window, splitHost.Windows);
    }

    [Fact]
    public void DockWorkspace_WorkspaceOwnedTabDrag_CanFloatWithoutChildParentCrash()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost splitHost = workspace.DockHosts[1];
        splitHost.AllowDetach = true;
        splitHost.ExternalDragHandling = true;
        splitHost.CanDetachWindowPredicate = _ => true;

        UiWindow window = new()
        {
            Id = "floatable-window",
            Title = "Floatable"
        };
        splitHost.DockWindow(window);
        splitHost.ActivateWindow(0);

        Update(workspace, new UiInputState());

        UiRect tabBounds = splitHost.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint splitterDropPoint = new(workspace.RootHost.Bounds.Right + workspace.SplitterThickness / 2, dragStart.Y);

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = dragStart,
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = splitterDropPoint,
            ScreenMousePosition = splitterDropPoint,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = splitterDropPoint,
            ScreenMousePosition = splitterDropPoint,
            LeftReleased = true
        });

        Assert.Contains(window, workspace.FloatingWindows);
        Assert.DoesNotContain(window, splitHost.Windows);
        Assert.Same(workspace, window.Parent);
    }

    private static UiDockWorkspace CreateWorkspace()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 300, 180),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right);
        return workspace;
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
