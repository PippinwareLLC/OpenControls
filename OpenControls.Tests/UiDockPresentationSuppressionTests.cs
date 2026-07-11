using System.Text.Json;
using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockPresentationSuppressionTests
{
    [Fact]
    public void AtomicBatchReclaimsDockBranchAndRestoresExactAuthoredState()
    {
        Fixture fixture = CreateFixture(includeFloating: true);
        string authoredState = StateJson(fixture.Workspace);
        UiRect rootBounds = fixture.Workspace.RootHost.Bounds;
        UiRect colorBounds = fixture.ColorHost.Bounds;
        UiRect layersBounds = fixture.LayersHost.Bounds;
        UiRect floatingBounds = fixture.Floating!.Bounds;

        fixture.Workspace.SetPresentationSuppressedWindows(
            [fixture.Color, fixture.Swatches, fixture.Layers, fixture.Floating]);

        Assert.Equal(fixture.Workspace.Bounds, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(default, fixture.ColorHost.Bounds);
        Assert.Equal(default, fixture.LayersHost.Bounds);
        Assert.False(fixture.ColorHost.Visible);
        Assert.False(fixture.LayersHost.Visible);
        Assert.False(fixture.Color.Visible);
        Assert.False(fixture.Swatches.Visible);
        Assert.False(fixture.Layers.Visible);
        Assert.False(fixture.Floating.Visible);
        Assert.Equal(floatingBounds, fixture.Floating.Bounds);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
        Assert.Equal(4, fixture.Workspace.PresentationSuppressedWindows.Count);
        Assert.True(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Swatches));

        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.Equal(rootBounds, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(colorBounds, fixture.ColorHost.Bounds);
        Assert.Equal(layersBounds, fixture.LayersHost.Bounds);
        Assert.Equal(floatingBounds, fixture.Floating.Bounds);
        Assert.True(fixture.ColorHost.Visible);
        Assert.True(fixture.LayersHost.Visible);
        Assert.False(fixture.Color.Visible);
        Assert.True(fixture.Swatches.Visible);
        Assert.True(fixture.Layers.Visible);
        Assert.True(fixture.Floating.Visible);
        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Equal([fixture.Color, fixture.Swatches], fixture.ColorHost.Windows);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
    }

    [Fact]
    public void SuppressedLeafTakesZeroExtentAndVisibleSiblingGetsFreedSpace()
    {
        Fixture fixture = CreateFixture();
        string authoredState = StateJson(fixture.Workspace);
        UiRect authoredColorBounds = fixture.ColorHost.Bounds;
        UiRect authoredLayersBounds = fixture.LayersHost.Bounds;
        int authoredRootWidth = fixture.Workspace.RootHost.Bounds.Width;

        fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Layers], suppressed: true);

        Assert.Equal(authoredRootWidth, fixture.Workspace.RootHost.Bounds.Width);
        Assert.Equal(default, fixture.LayersHost.Bounds);
        Assert.Equal(fixture.Workspace.Bounds.Height, fixture.ColorHost.Bounds.Height);
        Assert.Equal(authoredColorBounds.X, fixture.ColorHost.Bounds.X);
        Assert.Equal(authoredColorBounds.Width, fixture.ColorHost.Bounds.Width);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));

        fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Layers], suppressed: false);

        Assert.Equal(authoredColorBounds, fixture.ColorHost.Bounds);
        Assert.Equal(authoredLayersBounds, fixture.LayersHost.Bounds);
        Assert.True(fixture.Layers.Visible);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
    }

    [Fact]
    public void CollapsedBranchSuppressionDoesNotChangeCollapseOrRatios()
    {
        Fixture fixture = CreateFixture();
        Assert.True(fixture.Workspace.SetCollapseRegionCollapsed(fixture.LayersHost, collapsed: true));
        fixture.Workspace.Arrange();
        string collapsedState = StateJson(fixture.Workspace);
        UiRect collapsedBounds = fixture.Workspace.GetCollapseRegionBounds(fixture.ColorHost);

        fixture.Workspace.SetPresentationSuppressedWindows(
            [fixture.Color, fixture.Swatches, fixture.Layers]);

        Assert.Equal(fixture.Workspace.Bounds, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(default, fixture.ColorHost.Bounds);
        Assert.Equal(default, fixture.LayersHost.Bounds);
        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
        Assert.Equal(collapsedState, StateJson(fixture.Workspace));

        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.LayersHost));
        Assert.Equal(collapsedBounds, fixture.Workspace.GetCollapseRegionBounds(fixture.LayersHost));
        Assert.Equal(collapsedState, StateJson(fixture.Workspace));
        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
    }

    [Fact]
    public void PartialTabHostBatchIsRejectedWithoutChangingExistingSuppression()
    {
        Fixture fixture = CreateFixture();
        fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Layers], suppressed: true);
        string stateBeforeFailure = StateJson(fixture.Workspace);
        UiRect rootBoundsBeforeFailure = fixture.Workspace.RootHost.Bounds;
        UiRect colorBoundsBeforeFailure = fixture.ColorHost.Bounds;

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Color], suppressed: true));

        Assert.Contains("complete tab group", error.Message, StringComparison.Ordinal);
        Assert.True(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Layers));
        Assert.False(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Color));
        Assert.Equal(rootBoundsBeforeFailure, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(colorBoundsBeforeFailure, fixture.ColorHost.Bounds);
        Assert.False(fixture.Layers.Visible);
        Assert.True(fixture.Swatches.Visible);
        Assert.Equal(stateBeforeFailure, StateJson(fixture.Workspace));
    }

    [Fact]
    public void ForeignAndNullWindowsAreRejectedAtomically()
    {
        Fixture fixture = CreateFixture();
        UiWindow foreign = new() { Id = "foreign" };
        string authoredState = StateJson(fixture.Workspace);
        UiRect authoredRootBounds = fixture.Workspace.RootHost.Bounds;

        Assert.Throws<ArgumentException>(() =>
            fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers, foreign]));
        Assert.Throws<ArgumentException>(() =>
            fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers, null!]));

        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
        Assert.True(fixture.Layers.Visible);
        Assert.Equal(authoredRootBounds, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
    }

    [Fact]
    public void ReconciliationDropsDetachedStaleWindowAndRestoresItsVisibility()
    {
        UiDockWorkspace workspace = new() { Bounds = new UiRect(0, 0, 600, 400) };
        UiWindow document = new() { Id = "document" };
        UiWindow palette = new() { Id = "palette", Bounds = new UiRect(30, 40, 180, 220) };
        workspace.RootHost.DockWindow(document);
        workspace.AddFloatingWindow(palette);
        workspace.Arrange();

        workspace.SetPresentationSuppressedWindows([palette]);
        Assert.False(palette.Visible);

        workspace.DockWindow(palette, workspace.RootHost);
        Assert.True(workspace.RootHost.RemoveWindow(palette));
        Assert.Null(palette.Parent);
        workspace.SetPresentationSuppressedWindows(Array.Empty<UiWindow>());

        Assert.True(palette.Visible);
        Assert.False(workspace.IsWindowPresentationSuppressed(palette));
        Assert.Empty(workspace.PresentationSuppressedWindows);
    }

    [Fact]
    public void ArrangeReconcilesAHostThatBecomesPartialAfterDirectTabMutation()
    {
        Fixture fixture = CreateFixture();
        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Color, fixture.Swatches]);
        UiWindow extra = new() { Id = "extra", Title = "Extra" };

        fixture.ColorHost.DockWindow(extra);
        Assert.False(fixture.ColorHost.Visible);
        fixture.Workspace.Arrange();

        Assert.True(fixture.ColorHost.Visible);
        Assert.True(fixture.Swatches.Visible);
        Assert.False(extra.Visible);
        Assert.False(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Color));
        Assert.False(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Swatches));
        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
    }

    [Fact]
    public void ResetLayoutReconcilesAndRestoresAFormerlyFloatingWindow()
    {
        Fixture fixture = CreateFixture(includeFloating: true);
        UiWindow floating = fixture.Floating!;
        fixture.Workspace.SetPresentationSuppressedWindows([floating]);

        fixture.Workspace.ResetLayout();
        fixture.Workspace.Arrange();

        Assert.True(floating.Visible);
        Assert.Null(floating.Parent);
        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
    }

    [Fact]
    public void IdenticalReconciliationIsATrueNoOp()
    {
        Fixture fixture = CreateFixture();
        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
        long localVersion = fixture.Workspace.LocalInvalidationVersion;
        long subtreeVersion = fixture.Workspace.SubtreeInvalidationVersion;
        UiRect rootBounds = fixture.Workspace.RootHost.Bounds;
        UiRect colorBounds = fixture.ColorHost.Bounds;

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.Equal(localVersion, fixture.Workspace.LocalInvalidationVersion);
        Assert.Equal(subtreeVersion, fixture.Workspace.SubtreeInvalidationVersion);
        Assert.Equal(rootBounds, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(colorBounds, fixture.ColorHost.Bounds);
    }

    [Fact]
    public void SuppressionSetterDoesNotInvokeConsumerLayoutCallbacks()
    {
        Fixture fixture = CreateFixture();
        fixture.Document.LayoutContent = _ => throw new InvalidOperationException("consumer layout failure");

        fixture.Workspace.SetPresentationSuppressedWindows(
            [fixture.Color, fixture.Swatches, fixture.Layers]);
        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.True(fixture.Document.Visible);
        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
    }

    [Theory]
    [InlineData(UiDockWorkspace.DockTarget.Left)]
    [InlineData(UiDockWorkspace.DockTarget.Right)]
    [InlineData(UiDockWorkspace.DockTarget.Top)]
    [InlineData(UiDockWorkspace.DockTarget.Bottom)]
    public void CollapsedSurvivingSiblingKeepsAnEdgeStrip(UiDockWorkspace.DockTarget edge)
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 1000, 600),
            SplitterThickness = 6,
            CollapsedStripSize = 28
        };
        UiDockHost panelHost = workspace.SplitHost(workspace.RootHost, edge, 0.70f);
        UiWindow document = new() { Id = "document" };
        UiWindow panel = new() { Id = "panel" };
        workspace.RootHost.DockWindow(document);
        panelHost.DockWindow(panel);
        Assert.True(workspace.SetCollapseRegionCollapsed(panelHost, collapsed: true));
        workspace.Arrange();
        string collapsedState = StateJson(workspace);

        workspace.SetPresentationSuppressedWindows([document]);

        UiRect expected = edge switch
        {
            UiDockWorkspace.DockTarget.Left => new UiRect(0, 0, 28, 600),
            UiDockWorkspace.DockTarget.Top => new UiRect(0, 0, 1000, 28),
            UiDockWorkspace.DockTarget.Bottom => new UiRect(0, 572, 1000, 28),
            _ => new UiRect(972, 0, 28, 600)
        };
        Assert.Equal(expected, workspace.GetCollapseRegionBounds(panelHost));
        Assert.Equal(default, workspace.RootHost.Bounds);
        Assert.Equal(collapsedState, StateJson(workspace));

        workspace.ClearWindowsPresentationSuppression();

        Assert.Equal(collapsedState, StateJson(workspace));
        Assert.Equal(expected, workspace.GetCollapseRegionBounds(panelHost));
    }

    [Fact]
    public void SuppressedFloatingWindowStopsInteractionAndDoesNotUpdateChildren()
    {
        Fixture fixture = CreateFixture(includeFloating: true);
        CountingElement counter = new() { Bounds = fixture.Floating!.ContentBounds };
        fixture.Floating.AddContentChild(counter);
        UiRect authoredBounds = fixture.Floating.Bounds;
        UiPoint titlePoint = new(authoredBounds.X + 10, authoredBounds.Y + 10);

        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = titlePoint,
            ScreenMousePosition = titlePoint,
            LeftClicked = true,
            LeftDown = true
        });
        Assert.True(fixture.Floating.IsDragging);
        int updatesBeforeSuppression = counter.UpdateCount;

        fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Floating], suppressed: true);
        Assert.False(fixture.Floating.IsDragging);

        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = new UiPoint(titlePoint.X + 100, titlePoint.Y + 100),
            ScreenMousePosition = new UiPoint(titlePoint.X + 100, titlePoint.Y + 100),
            LeftDown = true
        });

        Assert.Equal(updatesBeforeSuppression, counter.UpdateCount);
        Assert.Equal(authoredBounds, fixture.Floating.Bounds);

        fixture.Workspace.SetWindowsPresentationSuppressed([fixture.Floating], suppressed: false);
        Update(fixture.Workspace, new UiInputState());

        Assert.Equal(updatesBeforeSuppression + 1, counter.UpdateCount);
        Assert.Equal(authoredBounds, fixture.Floating.Bounds);
    }

    [Fact]
    public void SuppressingEveryDockWindowMakesTheWholeTreeZeroExtent()
    {
        Fixture fixture = CreateFixture();

        fixture.Workspace.SetPresentationSuppressedWindows(
            [fixture.Document, fixture.Color, fixture.Swatches, fixture.Layers]);

        Assert.Equal(default, fixture.Workspace.RootHost.Bounds);
        Assert.Equal(default, fixture.ColorHost.Bounds);
        Assert.Equal(default, fixture.LayersHost.Bounds);
        Assert.All(fixture.Workspace.DockHosts, host => Assert.False(host.Visible));

        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.NotEqual(default, fixture.Workspace.RootHost.Bounds);
        Assert.NotEqual(default, fixture.ColorHost.Bounds);
        Assert.NotEqual(default, fixture.LayersHost.Bounds);
        Assert.All(fixture.Workspace.DockHosts, host => Assert.True(host.Visible));
    }

    [Fact]
    public void HostAuthoredVisibilityIsRestoredRatherThanForcedVisible()
    {
        Fixture fixture = CreateFixture();
        fixture.LayersHost.Visible = false;

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.False(fixture.LayersHost.Visible);
    }

    [Fact]
    public void ReentrantSuppressionDuringDetachValidationIsRejectedWithoutMutation()
    {
        Fixture fixture = CreateFixture();
        string authoredState = StateJson(fixture.Workspace);
        fixture.LayersHost.AllowDetach = true;
        fixture.LayersHost.CanDetachWindowPredicate = _ =>
        {
            fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
            return true;
        };

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Workspace.BeginExternalDockGroup([fixture.Layers]));

        Assert.Contains("presentation suppression", error.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Workspace.PresentationSuppressedWindows);
        Assert.True(fixture.LayersHost.Visible);
        Assert.True(fixture.Layers.Visible);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
    }

    [Fact]
    public void RepeatedSuppressionRepairsVisibilityDriftThenConverges()
    {
        Fixture fixture = CreateFixture();
        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
        fixture.LayersHost.Visible = true;
        fixture.Layers.Visible = true;

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.False(fixture.LayersHost.Visible);
        Assert.False(fixture.Layers.Visible);
        long localVersion = fixture.Workspace.LocalInvalidationVersion;
        long subtreeVersion = fixture.Workspace.SubtreeInvalidationVersion;

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.Equal(localVersion, fixture.Workspace.LocalInvalidationVersion);
        Assert.Equal(subtreeVersion, fixture.Workspace.SubtreeInvalidationVersion);
    }

    [Fact]
    public void PopupCloseCallbackCannotLeaveSuppressedHostOrActiveTabVisible()
    {
        Fixture fixture = CreateFixture();
        UiPopup popup = new();
        fixture.Layers.AddContentChild(popup);
        popup.Closed += () =>
        {
            fixture.LayersHost.Visible = true;
            fixture.Layers.Visible = true;
        };
        popup.Open();

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.True(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Layers));
        Assert.False(fixture.LayersHost.Visible);
        Assert.False(fixture.Layers.Visible);
    }

    [Fact]
    public void PopupCloseCallbackCannotChangeAuthoredActiveTabDuringSuppression()
    {
        Fixture fixture = CreateFixture();
        UiPopup popup = new();
        fixture.Swatches.AddContentChild(popup);
        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Same(fixture.Swatches, fixture.ColorHost.ActiveWindow);
        popup.Closed += () => fixture.ColorHost.ActivateWindow(0);
        popup.Open();

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Color, fixture.Swatches]);

        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Same(fixture.Swatches, fixture.ColorHost.ActiveWindow);
        Assert.False(fixture.Color.Visible);
        Assert.False(fixture.Swatches.Visible);

        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Same(fixture.Swatches, fixture.ColorHost.ActiveWindow);
        Assert.False(fixture.Color.Visible);
        Assert.True(fixture.Swatches.Visible);
    }

    [Fact]
    public void CrossHostCloseCallbacksCannotDriftEarlierOrLaterSuppressedHosts()
    {
        Fixture fixture = CreateFixture();
        UiWindow history = new() { Id = "history", Title = "History" };
        fixture.LayersHost.DockWindow(history);
        fixture.LayersHost.ActivateWindow(1);
        fixture.Workspace.Arrange();
        string authoredState = StateJson(fixture.Workspace);
        UiPopup colorPopup = new();
        UiPopup historyPopup = new();
        UiPopup crossHostPopup = new();
        fixture.Swatches.AddContentChild(colorPopup);
        history.AddContentChild(historyPopup);
        int callbackAttempts = 0;
        colorPopup.Closed += () =>
        {
            callbackAttempts++;
            fixture.LayersHost.Visible = true;
            fixture.LayersHost.ActivateWindow(0);
            fixture.Layers.Visible = true;
        };
        historyPopup.Closed += () =>
        {
            callbackAttempts++;
            fixture.ColorHost.Visible = true;
            fixture.ColorHost.ActivateWindow(0);
            fixture.Color.Visible = true;
            crossHostPopup.Open();
            fixture.Swatches.AddContentChild(crossHostPopup);
        };
        colorPopup.Open();
        historyPopup.Open();

        fixture.Workspace.SetPresentationSuppressedWindows(
            [fixture.Color, fixture.Swatches, fixture.Layers, history]);

        Assert.Equal(2, callbackAttempts);
        Assert.False(fixture.ColorHost.Visible);
        Assert.False(fixture.LayersHost.Visible);
        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Same(fixture.Swatches, fixture.ColorHost.ActiveWindow);
        Assert.Equal(1, fixture.LayersHost.ActiveIndex);
        Assert.Same(history, fixture.LayersHost.ActiveWindow);
        Assert.False(fixture.Color.Visible);
        Assert.False(fixture.Swatches.Visible);
        Assert.False(fixture.Layers.Visible);
        Assert.False(history.Visible);
        Assert.Same(fixture.Swatches, crossHostPopup.Parent);
        Assert.False(crossHostPopup.IsOpen);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));

        fixture.Workspace.ClearWindowsPresentationSuppression();

        Assert.True(fixture.ColorHost.Visible);
        Assert.True(fixture.LayersHost.Visible);
        Assert.Equal(1, fixture.ColorHost.ActiveIndex);
        Assert.Same(fixture.Swatches, fixture.ColorHost.ActiveWindow);
        Assert.Equal(1, fixture.LayersHost.ActiveIndex);
        Assert.Same(history, fixture.LayersHost.ActiveWindow);
        Assert.False(fixture.Color.Visible);
        Assert.True(fixture.Swatches.Visible);
        Assert.False(fixture.Layers.Visible);
        Assert.True(history.Visible);
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
    }

    [Fact]
    public void SuppressionDismissesNestedInputLayersAndQueuedFocus()
    {
        Fixture fixture = CreateFixture();
        UiTextField deferredField = new() { Bounds = new UiRect(20, 20, 120, 24) };
        UiPopup nested = new() { Bounds = new UiRect(20, 50, 160, 100) };
        UiPopup popup = new() { Bounds = new UiRect(10, 10, 200, 180) };
        UiMenuBar menu = new() { DisplayMode = UiMenuDisplayMode.Popup };
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Transient command" });
        nested.AddChild(deferredField);
        popup.AddChild(nested);
        fixture.Layers.AddContentChild(popup);
        fixture.Layers.AddContentChild(menu);
        popup.Open();
        nested.Open();
        menu.OpenPopup();
        int queuedFocusCallbacks = 0;
        nested.QueueFocus(deferredField, () => queuedFocusCallbacks++);
        popup.Closed += popup.Open;
        nested.Closed += () => throw new InvalidOperationException("consumer close failure");

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.False(popup.IsOpen);
        Assert.False(nested.IsOpen);
        Assert.False(menu.HasOpenMenu);

        popup.Open();
        nested.Open();
        menu.OpenPopup();
        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
        Assert.False(popup.IsOpen);
        Assert.False(nested.IsOpen);
        Assert.False(menu.HasOpenMenu);

        fixture.Workspace.ClearWindowsPresentationSuppression();
        popup.Open();
        nested.Open();
        UiContext context = new(fixture.Workspace);
        context.Update(new UiInputState());

        Assert.Equal(0, queuedFocusCallbacks);
    }

    [Fact]
    public void SuppressionCancelsDragDropBeforeVisibleTargetsCanAccept()
    {
        Fixture fixture = CreateFixture();
        TestElement source = new() { Bounds = new UiRect(20, 20, 20, 20) };
        DropTargetElement target = new() { Bounds = new UiRect(100, 100, 120, 80) };
        fixture.Layers.AddContentChild(source);
        fixture.Document.AddContentChild(target);
        UiContext context = new(fixture.Workspace);
        Assert.True(context.DragDrop.TryBeginDrag(
            source,
            new UiDragDropPayload("suppression-test", new object()),
            new UiPoint(25, 25)));

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(110, 110),
            ScreenMousePosition = new UiPoint(110, 110),
            LeftReleased = true
        });

        Assert.False(context.DragDrop.IsDragging);
        Assert.Equal(0, target.AcceptedDrops);
    }

    [Fact]
    public void PublicWindowCancellationDismissesExternalPeerInputLayers()
    {
        UiWindow peer = new() { Bounds = new UiRect(0, 0, 300, 240) };
        UiPopup popup = new() { Bounds = new UiRect(10, 10, 180, 120) };
        UiMenuBar menu = new() { DisplayMode = UiMenuDisplayMode.Popup };
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Peer command" });
        peer.AddContentChild(popup);
        peer.AddContentChild(menu);
        popup.Open();
        menu.OpenPopup();

        peer.CancelTransientInteractions();

        Assert.False(popup.IsOpen);
        Assert.False(menu.HasOpenMenu);
    }

    [Fact]
    public void SuppressionCallbacksCannotMutateDockTopologyOrPartiallyReplaceBatch()
    {
        Fixture fixture = CreateFixture();
        UiWindow intruder = new() { Id = "intruder" };
        UiPopup splitPopup = new();
        UiPopup dockPopup = new();
        UiPopup replacePopup = new();
        fixture.Layers.AddContentChild(splitPopup);
        fixture.Layers.AddContentChild(dockPopup);
        fixture.Layers.AddContentChild(replacePopup);
        int callbackAttempts = 0;
        splitPopup.Closed += () =>
        {
            callbackAttempts++;
            fixture.Workspace.SplitHost(
                fixture.LayersHost,
                UiDockWorkspace.DockTarget.Left);
        };
        dockPopup.Closed += () =>
        {
            callbackAttempts++;
            fixture.LayersHost.DockWindow(intruder);
        };
        replacePopup.Closed += () =>
        {
            callbackAttempts++;
            fixture.Workspace.ClearWindowsPresentationSuppression();
        };
        splitPopup.Open();
        dockPopup.Open();
        replacePopup.Open();
        int authoredHostCount = fixture.Workspace.DockHosts.Count;
        string authoredState = StateJson(fixture.Workspace);

        fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]);

        Assert.Equal(3, callbackAttempts);
        Assert.Equal(authoredHostCount, fixture.Workspace.DockHosts.Count);
        Assert.Equal([fixture.Layers], fixture.LayersHost.Windows);
        Assert.Null(intruder.Parent);
        Assert.True(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Layers));
        Assert.Equal(authoredState, StateJson(fixture.Workspace));
    }

    [Fact]
    public void SameFrameDescendantSuppressionCancelsBeforeLaterDropTarget()
    {
        Fixture fixture = CreateFixture();
        SuppressingDragSourceElement source = new(
            () => fixture.Workspace.SetPresentationSuppressedWindows([fixture.Layers]))
        {
            Bounds = new UiRect(20, 20, 20, 20)
        };
        DropTargetElement target = new() { Bounds = new UiRect(100, 100, 120, 80) };
        fixture.Document.AddContentChild(source);
        fixture.Document.AddContentChild(target);
        UiContext context = new(fixture.Workspace);
        Assert.True(context.DragDrop.TryBeginDrag(
            source,
            new UiDragDropPayload("suppression-test", new object()),
            new UiPoint(25, 25)));

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(110, 110),
            ScreenMousePosition = new UiPoint(110, 110),
            LeftReleased = true
        });

        Assert.True(source.SuppressionRequested);
        Assert.False(context.DragDrop.IsDragging);
        Assert.Equal(0, target.AcceptedDrops);
        Assert.True(fixture.Workspace.IsWindowPresentationSuppressed(fixture.Layers));

        Assert.True(context.DragDrop.TryBeginDrag(
            source,
            new UiDragDropPayload("fresh-drag", new object()),
            new UiPoint(25, 25)));
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(40, 40),
            ScreenMousePosition = new UiPoint(40, 40),
            LeftDown = true
        });

        Assert.True(context.DragDrop.IsDragging);
        context.DragDrop.Cancel();
    }

    [Fact]
    public void WindowCancellationRescansAddedAndReparentedPopupSiblings()
    {
        UiWindow peer = new() { Bounds = new UiRect(0, 0, 300, 240) };
        TestElement outside = new();
        UiPopup original = new();
        UiPopup addedSibling = new();
        UiPopup reparentedSibling = new();
        peer.AddContentChild(original);
        outside.AddChild(reparentedSibling);
        original.Closed += () =>
        {
            addedSibling.Open();
            peer.AddContentChild(addedSibling);
            Assert.True(outside.RemoveChild(reparentedSibling));
            peer.AddContentChild(reparentedSibling);
        };
        original.Open();
        reparentedSibling.Open();

        peer.CancelTransientInteractions();

        Assert.Same(peer, addedSibling.Parent);
        Assert.Same(peer, reparentedSibling.Parent);
        Assert.False(original.IsOpen);
        Assert.False(addedSibling.IsOpen);
        Assert.False(reparentedSibling.IsOpen);
    }

    [Fact]
    public void AncestorSuppressionBlocksReopenWithoutContradictoryLifecycleEvents()
    {
        UiWindow peer = new() { Bounds = new UiRect(0, 0, 300, 240) };
        UiPopup popup = new();
        peer.AddContentChild(popup);
        List<string> events = new();
        popup.Opened += () => events.Add("opened");
        popup.Closed += () =>
        {
            events.Add("closed");
            popup.Open();
        };
        popup.Open();

        peer.CancelTransientInteractions();

        Assert.Equal(["opened", "closed"], events);
        Assert.False(popup.IsOpen);
    }

    [Theory]
    [InlineData(UiDockWorkspace.DockTarget.Left)]
    [InlineData(UiDockWorkspace.DockTarget.Right)]
    [InlineData(UiDockWorkspace.DockTarget.Top)]
    [InlineData(UiDockWorkspace.DockTarget.Bottom)]
    public void NestedOrthogonalSuppressionPreservesExactCollapseEdge(
        UiDockWorkspace.DockTarget collapseEdge)
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 1000, 600),
            SplitterThickness = 6,
            CollapsedStripSize = 28
        };
        bool verticalCollapse = collapseEdge is UiDockWorkspace.DockTarget.Left
            or UiDockWorkspace.DockTarget.Right;
        UiDockHost orthogonalSibling = workspace.SplitHost(
            workspace.RootHost,
            verticalCollapse ? UiDockWorkspace.DockTarget.Bottom : UiDockWorkspace.DockTarget.Right,
            0.70f);
        UiDockHost collapsedHost = workspace.SplitHost(
            workspace.RootHost,
            collapseEdge,
            0.62f);
        UiWindow document = new() { Id = "document" };
        UiWindow sibling = new() { Id = "orthogonal-sibling" };
        UiWindow collapsedPanel = new() { Id = "collapsed-panel" };
        workspace.RootHost.DockWindow(document);
        orthogonalSibling.DockWindow(sibling);
        collapsedHost.DockWindow(collapsedPanel);
        workspace.Arrange();
        Assert.True(workspace.SetCollapseRegionCollapsed(collapsedHost, collapsed: true));
        workspace.Arrange();
        UiRect authoredStrip = workspace.GetCollapseRegionBounds(collapsedHost);
        UiRect authoredSiblingBounds = orthogonalSibling.Bounds;
        UiDockCollapseEdge authoredEdge = collapsedHost.CollapseEdge;
        string authoredState = StateJson(workspace);

        workspace.SetPresentationSuppressedWindows([document]);

        Assert.Equal(authoredStrip, workspace.GetCollapseRegionBounds(collapsedHost));
        Assert.Equal(authoredSiblingBounds, orthogonalSibling.Bounds);
        Assert.Equal(authoredEdge, collapsedHost.CollapseEdge);
        Assert.Equal(collapseEdge switch
        {
            UiDockWorkspace.DockTarget.Left => UiDockCollapseEdge.Left,
            UiDockWorkspace.DockTarget.Top => UiDockCollapseEdge.Top,
            UiDockWorkspace.DockTarget.Bottom => UiDockCollapseEdge.Bottom,
            _ => UiDockCollapseEdge.Right
        }, collapsedHost.CollapseEdge);
        Assert.Equal(authoredState, StateJson(workspace));

        workspace.ClearWindowsPresentationSuppression();

        Assert.Equal(authoredStrip, workspace.GetCollapseRegionBounds(collapsedHost));
        Assert.Equal(authoredSiblingBounds, orthogonalSibling.Bounds);
        Assert.Equal(authoredState, StateJson(workspace));
    }

    private static Fixture CreateFixture(bool includeFloating = false)
    {
        UiDockWorkspace workspace = new()
        {
            Id = "workspace",
            Bounds = new UiRect(0, 0, 1000, 600),
            SplitterThickness = 6,
            MinPaneSize = 80
        };
        UiDockHost colorHost = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Right,
            0.70f);
        UiDockHost layersHost = workspace.SplitHost(
            colorHost,
            UiDockWorkspace.DockTarget.Bottom,
            0.56f);
        UiWindow document = new() { Id = "document", Title = "Document" };
        UiWindow color = new() { Id = "color", Title = "Color" };
        UiWindow swatches = new() { Id = "swatches", Title = "Swatches" };
        UiWindow layers = new() { Id = "layers", Title = "Layers" };
        workspace.RootHost.DockWindow(document);
        colorHost.DockWindow(color);
        colorHost.DockWindow(swatches);
        colorHost.ActivateWindow(1);
        layersHost.DockWindow(layers);

        UiWindow? floating = null;
        if (includeFloating)
        {
            floating = new UiWindow
            {
                Id = "floating",
                Title = "Floating",
                Bounds = new UiRect(40, 50, 180, 220)
            };
            workspace.AddFloatingWindow(floating);
        }

        workspace.Arrange();
        return new Fixture(
            workspace,
            colorHost,
            layersHost,
            document,
            color,
            swatches,
            layers,
            floating);
    }

    private static string StateJson(UiDockWorkspace workspace) =>
        JsonSerializer.Serialize(workspace.CaptureState());

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

    private sealed class CountingElement : UiElement
    {
        public int UpdateCount { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            UpdateCount++;
            base.Update(context);
        }
    }

    private sealed class TestElement : UiElement
    {
    }

    private sealed class DropTargetElement : UiElement
    {
        public int AcceptedDrops { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            if (context.Input.LeftReleased
                && context.DragDrop.AcceptPayload(this, "suppression-test") != null)
            {
                AcceptedDrops++;
            }

            base.Update(context);
        }
    }

    private sealed class SuppressingDragSourceElement : UiElement
    {
        private readonly Action _suppress;

        public SuppressingDragSourceElement(Action suppress)
        {
            _suppress = suppress;
        }

        public bool SuppressionRequested { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            if (!SuppressionRequested && context.Input.LeftReleased)
            {
                SuppressionRequested = true;
                _suppress();
            }

            base.Update(context);
        }
    }

    private sealed record Fixture(
        UiDockWorkspace Workspace,
        UiDockHost ColorHost,
        UiDockHost LayersHost,
        UiWindow Document,
        UiWindow Color,
        UiWindow Swatches,
        UiWindow Layers,
        UiWindow? Floating);
}
