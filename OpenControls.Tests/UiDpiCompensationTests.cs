using Xunit;

namespace OpenControls.Tests;

public sealed class UiDpiCompensationTests
{
    private sealed class RecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public UiRect LastFillRect { get; private set; }
        public UiRect LastDrawRect { get; private set; }
        public int LastThickness { get; private set; }
        public UiRect LastClipRect { get; private set; }
        public UiPoint LastTextPosition { get; private set; }
        public int LastTextScale { get; private set; }
        public UiFont? LastTextFont { get; private set; }
        public UiFont? LastMeasureFont { get; private set; }

        public void FillRect(UiRect rect, UiColor color)
        {
            LastFillRect = rect;
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
            LastDrawRect = rect;
            LastThickness = thickness;
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            LastFillRect = rect;
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            LastFillRect = rect;
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            DrawText(text, position, color, scale, null);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            LastTextPosition = position;
            LastTextScale = scale;
            LastTextFont = font ?? DefaultFont;
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, null);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            LastMeasureFont = font ?? DefaultFont;
            return (font ?? DefaultFont).MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, null);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            LastMeasureFont = font ?? DefaultFont;
            return (font ?? DefaultFont).MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
            LastClipRect = rect;
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void ResolveScaleFactor_Uses96DpiBaselineAndClampsFramebufferScaleToOneByDefault()
    {
        Assert.Equal(2f, UiDpiCompensation.ResolveScaleFactor(1280, 720, 2560, 1440));
        Assert.Equal(1f, UiDpiCompensation.ResolveScaleFactor(1280, 720, 960, 540));
    }

    [Fact]
    public void ScaleFactor_MultipliesFramebufferScaleAndUserScale()
    {
        UiDpiCompensation compensation = new()
        {
            UserScaleFactor = 0.85f
        };

        compensation.SetScaleFromContentSize(1280, 720, 2560, 1440);
        Assert.Equal(1.7f, compensation.ScaleFactor, 3);

        compensation.SetScaleFromContentSize(1280, 720, 1280, 720);
        Assert.Equal(0.85f, compensation.ScaleFactor, 3);
    }

    [Fact]
    public void ToLogical_InputStateScalesPointerAndDragOrigins()
    {
        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(2f);

        UiInputState logical = compensation.ToLogical(new UiInputState
        {
            MousePosition = new UiPoint(240, 120),
            ScreenMousePosition = new UiPoint(240, 120),
            LeftDragOrigin = new UiPoint(200, 100),
            RightDragOrigin = new UiPoint(160, 80),
            MiddleDragOrigin = new UiPoint(80, 40),
            DragThreshold = 6
        });

        Assert.Equal(new UiPoint(120, 60), logical.MousePosition);
        Assert.Equal(new UiPoint(120, 60), logical.ScreenMousePosition);
        Assert.Equal(new UiPoint(100, 50), logical.LeftDragOrigin);
        Assert.Equal(new UiPoint(80, 40), logical.RightDragOrigin);
        Assert.Equal(new UiPoint(40, 20), logical.MiddleDragOrigin);
        Assert.Equal(6, logical.DragThreshold);
    }

    [Fact]
    public void ToPhysical_TextInputRequestScalesBounds()
    {
        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(1.5f);

        UiTextInputRequest physical = compensation.ToPhysical(new UiTextInputRequest(
            new UiRect(10, 20, 120, 24),
            isMultiLine: true,
            caretBounds: new UiRect(50, 20, 2, 24),
            candidateBounds: new UiRect(50, 20, 60, 24)));

        Assert.Equal(new UiRect(15, 30, 180, 36), physical.Bounds);
        Assert.Equal(new UiRect(75, 30, 3, 36), physical.CaretBounds);
        Assert.Equal(new UiRect(75, 30, 90, 36), physical.CandidateBounds);
        Assert.True(physical.IsMultiLine);
    }

    [Fact]
    public void ToPhysical_RectUsesRoundedAnchorAndCeiledExtentAtFractionalScale()
    {
        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(0.8f);

        UiRect physical = compensation.ToPhysical(new UiRect(2, 3, 1, 1));

        Assert.Equal(new UiRect(2, 2, 1, 1), physical);
    }

    [Fact]
    public void ToPhysical_RectPreservesThinExtentsAtFractionalScale()
    {
        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(0.8f);

        for (int x = 0; x < 24; x++)
        {
            UiRect verticalLine = compensation.ToPhysical(new UiRect(x, 0, 1, 12));
            Assert.True(verticalLine.Width >= 1, $"Expected vertical line at logical x={x} to preserve at least one physical pixel.");
        }

        for (int y = 0; y < 24; y++)
        {
            UiRect horizontalLine = compensation.ToPhysical(new UiRect(0, y, 12, 1));
            Assert.True(horizontalLine.Height >= 1, $"Expected horizontal line at logical y={y} to preserve at least one physical pixel.");
        }
    }

    [Fact]
    public void ScaledRenderer_ScalesGeometryButPreservesLogicalMeasurements()
    {
        RecordingRenderer inner = new();
        UiFont baseFont = UiFont.FromTinyBitmap(new TinyBitmapFont(), "TinyBitmap-Test");
        inner.DefaultFont = baseFont;

        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(2f);

        UiScaledRenderer renderer = new(inner, compensation)
        {
            DefaultFont = baseFont
        };

        int logicalHeight = renderer.MeasureTextHeight(scale: 2, font: baseFont);
        int logicalWidth = renderer.MeasureTextWidth("OpenControls", scale: 2, font: baseFont);
        renderer.DrawRect(new UiRect(10, 8, 30, 12), UiColor.White, thickness: 1);
        renderer.DrawText("Scaled", new UiPoint(12, 18), UiColor.White, scale: 2, font: baseFont);
        renderer.PushClip(new UiRect(4, 6, 20, 10));

        Assert.Equal(baseFont.MeasureTextHeight(2), logicalHeight);
        Assert.Equal(baseFont.MeasureTextWidth("OpenControls", 2), logicalWidth);
        Assert.Equal(new UiRect(20, 16, 60, 24), inner.LastDrawRect);
        Assert.Equal(2, inner.LastThickness);
        Assert.Equal(new UiPoint(24, 36), inner.LastTextPosition);
        Assert.Equal(2, inner.LastTextScale);
        Assert.NotNull(inner.LastTextFont);
        Assert.NotNull(inner.LastMeasureFont);
        Assert.Equal(baseFont.PixelSize * 2, inner.LastTextFont!.PixelSize);
        Assert.Equal(baseFont.PixelSize * 2, inner.LastMeasureFont!.PixelSize);
        Assert.Equal(new UiRect(8, 12, 40, 20), inner.LastClipRect);
    }

    [Fact]
    public void ScaledRenderer_PreservesThinGeometryAtFractionalScale()
    {
        RecordingRenderer inner = new();
        UiDpiCompensation compensation = new();
        compensation.SetScaleFactor(0.8f);

        UiScaledRenderer renderer = new(inner, compensation);

        renderer.FillRect(new UiRect(2, 3, 1, 1), UiColor.White);
        renderer.PushClip(new UiRect(7, 11, 1, 1));

        Assert.Equal(new UiRect(2, 2, 1, 1), inner.LastFillRect);
        Assert.Equal(new UiRect(6, 9, 1, 1), inner.LastClipRect);
    }
}
