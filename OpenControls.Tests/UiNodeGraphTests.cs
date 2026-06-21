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
        Assert.Contains(renderer.DrawnTexts, text => text.Text == "128|");
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
