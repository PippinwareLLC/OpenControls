using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class
    UiTextureCapabilityForwardingTests
{
    private sealed class TextureProbe
        : UiElement
    {
        internal IUiTextureRenderer?
            ResourceOwner { get; private set; }

        public override void Render(
            UiRenderContext context)
        {
            IUiTextureRenderer textures =
                Assert.IsAssignableFrom<
                    IUiTextureRenderer>(
                    context.Renderer);
            var owner =
                Assert.IsAssignableFrom<
                    IUiTextureRendererResourceOwner>(
                    context.Renderer);
            var sampling =
                Assert.IsAssignableFrom<
                    IUiTextureSamplingRenderer>(
                    context.Renderer);
            ResourceOwner =
                owner.TextureRendererResourceOwner;
            uint textureId =
                textures.CreateRgbaTexture(
                    1,
                    1,
                    new byte[]
                    {
                        255,
                        255,
                        255,
                        255
                    });
            sampling.SetTextureSampling(
                textureId,
                UiTextureSampling.Nearest);
            textures.DrawTexture(
                textureId,
                Bounds,
                0,
                0,
                1,
                1);
        }
    }

    private sealed class RecordingRenderer
        : IUiRenderer,
          IUiTextureRenderer,
          IUiTextureSamplingRenderer
    {
        private uint _nextTextureId = 1;

        public UiFont DefaultFont
        {
            get;
            set;
        } = UiFont.Default;

        internal List<UiRect>
            DrawnTextures { get; } = [];

        internal List<UiTextureSampling>
            Sampling { get; } = [];

        public void FillRect(
            UiRect rect,
            UiColor color)
        {
        }

        public void DrawRect(
            UiRect rect,
            UiColor color,
            int thickness = 1)
        {
        }

        public void FillRectGradient(
            UiRect rect,
            UiColor topLeft,
            UiColor topRight,
            UiColor bottomLeft,
            UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(
            UiRect rect,
            int cellSize,
            UiColor colorA,
            UiColor colorB)
        {
        }

        public void DrawText(
            string text,
            UiPoint position,
            UiColor color,
            int scale = 1)
        {
        }

        public void DrawText(
            string text,
            UiPoint position,
            UiColor color,
            int scale,
            UiFont? font)
        {
        }

        public int MeasureTextWidth(
            string text,
            int scale = 1) =>
            DefaultFont.MeasureTextWidth(
                text,
                scale);

        public int MeasureTextWidth(
            string text,
            int scale,
            UiFont? font) =>
            (font ?? DefaultFont)
            .MeasureTextWidth(
                text,
                scale);

        public int MeasureTextHeight(
            int scale = 1) =>
            DefaultFont
                .MeasureTextHeight(scale);

        public int MeasureTextHeight(
            int scale,
            UiFont? font) =>
            (font ?? DefaultFont)
            .MeasureTextHeight(scale);

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }

        public uint CreateRgbaTexture(
            int width,
            int height,
            ReadOnlySpan<byte> rgbaPixels) =>
            _nextTextureId++;

        public void UpdateRgbaTexture(
            uint textureId,
            int width,
            int height,
            ReadOnlySpan<byte> rgbaPixels)
        {
        }

        public void DrawTexture(
            uint textureId,
            UiRect rect,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight,
            bool flipVertical = false,
            UiColor? tint = null) =>
            DrawnTextures.Add(rect);

        public void SetTextureSampling(
            uint textureId,
            UiTextureSampling sampling) =>
            Sampling.Add(sampling);
    }

    [Fact]
    public void Canvas_ForwardsTextureSamplingAndStableResourceOwner()
    {
        var renderer =
            new RecordingRenderer();
        var probe =
            new TextureProbe
            {
                Bounds =
                    new UiRect(
                        2,
                        3,
                        8,
                        9)
            };
        var canvas =
            new UiCanvas
            {
                Bounds =
                    new UiRect(
                        10,
                        20,
                        100,
                        80),
                Padding = 0,
                ShowGrid = false,
                ShowOrigin = false,
                Background =
                    UiColor.Transparent,
                Border =
                    UiColor.Transparent
            };
        canvas.AddChild(probe);

        canvas.Render(
            new UiRenderContext(
                renderer,
                UiFont.Default));

        Assert.Same(
            renderer,
            probe.ResourceOwner);
        Assert.Contains(
            UiTextureSampling.Nearest,
            renderer.Sampling);
        Assert.Contains(
            new UiRect(
                12,
                23,
                8,
                9),
            renderer.DrawnTextures);
    }

    [Fact]
    public void DpiFactory_PreservesTextureCapabilitiesAndScalesDrawBounds()
    {
        var renderer = new RecordingRenderer();
        var dpi = new UiDpiCompensation();
        dpi.SetScaleFactor(2f);

        IUiRenderer scaled =
            UiScaledRenderer.Create(
                renderer,
                dpi);
        var textures =
            Assert.IsAssignableFrom<IUiTextureRenderer>(
                scaled);
        var owner =
            Assert.IsAssignableFrom<IUiTextureRendererResourceOwner>(
                scaled);
        var sampling =
            Assert.IsAssignableFrom<IUiTextureSamplingRenderer>(
                scaled);

        uint textureId =
            textures.CreateRgbaTexture(
                1,
                1,
                new byte[] { 255, 255, 255, 255 });
        sampling.SetTextureSampling(
            textureId,
            UiTextureSampling.Nearest);
        textures.DrawTexture(
            textureId,
            new UiRect(2, 3, 8, 9),
            0,
            0,
            1,
            1);

        Assert.Same(
            renderer,
            owner.TextureRendererResourceOwner);
        Assert.Contains(
            UiTextureSampling.Nearest,
            renderer.Sampling);
        Assert.Contains(
            new UiRect(4, 6, 16, 18),
            renderer.DrawnTextures);
    }
}
