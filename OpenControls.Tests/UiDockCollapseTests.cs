using OpenControls.Controls;
using OpenControls.State;
using System.Text.Json;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDockCollapseTests
{
    [Fact]
    public void CollapsingAnyRightPanelCollapsesSharedBranchAndPreservesAuthoredState()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        fixture.Workspace.Arrange();
        UiDockWorkspaceState before = fixture.Workspace.CaptureState();
        int expandedWidth = fixture.ColorHost.Bounds.Width;

        Assert.True(fixture.Workspace.SetCollapseRegionCollapsed(fixture.LayersHost, collapsed: true));
        fixture.Workspace.Arrange();

        UiRect strip = fixture.Workspace.GetCollapseRegionBounds(fixture.PropertiesHost);
        Assert.Equal(new UiRect(972, 0, 28, 600), strip);
        Assert.Equal(972, fixture.Workspace.RootHost.Bounds.Width);
        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.PropertiesHost));
        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.LayersHost));
        Assert.Equal(default, fixture.ColorHost.Bounds);
        Assert.False(fixture.ColorPrimary.Visible);
        Assert.False(fixture.ColorSecondary.Visible);

        UiDockWorkspaceState collapsed = fixture.Workspace.CaptureState();
        Assert.Equal(before.Root!.SplitRatio, collapsed.Root!.SplitRatio);
        Assert.Equal(before.Root.Second!.SplitRatio, collapsed.Root.Second!.SplitRatio);
        Assert.Equal(before.Root.Second.Second!.SplitRatio, collapsed.Root.Second.Second!.SplitRatio);
        Assert.True(collapsed.Root.Second!.IsCollapsed);
        Assert.Equal([fixture.ColorPrimary, fixture.ColorSecondary], fixture.ColorHost.Windows);
        Assert.Same(fixture.ColorSecondary, fixture.ColorHost.ActiveWindow);

        Assert.True(fixture.Workspace.SetCollapseRegionCollapsed(fixture.PropertiesHost, collapsed: false));
        fixture.Workspace.Arrange();

        Assert.Equal(expandedWidth, fixture.ColorHost.Bounds.Width);
        Assert.Equal(before.Root.SplitRatio, fixture.Workspace.CaptureState().Root!.SplitRatio);
        Assert.Equal([fixture.ColorPrimary, fixture.ColorSecondary], fixture.ColorHost.Windows);
        Assert.Same(fixture.ColorSecondary, fixture.ColorHost.ActiveWindow);
        Assert.True(fixture.ColorSecondary.Visible);
    }

    [Fact]
    public void CollapseAndRestoreButtonsRelayoutTheBranchInTheSameUpdate()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        Update(fixture.Workspace, new UiInputState());
        UiRect collapseButton = fixture.ColorHost.CollapseToggleBounds;
        Assert.True(collapseButton.Width > 0);
        Assert.Equal(UiDockCollapseEdge.Right, fixture.ColorHost.CollapseEdge);
        Assert.Equal(default, fixture.PropertiesHost.CollapseToggleBounds);
        Assert.Equal(default, fixture.LayersHost.CollapseToggleBounds);
        Assert.Same(
            fixture.ColorHost,
            fixture.Workspace.GetCollapseRegionRepresentative(fixture.LayersHost));

        Update(fixture.Workspace, Click(Center(collapseButton)));

        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
        Assert.Equal(28, fixture.Workspace.GetCollapseRegionBounds(fixture.ColorHost).Width);
        Assert.Equal(972, fixture.Workspace.RootHost.Bounds.Width);

        UiRect restoreButton = fixture.Workspace.GetCollapseRegionRestoreBounds(fixture.LayersHost);
        Update(fixture.Workspace, Click(Center(restoreButton)));

        Assert.False(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
        Assert.True(fixture.ColorHost.Bounds.Width > 28);
        Assert.Same(fixture.ColorSecondary, fixture.ColorHost.ActiveWindow);
    }

    [Fact]
    public void CollapsedBranchRoundTripsWithTabsActiveWindowAndRatios()
    {
        DockFixture source = CreatePhotoshopWorkspace();
        Assert.True(source.Workspace.SetCollapseRegionCollapsed(source.PropertiesHost, collapsed: true));
        UiDockWorkspaceState state = source.Workspace.CaptureState();
        DockFixture target = CreatePhotoshopWorkspace(addWindows: false);
        Dictionary<string, UiWindow> windows = CreateRestoredWindows();

        target.Workspace.ApplyState(state, windows);
        target.Workspace.Arrange();

        UiDockHost restoredColor = HostForWindow(target.Workspace, windows["color-primary"]);
        Assert.True(target.Workspace.IsCollapseRegionCollapsed(restoredColor));
        Assert.Equal(28, target.Workspace.GetCollapseRegionBounds(restoredColor).Width);
        Assert.Equal([windows["color-primary"], windows["color-secondary"]], restoredColor.Windows);
        Assert.Same(windows["color-secondary"], restoredColor.ActiveWindow);
        Assert.Equal(state.Root!.SplitRatio, target.Workspace.CaptureState().Root!.SplitRatio);
        Assert.True(target.Workspace.CaptureState().Root!.Second!.IsCollapsed);
    }

    [Fact]
    public void RootDocumentCollapseIsAlwaysRejected()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();

        Assert.False(fixture.Workspace.CanCollapseRegion(fixture.Workspace.RootHost));
        Assert.False(fixture.Workspace.SetCollapseRegionCollapsed(fixture.Workspace.RootHost, collapsed: true));

        fixture.Workspace.RootHost.AllowCollapse = true;
        Assert.False(fixture.Workspace.CanCollapseRegion(fixture.Workspace.RootHost));
        Assert.False(fixture.Workspace.SetCollapseRegionCollapsed(fixture.Workspace.RootHost, collapsed: true));
    }

    [Fact]
    public void ApplyStateRejectsRootPolicyViolationAndPartialCollapsedBranchWithoutMutation()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        UiDockWorkspaceState rootViolation = fixture.Workspace.CaptureState();
        rootViolation.Root!.First!.IsCollapsed = true;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Workspace.ApplyState(rootViolation, ExistingWindows(fixture)));
        Assert.False(fixture.Workspace.IsCollapseRegionCollapsed(fixture.Workspace.RootHost));
        Assert.Equal([fixture.ColorPrimary, fixture.ColorSecondary], fixture.ColorHost.Windows);

        UiDockWorkspaceState partialBranch = fixture.Workspace.CaptureState();
        UiDockNodeState rightBranch = partialBranch.Root!.Second!;
        rightBranch.IsCollapsed = false;
        FindLeaf(rightBranch).IsCollapsed = true;

        Assert.Throws<ArgumentException>(() =>
            fixture.Workspace.ApplyState(partialBranch, ExistingWindows(fixture)));
        Assert.False(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
        Assert.Same(fixture.ColorSecondary, fixture.ColorHost.ActiveWindow);

        UiDockWorkspaceState twoSiblings = fixture.Workspace.CaptureState();
        UiDockNodeState siblingBranch = twoSiblings.Root!.Second!;
        siblingBranch.First!.IsCollapsed = true;
        siblingBranch.Second!.IsCollapsed = true;

        Assert.Throws<ArgumentException>(() =>
            fixture.Workspace.ApplyState(twoSiblings, ExistingWindows(fixture)));
        Assert.False(fixture.Workspace.IsCollapseRegionCollapsed(fixture.ColorHost));
    }

    [Fact]
    public void ProtectedOrEmptySiblingDisablesTheWholeCollapseRegion()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        fixture.PropertiesHost.AllowCollapse = false;

        Assert.False(fixture.Workspace.CanCollapseRegion(fixture.ColorHost));
        Assert.False(fixture.Workspace.SetCollapseRegionCollapsed(fixture.LayersHost, collapsed: true));
        Update(fixture.Workspace, new UiInputState());
        Assert.Equal(default, fixture.ColorHost.CollapseToggleBounds);

        DockFixture emptySibling = CreatePhotoshopWorkspace(addWindows: false);
        emptySibling.Workspace.RootHost.DockWindow(emptySibling.Document);
        emptySibling.ColorHost.DockWindow(emptySibling.ColorPrimary);
        emptySibling.LayersHost.DockWindow(emptySibling.LayersWindow);

        Assert.False(emptySibling.Workspace.CanCollapseRegion(emptySibling.ColorHost));
        Assert.False(emptySibling.Workspace.SetCollapseRegionCollapsed(
            emptySibling.ColorHost,
            collapsed: true));
    }

    [Fact]
    public void ProgrammaticCollapseCancelsNestedSplitterDragAndPreservesNestedRatios()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        UiFocusManager focus = new();
        Update(fixture.Workspace, new UiInputState(), focus);
        UiPoint nestedSplitter = new(
            fixture.ColorHost.Bounds.X + 12,
            fixture.ColorHost.Bounds.Bottom + fixture.Workspace.SplitterThickness / 2);

        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = nestedSplitter,
            ScreenMousePosition = nestedSplitter,
            LeftClicked = true,
            LeftDown = true
        }, focus);
        UiDockWorkspaceState beforeCollapse = fixture.Workspace.CaptureState();

        Assert.True(fixture.Workspace.SetCollapseRegionCollapsed(fixture.ColorHost, collapsed: true));
        Assert.True(fixture.Workspace.IsCollapseRegionCollapsed(fixture.LayersHost));

        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = new UiPoint(nestedSplitter.X, nestedSplitter.Y + 120),
            ScreenMousePosition = new UiPoint(nestedSplitter.X, nestedSplitter.Y + 120),
            LeftDown = true
        }, focus);

        UiDockWorkspaceState afterMovement = fixture.Workspace.CaptureState();
        Assert.Equal(beforeCollapse.Root!.Second!.SplitRatio, afterMovement.Root!.Second!.SplitRatio);
        Assert.Equal(
            beforeCollapse.Root.Second.Second!.SplitRatio,
            afterMovement.Root.Second.Second!.SplitRatio);
    }

    [Fact]
    public void ProgrammaticTopologyReplacementCancelsActiveSplitterDrag()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        UiFocusManager focus = new();
        Update(fixture.Workspace, new UiInputState(), focus);
        UiPoint rootSplitter = new(
            fixture.Workspace.RootHost.Bounds.Right + fixture.Workspace.SplitterThickness / 2,
            fixture.Workspace.Bounds.Y + 100);

        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = rootSplitter,
            ScreenMousePosition = rootSplitter,
            LeftClicked = true,
            LeftDown = true
        }, focus);
        UiDockWorkspaceState beforeReplacement = fixture.Workspace.CaptureState();

        fixture.Workspace.SplitHost(
            fixture.LayersHost,
            UiDockWorkspace.DockTarget.Left,
            splitRatio: 0.50f);
        Update(fixture.Workspace, new UiInputState
        {
            MousePosition = new UiPoint(rootSplitter.X + 180, rootSplitter.Y),
            ScreenMousePosition = new UiPoint(rootSplitter.X + 180, rootSplitter.Y),
            LeftDown = true
        }, focus);

        Assert.Equal(
            beforeReplacement.Root!.SplitRatio,
            fixture.Workspace.CaptureState().Root!.SplitRatio);
    }

    [Fact]
    public void ApplyStatePolicyCannotMutateTopologyAndLeavesWorkspaceAtomic()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        UiDockWorkspaceState state = fixture.Workspace.CaptureState();
        string before = StateJson(fixture.Workspace);
        int hostCount = fixture.Workspace.DockHosts.Count;
        fixture.Workspace.CanDockWindowPredicate = (_, _, _) =>
        {
            fixture.Workspace.SplitHost(
                fixture.ColorHost,
                UiDockWorkspace.DockTarget.Left,
                splitRatio: 0.50f);
            return true;
        };

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Workspace.ApplyState(state, ExistingWindows(fixture)));

        Assert.Equal(hostCount, fixture.Workspace.DockHosts.Count);
        Assert.Equal(before, StateJson(fixture.Workspace));
        Assert.Same(fixture.ColorSecondary, fixture.ColorHost.ActiveWindow);
    }

    [Fact]
    public void ApplyStateFinalValidationRejectsLateHostIdentityDivergenceAtomically()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        UiDockWorkspaceState state = fixture.Workspace.CaptureState();
        string before = StateJson(fixture.Workspace);
        bool mutated = false;
        fixture.Workspace.CanDockWindowPredicate = (_, host, _) =>
        {
            if (!mutated)
            {
                host.Id += "-callback-mutation";
                mutated = true;
            }

            return true;
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            fixture.Workspace.ApplyState(state, ExistingWindows(fixture)));

        Assert.Contains("membership changed", exception.Message, StringComparison.Ordinal);
        Assert.Equal(before, StateJson(fixture.Workspace));
    }

    [Fact]
    public void CollapsedMutationsExpandBeforeRemoveClearAndSplitThenRoundTrip()
    {
        DockFixture source = CreatePhotoshopWorkspace();
        Assert.True(source.Workspace.SetCollapseRegionCollapsed(source.ColorHost, collapsed: true));

        Assert.True(source.LayersHost.RemoveWindow(source.LayersWindow));
        Assert.False(source.Workspace.IsCollapseRegionCollapsed(source.ColorHost));
        Assert.DoesNotContain(source.LayersHost, source.Workspace.DockHosts);

        Assert.True(source.Workspace.SetCollapseRegionCollapsed(source.ColorHost, collapsed: true));
        source.ColorHost.ClearWindows();
        Assert.DoesNotContain(source.ColorHost, source.Workspace.DockHosts);
        Assert.False(source.Workspace.IsCollapseRegionCollapsed(source.PropertiesHost));

        Assert.True(source.Workspace.SetCollapseRegionCollapsed(source.PropertiesHost, collapsed: true));
        UiDockHost newHost = source.Workspace.SplitHost(
            source.PropertiesHost,
            UiDockWorkspace.DockTarget.Right,
            splitRatio: 0.60f);
        UiWindow navigator = new() { Id = "navigator", Title = "Navigator" };
        newHost.DockWindow(navigator);
        Assert.False(source.Workspace.IsCollapseRegionCollapsed(source.PropertiesHost));

        UiDockWorkspaceState state = source.Workspace.CaptureState();
        string expected = JsonSerializer.Serialize(state);
        DockFixture target = CreatePhotoshopWorkspace(addWindows: false);
        Dictionary<string, UiWindow> windows = new(StringComparer.Ordinal)
        {
            [source.Document.Id] = new UiWindow { Id = source.Document.Id, Title = source.Document.Title },
            [source.PropertiesWindow.Id] = new UiWindow
            {
                Id = source.PropertiesWindow.Id,
                Title = source.PropertiesWindow.Title
            },
            [navigator.Id] = new UiWindow { Id = navigator.Id, Title = navigator.Title }
        };

        target.Workspace.ApplyState(state, windows);

        Assert.Equal(expected, StateJson(target.Workspace));
    }

    [Fact]
    public void CollapseClickClearsFocusAndCannotClickThroughToExpandedSibling()
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 600, 400),
            SplitterThickness = 6,
            CollapsedStripSize = 28
        };
        UiDockHost left = workspace.SplitHost(
            workspace.RootHost,
            UiDockWorkspace.DockTarget.Left,
            0.30f);
        UiWindow document = new() { Id = "document" };
        UiWindow siblingDocument = new() { Id = "sibling-document" };
        UiWindow panel = new() { Id = "panel" };
        UiButton focusedPanelButton = new() { Text = "Focused panel" };
        panel.AddContentChild(focusedPanelButton);
        panel.LayoutContent = bounds => focusedPanelButton.Bounds = bounds;
        workspace.RootHost.DockWindow(document);
        workspace.RootHost.DockWindow(siblingDocument);
        workspace.RootHost.ActivateWindow(0);
        left.DockWindow(panel);
        UiFocusManager focus = new();
        Update(workspace, new UiInputState(), focus);
        focus.RequestFocus(focusedPanelButton);
        Assert.Same(focusedPanelButton, focus.Focused);
        UiPoint collapsePoint = Center(left.CollapseToggleBounds);

        Update(workspace, Click(collapsePoint), focus);

        Assert.Null(focus.Focused);
        Assert.True(workspace.IsCollapseRegionCollapsed(left));
        Assert.True(workspace.RootHost.GetTabBounds(1).Contains(collapsePoint));
        Assert.Same(document, workspace.RootHost.ActiveWindow);
        Assert.Null(focus.Focused);
    }

    [Fact]
    public void CollapsedHostsSkipWindowLayoutContent()
    {
        DockFixture fixture = CreatePhotoshopWorkspace();
        int colorLayouts = 0;
        int propertiesLayouts = 0;
        int layersLayouts = 0;
        fixture.ColorPrimary.LayoutContent = _ => colorLayouts++;
        fixture.PropertiesWindow.LayoutContent = _ => propertiesLayouts++;
        fixture.LayersWindow.LayoutContent = _ => layersLayouts++;
        fixture.Workspace.Arrange();
        Assert.True(fixture.Workspace.SetCollapseRegionCollapsed(fixture.ColorHost, collapsed: true));
        int colorBefore = colorLayouts;
        int propertiesBefore = propertiesLayouts;
        int layersBefore = layersLayouts;

        fixture.Workspace.Arrange();
        fixture.Workspace.Arrange();

        Assert.Equal(colorBefore, colorLayouts);
        Assert.Equal(propertiesBefore, propertiesLayouts);
        Assert.Equal(layersBefore, layersLayouts);
    }

    [Theory]
    [InlineData(UiDockWorkspace.DockTarget.Left)]
    [InlineData(UiDockWorkspace.DockTarget.Right)]
    [InlineData(UiDockWorkspace.DockTarget.Top)]
    [InlineData(UiDockWorkspace.DockTarget.Bottom)]
    public void CollapseRegionUsesExactEdgeRailWithoutAdjacentSplitter(UiDockWorkspace.DockTarget edge)
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 600, 400),
            SplitterThickness = 6,
            CollapsedStripSize = 28
        };
        UiDockHost side = workspace.SplitHost(workspace.RootHost, edge, 0.40f);
        workspace.RootHost.DockWindow(new UiWindow { Id = "document" });
        side.DockWindow(new UiWindow { Id = "panel" });
        workspace.Arrange();
        UiRect expandedDocument = workspace.RootHost.Bounds;
        UiRect expandedSide = side.Bounds;

        Assert.True(workspace.SetCollapseRegionCollapsed(side, collapsed: true));
        workspace.Arrange();

        UiRect expectedStrip = edge switch
        {
            UiDockWorkspace.DockTarget.Left => new UiRect(0, 0, 28, 400),
            UiDockWorkspace.DockTarget.Top => new UiRect(0, 0, 600, 28),
            UiDockWorkspace.DockTarget.Bottom => new UiRect(0, 372, 600, 28),
            _ => new UiRect(572, 0, 28, 400)
        };
        UiRect expectedDocument = edge switch
        {
            UiDockWorkspace.DockTarget.Left => new UiRect(28, 0, 572, 400),
            UiDockWorkspace.DockTarget.Top => new UiRect(0, 28, 600, 372),
            UiDockWorkspace.DockTarget.Bottom => new UiRect(0, 0, 600, 372),
            _ => new UiRect(0, 0, 572, 400)
        };
        Assert.Equal(expectedStrip, workspace.GetCollapseRegionBounds(side));
        Assert.Equal(expectedDocument, workspace.RootHost.Bounds);

        UiRect restoreButton = workspace.GetCollapseRegionRestoreBounds(side);
        Update(workspace, Click(Center(restoreButton)));

        Assert.False(workspace.IsCollapseRegionCollapsed(side));
        Assert.Equal(expandedDocument, workspace.RootHost.Bounds);
        Assert.Equal(expandedSide, side.Bounds);
    }

    [Theory]
    [InlineData(UiArrowDirection.Left, 9, 7)]
    [InlineData(UiArrowDirection.Right, 9, 7)]
    [InlineData(UiArrowDirection.Up, 7, 9)]
    [InlineData(UiArrowDirection.Down, 7, 9)]
    public void DoubleChevronIsPetiteAndPointsTowardEveryEdge(
        UiArrowDirection direction,
        int expectedWidth,
        int expectedHeight)
    {
        FillRecordingRenderer renderer = new();

        UiDoubleChevron.Draw(
            renderer,
            new UiRect(0, 0, 28, 28),
            direction,
            new UiColor(240, 240, 240));

        Assert.NotEmpty(renderer.Fills);
        Assert.All(renderer.Fills, fill =>
        {
            Assert.Equal(1, fill.rect.Width);
            Assert.Equal(1, fill.rect.Height);
        });
        int minX = renderer.Fills.Min(fill => fill.rect.X);
        int maxX = renderer.Fills.Max(fill => fill.rect.X);
        int minY = renderer.Fills.Min(fill => fill.rect.Y);
        int maxY = renderer.Fills.Max(fill => fill.rect.Y);
        Assert.Equal(expectedWidth, maxX - minX + 1);
        Assert.Equal(expectedHeight, maxY - minY + 1);
        int centerX = (minX + maxX) / 2;
        int centerY = (minY + maxY) / 2;

        Assert.Contains(renderer.Fills, direction switch
        {
            UiArrowDirection.Left => fill => fill.rect.X == minX && fill.rect.Y == centerY,
            UiArrowDirection.Up => fill => fill.rect.X == centerX && fill.rect.Y == minY,
            UiArrowDirection.Down => fill => fill.rect.X == centerX && fill.rect.Y == maxY,
            _ => fill => fill.rect.X == maxX && fill.rect.Y == centerY
        });
    }

    private static DockFixture CreatePhotoshopWorkspace(bool addWindows = true)
    {
        UiDockWorkspace workspace = new()
        {
            Bounds = new UiRect(0, 0, 1000, 600),
            SplitterThickness = 6,
            MinPaneSize = 80,
            CollapsedStripSize = 28
        };
        UiDockHost color = workspace.SplitHost(workspace.RootHost, UiDockWorkspace.DockTarget.Right, 0.70f);
        UiDockHost properties = workspace.SplitHost(color, UiDockWorkspace.DockTarget.Bottom, 0.34f);
        UiDockHost layers = workspace.SplitHost(properties, UiDockWorkspace.DockTarget.Bottom, 0.52f);
        UiWindow document = new() { Id = "document", Title = "Document" };
        UiWindow colorPrimary = new() { Id = "color-primary", Title = "Color", TabIconText = "C" };
        UiWindow colorSecondary = new() { Id = "color-secondary", Title = "Swatches", TabIconText = "S" };
        UiWindow propertiesWindow = new() { Id = "properties", Title = "Properties", TabIconText = "P" };
        UiWindow layersWindow = new() { Id = "layers", Title = "Layers", TabIconText = "L" };
        if (addWindows)
        {
            workspace.RootHost.DockWindow(document);
            color.DockWindow(colorPrimary);
            color.DockWindow(colorSecondary);
            color.ActivateWindow(1);
            properties.DockWindow(propertiesWindow);
            layers.DockWindow(layersWindow);
        }

        return new DockFixture(
            workspace,
            color,
            properties,
            layers,
            document,
            colorPrimary,
            colorSecondary,
            propertiesWindow,
            layersWindow);
    }

    private static Dictionary<string, UiWindow> CreateRestoredWindows()
    {
        return new Dictionary<string, UiWindow>(StringComparer.Ordinal)
        {
            ["document"] = new UiWindow { Id = "document", Title = "Document" },
            ["color-primary"] = new UiWindow { Id = "color-primary", Title = "Color" },
            ["color-secondary"] = new UiWindow { Id = "color-secondary", Title = "Swatches" },
            ["properties"] = new UiWindow { Id = "properties", Title = "Properties" },
            ["layers"] = new UiWindow { Id = "layers", Title = "Layers" }
        };
    }

    private static Dictionary<string, UiWindow> ExistingWindows(DockFixture fixture)
    {
        return new Dictionary<string, UiWindow>(StringComparer.Ordinal)
        {
            [fixture.Document.Id] = fixture.Document,
            [fixture.ColorPrimary.Id] = fixture.ColorPrimary,
            [fixture.ColorSecondary.Id] = fixture.ColorSecondary,
            [fixture.PropertiesWindow.Id] = fixture.PropertiesWindow,
            [fixture.LayersWindow.Id] = fixture.LayersWindow
        };
    }

    private static UiDockHost HostForWindow(UiDockWorkspace workspace, UiWindow window)
    {
        return Assert.Single(workspace.DockHosts, host => host.Windows.Contains(window));
    }

    private static string StateJson(UiDockWorkspace workspace) =>
        JsonSerializer.Serialize(workspace.CaptureState());

    private static UiDockNodeState FindLeaf(UiDockNodeState node)
    {
        if (!string.IsNullOrWhiteSpace(node.HostId))
        {
            return node;
        }

        return FindLeaf(node.First ?? node.Second!);
    }

    private static UiPoint Center(UiRect bounds) =>
        new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

    private static UiInputState Click(UiPoint point) => new()
    {
        MousePosition = point,
        ScreenMousePosition = point,
        LeftClicked = true,
        LeftDown = true
    };

    private static void Update(UiElement element, UiInputState input)
    {
        Update(element, input, new UiFocusManager());
    }

    private static void Update(UiElement element, UiInputState input, UiFocusManager focus)
    {
        element.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }

    private sealed record DockFixture(
        UiDockWorkspace Workspace,
        UiDockHost ColorHost,
        UiDockHost PropertiesHost,
        UiDockHost LayersHost,
        UiWindow Document,
        UiWindow ColorPrimary,
        UiWindow ColorSecondary,
        UiWindow PropertiesWindow,
        UiWindow LayersWindow);

    private sealed class FillRecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<(UiRect rect, UiColor color)> Fills { get; } = new();

        public void FillRect(UiRect rect, UiColor color) => Fills.Add((rect, color));
        public void DrawRect(UiRect rect, UiColor color, int thickness = 1) { }
        public void FillRectGradient(
            UiRect rect,
            UiColor topLeft,
            UiColor topRight,
            UiColor bottomLeft,
            UiColor bottomRight) { }
        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB) { }
        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1) { }
        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font) { }
        public int MeasureTextWidth(string text, int scale = 1) => text.Length * 8 * scale;
        public int MeasureTextWidth(string text, int scale, UiFont? font) => MeasureTextWidth(text, scale);
        public int MeasureTextHeight(int scale = 1) => 8 * scale;
        public int MeasureTextHeight(int scale, UiFont? font) => MeasureTextHeight(scale);
        public void PushClip(UiRect rect) { }
        public void PopClip() { }
    }
}
