using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiNodeGraphTests
{
    private sealed class RecordingRenderer : IUiRenderer, IUiVectorRenderer, IUiVectorPassRenderer, IUiTransformedVectorRenderer, IUiShapeRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<UiRect> FilledRects { get; } = new();
        public List<(UiRect Rect, UiColor Color)> FillCalls { get; } = new();
        public List<(string Text, int Scale, int PixelSize)> DrawnTexts { get; } = new();
        public List<(UiPoint[] Points, int Thickness, UiColor Color)> Polylines { get; } = new();
        public List<IReadOnlyList<UiPoint>> PolylineSources { get; } = new();
        public List<(UiRect Rect, int Radius, UiColor Color, int Thickness)> RoundedRects { get; } = new();
        public List<(UiPoint Center, int Radius, UiColor Color)> FilledCircles { get; } = new();
        public List<(UiPoint Center, int Radius, UiColor Color, int Thickness)> DrawnCircles { get; } = new();
        public List<string> Operations { get; } = new();
        public int BeginVectorPassCount { get; private set; }
        public int EndVectorPassCount { get; private set; }

        public void FillRect(UiRect rect, UiColor color)
        {
            Operations.Add("fill-rect");
            FilledRects.Add(rect);
            FillCalls.Add((rect, color));
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
            Operations.Add("draw-rect");
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            FillRect(rect, topLeft);
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            FillRect(rect, colorA);
        }

        public void FillRoundedRect(UiRect rect, int radius, UiColor color)
        {
            FillRect(rect, color);
        }

        public void FillTopRoundedRect(UiRect rect, int radius, UiColor color)
        {
            FillRect(rect, color);
        }

        public void DrawRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
        {
            Operations.Add("draw-rounded-rect");
            RoundedRects.Add((rect, radius, color, thickness));
        }

        public void DrawTopRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
        {
            Operations.Add("draw-top-rounded-rect");
            RoundedRects.Add((rect, radius, color, thickness));
        }

        public void FillCircle(UiPoint center, int radius, UiColor color)
        {
            Operations.Add("fill-circle");
            FilledCircles.Add((center, radius, color));
        }

        public void DrawCircle(UiPoint center, int radius, UiColor color, int thickness = 1)
        {
            Operations.Add("draw-circle");
            DrawnCircles.Add((center, radius, color, thickness));
        }

        public void FillTriangleRight(UiRect rect, UiColor color)
        {
            Operations.Add("fill-triangle-right");
            FillRect(rect, color);
        }

        public void DrawPolyline(IReadOnlyList<UiPoint> points, int thickness, UiColor color)
        {
            Operations.Add("draw-polyline");
            PolylineSources.Add(points);
            Polylines.Add((points.ToArray(), thickness, color));
        }

        public void DrawPolylineTransformed(
            IReadOnlyList<UiPoint> points,
            int thickness,
            UiColor color,
            UiPoint origin,
            float zoom,
            float panX,
            float panY)
        {
            PolylineSources.Add(points);
            UiPoint[] transformed = new UiPoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                transformed[i] = new UiPoint(
                    origin.X + (int)MathF.Round((points[i].X - panX) * zoom),
                    origin.Y + (int)MathF.Round((points[i].Y - panY) * zoom));
            }

            Operations.Add("draw-polyline-transformed");
            Polylines.Add((transformed, thickness, color));
        }

        public void BeginVectorPass()
        {
            BeginVectorPassCount++;
        }

        public void EndVectorPass()
        {
            EndVectorPassCount++;
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            DrawText(text, position, color, scale, DefaultFont);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            Operations.Add("draw-text");
            DrawnTexts.Add((text, scale, (font ?? DefaultFont).PixelSize));
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
    public void NodeGraph_SelectionMarqueeStoresBoundsAndDoesNotHitTest()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(50, 70, 320, 240)
        };
        UiRect marquee = new(24, 36, 120, 80);

        graph.SelectionMarqueeBounds = marquee;

        Assert.Equal(marquee, graph.SelectionMarqueeBounds);
        Assert.Null(new UiSelectionMarquee { Bounds = marquee }.HitTest(new UiPoint(40, 50)));

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        RecordingRenderer renderer = new();
        context.Render(renderer);

        Assert.Contains(renderer.FillCalls, call =>
            call.Rect.Equals(new UiRect(74, 106, 120, 80))
            && call.Color.Equals(new UiColor(76, 151, 255, 44)));

        graph.SelectionMarqueeBounds = null;

        Assert.Null(graph.SelectionMarqueeBounds);
    }

    [Fact]
    public void NodeGraph_BoxSelectionDragRaisesEventsAndClearsMarqueeOnRelease()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        List<UiNodeBoxSelectionEvent> started = [];
        List<UiNodeBoxSelectionEvent> updated = [];
        List<UiNodeBoxSelectionEvent> ended = [];
        List<UiNodeBoxSelectionEvent> cancelled = [];
        graph.BoxSelectionStarted += started.Add;
        graph.BoxSelectionUpdated += updated.Add;
        graph.BoxSelectionEnded += ended.Add;
        graph.BoxSelectionCancelled += cancelled.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint start = new(20, 20);
        UiPoint end = new(220, 220);
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);

        Assert.True(graph.IsBoxSelecting);
        Assert.Equal(new UiRect(20, 20, 200, 200), graph.SelectionMarqueeBounds);
        Assert.Equal(new UiRect(20, 20, 200, 200), graph.SelectionMarqueeWorldBounds);
        Assert.Single(started);
        Assert.Single(updated);
        Assert.Same(graph, started[0].Graph);
        Assert.Equal(new UiRect(20, 20, 200, 200), started[0].GraphLocalBounds);
        Assert.Equal(new UiRect(20, 20, 200, 200), started[0].WorldBounds);
        Assert.Contains(entry, started[0].HitNodes);
        Assert.False(started[0].IsCompleting);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeBoxSelectionEvent completed = Assert.Single(ended);
        Assert.Same(entry, Assert.Single(completed.HitNodes));
        Assert.True(completed.IsCompleting);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
        Assert.Null(graph.SelectionMarqueeWorldBounds);
        Assert.Empty(cancelled);
    }

    [Fact]
    public void NodeGraph_EmptyCanvasClickRequestsClearSelectionWithoutStartingBoxSelection()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        List<UiNodeGraphCommandRequestedEvent> commands = [];
        List<UiNodeBoxSelectionEvent> boxEvents = RecordBoxSelectionEvents(graph);
        graph.CommandRequested += commands.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint point = new(20, 20);
        context.Update(new UiInputState
        {
            MousePosition = point,
            ScreenMousePosition = point,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.False(graph.IsBoxSelecting);
        Assert.Empty(commands);
        Assert.Empty(boxEvents);

        context.Update(new UiInputState
        {
            MousePosition = point,
            ScreenMousePosition = point,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeGraphCommandRequestedEvent command = Assert.Single(commands);
        Assert.Same(graph, command.Graph);
        Assert.Equal(UiNodeGraphCommand.ClearSelection, command.Command);
        Assert.Equal(UiModifierKeys.None, command.Modifiers);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
        Assert.Empty(boxEvents);
    }

    [Fact]
    public void NodeGraph_RightClickEmptyCanvasOpensNodeSearchAtWorldPosition()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        graph.PanX = 100;
        graph.PanY = 50;
        graph.Zoom = 1.5f;
        UiNodeSearchRequestedEvent? requested = null;
        graph.NodeSearchRequested += ev =>
        {
            requested = ev;
            graph.SetNodeSearchResults(
            [
                new UiNodeSearchItem("nodesharp.console.print", "Print String", "Console"),
                new UiNodeSearchItem("nodesharp.flow.branch", "Branch", "Flow")
            ]);
        };

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint click = new(720, 420);
        UiPoint expectedWorld = graph.Canvas.ScreenToWorld(click);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            RightClicked = true,
            RightDown = true
        }, 1f / 60f);

        Assert.True(graph.IsNodeSearchOpen);
        Assert.True(context.WantTextInput);
        Assert.Same(graph, context.Focus.Focused);
        Assert.NotNull(requested);
        Assert.Equal(expectedWorld, requested.WorldPosition);
        Assert.Equal(click, requested.ScreenPosition);
        Assert.Equal("", graph.NodeSearchQuery);
        Assert.Equal(2, graph.NodeSearchItems.Count);
        Assert.Contains(graph.NodeSearchDisplayRows, row => row.Kind == "category" && row.Text.Contains("Console", StringComparison.Ordinal));
        Assert.Contains(graph.NodeSearchDisplayRows, row => row.Kind == "category" && row.Text.Contains("Flow", StringComparison.Ordinal));
        Assert.Contains(graph.GetNodeSearchDebugRows(), row => row.Kind == "item" && row.ItemId == "nodesharp.console.print");
        Assert.DoesNotContain(graph.GetNodeSearchDebugRows(), row => row.Kind == "item" && row.ItemId == "nodesharp.flow.branch");
        Assert.True(graph.NodeSearchPopupBounds.Width > 0);
        Assert.True(graph.NodeSearchPopupBounds.Height > 0);

        RecordingRenderer renderer = new();
        graph.RenderOverlay(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "Search nodes...");
        Assert.Contains(renderer.DrawnTexts, text => text.Text.Contains("Print String", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeGraph_NodeSearchTypingFiltersAndEnterInvokesSelection()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        UiNodeSearchItem[] allItems =
        [
            new("nodesharp.console.print", "Print String", "Console", SearchText: "print console"),
            new("nodesharp.flow.branch", "Branch", "Flow", SearchText: "branch flow")
        ];
        graph.NodeSearchRequested += _ => graph.SetNodeSearchResults(allItems);
        graph.NodeSearchQueryChanged += ev =>
        {
            var filtered = allItems
                .Where(item => item.SearchText.Contains(ev.Query, StringComparison.OrdinalIgnoreCase)
                    || item.Title.Contains(ev.Query, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            graph.SetNodeSearchResults(filtered);
        };
        UiNodeSearchItemInvokedEvent? invoked = null;
        graph.NodeSearchItemInvoked += ev => invoked = ev;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint click = new(720, 420);
        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            RightClicked = true,
            RightDown = true
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            TextInput = "pri".ToCharArray()
        }, 1f / 60f);

        Assert.True(graph.IsNodeSearchOpen);
        Assert.Equal("pri", graph.NodeSearchQuery);
        Assert.Single(graph.NodeSearchItems);
        Assert.Equal("nodesharp.console.print", graph.NodeSearchItems[0].Id);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            KeysPressed = new[] { UiKey.Enter },
            Navigation = new UiNavigationInput { Enter = true }
        }, 1f / 60f);

        Assert.False(graph.IsNodeSearchOpen);
        Assert.NotNull(invoked);
        Assert.Equal("nodesharp.console.print", invoked.Item.Id);
        Assert.Equal("pri", invoked.Query);
    }

    [Fact]
    public void NodeGraph_NodeSearchHoverUpdatesSelectionAndWheelScrollsRows()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        UiNodeSearchItem[] allItems = Enumerable.Range(0, 36)
            .Select(index => new UiNodeSearchItem($"nodesharp.test.{index:00}", $"Test Node {index:00}", "Testing"))
            .ToArray();
        graph.NodeSearchRequested += _ => graph.SetNodeSearchResults(allItems);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint click = new(720, 420);
        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            RightClicked = true,
            RightDown = true
        }, 1f / 60f);

        var row = graph.GetNodeSearchDebugRows().Single(debug => debug.ItemId == "nodesharp.test.01");
        UiPoint hover = Center(row.Bounds);
        context.Update(new UiInputState
        {
            MousePosition = hover,
            ScreenMousePosition = hover
        }, 1f / 60f);

        Assert.Equal(1, graph.NodeSearchSelectedIndex);
        Assert.Equal(0, graph.NodeSearchScrollY);

        context.Update(new UiInputState
        {
            MousePosition = hover,
            ScreenMousePosition = hover,
            ScrollDelta = -120
        }, 1f / 60f);

        Assert.True(graph.NodeSearchScrollY > 0);
        Assert.True(graph.NodeSearchRowsViewportBounds.Height <= 320);
    }

    [Fact]
    public void NodeGraph_RightClickNodeDoesNotOpenNodeSearch()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        var opened = false;
        graph.NodeSearchRequested += _ => opened = true;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint click = graph.Canvas.WorldToScreen(Center(entry.Bounds));

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            RightClicked = true,
            RightDown = true
        }, 1f / 60f);

        Assert.False(opened);
        Assert.False(graph.IsNodeSearchOpen);
    }

    [Fact]
    public void NodeGraph_BoxSelectionBoundsAreCorrectUnderPanAndZoom()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        graph.PanX = 100;
        graph.PanY = 40;
        graph.Zoom = 2f;
        List<UiNodeBoxSelectionEvent> ended = [];
        graph.BoxSelectionEnded += ended.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint start = graph.Canvas.WorldToScreen(new UiPoint(120, 50));
        UiPoint end = graph.Canvas.WorldToScreen(new UiPoint(220, 150));
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);

        Assert.Equal(new UiRect(40, 20, 200, 200), graph.SelectionMarqueeBounds);
        Assert.Equal(new UiRect(120, 50, 100, 100), graph.SelectionMarqueeWorldBounds);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeBoxSelectionEvent ev = Assert.Single(ended);
        Assert.Equal(new UiRect(40, 20, 200, 200), ev.GraphLocalBounds);
        Assert.Equal(new UiRect(120, 50, 100, 100), ev.WorldBounds);
        Assert.Contains(entry, ev.HitNodes);
    }

    [Fact]
    public void NodeGraph_BoxSelectionHitsVisibleEnabledNodesButNotComments()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out _, out _);
        UiNodeControl hidden = new()
        {
            Id = "hidden",
            Bounds = new UiRect(120, 160, 140, 80),
            Title = "Hidden",
            Visible = false
        };
        UiNodeControl disabled = new()
        {
            Id = "disabled",
            Bounds = new UiRect(160, 180, 140, 80),
            Title = "Disabled",
            Enabled = false
        };
        UiNodeCommentBox comment = new()
        {
            Id = "comment",
            Bounds = new UiRect(30, 60, 520, 240),
            Title = "Comment",
            Text = "This should not appear in node hits."
        };
        graph.AddNode(hidden);
        graph.AddNode(disabled);
        graph.AddCommentBox(comment);
        List<UiNodeBoxSelectionEvent> ended = [];
        graph.BoxSelectionEnded += ended.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint start = new(20, 350);
        UiPoint end = new(540, 60);
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeBoxSelectionEvent ev = Assert.Single(ended);
        Assert.Equal(new[] { entry, print }, ev.HitNodes);
        Assert.DoesNotContain(hidden, ev.HitNodes);
        Assert.DoesNotContain(disabled, ev.HitNodes);
        Assert.Single(graph.Comments);
    }

    [Fact]
    public void NodeControl_LayoutKeepsPinsAndTextInSeparateBounds()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out _, out _);
        UiContext context = new(graph);

        context.Update(new UiInputState(), 1f / 60f);

        UiNodeDebugLayout layout = print.DebugLayout;
        Assert.Equal(new UiRect(260, 100, 240, 148), layout.Bounds);
        Assert.True(layout.HeaderBounds.Height >= 30);
        Assert.True(layout.TitleBounds.Bottom <= layout.HeaderBounds.Bottom);
        Assert.True(layout.SubtitleBounds.Width > 0);
        Assert.True(layout.SubtitleBounds.Bottom <= layout.HeaderBounds.Bottom);
        Assert.False(Intersects(layout.TitleBounds, layout.SubtitleBounds));
        Assert.All(layout.Pins, pin =>
        {
            Assert.True(pin.HitBounds.Width >= print.PinHitSize);
            Assert.True(pin.HitBounds.Height >= print.PinHitSize);
            Assert.False(Intersects(pin.HitBounds, layout.TitleBounds));
            Assert.False(Intersects(pin.HitBounds, layout.SubtitleBounds));
            Assert.False(Intersects(pin.HitBounds, layout.BodyTextBounds));
        });

        UiNodePinLayout input = Assert.Single(layout.Pins, pin => pin.Pin?.Id == "in");
        UiNodePinLayout output = Assert.Single(layout.Pins, pin => pin.Pin?.Id == "then");
        Assert.True(input.Center.X < output.Center.X);
    }

    [Fact]
    public void NodeControl_SubtitleUsesDedicatedHeaderRegion()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "subtitle-node",
            AutomationId = "subtitle-node",
            AutomationName = "Subtitle Node",
            AutomationRole = "node",
            Bounds = new UiRect(120, 96, 240, 132),
            Title = "Print String",
            Subtitle = "Console / Development",
            HeaderHeight = 52,
            Padding = 10
        };
        node.AddInput("in", "In", UiNodePinKind.Exec);
        node.AddInput("message", "Message", UiNodePinKind.Data);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(node.DebugLayout.HeaderBounds.Height >= 30);
        Assert.True(node.DebugLayout.SubtitleBounds.Width > 0);
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.TitleBounds));
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.SubtitleBounds));
        Assert.False(Intersects(node.DebugLayout.TitleBounds, node.DebugLayout.SubtitleBounds));
        Assert.All(node.DebugLayout.Pins, pin =>
        {
            Assert.False(Intersects(pin.HitBounds, node.DebugLayout.TitleBounds));
            Assert.False(Intersects(pin.HitBounds, node.DebugLayout.SubtitleBounds));
        });
    }

    [Fact]
    public void NodeControl_HeaderIconUsesDedicatedRegionAndReservesTitleSpace()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "icon-node",
            AutomationId = "icon-node",
            AutomationName = "Icon Node",
            AutomationRole = "node",
            Bounds = new UiRect(120, 96, 260, 132),
            Icon = "\uF121",
            Title = "Call Function",
            Subtitle = "Target is Object",
            HeaderHeight = 52,
            Padding = 10
        };
        node.AddInput("in", "In", UiNodePinKind.Exec);
        node.AddOutput("then", "Then", UiNodePinKind.Exec);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(node.DebugLayout.IconBounds.Width > 0);
        Assert.True(node.DebugLayout.IconBounds.Height > 0);
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.IconBounds));
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.TitleBounds));
        Assert.False(Intersects(node.DebugLayout.IconBounds, node.DebugLayout.TitleBounds));
        Assert.True(node.DebugLayout.TitleBounds.X >= node.DebugLayout.IconBounds.Right);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "\uF121");
    }

    [Fact]
    public void NodeControl_InputDataPinValueTextUsesDedicatedInlineBox()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "value-node",
            Bounds = new UiRect(120, 96, 260, 126),
            Title = "Branch",
            Padding = 10,
            PinHitSize = 18,
            PinVisualSize = 10
        };
        node.AddInput("in", "In", UiNodePinKind.Exec);
        UiNodePin condition = node.AddInput("condition", "Condition", UiNodePinKind.Data);
        condition.ValueText = "false";
        node.AddOutput("true", "True", UiNodePinKind.Exec);
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "condition");
        Assert.True(layout.ValueBounds.Width >= node.ValueBoxMinWidth);
        Assert.True(layout.ValueBounds.Height > 0);
        Assert.True(Contains(layout.RowBounds, layout.ValueBounds));
        Assert.False(Intersects(layout.HitBounds, layout.ValueBounds));
        Assert.False(Intersects(layout.LabelBounds, layout.ValueBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "false");
    }

    [Fact]
    public void NodeControl_OutputDataPinValueTextUsesDedicatedInlineBox()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        Assert.True(layout.ValueBounds.Width >= node.ValueBoxMinWidth);
        Assert.True(layout.ValueBounds.Height > 0);
        Assert.True(Contains(layout.RowBounds, layout.ValueBounds));
        Assert.True(layout.ValueBounds.Right < layout.LabelBounds.X);
        Assert.False(Intersects(layout.HitBounds, layout.ValueBounds));
        Assert.False(Intersects(layout.LabelBounds, layout.ValueBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "41");
    }

    [Fact]
    public void NodeControl_OutputDataPinEmptyEditableValueFieldUsesDedicatedInlineBox()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "String Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueFieldVisible = true;
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        Assert.True(layout.ValueBounds.Width >= node.ValueBoxMinWidth);
        Assert.True(layout.ValueBounds.Height > 0);
        Assert.True(Contains(layout.RowBounds, layout.ValueBounds));
        Assert.True(layout.ValueBounds.Right < layout.LabelBounds.X);
        Assert.False(Intersects(layout.HitBounds, layout.ValueBounds));
        Assert.False(Intersects(layout.LabelBounds, layout.ValueBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.RoundedRects, rect => rect.Rect.Equals(layout.ValueBounds));
    }

    [Fact]
    public void NodeControl_ValueBoxEditingRendersInsideInlineBox()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        value.IsValueEditing = true;
        value.EditingValueText = "128";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        Assert.True(layout.ValueBounds.Width > 0);
        Assert.True(Contains(layout.RowBounds, layout.ValueBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "128");
        Assert.Contains(renderer.FillCalls, call =>
            call.Color.Equals(node.ValueBoxCaretColor)
            && Contains(layout.ValueBounds, call.Rect));
    }

    [Fact]
    public void NodeGraph_ClickingValueFieldEditsInlineAndCommitsText()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        UiNodeValueEditStartedEvent? started = null;
        UiNodeValueEditCommittedEvent? committed = null;
        var selectionRequests = 0;
        var previewStarts = 0;
        var connectionRequests = 0;
        graph.ValueEditStarted += ev => started = ev;
        graph.ValueEditCommitted += ev => committed = ev;
        graph.NodeSelectionRequested += _ => selectionRequests++;
        graph.WirePreviewStarted += _ => previewStarts++;
        graph.WireConnectionRequested += _ => connectionRequests++;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        UiPoint click = graph.Canvas.WorldToScreen(Center(layout.ValueBounds));

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.IsEditingValue);
        Assert.Same(graph, context.Focus.Focused);
        Assert.True(context.WantTextInput);
        Assert.DoesNotContain(graph.Canvas.Children, child => child.AutomationId == "node-inline-value-editor");
        Assert.Same(value, graph.HoveredValuePin);
        Assert.NotNull(started);
        Assert.Equal("41", started.InitialText);
        Assert.Equal(0, selectionRequests);
        Assert.Equal(0, previewStarts);
        Assert.Equal(0, connectionRequests);
        Assert.False(graph.PreviewWire.Active);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            TextInput = new[] { '9' }
        }, 1f / 60f);

        Assert.Equal("9", value.EditingValueText);
        Assert.Equal(1, value.EditingCaretIndex);
        Assert.Equal(value.EditingSelectionStart, value.EditingSelectionEnd);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            KeysPressed = new[] { UiKey.Enter },
            Navigation = new UiNavigationInput { Enter = true }
        }, 1f / 60f);

        Assert.NotNull(committed);
        Assert.Same(value, committed.Pin);
        Assert.Equal("9", committed.Text);
        Assert.Equal("9", value.ValueText);
        Assert.False(graph.IsEditingValue);
    }

    [Fact]
    public void NodeGraph_MiddlePanAndWheelZoomRaiseViewportChangedEvents()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        graph.Canvas.PanButton = UiCanvas.UiCanvasPanButton.Middle;
        graph.AddNode(new UiNodeControl
        {
            Id = "node-under-pointer",
            Bounds = new UiRect(20, 20, 140, 90),
            Title = "Focusable Node"
        });

        List<UiNodeGraphViewportChangedEvent> changes = [];
        graph.ViewportChanged += changes.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(40, 40),
            ScreenMousePosition = new UiPoint(40, 40),
            MiddleClicked = true,
            MiddleDown = true
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(90, 65),
            ScreenMousePosition = new UiPoint(90, 65),
            MiddleDown = true
        }, 1f / 60f);

        Assert.Contains(changes, change => change.Reason == UiCanvas.UiCanvasViewportChangeReason.Pan);
        Assert.Equal(-50, graph.PanX);
        Assert.Equal(-25, graph.PanY);

        var zoomBefore = graph.Zoom;
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(100, 100),
            ScreenMousePosition = new UiPoint(100, 100),
            ScrollDelta = 120
        }, 1f / 60f);

        Assert.Contains(changes, change => change.Reason == UiCanvas.UiCanvasViewportChangeReason.Zoom);
        Assert.True(graph.Zoom > zoomBefore);
    }

    [Fact]
    public void NodeGraph_BoxSelectionDoesNotStartFromInteractiveTargetsOrWhileEditingText()
    {
        AssertNoBoxSelectionFromNodeTarget(useHeader: true);
        AssertNoBoxSelectionFromNodeTarget(useHeader: false);
        AssertNoBoxSelectionFromPinWirePreview();
        AssertNoBoxSelectionFromCommentEditTarget();
        AssertNoBoxSelectionFromCommentInterior();
        AssertNoBoxSelectionFromValueFieldDrag();
        AssertNoBoxSelectionFromHoveredWire();
        AssertNoBoxSelectionWhileTextEditing();
    }

    [Fact]
    public void NodeGraph_BoxSelectionEscapeCancelsAndClearsMarquee()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        List<UiNodeBoxSelectionEvent> started = [];
        List<UiNodeBoxSelectionEvent> updated = [];
        List<UiNodeBoxSelectionEvent> ended = [];
        List<UiNodeBoxSelectionEvent> cancelled = [];
        graph.BoxSelectionStarted += started.Add;
        graph.BoxSelectionUpdated += updated.Add;
        graph.BoxSelectionEnded += ended.Add;
        graph.BoxSelectionCancelled += cancelled.Add;

        UiContext context = new(graph);
        UiPoint start = new(20, 20);
        UiPoint end = new(220, 220);
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);

        Assert.True(graph.IsBoxSelecting);
        Assert.NotNull(graph.SelectionMarqueeBounds);
        Assert.NotNull(graph.SelectionMarqueeWorldBounds);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            KeysPressed = new[] { UiKey.Escape },
            Navigation = new UiNavigationInput { Escape = true }
        }, 1f / 60f);

        Assert.Single(started);
        Assert.Single(updated);
        Assert.Empty(ended);
        UiNodeBoxSelectionEvent ev = Assert.Single(cancelled);
        Assert.False(ev.IsCompleting);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
        Assert.Null(graph.SelectionMarqueeWorldBounds);
    }

    [Fact]
    public void NodeGraph_MiddleAndSpaceLeftPanDoNotStartBoxSelection()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        graph.Canvas.PanButton = UiCanvas.UiCanvasPanButton.Middle;
        graph.Canvas.PanWithSpaceLeftButton = true;
        List<UiNodeBoxSelectionEvent> boxEvents = RecordBoxSelectionEvents(graph);
        List<UiNodeGraphViewportChangedEvent> viewportEvents = [];
        graph.ViewportChanged += viewportEvents.Add;

        UiContext context = new(graph);
        UiPoint middleStart = new(50, 50);
        UiPoint middleEnd = new(100, 75);
        context.Update(new UiInputState
        {
            MousePosition = middleStart,
            ScreenMousePosition = middleStart,
            MiddleClicked = true,
            MiddleDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = middleEnd,
            ScreenMousePosition = middleEnd,
            MiddleDown = true,
            MiddleDragOrigin = middleStart
        }, 1f / 60f);

        Assert.Contains(viewportEvents, change => change.Reason == UiCanvas.UiCanvasViewportChangeReason.Pan);
        Assert.Equal(-50, graph.PanX);
        Assert.Equal(-25, graph.PanY);
        Assert.Empty(boxEvents);
        Assert.False(graph.IsBoxSelecting);

        graph.PanX = 0;
        graph.PanY = 0;
        viewportEvents.Clear();
        UiPoint spaceStart = new(60, 60);
        UiPoint spaceEnd = new(90, 90);
        context.Update(new UiInputState
        {
            MousePosition = spaceStart,
            ScreenMousePosition = spaceStart,
            LeftClicked = true,
            LeftDown = true,
            KeysDown = new[] { UiKey.Space }
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = spaceEnd,
            ScreenMousePosition = spaceEnd,
            LeftDown = true,
            LeftDragOrigin = spaceStart,
            KeysDown = new[] { UiKey.Space }
        }, 1f / 60f);

        Assert.Contains(viewportEvents, change => change.Reason == UiCanvas.UiCanvasViewportChangeReason.Pan);
        Assert.Equal(-30, graph.PanX);
        Assert.Equal(-30, graph.PanY);
        Assert.Empty(boxEvents);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromNodeTarget(bool useHeader)
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint world = useHeader
            ? Center(entry.DebugLayout.HeaderBounds)
            : new UiPoint(entry.Bounds.X + entry.Bounds.Width / 2, entry.DebugLayout.HeaderBounds.Bottom + 12);
        DragFrom(context, graph.Canvas.WorldToScreen(world));

        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromPinWirePreview()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out UiNodePin entryThen, out _);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        DragFrom(context, graph.Canvas.WorldToScreen(entryThen.Layout.Center));

        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromCommentEditTarget()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-edit-target",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Editable Comment",
            Text = "Body text",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        DragFrom(context, graph.Canvas.WorldToScreen(Center(comment.DebugLayout.TitleBounds)));

        Assert.True(graph.IsEditingComment);
        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromCommentInterior()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-interior-target",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Non Selection Comment",
            Text = "Dragging from anywhere inside a comment should not begin a node selection box.",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        DragFrom(context, graph.Canvas.WorldToScreen(new UiPoint(comment.Bounds.X + 8, comment.Bounds.Bottom - 8)));

        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromValueFieldDrag()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeControl node = new()
        {
            Id = "literal-value-drag-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");

        DragFrom(context, graph.Canvas.WorldToScreen(Center(layout.ValueBounds)));

        Assert.True(graph.IsEditingValue);
        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionFromHoveredWire()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        graph.Connect(entry, entryThen, print, printIn);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint wireWorld = new(
            (entryThen.Layout.Center.X + printIn.Layout.Center.X) / 2,
            (entryThen.Layout.Center.Y + printIn.Layout.Center.Y) / 2);
        UiPoint wireScreen = graph.Canvas.WorldToScreen(wireWorld);
        context.Update(new UiInputState
        {
            MousePosition = wireScreen,
            ScreenMousePosition = wireScreen
        }, 1f / 60f);

        Assert.NotNull(graph.HoveredWire);

        DragFrom(context, wireScreen);

        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    private static void AssertNoBoxSelectionWhileTextEditing()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);
        List<UiNodeBoxSelectionEvent> events = RecordBoxSelectionEvents(graph);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        UiPoint valueClick = graph.Canvas.WorldToScreen(Center(layout.ValueBounds));
        context.Update(new UiInputState
        {
            MousePosition = valueClick,
            ScreenMousePosition = valueClick,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.IsEditingText);

        DragFrom(context, new UiPoint(20, 20));

        Assert.Empty(events);
        Assert.False(graph.IsBoxSelecting);
        Assert.Null(graph.SelectionMarqueeBounds);
    }

    [Fact]
    public void NodeGraph_CommentBoxesRenderBehindNodesWithDedicatedTextRegions()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        graph.Connect(graph.Nodes[0], graph.Nodes[0].Pins.Single(pin => pin.Id == "then"), graph.Nodes[1], graph.Nodes[1].Pins.Single(pin => pin.Id == "in"));

        UiNodeCommentBox comment = new()
        {
            Id = "comment-startup",
            Bounds = new UiRect(50, 70, 500, 220),
            Title = "Startup Flow",
            Text = "Groups the BeginPlay path and explains the first print.",
            Background = new UiColor(61, 44, 92, 120),
            HeaderBackground = new UiColor(96, 72, 136, 170),
            Border = new UiColor(180, 148, 235, 200)
        };
        graph.AddCommentBox(comment);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.Same(comment, Assert.Single(graph.Comments));
        Assert.True(Contains(comment.DebugLayout.Bounds, comment.DebugLayout.HeaderBounds));
        Assert.True(Contains(comment.DebugLayout.HeaderBounds, comment.DebugLayout.TitleBounds));
        Assert.True(Contains(comment.DebugLayout.Bounds, comment.DebugLayout.BodyBounds));
        Assert.True(Contains(comment.DebugLayout.BodyBounds, comment.DebugLayout.BodyTextBounds));
        Assert.False(Intersects(comment.DebugLayout.TitleBounds, comment.DebugLayout.BodyTextBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        int commentFillIndex = renderer.FillCalls.FindIndex(fill => fill.Color.Equals(comment.Background));
        int nodeFillIndex = renderer.FillCalls.FindIndex(fill => fill.Color.Equals(graph.Nodes[0].Background));
        Assert.True(commentFillIndex >= 0);
        Assert.True(nodeFillIndex >= 0);
        Assert.True(commentFillIndex < nodeFillIndex);
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "Startup Flow");
    }

    [Fact]
    public void NodeGraph_CommentBoxEditingRendersInsideCommentRegions()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeCommentBox comment = new()
        {
            Id = "comment-edit",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Original body",
            Padding = 16,
            IsTitleEditing = true,
            EditingTitleText = "Startup Setup",
            IsBodyEditing = true,
            EditingBodyText = "Edited body"
        };
        graph.AddCommentBox(comment);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(Contains(comment.DebugLayout.HeaderBounds, comment.DebugLayout.TitleBounds));
        Assert.True(Contains(comment.DebugLayout.BodyBounds, comment.DebugLayout.BodyTextBounds));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Contains(renderer.DrawnTexts, text => text.Text == "Startup Setup|");
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "Edited body|");
    }

    [Fact]
    public void NodeGraph_ClickingCommentBodyEditsInlineAndCommitsText()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-body-edit",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Original body",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        UiNodeCommentEditCommittedEvent? committed = null;
        graph.CommentEditCommitted += ev => committed = ev;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint bodyClick = graph.Canvas.WorldToScreen(Center(comment.DebugLayout.BodyTextBounds));

        context.Update(new UiInputState
        {
            MousePosition = bodyClick,
            ScreenMousePosition = bodyClick,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.IsEditingComment);
        Assert.True(graph.IsEditingText);
        Assert.True(comment.IsBodyEditing);
        Assert.False(comment.IsTitleEditing);
        Assert.Same(graph, context.Focus.Focused);
        Assert.True(context.WantTextInput);

        context.Update(new UiInputState
        {
            MousePosition = bodyClick,
            ScreenMousePosition = bodyClick,
            TextInput = "Edited body".ToCharArray()
        }, 1f / 60f);

        Assert.Equal("Edited body", comment.EditingBodyText);

        context.Update(new UiInputState
        {
            KeysPressed = new[] { UiKey.Enter },
            Navigation = new UiNavigationInput { Enter = true }
        }, 1f / 60f);

        Assert.False(graph.IsEditingComment);
        Assert.Equal("Edited body", comment.Text);
        Assert.NotNull(committed);
        Assert.Equal("comment-body-edit", committed.Comment.Id);
        Assert.Equal("text", committed.Key);
        Assert.Equal("Edited body", committed.Text);
    }

    [Fact]
    public void NodeGraph_ClickingCommentTitleEditsInlineAndCommitsText()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-title-edit",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Original body",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        UiNodeCommentEditCommittedEvent? committed = null;
        graph.CommentEditCommitted += ev => committed = ev;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint titleClick = graph.Canvas.WorldToScreen(Center(comment.DebugLayout.TitleBounds));

        context.Update(new UiInputState
        {
            MousePosition = titleClick,
            ScreenMousePosition = titleClick,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.IsEditingComment);
        Assert.True(comment.IsTitleEditing);
        Assert.False(comment.IsBodyEditing);
        Assert.Same(graph, context.Focus.Focused);
        Assert.True(context.WantTextInput);

        context.Update(new UiInputState
        {
            MousePosition = titleClick,
            ScreenMousePosition = titleClick,
            TextInput = "Edited title".ToCharArray()
        }, 1f / 60f);

        Assert.Equal("Edited title", comment.EditingTitleText);

        context.Update(new UiInputState
        {
            KeysPressed = new[] { UiKey.Enter },
            Navigation = new UiNavigationInput { Enter = true }
        }, 1f / 60f);

        Assert.False(graph.IsEditingComment);
        Assert.Equal("Edited title", comment.Title);
        Assert.NotNull(committed);
        Assert.Equal("comment-title-edit", committed.Comment.Id);
        Assert.Equal("title", committed.Key);
        Assert.Equal("Edited title", committed.Text);
    }

    [Fact]
    public void NodeGraph_DoesNotRaiseKeyboardCommandsWhileCommentIsEditing()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-command-edit",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Original body",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        List<UiNodeGraphCommandRequestedEvent> commands = [];
        graph.CommandRequested += commands.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint bodyClick = graph.Canvas.WorldToScreen(Center(comment.DebugLayout.BodyTextBounds));
        context.Update(new UiInputState
        {
            MousePosition = bodyClick,
            ScreenMousePosition = bodyClick,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            KeysPressed = new[] { UiKey.Backspace },
            Navigation = new UiNavigationInput { Backspace = true }
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Z }
        }, 1f / 60f);

        Assert.Empty(commands);
        Assert.True(graph.IsEditingComment);
        Assert.True(comment.IsBodyEditing);
    }

    [Fact]
    public void NodeGraph_CommentBoxWrapsMultilineTextInsideBodyRegion()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeCommentBox comment = new()
        {
            Id = "comment-wrap",
            Bounds = new UiRect(80, 80, 220, 150),
            Title = "Movement System",
            Text = "Use comments to group related Blueprint nodes and explain logic without overlapping nearby pins.",
            Padding = 16
        };
        graph.AddCommentBox(comment);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(comment.DebugLayout.BodyLineBounds.Count > 1);
        Assert.All(comment.DebugLayout.BodyLineBounds, line => Assert.True(Contains(comment.DebugLayout.BodyBounds, line)));
        Assert.True(Contains(comment.DebugLayout.Bounds, comment.DebugLayout.BodyTextBounds));
        Assert.False(Intersects(comment.DebugLayout.HeaderBounds, comment.DebugLayout.BodyTextBounds));
    }

    [Fact]
    public void NodeGraph_DragsCommentBoxFromInteriorAndRaisesEvents()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-drag",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Drag from padding, not text.",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        List<UiNodeCommentDragEvent> started = [];
        List<UiNodeCommentDragEvent> dragged = [];
        List<UiNodeCommentDragEvent> ended = [];
        List<UiNodeCommentDragEvent> cancelled = [];
        graph.CommentDragStarted += started.Add;
        graph.CommentDragged += dragged.Add;
        graph.CommentDragEnded += ended.Add;
        graph.CommentDragCancelled += cancelled.Add;
        List<UiNodeBoxSelectionEvent> boxEvents = RecordBoxSelectionEvents(graph);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint startWorld = new(comment.Bounds.X + 8, comment.Bounds.Bottom - 8);
        UiPoint start = graph.Canvas.WorldToScreen(startWorld);
        UiPoint end = graph.Canvas.WorldToScreen(new UiPoint(startWorld.X + 80, startWorld.Y + 40));

        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.False(graph.IsDraggingComment);
        Assert.False(graph.IsEditingComment);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);

        Assert.True(graph.IsDraggingComment);
        Assert.Equal(new UiRect(160, 120, 420, 180), comment.Bounds);
        UiNodeCommentDragEvent startedEvent = Assert.Single(started);
        Assert.Same(graph, startedEvent.Graph);
        Assert.Same(comment, startedEvent.Comment);
        Assert.Equal(new UiRect(80, 80, 420, 180), startedEvent.StartBounds);
        Assert.Equal(new UiRect(160, 120, 420, 180), startedEvent.CurrentBounds);
        Assert.Equal(new UiPoint(80, 40), startedEvent.Delta);
        Assert.False(startedEvent.IsCompleting);
        Assert.Single(dragged);
        Assert.Empty(boxEvents);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeCommentDragEvent endedEvent = Assert.Single(ended);
        Assert.Equal(new UiRect(160, 120, 420, 180), endedEvent.CurrentBounds);
        Assert.Equal(new UiPoint(80, 40), endedEvent.Delta);
        Assert.True(endedEvent.IsCompleting);
        Assert.False(graph.IsDraggingComment);
        Assert.Empty(cancelled);
    }

    [Fact]
    public void NodeGraph_CommentDragEscapeCancelsAndRestoresBounds()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        UiNodeCommentBox comment = new()
        {
            Id = "comment-drag-cancel",
            Bounds = new UiRect(80, 80, 420, 180),
            Title = "Startup Flow",
            Text = "Drag from padding, then cancel.",
            Padding = 16
        };
        graph.AddCommentBox(comment);
        List<UiNodeCommentDragEvent> cancelled = [];
        graph.CommentDragCancelled += cancelled.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiPoint startWorld = new(comment.Bounds.X + 8, comment.Bounds.Bottom - 8);
        UiPoint start = graph.Canvas.WorldToScreen(startWorld);
        UiPoint end = graph.Canvas.WorldToScreen(new UiPoint(startWorld.X + 80, startWorld.Y + 40));
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);

        Assert.True(graph.IsDraggingComment);
        Assert.Equal(new UiRect(160, 120, 420, 180), comment.Bounds);

        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            KeysPressed = new[] { UiKey.Escape },
            Navigation = new UiNavigationInput { Escape = true }
        }, 1f / 60f);

        UiNodeCommentDragEvent ev = Assert.Single(cancelled);
        Assert.Equal(new UiRect(80, 80, 420, 180), ev.StartBounds);
        Assert.Equal(new UiRect(160, 120, 420, 180), ev.CurrentBounds);
        Assert.Equal(new UiPoint(80, 40), ev.Delta);
        Assert.False(ev.IsCompleting);
        Assert.False(graph.IsDraggingComment);
        Assert.Equal(new UiRect(80, 80, 420, 180), comment.Bounds);
    }

    [Fact]
    public void NodeControl_LongOpposingPinLabelsReserveSeparateTextLanes()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "long-labels",
            AutomationId = "long-labels",
            AutomationName = "Long Label Node",
            AutomationRole = "node",
            Bounds = new UiRect(120, 96, 220, 126),
            Title = "Very Long Function Call Node Title",
            BodyText = "Long pin lane stress",
            Padding = 10,
            PinHitSize = 18,
            PinVisualSize = 10
        };
        node.AddInput("payload", "Extremely Long Input Payload Name", UiNodePinKind.Data);
        node.AddOutput("result", "Very Long Output Result Name", UiNodePinKind.Data);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout input = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "payload");
        UiNodePinLayout output = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "result");

        Assert.False(Intersects(input.LabelBounds, output.LabelBounds));
        Assert.False(Intersects(input.HitBounds, input.LabelBounds));
        Assert.False(Intersects(output.HitBounds, output.LabelBounds));
        Assert.True(input.LabelBounds.X >= node.Bounds.X);
        Assert.True(input.LabelBounds.Right < output.LabelBounds.X);
        Assert.True(output.LabelBounds.Right <= node.Bounds.Right);
    }

    [Fact]
    public void NodeControl_MeasureDesiredSizeExpandsForMeasuredContent()
    {
        UiNodeControl node = new()
        {
            Id = "measured-node",
            Bounds = new UiRect(120, 96, 120, 74),
            Title = "Very Long Blueprint Function Node Title",
            Subtitle = "Target is My Deep Object",
            BodyText = "Measured subtitle body",
            HeaderHeight = 52,
            Padding = 10,
            PinRowHeight = 24,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentWidth = 140,
            MaximumContentWidth = 460
        };
        node.AddInput("payload", "Extremely Long Input Payload Name", UiNodePinKind.Data);
        node.AddOutput("result", "Very Long Output Result Name", UiNodePinKind.Data);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        Assert.True(size.Width > node.Bounds.Width);
        Assert.InRange(size.Width, node.MinimumContentWidth, node.MaximumContentWidth);
        Assert.True(size.Height > node.Bounds.Height);

        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodePinLayout input = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "payload");
        UiNodePinLayout output = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "result");
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.TitleBounds));
        Assert.True(Contains(node.DebugLayout.HeaderBounds, node.DebugLayout.SubtitleBounds));
        Assert.False(Intersects(input.LabelBounds, output.LabelBounds));
        Assert.True(Contains(node.Bounds, input.LabelBounds));
        Assert.True(Contains(node.Bounds, output.LabelBounds));
    }

    [Fact]
    public void NodeControl_CompactModeUsesThinHeaderAndSuppressesExecPinsAndBodyText()
    {
        UiNodeControl node = new()
        {
            Id = "compact-pure",
            Bounds = new UiRect(120, 96, 180, 86),
            Title = "Add",
            Subtitle = "Math",
            BodyText = "Should not render",
            Compact = true,
            HeaderHeight = 24,
            Padding = 8,
            PinRowHeight = 22,
            PinHitSize = 18,
            MinimumContentHeight = 48
        };
        node.AddInput("exec", "In", UiNodePinKind.Exec);
        node.AddInput("left", "Left", UiNodePinKind.Data);
        node.AddInput("right", "Right", UiNodePinKind.Data);
        node.AddOutput("value", "Value", UiNodePinKind.Data);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        Assert.True(size.Height < 112);

        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(node.DebugLayout.HeaderBounds.Height < 30);
        Assert.Equal(default, node.DebugLayout.SubtitleBounds);
        Assert.Equal(default, node.DebugLayout.BodyTextBounds);
        Assert.DoesNotContain(node.DebugLayout.Pins, pin => pin.Pin?.Id == "exec");
        Assert.All(node.DebugLayout.Pins, pin => Assert.Equal(UiNodePinKind.Data, pin.Pin?.Kind));
    }

    [Fact]
    public void NodeControl_DensePinsHideBodyTextInsteadOfOverlappingRows()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "dense",
            AutomationId = "dense",
            AutomationName = "Dense Node",
            AutomationRole = "node",
            Bounds = new UiRect(120, 96, 220, 112),
            Title = "Dense Node",
            BodyText = "This subtitle should not cover pin rows",
            Padding = 10,
            PinRowHeight = 24,
            PinHitSize = 18
        };
        node.AddInput("in", "In", UiNodePinKind.Exec);
        node.AddOutput("then", "Then", UiNodePinKind.Exec);
        node.AddInput("first", "First Value", UiNodePinKind.Data);
        node.AddOutput("valid", "Is Valid", UiNodePinKind.Data);
        node.AddInput("second", "Second Value", UiNodePinKind.Data);
        node.AddOutput("result", "Result Value", UiNodePinKind.Data);
        graph.AddNode(node);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.Equal(default, node.DebugLayout.BodyTextBounds);
        Assert.All(node.DebugLayout.Pins, pin =>
        {
            Assert.False(Intersects(pin.HitBounds, node.DebugLayout.TitleBounds));
        });
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
        Assert.Equal(graph.ExecWireThickness, debug.Thickness);
        Assert.Equal(graph.ExecWireColor, debug.Color);
        Assert.True(debug.Bounds.Width > 0);
        Assert.True(debug.HitBounds.Width > debug.Bounds.Width);
        Assert.True(debug.HitBounds.Height > debug.Bounds.Height);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.NotEmpty(renderer.FilledRects);
        Assert.NotEmpty(renderer.Polylines);
        Assert.Contains(renderer.Polylines, polyline => polyline.Thickness == graph.ExecWireThickness && polyline.Points.Length >= debug.Route.Count);
    }

    [Fact]
    public void NodeGraph_ReusesStaticWireRouteAndTessellation()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        UiContext context = new(graph);

        context.Update(new UiInputState(), 1f / 60f);
        IReadOnlyList<UiPoint> firstRoute = wire.Route;

        context.Update(new UiInputState(), 1f / 60f);
        Assert.Same(firstRoute, wire.Route);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.True(renderer.Polylines.Count >= 2);
        Assert.Equal(graph.WireShadowColor, renderer.Polylines[0].Color);
        Assert.Equal(graph.ExecWireColor, renderer.Polylines[1].Color);

        graph.EnableWireShadows = false;
        RecordingRenderer routeRenderer = new();
        graph.Render(new UiRenderContext(routeRenderer, UiFont.Default));
        graph.Render(new UiRenderContext(routeRenderer, UiFont.Default));

        Assert.True(routeRenderer.Polylines.Count >= 2);
        Assert.Same(routeRenderer.PolylineSources[0], routeRenderer.PolylineSources[1]);

        entry.Bounds = new UiRect(entry.Bounds.X + 24, entry.Bounds.Y, entry.Bounds.Width, entry.Bounds.Height);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.NotSame(firstRoute, wire.Route);
    }

    [Fact]
    public void NodeGraph_RenderRefreshesWireRoutesAfterNodeBoundsChangeBeforeRender()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        graph.EnableWireShadows = false;
        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        UiContext context = new(graph);

        context.Update(new UiInputState(), 1f / 60f);
        graph.Render(new UiRenderContext(new RecordingRenderer(), UiFont.Default));

        print.Bounds = new UiRect(print.Bounds.X + 96, print.Bounds.Y + 48, print.Bounds.Width, print.Bounds.Height);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        var route = Assert.Single(renderer.Polylines).Points;
        Assert.Equal(wire.FromPin.Layout.Center, route[0]);
        Assert.Equal(wire.ToPin.Layout.Center, route[^1]);
        Assert.True(print.Bounds.Contains(route[^1]));
    }

    [Fact]
    public void NodeGraph_RendersWiresInsideOneVectorPass()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodePin dataOut = entry.AddOutput("value", "Value", UiNodePinKind.Data);
        UiNodePin dataIn = print.Pins.Single(pin => pin.Id == "message");
        graph.Connect(entry, entryThen, print, printIn);
        graph.Connect(entry, dataOut, print, dataIn);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Equal(1, renderer.BeginVectorPassCount);
        Assert.Equal(1, renderer.EndVectorPassCount);
        Assert.Equal(4, renderer.Polylines.Count);
        Assert.Equal(2, renderer.Polylines.Count(polyline => polyline.Color.Equals(graph.WireShadowColor)));
    }

    [Fact]
    public void NodeGraph_RendersWireShadowBehindMainRoute()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        graph.Connect(entry, entryThen, print, printIn);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWireDebugLayout debug = Assert.Single(graph.GetWireDebugLayouts());
        Assert.True(debug.HasShadow);
        Assert.Equal(graph.WireShadowColor, debug.ShadowColor);
        Assert.Equal(graph.ExecWireThickness + graph.WireShadowExtraThickness, debug.ShadowThickness);
        Assert.True(debug.ShadowBounds.Right > debug.Bounds.Right);
        Assert.True(debug.ShadowBounds.Bottom > debug.Bounds.Bottom);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.True(renderer.Polylines.Count >= 2);
        Assert.Equal(graph.WireShadowColor, renderer.Polylines[0].Color);
        Assert.Equal(graph.ExecWireThickness + graph.WireShadowExtraThickness, renderer.Polylines[0].Thickness);
        Assert.Equal(graph.ExecWireColor, renderer.Polylines[1].Color);
        Assert.Equal(graph.ExecWireThickness, renderer.Polylines[1].Thickness);
    }

    [Fact]
    public void NodeGraph_RendersRerouteHandleForSelectedWire()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        wire.Selected = true;
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWireDebugLayout debug = Assert.Single(graph.GetWireDebugLayouts());
        UiPoint handleCenter = Assert.Single(debug.RerouteHandleCenters);
        UiRect handleBounds = Assert.Single(debug.RerouteHandleBounds);
        Assert.True(debug.HasRerouteHandles);
        Assert.True(handleBounds.Contains(handleCenter));

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Contains(renderer.FilledCircles, circle =>
            circle.Center.Equals(handleCenter)
            && circle.Radius == graph.RerouteHandleRadius
            && circle.Color.Equals(graph.RerouteHandleFillColor));
        Assert.Contains(renderer.DrawnCircles, circle =>
            circle.Center.Equals(handleCenter)
            && circle.Radius == graph.RerouteHandleRadius
            && circle.Color.Equals(graph.SelectedRerouteHandleBorderColor));
    }

    [Fact]
    public void NodeControl_RendersSelectionGlowOutsideNodeBounds()
    {
        UiNodeGraph graph = CreateGraph(out _, out UiNodeControl print, out _, out _);
        print.Selected = true;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        Assert.True(print.TryGetGlow(out var glowBounds, out var glowColor, out var glowPasses));
        Assert.True(glowBounds.Left < print.Bounds.Left);
        Assert.True(glowBounds.Top < print.Bounds.Top);
        Assert.True(glowBounds.Right > print.Bounds.Right);
        Assert.True(glowBounds.Bottom > print.Bounds.Bottom);
        Assert.Equal(print.SelectedGlowColor, glowColor);
        Assert.Equal(print.GlowPasses, glowPasses);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Contains(renderer.RoundedRects, rounded =>
            rounded.Rect.Left < print.Bounds.Left
            && rounded.Rect.Top < print.Bounds.Top
            && rounded.Rect.Right > print.Bounds.Right
            && rounded.Rect.Bottom > print.Bounds.Bottom
            && rounded.Color.R == print.SelectedGlowColor.R
            && rounded.Color.G == print.SelectedGlowColor.G
            && rounded.Color.B == print.SelectedGlowColor.B
            && rounded.Color.A > 0);
    }

    [Fact]
    public void NodeGraph_RendersSelectionGlowBehindSelectedWireRoute()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        wire.Selected = true;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWireDebugLayout debug = Assert.Single(graph.GetWireDebugLayouts());
        Assert.True(debug.HasGlow);
        Assert.Equal(graph.SelectedWireGlowColor, debug.GlowColor);
        Assert.True(debug.GlowThickness > debug.Thickness);
        Assert.True(debug.GlowBounds.Width >= debug.Bounds.Width);
        Assert.True(debug.GlowBounds.Height >= debug.Bounds.Height);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Equal(graph.WireShadowColor, renderer.Polylines[0].Color);
        Assert.Contains(renderer.Polylines.Skip(1).Take(graph.WireGlowPasses), polyline =>
            polyline.Thickness > debug.Thickness
            && polyline.Color.R == graph.SelectedWireGlowColor.R
            && polyline.Color.G == graph.SelectedWireGlowColor.G
            && polyline.Color.B == graph.SelectedWireGlowColor.B
            && polyline.Color.A > 0);
        Assert.Contains(renderer.Polylines.Skip(1 + graph.WireGlowPasses), polyline =>
            polyline.Thickness == debug.Thickness
            && polyline.Color.Equals(graph.SelectedWireColor));
    }

    [Fact]
    public void NodeGraph_UsesCanonExecAndDataWireStyles()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodePin dataOut = entry.AddOutput("value", "Value", UiNodePinKind.Data);
        dataOut.Color = new UiColor(218, 45, 157);
        UiNodePin dataIn = print.Pins.Single(pin => pin.Id == "message");
        dataIn.Color = new UiColor(218, 45, 157);
        graph.Connect(entry, entryThen, print, printIn);
        graph.Connect(entry, dataOut, print, dataIn);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodeWireDebugLayout[] layouts = graph.GetWireDebugLayouts().ToArray();
        UiNodeWireDebugLayout exec = Assert.Single(layouts, layout => layout.Kind == UiNodePinKind.Exec);
        UiNodeWireDebugLayout data = Assert.Single(layouts, layout => layout.Kind == UiNodePinKind.Data);

        Assert.Equal(UiColor.White, exec.Color);
        Assert.Equal(graph.ExecWireThickness, exec.Thickness);
        Assert.Equal(new UiColor(218, 45, 157), data.Color);
        Assert.Equal(graph.DataWireThickness, data.Thickness);
        Assert.True(exec.Thickness > data.Thickness);
    }

    [Fact]
    public void NodeGraph_UsesExplicitDataWireColorBeforePinFallback()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out _, out _);
        UiNodePin dataOut = entry.AddOutput("value", "Value", UiNodePinKind.Data);
        dataOut.Color = new UiColor(22, 130, 196);
        UiNodePin dataIn = print.Pins.Single(pin => pin.Id == "message");
        dataIn.Color = new UiColor(218, 45, 157);
        var wire = graph.Connect(entry, dataOut, print, dataIn);
        wire.Color = new UiColor(126, 219, 57);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodeWireDebugLayout data = Assert.Single(graph.GetWireDebugLayouts());

        Assert.Equal(new UiColor(126, 219, 57), data.Color);
    }

    [Fact]
    public void NodeControl_RendersWhiteExecWedgesAndTypedCircularDataPins()
    {
        UiNodeGraph graph = CreateGraph(out _, out UiNodeControl print, out _, out _);
        var message = print.Pins.Single(pin => pin.Id == "message");
        message.Color = new UiColor(218, 45, 157);

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        RecordingRenderer renderer = new();
        print.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Contains(renderer.FillCalls, fill => fill.Color.Equals(UiColor.White));
        Assert.Contains(renderer.FilledCircles, fill => fill.Color.Equals(new UiColor(218, 45, 157)));
    }

    [Fact]
    public void NodeControl_RendersPinChromeBeforeText()
    {
        UiNodeGraph graph = CreateGraph(out _, out UiNodeControl print, out _, out _);
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        RecordingRenderer renderer = new();
        print.Render(new UiRenderContext(renderer, UiFont.Default));

        int firstPinChrome = renderer.Operations.FindIndex(static operation =>
            operation is "fill-triangle-right" or "fill-circle" or "draw-circle");
        int firstText = renderer.Operations.FindIndex(static operation => operation == "draw-text");
        Assert.True(firstPinChrome >= 0);
        Assert.True(firstText >= 0);
        Assert.True(firstPinChrome < firstText);
    }

    [Fact]
    public void NodeGraph_TracksPinHoverAndWirePreview()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out UiNodePin entryThen, out _);
        List<UiNodeDragEvent> dragStartedEvents = [];
        List<UiNodeDragEvent> draggedEvents = [];
        List<UiNodeDragEvent> dragEndedEvents = [];
        List<UiNodeSelectionRequestedEvent> selectionRequests = [];
        List<UiNodeWireConnectionRequestedEvent> connectionRequests = [];
        UiNodeSearchRequestedEvent? searchRequested = null;
        graph.NodeDragStarted += dragStartedEvents.Add;
        graph.NodeDragged += draggedEvents.Add;
        graph.NodeDragEnded += dragEndedEvents.Add;
        graph.NodeSelectionRequested += selectionRequests.Add;
        graph.WireConnectionRequested += connectionRequests.Add;
        graph.NodeSearchRequested += ev => searchRequested = ev;
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
        Assert.Empty(dragStartedEvents);
        Assert.Empty(draggedEvents);
        Assert.Empty(selectionRequests);

        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftReleased = true
        }, 1f / 60f);

        Assert.False(graph.PreviewWire.Active);
        Assert.Empty(dragEndedEvents);
        Assert.Empty(connectionRequests);
        Assert.True(graph.IsNodeSearchOpen);
        Assert.NotNull(searchRequested);
        Assert.Same(entryThen, searchRequested.ContextPin);
    }

    [Fact]
    public void NodeGraph_ShowsDebugValuePopupWhenHoveringValuedPin()
    {
        UiNodeGraph graph = CreateGraph(out _, out UiNodeControl print, out _, out _);
        var messagePin = print.Pins.Single(static pin => pin.Id == "message");
        messagePin.DebugValueText = "\"Counter value: 42\"";
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint messageScreen = graph.Canvas.WorldToScreen(messagePin.Layout.Center);
        context.Update(new UiInputState
        {
            MousePosition = messageScreen,
            ScreenMousePosition = messageScreen
        }, 1f / 60f);

        RecordingRenderer renderer = new();
        graph.RenderOverlay(new UiRenderContext(renderer, UiFont.Default));

        Assert.Same(messagePin, graph.DebugPinValuePopupPin);
        Assert.Equal("\"Counter value: 42\"", graph.DebugPinValuePopupText);
        Assert.True(graph.DebugPinValuePopupBounds.Width > 0);
        Assert.Contains(renderer.DrawnTexts, text => text.Text.Contains("Counter value", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeGraph_RaisesConnectionRequestedWhenWireReleasedOnTargetPin()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        List<UiNodeWireConnectionRequestedEvent> connectionRequests = [];
        graph.WireConnectionRequested += connectionRequests.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiPoint outputScreen = graph.Canvas.WorldToScreen(entryThen.Layout.Center);
        context.Update(new UiInputState
        {
            MousePosition = outputScreen,
            ScreenMousePosition = outputScreen,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        UiPoint inputScreen = graph.Canvas.WorldToScreen(printIn.Layout.Center);
        context.Update(new UiInputState
        {
            MousePosition = inputScreen,
            ScreenMousePosition = inputScreen,
            LeftReleased = true
        }, 1f / 60f);

        var request = Assert.Single(connectionRequests);
        Assert.Same(entry, request.StartNode);
        Assert.Same(entryThen, request.StartPin);
        Assert.Same(print, request.TargetNode);
        Assert.Same(printIn, request.TargetPin);
        Assert.True(request.Preview.Active);
        Assert.Same(entryThen, request.Preview.StartPin);
        Assert.False(graph.PreviewWire.Active);
    }

    [Fact]
    public void NodeGraph_RaisesWireRerouteRequestedWhenWireIsDoubleClicked()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out UiNodeControl print, out UiNodePin entryThen, out UiNodePin printIn);
        UiNodeWire wire = graph.Connect(entry, entryThen, print, printIn);
        wire.Selected = true;
        List<UiNodeWireRerouteRequestedEvent> reroutes = [];
        graph.WireRerouteRequested += reroutes.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiNodeWireDebugLayout debug = Assert.Single(graph.GetWireDebugLayouts());
        UiPoint worldPoint = Assert.Single(debug.RerouteHandleCenters);
        UiPoint screenPoint = graph.Canvas.WorldToScreen(worldPoint);
        context.Update(new UiInputState
        {
            MousePosition = screenPoint,
            ScreenMousePosition = screenPoint
        }, 1f / 60f);

        Assert.Same(wire, graph.HoveredWire);

        context.Update(new UiInputState
        {
            MousePosition = screenPoint,
            ScreenMousePosition = screenPoint,
            LeftClicked = true,
            LeftDoubleClicked = true,
            LeftDown = true
        }, 1f / 60f);

        var request = Assert.Single(reroutes);
        Assert.Same(graph, request.Graph);
        Assert.Same(wire, request.Wire);
        Assert.Equal(worldPoint, request.WorldPosition);
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
    public void NodeGraph_RaisesNodeSelectionAndDragEventsWithWorldBounds()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        graph.Zoom = 2f;
        List<UiNodeSelectionRequestedEvent> selectionEvents = [];
        List<UiNodeDragEvent> dragStartedEvents = [];
        List<UiNodeDragEvent> draggedEvents = [];
        List<UiNodeDragEvent> dragEndedEvents = [];
        graph.NodeSelectionRequested += selectionEvents.Add;
        graph.NodeDragStarted += dragStartedEvents.Add;
        graph.NodeDragged += draggedEvents.Add;
        graph.NodeDragEnded += dragEndedEvents.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        UiRect originalBounds = entry.Bounds;
        UiPoint headerWorld = new(entry.DebugLayout.HeaderBounds.X + 12, entry.DebugLayout.HeaderBounds.Y + 8);
        UiPoint headerScreen = graph.Canvas.WorldToScreen(headerWorld);
        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            LeftClicked = true,
            LeftDown = true,
            CtrlDown = true
        }, 1f / 60f);

        Assert.Single(selectionEvents);
        Assert.Same(graph, selectionEvents[0].Graph);
        Assert.Same(entry, selectionEvents[0].Node);
        Assert.Equal(UiModifierKeys.Ctrl, selectionEvents[0].Modifiers);

        UiPoint dragScreen = new(headerScreen.X + 48, headerScreen.Y + 32);
        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftDown = true,
            LeftDragOrigin = headerScreen
        }, 1f / 60f);

        UiNodeDragEvent started = Assert.Single(dragStartedEvents);
        UiNodeDragEvent dragged = Assert.Single(draggedEvents);
        UiRect expectedBounds = new(originalBounds.X + 24, originalBounds.Y + 16, originalBounds.Width, originalBounds.Height);
        Assert.Same(graph, started.Graph);
        Assert.Same(entry, started.Node);
        Assert.Equal(originalBounds, started.StartBounds);
        Assert.Equal(originalBounds, started.CurrentBounds);
        Assert.Equal(new UiPoint(0, 0), started.Delta);
        Assert.Equal(originalBounds, dragged.StartBounds);
        Assert.Equal(expectedBounds, dragged.CurrentBounds);
        Assert.Equal(new UiPoint(24, 16), dragged.Delta);

        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeDragEvent ended = Assert.Single(dragEndedEvents);
        Assert.Equal(originalBounds, ended.StartBounds);
        Assert.Equal(expectedBounds, ended.CurrentBounds);
        Assert.Equal(new UiPoint(24, 16), ended.Delta);

        selectionEvents.Clear();
        entry.Selected = false;
        entry.Selected = true;
        Assert.Empty(selectionEvents);
    }

    [Fact]
    public void NodeGraph_RaisesNodeClickCompletedOnlyForPressReleaseWithoutDrag()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        List<UiNodeSelectionRequestedEvent> selectionEvents = [];
        List<UiNodeClickCompletedEvent> clickEvents = [];
        List<UiNodeDragEvent> dragStartedEvents = [];
        graph.NodeSelectionRequested += selectionEvents.Add;
        graph.NodeClickCompleted += clickEvents.Add;
        graph.NodeDragStarted += dragStartedEvents.Add;

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

        Assert.Single(selectionEvents);
        Assert.Empty(clickEvents);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            LeftReleased = true
        }, 1f / 60f);

        UiNodeClickCompletedEvent click = Assert.Single(clickEvents);
        Assert.Same(graph, click.Graph);
        Assert.Same(entry, click.Node);
        Assert.Equal(UiModifierKeys.None, click.Modifiers);
        Assert.Empty(dragStartedEvents);

        selectionEvents.Clear();
        clickEvents.Clear();

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        UiPoint dragScreen = new(headerScreen.X + 48, headerScreen.Y + 32);
        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftDown = true,
            LeftDragOrigin = headerScreen
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = dragScreen,
            ScreenMousePosition = dragScreen,
            LeftReleased = true
        }, 1f / 60f);

        Assert.Single(selectionEvents);
        Assert.Empty(clickEvents);
        Assert.Single(dragStartedEvents);
    }

    [Fact]
    public void NodeGraph_RaisesKeyboardCommandRequestsWhenGraphFocusIsActive()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        List<UiNodeGraphCommandRequestedEvent> commands = [];
        graph.CommandRequested += commands.Add;

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

        Assert.Same(entry, context.Focus.Focused);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.Delete },
            Navigation = new UiNavigationInput { Delete = true }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Z }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            SuperDown = true,
            ShiftDown = true,
            KeysPressed = new[] { UiKey.Z }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Y }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.Escape },
            Navigation = new UiNavigationInput { Escape = true }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.A }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.C }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.V }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.D }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.C }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.F }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.D0 }
        }, 1f / 60f);

        context.Update(new UiInputState
        {
            MousePosition = headerScreen,
            ScreenMousePosition = headerScreen,
            KeysPressed = new[] { UiKey.G }
        }, 1f / 60f);

        Assert.Equal(
            new[]
            {
                UiNodeGraphCommand.DeleteSelection,
                UiNodeGraphCommand.Undo,
                UiNodeGraphCommand.Redo,
                UiNodeGraphCommand.Redo,
                UiNodeGraphCommand.ClearSelection,
                UiNodeGraphCommand.SelectAll,
                UiNodeGraphCommand.CopySelection,
                UiNodeGraphCommand.PasteClipboard,
                UiNodeGraphCommand.DuplicateSelection,
                UiNodeGraphCommand.CreateCommentAroundSelection,
                UiNodeGraphCommand.FrameSelection,
                UiNodeGraphCommand.ResetZoom,
                UiNodeGraphCommand.ToggleGrid
            },
            commands.Select(command => command.Command).ToArray());
        Assert.Equal(UiModifierKeys.Super | UiModifierKeys.Shift, commands[2].Modifiers);
    }

    [Fact]
    public void NodeGraph_DoesNotRaiseKeyboardCommandRequestsWithoutGraphFocus()
    {
        UiNodeGraph graph = CreateGraph(out _, out _, out _, out _);
        List<UiNodeGraphCommandRequestedEvent> commands = [];
        graph.CommandRequested += commands.Add;

        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        context.Update(new UiInputState
        {
            KeysPressed = new[] { UiKey.Delete },
            Navigation = new UiNavigationInput { Delete = true }
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Z }
        }, 1f / 60f);

        Assert.Empty(commands);
    }

    [Fact]
    public void NodeGraph_DoesNotRaiseKeyboardCommandsWhileValueFieldIsEditing()
    {
        UiNodeGraph graph = new()
        {
            Bounds = new UiRect(0, 0, 700, 420)
        };
        graph.Canvas.Padding = 0;
        graph.Canvas.ShowGrid = false;

        UiNodeControl node = new()
        {
            Id = "literal-node",
            Bounds = new UiRect(120, 96, 220, 72),
            Title = "Integer Literal",
            Compact = true,
            Padding = 8,
            PinHitSize = 18,
            PinVisualSize = 10,
            MinimumContentHeight = 48
        };
        UiNodePin value = node.AddOutput("value", "Value", UiNodePinKind.Data);
        value.ValueText = "41";
        graph.AddNode(node);

        UiSize size = node.MeasureDesiredSize(UiFont.Default);
        node.Bounds = new UiRect(node.Bounds.X, node.Bounds.Y, size.Width, size.Height);

        List<UiNodeGraphCommandRequestedEvent> commands = [];
        graph.CommandRequested += commands.Add;
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);
        UiNodePinLayout layout = Assert.Single(node.DebugLayout.Pins, pin => pin.Pin?.Id == "value");
        UiPoint click = graph.Canvas.WorldToScreen(Center(layout.ValueBounds));

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);

        Assert.True(graph.IsEditingValue);
        Assert.Same(graph, context.Focus.Focused);

        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            KeysPressed = new[] { UiKey.Backspace },
            Navigation = new UiNavigationInput { Backspace = true }
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = click,
            ScreenMousePosition = click,
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Z }
        }, 1f / 60f);

        Assert.Empty(commands);
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

    [Fact]
    public void NodeGraph_ZoomedOutCanvasUsesSmallerFontForNodeText()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        graph.Zoom = 0.55f;
        UiContext context = new(graph);
        context.Update(new UiInputState(), 1f / 60f);

        RecordingRenderer renderer = new();
        graph.Render(new UiRenderContext(renderer, UiFont.Default));

        var entryTitle = Assert.Single(renderer.DrawnTexts, text => text.Text == entry.Title);
        Assert.True(entryTitle.PixelSize < UiFont.Default.PixelSize);
        Assert.True(entryTitle.PixelSize <= graph.Canvas.WorldToScreen(entry.DebugLayout.HeaderBounds).Height);
    }

    [Fact]
    public void NodeGraph_CanvasTextZoomUsesReusableBuckets()
    {
        UiNodeGraph graph = CreateGraph(out UiNodeControl entry, out _, out _, out _);
        UiContext context = new(graph);

        graph.Zoom = 0.55f;
        context.Update(new UiInputState(), 1f / 60f);
        RecordingRenderer firstRenderer = new();
        graph.Render(new UiRenderContext(firstRenderer, UiFont.Default));
        var firstTitle = Assert.Single(firstRenderer.DrawnTexts, text => text.Text == entry.Title);

        graph.Zoom = 0.61f;
        context.Update(new UiInputState(), 1f / 60f);
        RecordingRenderer secondRenderer = new();
        graph.Render(new UiRenderContext(secondRenderer, UiFont.Default));
        var secondTitle = Assert.Single(secondRenderer.DrawnTexts, text => text.Text == entry.Title);

        Assert.Equal(firstTitle.PixelSize, secondTitle.PixelSize);
    }

    private static List<UiNodeBoxSelectionEvent> RecordBoxSelectionEvents(UiNodeGraph graph)
    {
        List<UiNodeBoxSelectionEvent> events = [];
        graph.BoxSelectionStarted += events.Add;
        graph.BoxSelectionUpdated += events.Add;
        graph.BoxSelectionEnded += events.Add;
        graph.BoxSelectionCancelled += events.Add;
        return events;
    }

    private static void DragFrom(UiContext context, UiPoint start)
    {
        UiPoint end = new(start.X + 80, start.Y + 60);
        context.Update(new UiInputState
        {
            MousePosition = start,
            ScreenMousePosition = start,
            LeftClicked = true,
            LeftDown = true
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftDown = true,
            LeftDragOrigin = start
        }, 1f / 60f);
        context.Update(new UiInputState
        {
            MousePosition = end,
            ScreenMousePosition = end,
            LeftReleased = true
        }, 1f / 60f);
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
            Subtitle = "Console / Development",
            HeaderHeight = 52
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

    private static UiPoint Center(UiRect rect)
    {
        return new UiPoint(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }

    private static bool Contains(UiRect outer, UiRect inner)
    {
        if (outer.Width <= 0 || outer.Height <= 0 || inner.Width <= 0 || inner.Height <= 0)
        {
            return false;
        }

        return inner.Left >= outer.Left
            && inner.Top >= outer.Top
            && inner.Right <= outer.Right
            && inner.Bottom <= outer.Bottom;
    }
}
