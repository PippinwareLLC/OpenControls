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

    [Fact]
    public void DockWorkspace_WorkspaceOwnedTabDrag_DocksUsingReleasePoint()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiWindow root = new()
        {
            Id = "root-window",
            Title = "Root"
        };
        workspace.RootHost.DockWindow(root);

        UiDockHost splitHost = workspace.DockHosts[1];
        splitHost.ExternalDragHandling = true;
        UiWindow window = new()
        {
            Id = "dragged-window",
            Title = "Dragged"
        };
        splitHost.DockWindow(window);
        splitHost.ActivateWindow(0);

        Update(workspace, new UiInputState());

        UiRect tabBounds = splitHost.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint staleHoverPoint = new(dragStart.X + workspace.DragThreshold + 1, dragStart.Y);
        UiPoint releasePoint = new(
            workspace.RootHost.Bounds.X + workspace.RootHost.Bounds.Width / 2,
            workspace.RootHost.Bounds.Y + workspace.RootHost.Bounds.Height / 2);

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = dragStart,
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = staleHoverPoint,
            ScreenMousePosition = staleHoverPoint,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftReleased = true
        });

        Assert.Contains(window, workspace.RootHost.Windows);
        Assert.DoesNotContain(window, splitHost.Windows);
        Assert.Same(workspace.RootHost, window.Parent);
    }

    [Fact]
    public void DockWorkspace_WorkspaceOwnedTabDrag_CanSplitSameHostFromContentEdge()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 400, 220),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiWindow scene = new()
        {
            Id = "scene-window",
            Title = "Scene"
        };
        UiWindow game = new()
        {
            Id = "game-window",
            Title = "Game"
        };
        workspace.RootHost.DockWindow(scene);
        workspace.RootHost.DockWindow(game);
        workspace.RootHost.ActivateWindow(1);

        Update(workspace, new UiInputState());

        UiRect tabBounds = workspace.RootHost.GetTabBounds(1);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint releasePoint = new(
            workspace.RootHost.Bounds.Right - workspace.DropTargetSize,
            workspace.RootHost.Bounds.Y + workspace.RootHost.Bounds.Height / 2);

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = dragStart,
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftReleased = true
        });

        UiDockHost gameHost = Assert.Single(workspace.DockHosts, host => host.Windows.Contains(game));
        Assert.NotSame(workspace.RootHost, gameHost);
        Assert.Contains(scene, workspace.RootHost.Windows);
        Assert.DoesNotContain(game, workspace.RootHost.Windows);
        Assert.Same(gameHost, game.Parent);
    }

    [Fact]
    public void DockWorkspace_WorkspaceOwnedTabDrag_TopContentEdgeWinsCornerAmbiguity()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 400, 220),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiWindow scene = new()
        {
            Id = "scene-window",
            Title = "Scene"
        };
        UiWindow game = new()
        {
            Id = "game-window",
            Title = "Game"
        };
        workspace.RootHost.DockWindow(scene);
        workspace.RootHost.DockWindow(game);
        workspace.RootHost.ActivateWindow(1);

        Update(workspace, new UiInputState());

        UiRect hostBounds = workspace.RootHost.Bounds;
        UiRect tabBounds = workspace.RootHost.GetTabBounds(1);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint releasePoint = new(
            hostBounds.X + workspace.DropTargetSize / 2,
            hostBounds.Y + workspace.RootHost.TabBarHeight + 4);

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = dragStart,
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftReleased = true
        });

        Update(workspace, new UiInputState());

        UiDockHost gameHost = Assert.Single(workspace.DockHosts, host => host.Windows.Contains(game));
        Assert.NotSame(workspace.RootHost, gameHost);
        Assert.Contains(scene, workspace.RootHost.Windows);
        Assert.DoesNotContain(game, workspace.RootHost.Windows);
        Assert.True(gameHost.Bounds.Y < workspace.RootHost.Bounds.Y);
        Assert.Equal(workspace.RootHost.Bounds.X, gameHost.Bounds.X);
        Assert.Equal(workspace.RootHost.Bounds.Width, gameHost.Bounds.Width);
    }

    [Fact]
    public void DockWorkspace_WorkspaceOwnedTabDrag_TabBarReleaseDoesNotSplitSameHost()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 400, 220),
            SplitterThickness = 6,
            MinPaneSize = 80
        };

        UiWindow scene = new()
        {
            Id = "scene-window",
            Title = "Scene"
        };
        UiWindow game = new()
        {
            Id = "game-window",
            Title = "Game"
        };
        workspace.RootHost.DockWindow(scene);
        workspace.RootHost.DockWindow(game);
        workspace.RootHost.ActivateWindow(0);

        Update(workspace, new UiInputState());

        UiRect tabBounds = workspace.RootHost.GetTabBounds(0);
        UiPoint dragStart = new(tabBounds.X + 14, tabBounds.Y + 10);
        UiPoint releasePoint = new(
            workspace.RootHost.Bounds.Right - workspace.DropTargetSize,
            tabBounds.Y + tabBounds.Height / 2);

        Update(workspace, new UiInputState
        {
            MousePosition = dragStart,
            ScreenMousePosition = dragStart,
            LeftClicked = true,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftDown = true
        });

        Update(workspace, new UiInputState
        {
            MousePosition = releasePoint,
            ScreenMousePosition = releasePoint,
            LeftReleased = true
        });

        Assert.Single(workspace.DockHosts);
        Assert.Same(workspace.RootHost, scene.Parent);
        Assert.Same(workspace.RootHost, game.Parent);
    }

    [Fact]
    public void DockHost_RemovingTabBeforeActivePreservesActiveWindow()
    {
        UiDockHost host = new();
        UiWindow first = new() { Title = "First" };
        UiWindow second = new() { Title = "Second" };
        UiWindow third = new() { Title = "Third" };
        host.DockWindow(first);
        host.DockWindow(second);
        host.DockWindow(third);
        host.ActivateWindow(2);

        Assert.True(host.RemoveWindow(first));

        Assert.Same(third, host.ActiveWindow);
        Assert.Equal(1, host.ActiveIndex);
    }

    [Fact]
    public void DockWorkspace_SplitHostUsesAuthoredRatio()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 400, 200),
            SplitterThickness = 4
        };

        UiDockHost right = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Right,
            splitRatio: 0.75f);
        Update(workspace, new UiInputState());

        Assert.Equal(297, workspace.RootHost.Bounds.Width);
        Assert.Equal(99, right.Bounds.Width);
        Assert.Equal(0.75f, workspace.CaptureState().Root!.SplitRatio);
    }

    [Fact]
    public void DockWorkspace_DockPolicyProtectsTargetHosts()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost protectedHost = workspace.RootHost;
        UiWindow panel = new() { Id = "panel", Title = "Panel" };
        workspace.CanDockWindowPredicate = (window, host, _) =>
            !ReferenceEquals(window, panel) || !ReferenceEquals(host, protectedHost);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => workspace.DockWindow(panel, protectedHost));

        Assert.Contains("cannot dock", exception.Message, StringComparison.Ordinal);
        Assert.Null(panel.Parent);
    }

    [Fact]
    public void DockWorkspace_ClosingFinalTabCollapsesEmptySplitHost()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost splitHost = workspace.DockHosts[1];
        splitHost.AllowClosingLastWindow = true;
        splitHost.DockWindow(new UiWindow { Id = "panel", Title = "Panel" });

        Assert.Equal(1, splitHost.CloseAllWindows());

        Assert.Single(workspace.DockHosts);
        Assert.Same(workspace.RootHost, workspace.DockHosts[0]);
    }

    [Fact]
    public void DockWorkspace_FloatingModeResizesAndFitsOversizedWindow()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(10, 20, 300, 180)
        };
        UiWindow window = new()
        {
            Id = "floating",
            Title = "Floating",
            Bounds = new UiRect(-50, -40, 600, 400)
        };

        workspace.AddFloatingWindow(window);
        Update(workspace, new UiInputState());

        Assert.True(window.AllowDrag);
        Assert.True(window.AllowResize);
        Assert.True(window.ShowResizeGrip);
        Assert.Equal(workspace.Bounds, window.Bounds);

        workspace.DockWindow(window, workspace.RootHost);

        Assert.False(window.AllowDrag);
        Assert.False(window.AllowResize);
        Assert.False(window.ShowResizeGrip);
        Assert.Same(workspace.RootHost, window.Parent);
    }

    [Fact]
    public void Window_LayoutContentRunsBeforeChildrenUpdate()
    {
        UiWindow window = new()
        {
            Bounds = new UiRect(10, 20, 240, 160),
            ShowTitleBar = true,
            TitleBarHeight = 24
        };
        BoundsProbe child = new();
        window.AddChild(child);
        window.LayoutContent = bounds => child.Bounds = bounds;

        Update(window, new UiInputState());

        Assert.Equal(window.ContentBounds, child.BoundsSeenDuringUpdate);
    }

    [Fact]
    public void DockWorkspace_ApplyStateEnforcesPolicyAndResetsFloatingMode()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost target = workspace.DockHosts[1];
        string targetId = target.Id;
        UiWindow panel = new() { Id = "panel", Title = "Panel" };
        target.DockWindow(panel);
        UiDockWorkspaceState dockedState = workspace.CaptureState();
        workspace.DetachWindow(panel);
        panel.Bounds = new UiRect(20, 30, 140, 90);
        workspace.AddFloatingWindow(panel);
        workspace.CanDockWindowPredicate = (window, host, _) =>
            !ReferenceEquals(window, panel) || !string.Equals(host.Id, targetId, StringComparison.Ordinal);

        Assert.Throws<InvalidOperationException>(
            () => workspace.ApplyState(dockedState, new Dictionary<string, UiWindow> { [panel.Id] = panel }));
        Assert.Contains(panel, workspace.FloatingWindows);
        Assert.True(panel.AllowDrag);
        Assert.True(panel.AllowResize);
        Assert.True(panel.ShowResizeGrip);

        workspace.CanDockWindowPredicate = null;
        workspace.ApplyState(dockedState, new Dictionary<string, UiWindow> { [panel.Id] = panel });

        UiDockHost restoredTarget = Assert.IsType<UiDockHost>(panel.Parent);
        Assert.Contains(restoredTarget, workspace.DockHosts);
        Assert.DoesNotContain(panel, workspace.FloatingWindows);
        Assert.False(panel.AllowDrag);
        Assert.False(panel.AllowResize);
        Assert.False(panel.ShowResizeGrip);
    }

    [Fact]
    public void DockWorkspace_ExternalGroupValidatesEveryWindowBeforeMutation()
    {
        UiDockWorkspace workspace = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiWindow allowed = new() { Id = "allowed", Bounds = new UiRect(10, 20, 120, 80) };
        UiWindow protectedWindow = new() { Id = "protected", Bounds = new UiRect(20, 30, 120, 80) };
        workspace.AddFloatingWindow(allowed);
        workspace.AddFloatingWindow(protectedWindow);
        workspace.Arrange();
        workspace.CanDockWindowPredicate = (window, _, _) => !ReferenceEquals(window, protectedWindow);
        UiPoint center = new(
            workspace.RootHost.Bounds.X + workspace.RootHost.Bounds.Width / 2,
            workspace.RootHost.Bounds.Y + workspace.RootHost.Bounds.Height / 2);
        workspace.PreviewExternalDock(allowed, center, allowed.Bounds);

        Assert.False(workspace.CommitExternalDockGroup([allowed, protectedWindow], allowed));
        Assert.Empty(workspace.RootHost.Windows);
        Assert.Contains(allowed, workspace.FloatingWindows);
        Assert.Contains(protectedWindow, workspace.FloatingWindows);

        workspace.CanDockWindowPredicate = null;
        workspace.PreviewExternalDock(allowed, center, allowed.Bounds);
        Assert.True(workspace.CommitExternalDockGroup([allowed, protectedWindow], allowed));
        Assert.Equal([allowed, protectedWindow], workspace.RootHost.Windows);
        Assert.Empty(workspace.FloatingWindows);
        Assert.All(workspace.RootHost.Windows, window =>
        {
            Assert.False(window.AllowDrag);
            Assert.False(window.AllowResize);
            Assert.False(window.ShowResizeGrip);
        });
    }

    [Fact]
    public void DockWorkspace_ArrangePreservesFloatingBoundsUntilWorkspaceHasSize()
    {
        UiDockWorkspace workspace = new();
        UiWindow floating = new() { Bounds = new UiRect(12, 18, 120, 80) };
        workspace.AddFloatingWindow(floating);

        workspace.Arrange();

        Assert.Equal(new UiRect(12, 18, 120, 80), floating.Bounds);
    }

    [Fact]
    public void DockWorkspace_ArrangeLaysOutWindowContentWithoutAnUpdatePass()
    {
        UiDockWorkspace workspace = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiWindow window = new() { TitleBarHeight = 24 };
        BoundsProbe child = new();
        window.AddChild(child);
        window.LayoutContent = bounds => child.Bounds = bounds;
        workspace.RootHost.DockWindow(window);

        workspace.Arrange();

        Assert.Equal(window.ContentBounds, child.Bounds);
        Assert.True(child.Bounds.Width > 0);
        Assert.True(child.Bounds.Height > 0);
    }

    [Fact]
    public void DockWorkspace_FinalCloseSubscriberCanReplaceTabWithoutOrphaningHost()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost target = workspace.DockHosts[1];
        UiWindow original = new() { Id = "original" };
        UiWindow replacement = new() { Id = "replacement" };
        target.AllowClosingLastWindow = true;
        target.DockWindow(original);
        target.TabClosed += _ => target.DockWindow(replacement);

        Assert.Equal(1, target.CloseAllWindows());

        UiDockHost replacementHost = Assert.IsType<UiDockHost>(replacement.Parent);
        Assert.Contains(replacementHost, workspace.DockHosts);
        Assert.Contains(replacement, replacementHost.Windows);
    }

    [Fact]
    public void DockWorkspace_DockPolicyCannotInvalidateTargetAndStillDock()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost target = workspace.DockHosts[1];
        UiWindow resident = new() { Id = "resident" };
        UiWindow incoming = new() { Id = "incoming" };
        target.DockWindow(resident);
        workspace.CanDockWindowPredicate = (window, _, _) =>
        {
            if (ReferenceEquals(window, incoming))
            {
                workspace.DetachWindow(resident);
            }

            return true;
        };

        Assert.Throws<InvalidOperationException>(() => workspace.DockWindow(incoming, target));

        Assert.Null(incoming.Parent);
        Assert.DoesNotContain(target, workspace.DockHosts);
    }

    [Fact]
    public void DockWorkspace_NormalizationHonorsDockPolicy()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost fallback = workspace.DockHosts[1];
        UiWindow panel = new() { Id = "panel" };
        fallback.DockWindow(panel);
        workspace.CanDockWindowPredicate = (window, host, _) =>
            !ReferenceEquals(window, panel) || !ReferenceEquals(host, workspace.RootHost);

        workspace.DockWindow(panel, fallback);

        Assert.Same(fallback, panel.Parent);
        Assert.Contains(fallback, workspace.DockHosts);
        Assert.Empty(workspace.RootHost.Windows);
    }

    [Fact]
    public void DockWorkspace_RejectsNonFiniteAuthoredAndRestoredRatios()
    {
        UiDockWorkspace workspace = CreateWorkspace();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Bottom, float.NaN));

        UiDockWorkspaceState state = workspace.CaptureState();
        state.Root!.SplitRatio = float.PositiveInfinity;
        Assert.Throws<ArgumentOutOfRangeException>(
            () => workspace.ApplyState(state, new Dictionary<string, UiWindow>()));
    }

    [Fact]
    public void DockWorkspace_ApplyStateRejectsDuplicateHostLeavesWithoutMutation()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockWorkspaceState state = workspace.CaptureState();
        int originalHostCount = workspace.DockHosts.Count;
        state.Root!.Second = new UiDockNodeState { HostId = workspace.RootHost.Id };

        Assert.Throws<ArgumentException>(
            () => workspace.ApplyState(state, new Dictionary<string, UiWindow>()));

        Assert.Equal(originalHostCount, workspace.DockHosts.Count);
        Assert.Contains(workspace.RootHost, workspace.DockHosts);
        Assert.Equal(workspace.RootHost.Id, workspace.CaptureState().Root!.First!.HostId);
    }

    [Fact]
    public void DockWorkspace_ApplyStateRejectsForeignParentBeforeClearingLayout()
    {
        UiDockWorkspace workspace = new();
        UiWindow resident = new() { Id = "resident" };
        workspace.RootHost.DockWindow(resident);
        UiDockWorkspaceState state = workspace.CaptureState();
        UiWindow foreign = new() { Id = "foreign", AllowResize = true };
        UiPanel foreignParent = new();
        foreignParent.AddChild(foreign);
        Assert.Single(state.Hosts).WindowIds = [foreign.Id];

        Assert.Throws<InvalidOperationException>(
            () => workspace.ApplyState(state, new Dictionary<string, UiWindow> { [foreign.Id] = foreign }));

        Assert.Equal([resident], workspace.RootHost.Windows);
        Assert.Same(workspace.RootHost, resident.Parent);
        Assert.Same(foreignParent, foreign.Parent);
        Assert.True(foreign.AllowResize);
    }

    [Fact]
    public void DockWorkspace_NormalizationToleratesPolicyMutation()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost fallback = workspace.DockHosts[1];
        UiWindow first = new() { Id = "first" };
        UiWindow removedByPolicy = new() { Id = "removed" };
        fallback.DockWindow(first);
        fallback.DockWindow(removedByPolicy);
        workspace.CanDockWindowPredicate = (window, host, _) =>
        {
            if (ReferenceEquals(host, workspace.RootHost) && ReferenceEquals(window, first))
            {
                fallback.RemoveWindow(removedByPolicy);
            }

            return true;
        };

        workspace.DockWindow(first, fallback);

        Assert.Same(fallback, first.Parent);
        Assert.Null(removedByPolicy.Parent);
        Assert.Contains(fallback, workspace.DockHosts);
    }

    [Fact]
    public void DockWorkspace_ArrangeToleratesContentCallbackDetachingItsHost()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiWindow document = new() { Id = "document" };
        UiDockHost split = workspace.DockHosts[1];
        UiWindow panel = new() { Id = "panel" };
        workspace.RootHost.DockWindow(document);
        split.DockWindow(panel);
        panel.LayoutContent = _ => workspace.DetachWindow(panel);

        workspace.Arrange();

        Assert.Null(panel.Parent);
        Assert.DoesNotContain(split, workspace.DockHosts);
        Assert.Same(workspace.RootHost, document.Parent);
    }

    [Fact]
    public void StateSerializer_RoundTripsDockAndElementBounds()
    {
        UiStateSnapshot snapshot = new();
        snapshot.Elements.Add(new UiElementState
        {
            Id = "panel",
            Bounds = new UiRect(11, 22, 333, 144)
        });
        snapshot.DockWorkspaces.Add(new UiDockWorkspaceState
        {
            Id = "workspace",
            Root = new UiDockNodeState { HostId = "root" },
            FloatingWindows =
            [
                new UiFloatingWindowState
                {
                    WindowId = "floating",
                    Bounds = new UiRect(40, 50, 260, 170)
                }
            ]
        });

        UiStateSnapshot restored = UiStateSerializer.FromJson(UiStateSerializer.ToJson(snapshot));

        UiRect elementBounds = Assert.Single(restored.Elements).Bounds;
        Assert.Equal((11, 22, 333, 144), (elementBounds.X, elementBounds.Y, elementBounds.Width, elementBounds.Height));
        UiRect floatingBounds = Assert.Single(Assert.Single(restored.DockWorkspaces).FloatingWindows).Bounds;
        Assert.Equal((40, 50, 260, 170), (floatingBounds.X, floatingBounds.Y, floatingBounds.Width, floatingBounds.Height));
    }

    [Fact]
    public void DockWorkspace_DirectDockRevalidatesWindowAfterPolicyCallback()
    {
        UiDockWorkspace workspace = new();
        UiWindow resident = new() { Id = "resident" };
        UiWindow incoming = new() { Id = "incoming" };
        UiPanel foreignParent = new();
        workspace.RootHost.DockWindow(resident);
        workspace.CanDockWindowPredicate = (window, _, _) =>
        {
            if (ReferenceEquals(window, incoming))
            {
                foreignParent.AddChild(incoming);
            }

            return true;
        };

        Assert.Throws<InvalidOperationException>(() => workspace.DockWindow(incoming, workspace.RootHost));

        Assert.Equal([resident], workspace.RootHost.Windows);
        Assert.Same(foreignParent, incoming.Parent);
    }

    [Fact]
    public void DockWorkspace_ApplyStateRevalidatesWindowAfterPolicyCallback()
    {
        UiDockWorkspace workspace = new();
        UiWindow resident = new() { Id = "resident" };
        workspace.RootHost.DockWindow(resident);
        UiDockWorkspaceState state = workspace.CaptureState();
        UiWindow incoming = new() { Id = "incoming" };
        UiPanel foreignParent = new();
        Assert.Single(state.Hosts).WindowIds = [incoming.Id];
        workspace.CanDockWindowPredicate = (window, _, _) =>
        {
            if (ReferenceEquals(window, incoming))
            {
                foreignParent.AddChild(incoming);
            }

            return true;
        };

        Assert.Throws<InvalidOperationException>(
            () => workspace.ApplyState(state, new Dictionary<string, UiWindow> { [incoming.Id] = incoming }));

        Assert.Equal([resident], workspace.RootHost.Windows);
        Assert.Same(workspace.RootHost, resident.Parent);
        Assert.Same(foreignParent, incoming.Parent);
    }

    [Fact]
    public void DockWorkspace_NormalizationPreservesTabsAddedByPolicyCallback()
    {
        UiDockWorkspace workspace = CreateWorkspace();
        UiDockHost fallback = workspace.DockHosts[1];
        UiWindow first = new() { Id = "first" };
        UiWindow second = new() { Id = "second" };
        UiWindow addedByPolicy = new() { Id = "added" };
        fallback.DockWindow(first);
        fallback.DockWindow(second);
        workspace.CanDockWindowPredicate = (window, host, _) =>
        {
            if (ReferenceEquals(host, workspace.RootHost)
                && ReferenceEquals(window, first)
                && addedByPolicy.Parent == null)
            {
                fallback.DockWindow(addedByPolicy);
            }

            return true;
        };

        workspace.DockWindow(first, fallback);

        Assert.Contains(fallback, workspace.DockHosts);
        Assert.Same(fallback, addedByPolicy.Parent);
        Assert.Contains(addedByPolicy, fallback.Windows);
        Assert.Empty(workspace.RootHost.Windows);
    }

    [Fact]
    public void DockWorkspace_ApplyStateCannotFloatWindowProtectedBySourceHost()
    {
        UiDockWorkspace workspace = new();
        UiWindow document = new() { Id = "document" };
        workspace.RootHost.AllowDetach = true;
        workspace.RootHost.CanDetachWindowPredicate = window => !ReferenceEquals(window, document);
        workspace.RootHost.DockWindow(document);
        UiDockWorkspaceState state = workspace.CaptureState();
        Assert.Single(state.Hosts).WindowIds.Clear();
        state.FloatingWindows.Add(new UiFloatingWindowState
        {
            WindowId = document.Id,
            Bounds = new UiRect(20, 30, 300, 200)
        });

        Assert.Throws<InvalidOperationException>(
            () => workspace.ApplyState(state, new Dictionary<string, UiWindow> { [document.Id] = document }));

        Assert.Same(workspace.RootHost, document.Parent);
        Assert.Equal([document], workspace.RootHost.Windows);
        Assert.Empty(workspace.FloatingWindows);
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

    private sealed class BoundsProbe : UiElement
    {
        public UiRect BoundsSeenDuringUpdate { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            BoundsSeenDuringUpdate = Bounds;
            base.Update(context);
        }
    }
}
