using OpenControls.Controls;
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
}
