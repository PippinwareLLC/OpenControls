using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiColorPickerRenderingTests
{
    private sealed class RecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<(UiRect Rect, UiColor TopLeft, UiColor TopRight, UiColor BottomLeft, UiColor BottomRight)> Gradients { get; } = [];

        public void FillRect(UiRect rect, UiColor color)
        {
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(
            UiRect rect,
            UiColor topLeft,
            UiColor topRight,
            UiColor bottomLeft,
            UiColor bottomRight)
        {
            Gradients.Add((rect, topLeft, topRight, bottomLeft, bottomRight));
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
        }

        public int MeasureTextWidth(string text, int scale = 1) => MeasureTextWidth(text, scale, DefaultFont);

        public int MeasureTextWidth(string text, int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextWidth(text, scale);

        public int MeasureTextHeight(int scale = 1) => MeasureTextHeight(scale, DefaultFont);

        public int MeasureTextHeight(int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextHeight(scale);

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void DefaultSvSurfaceUsesOneSmoothBilinearGradient()
    {
        UiColorPicker picker = CreatePicker();
        RecordingRenderer renderer = new();

        picker.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        var gradient = Assert.Single(renderer.Gradients);
        Assert.Equal(new UiColor(255, 255, 255), gradient.TopLeft);
        Assert.Equal(new UiColor(255, 0, 0), gradient.TopRight);
        Assert.Equal(new UiColor(0, 0, 0), gradient.BottomLeft);
        Assert.Equal(new UiColor(0, 0, 0), gradient.BottomRight);
    }

    [Fact]
    public void ExplicitGridSizePreservesDeliberatelySteppedRendering()
    {
        UiColorPicker picker = CreatePicker();
        picker.GridSize = 12;
        RecordingRenderer renderer = new();

        picker.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        Assert.Empty(renderer.Gradients);
    }

    private static UiColorPicker CreatePicker()
    {
        return new UiColorPicker
        {
            Bounds = new UiRect(0, 0, 180, 140),
            ShowAlpha = false,
            ShowPreview = false,
            ShowInputFields = false,
            GridSize = 0,
            Color = new UiColor(255, 0, 0)
        };
    }
}
