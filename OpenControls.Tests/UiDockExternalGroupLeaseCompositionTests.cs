using OpenControls.Controls;
using OpenControls.State;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockExternalGroupLeaseCompositionTests
{
    [Fact]
    public void PartialAndAdjacentCompleteLeasesDoNotRequireOneReturnCohort()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 900, 600)
        };
        UiDockHost rootHost = workspace.RootHost;
        rootHost.AllowDetach = true;
        UiWindow first = new() { Id = "first", Title = "First" };
        UiWindow stable = new() { Id = "stable", Title = "Stable" };
        rootHost.DockWindow(first);
        rootHost.DockWindow(stable);
        UiDockHost siblingHost = workspace.SplitHost(
            rootHost,
            UiDockWorkspace.DockTarget.Right,
            0.7f);
        UiWindow sibling = new() { Id = "sibling", Title = "Sibling" };
        siblingHost.DockWindow(sibling);

        UiDockExternalGroupLease partialLease =
            workspace.BeginExternalDockGroup([first]);
        UiDockExternalGroupLease completeLease =
            workspace.BeginExternalDockGroup([sibling]);
        UiDockHost firstExternal = HostExternally(first);
        UiDockHost siblingExternal = HostExternally(sibling);

        Assert.False(workspace.ExternalDockGroupLeasesShareReturnTopology(
            partialLease,
            completeLease));
        Assert.True(workspace.RestoreExternalDockGroup(partialLease));
        Assert.True(workspace.RestoreExternalDockGroup(completeLease));
        Assert.Empty(firstExternal.Windows);
        Assert.Empty(siblingExternal.Windows);
        Assert.Equal([first, stable], rootHost.Windows);
        Assert.Equal([sibling], siblingHost.Windows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedCompleteHostLeasesComposeInReverseAfterSiblingMutation(
        bool detachOuterHostFirst)
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 900, 600)
        };
        UiDockHost documentHost = workspace.RootHost;
        documentHost.AllowDetach = true;
        documentHost.Id = "document-host";
        UiWindow document = new() { Id = "document", Title = "Document" };
        documentHost.DockWindow(document);

        UiDockHost colorHost = workspace.SplitHost(
            documentHost,
            UiDockWorkspace.DockTarget.Right,
            0.75f);
        colorHost.Id = "color-host";
        UiWindow color = new() { Id = "color", Title = "Color" };
        colorHost.DockWindow(color);
        UiDockHost propertiesHost = workspace.SplitHost(
            colorHost,
            UiDockWorkspace.DockTarget.Bottom,
            0.19f);
        propertiesHost.Id = "properties-host";
        UiWindow properties = new() { Id = "properties", Title = "Properties" };
        propertiesHost.DockWindow(properties);
        UiDockHost layersHost = workspace.SplitHost(
            propertiesHost,
            UiDockWorkspace.DockTarget.Bottom,
            0.52f);
        layersHost.Id = "layers-host";
        UiWindow layers = new() { Id = "layers", Title = "Layers" };
        layersHost.DockWindow(layers);

        UiDockExternalGroupLease[] leases = detachOuterHostFirst
            ?
            [
                workspace.BeginExternalDockGroup([color]),
                workspace.BeginExternalDockGroup([properties]),
                workspace.BeginExternalDockGroup([layers])
            ]
            :
            [
                workspace.BeginExternalDockGroup([properties]),
                workspace.BeginExternalDockGroup([layers]),
                workspace.BeginExternalDockGroup([color])
            ];
        UiDockHost propertiesExternal = HostExternally(properties);
        UiDockHost layersExternal = HostExternally(layers);
        UiDockHost colorExternal = HostExternally(color);

        Assert.True(workspace.ExternalDockGroupLeasesShareReturnTopology(
            leases[0],
            leases[1]));
        Assert.True(workspace.ExternalDockGroupLeasesShareReturnTopology(
            leases[1],
            leases[2]));

        UiDockHost timelineHost = workspace.SplitHost(
            documentHost,
            UiDockWorkspace.DockTarget.Bottom,
            0.61f);
        timelineHost.Id = "timeline-host";
        UiWindow timeline = new() { Id = "timeline", Title = "Timeline" };
        workspace.DockWindow(timeline, timelineHost);

        foreach (UiDockExternalGroupLease lease in leases.Reverse())
        {
            Assert.True(workspace.RestoreExternalDockGroup(lease));
        }

        Assert.Empty(colorExternal.Windows);
        Assert.Empty(layersExternal.Windows);
        Assert.Empty(propertiesExternal.Windows);
        Assert.Equal([color], colorHost.Windows);
        Assert.Equal([properties], propertiesHost.Windows);
        Assert.Equal([layers], layersHost.Windows);
        Assert.Equal([timeline], timelineHost.Windows);
        Assert.Equal(
            "split(horizontal=False,ratio=0.75,"
                + "split(horizontal=True,ratio=0.61,host(document-host),host(timeline-host)),"
                + "split(horizontal=True,ratio=0.19,host(color-host),"
                + "split(horizontal=True,ratio=0.52,host(properties-host),host(layers-host))))",
            Describe(workspace.CaptureState().Root));
    }

    private static UiDockHost HostExternally(UiWindow window)
    {
        UiDockHost host = new();
        host.DockWindow(window);
        return host;
    }

    private static string Describe(UiDockNodeState? node)
    {
        if (node == null)
        {
            return "null";
        }

        if (node.HostId != null)
        {
            return $"host({node.HostId})";
        }

        return $"split(horizontal={node.SplitHorizontal},ratio={node.SplitRatio:R},"
            + $"{Describe(node.First)},{Describe(node.Second)})";
    }
}
