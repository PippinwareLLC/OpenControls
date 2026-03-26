using OpenControls.Controls;
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
