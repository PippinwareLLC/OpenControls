using OpenControls.Controls;
using OpenControls.State;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockHostVisibilityTests
{
    [Fact]
    public void DockHost_AddingWindowToExistingHost_HidesTheNewInactiveWindowImmediately()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };

        UiWindow first = new()
        {
            Title = "First"
        };
        UiWindow second = new()
        {
            Title = "Second"
        };

        host.AddWindow(first);
        host.AddWindow(second);

        Assert.True(first.Visible);
        Assert.False(second.Visible);
        Assert.Equal(first, host.ActiveWindow);
    }

    [Fact]
    public void DockHost_ActivatingDifferentTab_UpdatesWindowVisibilityImmediately()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };

        UiWindow first = new()
        {
            Title = "First"
        };
        UiWindow second = new()
        {
            Title = "Second"
        };

        host.AddWindow(first);
        host.AddWindow(second);
        host.ActivateWindow(1);

        Assert.False(first.Visible);
        Assert.True(second.Visible);
        Assert.Equal(second, host.ActiveWindow);
    }

    [Fact]
    public void DockHost_SyncWindowVisibilityToActiveTab_HidesInactiveTabsAfterExternalVisibilityMutation()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160)
        };

        UiWindow first = new()
        {
            Title = "First"
        };
        UiWindow second = new()
        {
            Title = "Second"
        };

        host.AddWindow(first);
        host.AddWindow(second);
        host.ActivateWindow(0);

        first.Visible = true;
        second.Visible = true;

        host.SyncWindowVisibilityToActiveTab();

        Assert.True(first.Visible);
        Assert.False(second.Visible);
        Assert.Equal(first, host.ActiveWindow);
    }

    [Fact]
    public void DockHost_CollapsedState_HidesContentAndRoundTripsThroughWorkspaceState()
    {
        UiDockWorkspace workspace = new()
        {
            Id = "workspace",
            Bounds = new UiRect(0, 0, 900, 500),
            MinPaneSize = 80,
            SplitterThickness = 6
        };
        UiDockHost left = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Left);
        left.Id = "left";
        left.Collapsible = true;
        left.CollapseEdge = UiDockCollapseEdge.Left;
        left.CollapsedExtent = 30;
        UiWindow leftWindow = new() { Id = "hierarchy", Title = "Hierarchy" };
        UiWindow centerWindow = new() { Id = "scene", Title = "Scene" };
        left.DockWindow(leftWindow);
        workspace.RootHost.DockWindow(centerWindow);

        UiDockWorkspaceState expanded = workspace.CaptureState();
        left.Collapsed = true;
        workspace.PerformLayout();

        Assert.Equal(30, left.Bounds.Width);
        Assert.False(leftWindow.Visible);
        Assert.True(centerWindow.Visible);
        Assert.True(workspace.CaptureState().Hosts.Single(host => host.HostId == "left").Collapsed);

        workspace.ApplyState(
            expanded,
            new Dictionary<string, UiWindow>
            {
                [leftWindow.Id] = leftWindow,
                [centerWindow.Id] = centerWindow
            });
        workspace.PerformLayout();

        Assert.False(left.Collapsed);
        Assert.True(leftWindow.Visible);
        Assert.True(left.Bounds.Width > left.CollapsedExtent);
    }
}
