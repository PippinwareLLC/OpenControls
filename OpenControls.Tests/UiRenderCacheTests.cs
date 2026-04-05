using Xunit;

namespace OpenControls.Tests;

public sealed class UiRenderCacheTests
{
    private sealed class CountingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public int FillRectCalls { get; private set; }
        public int DrawRectCalls { get; private set; }
        public int DrawTextCalls { get; private set; }
        public int PushClipCalls { get; private set; }
        public int PopClipCalls { get; private set; }

        public void FillRect(UiRect rect, UiColor color) => FillRectCalls++;
        public void DrawRect(UiRect rect, UiColor color, int thickness = 1) => DrawRectCalls++;
        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight) => FillRectCalls++;
        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB) => FillRectCalls++;
        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1) => DrawTextCalls++;
        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font) => DrawTextCalls++;
        public int MeasureTextWidth(string text, int scale = 1) => MeasureTextWidth(text, scale, DefaultFont);
        public int MeasureTextWidth(string text, int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextWidth(text, scale);
        public int MeasureTextHeight(int scale = 1) => MeasureTextHeight(scale, DefaultFont);
        public int MeasureTextHeight(int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextHeight(scale);
        public void PushClip(UiRect rect) => PushClipCalls++;
        public void PopClip() => PopClipCalls++;
    }

    private class CountingElement : UiElement
    {
        public int RenderCount { get; private set; }
        public int OverlayRenderCount { get; private set; }

        public override void Render(UiRenderContext context)
        {
            RenderCount++;
            context.Renderer.FillRect(Bounds, UiColor.White);
            base.Render(context);
        }

        public override void RenderOverlay(UiRenderContext context)
        {
            OverlayRenderCount++;
            context.Renderer.DrawRect(Bounds, UiColor.White, 1);
            base.RenderOverlay(context);
        }
    }

    private sealed class VolatileElement : CountingElement
    {
        public override bool IsRenderCacheVolatile(UiContext context) => true;
    }

    [Fact]
    public void RenderCaching_ReplaysRecordedCommandsWithoutReinvokingRender()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Render(renderer);

        Assert.Equal(1, root.RenderCount);
        Assert.Equal(1, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenUiInvalidationVersionChanges()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        root.Bounds = new UiRect(0, 0, 48, 48);
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenInteractionSignatureChanges()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(12, 12),
            ScreenMousePosition = new UiPoint(12, 12)
        });
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
    }

    [Fact]
    public void RenderCaching_BypassesReplayForVolatileElements()
    {
        VolatileElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    private static UiContext CreateContext(UiElement root)
    {
        UiContext context = new(root)
        {
            RenderCachingEnabled = true
        };

        context.Update(new UiInputState());
        return context;
    }
}
