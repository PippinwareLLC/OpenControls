using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiNodeGraphTests
{
    private sealed class RecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<UiRect> FilledRects { get; } = new();

        public void FillRect(UiRect rect, UiColor color)
        {
            FilledRects.Add(rect);
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            FillRect(rect, topLeft);
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            FillRect(rect, colorA);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextWidth(text ?? string.Empty, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void NodeControl_LayoutKeepsPinsAndTextInSeparateBounds()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out _, out _);
        UiContext context = new(graph);

        context.Update(new UiInputState(), 1f / 60f);

        UiNodeDebugLayout layout = print.DebugLayout;
        Assert.Equal(new UiRect(260, 100, 240, 148), layout.Bounds);
        Assert.True(layout.HeaderBounds.Height > 0);
        Assert.True(layout.TitleBounds.Bottom <= layout.HeaderBounds.Bottom);
        Assert.All(layout.Pins, pin =>
        {
            Assert.True(pin.HitBounds.Width >= print.PinHitSize);
            Assert.True(pin.HitBounds.Height >= print.PinHitSize);
            Assert.False(Intersects(pin.HitBounds, layout.TitleBounds));
            Assert.False(Intersects(pin.HitBounds, layout.BodyTextBounds));
        });

        UiNodePinLayout input = Assert.Single(layout.Pins, pin => pin.Pin?.Id == "in");
        UiNodePinLayout output = Assert.Single(layout.Pins, pin => pin.Pin?.Id == "then");
        Assert.True(input.Center.X < output.Center.X);
    }

    [Fact]
    public void NodeGraph_RoutesWiresAndExposesDebugHitBounds()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWireDebugLayout debug = Assert.Single(graph.GetWireDebugLayouts());

        Assert.Same(wire, debug.Wire);
        Assert.Equal(UiNodePinKind.Exec, debug.Kind);
        Assert.Equal(6, debug.Route.Count);
        Assert.True(debug.Bounds.Width > 0);
        Assert.True(debug.HitBounds.Width > debug.Bounds.Width);
        Assert.True(debug.HitBounds.Height > debug.Bounds.Height);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.NotEmpty(renderer.FilledRects);
    }

    [Fact]
    public void NodeGraph_TracksPinHoverAndWirePreview()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out UiNodePin entryThen, out _);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint outputScreen = graph.Canvas.WorldToScreen(entryThen.Layout.Center);
        context.Update(new UiInputState
        {
            MousePosition = outputScreen,
            ScreenMousePosition = outputScreen
        }, 1f / 60f);

        Assert.Same(entryThen, graph.HoveredPin);

        context.Update(new UiInputState
        {
            MousePosition = outputScreen,
            ScreenMousePosition = outputScreen,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        UiPoint dragScreen = new(outputScreen.X + 120, outputScreen.Y + 30);
        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.PreviewWire.Active);
        Assert.Same(entryThen, graph.PreviewWire.StartPin);
        Assert.True(graph.PreviewWire.Route.Count >= 5);

        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftReleased = true
        }, 1f / 60f);

        Assert.False(graph.PreviewWire.Active);
    }

    [Fact]
    public void NodeControl_SelectsAndDragsFromHeader()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint headerWorld = new(entry.DebugLayout.HeaderBounds.X + 12, entry.DebugLayout.HeaderBounds.Y + 8);
        UiPoint headerScreen = graph.Canvas.WorldToScreen(headerWorld);
        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(entry.Selected);
        Assert.True(context.GetItemState(entry).IsClicked);

        UiPoint dragScreen = new(headerScreen.X + 24, headerScreen.Y + 16);
        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftDown = true,
            LeftDragOrigin = headerScreen
        }, 1f / 60f);

        Assert.True(entry.IsDragging);
        Assert.Equal(new UiRect(104, 126, 150, 104), entry.Bounds);
        Assert.Equal(entry.Bounds, entry.DebugLayout.Bounds);

        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftReleased = true
        }, 1f / 60f);

        Assert.False(entry.IsDragging);
    }

    [Fact]
    public void NodeGraph_DebugBoundsTransformThroughCanvasPanAndZoom()
    {
        UiNodeGraph graph = CreateGraph(out _, out UiNodeControl print, out _, out _);
        graph.PanX = 100;
        graph.PanY = 40;
        graph.Zoom = 2;
        UiContext context = new(graph);

        context.Update(new UiInputState(), 1f / 60f);
        UiItemStateSnapshot state = context.GetItemState(print);

        Assert.Equal(graph.Canvas.WorldToScreen(print.Bounds), state.Bounds);
    }

    private static UiNodeGraph CreateGraph(
        out UiNodeControl entry,
        out UiNodeControl print,
        out UiNodePin entryThen,
        out UiNodePin printIn)
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 900, 600),
            WireHitSlop = 8
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        entry = new UiNodeControl
        {
            Id = "entry",
            AutomationId = "entry",
            AutomationName = "Entry",
            AutomationRole = "node",
            Bounds = new UiRect(80, 110, 150, 104),
            Title = "Entry"
        };
        entryThen = entry.AddOutput("then", "Then", UiNodePinKind.Exec);

        print = new UiNodeControl
        {
            Id = "print",
            AutomationId = "print",
            AutomationName = "Print String",
            AutomationRole = "node",
            Bounds = new UiRect(260, 100, 240, 148),
            Title = "Print String",
            BodyText = "Console / Development"
        };
        printIn = print.AddInput("in", "In", UiNodePinKind.Exec);
        print.AddInput("message", "Message", UiNodePinKind.Data).DataType = "string";
        print.AddOutput("then", "Then", UiNodePinKind.Exec);

        graph.AddNode(entry);
        graph.AddNode(print);
        return graph;
    }

    private static bool Intersects(UiRect a, UiRect b)
    {
        if (a.Width <= 0 || a.Height <= 0 || b.Width <= 0 || b.Height <= 0)
        {
            return false;
        }

        return a.Left < b.Right
            && a.Right > b.Left
            && a.Top < b.Bottom
            && a.Bottom > b.Top;
    }
}
