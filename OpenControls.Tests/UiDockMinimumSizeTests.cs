using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockMinimumSizeTests
{
    [Fact]
    public void RespectDockedWindowMinimumsClampsSplitterWithoutChangingAuthoredRatio()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 500, 300),
            SplitterThickness = 6,
            MinPaneSize = 40,
            RespectDockedWindowMinimums = true
        };
        UiDockHost panelHost = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Right,
            0.90f);
        workspace.RootHost.DockWindow(new UiWindow
        {
            Id = "document",
            MinSize = new UiPoint(100, 80)
        });
        panelHost.DockWindow(new UiWindow
        {
            Id = "layers",
            MinSize = new UiPoint(180, 160)
        });

        workspace.Arrange();

        Assert.Equal(180, panelHost.Bounds.Width);
        Assert.True(panelHost.Bounds.Height >= 160);
        Assert.Equal(0.90f, workspace.CaptureState().Root!.SplitRatio, 4);

        workspace.RespectDockedWindowMinimums = false;
        workspace.Arrange();

        Assert.True(panelHost.Bounds.Width < 180);
        Assert.Equal(0.90f, workspace.CaptureState().Root!.SplitRatio, 4);
    }
}
