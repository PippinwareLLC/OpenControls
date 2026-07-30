namespace OpenControls;

internal class UiOffsetRenderer : IUiRenderer
{
    private readonly IUiRenderer _inner;
    private readonly UiPoint _offset;

    public UiOffsetRenderer(IUiRenderer inner, UiPoint offset)
    {
        _inner = inner;
        _offset = offset;
    }

    internal static IUiRenderer Create(
        IUiRenderer inner,
        UiPoint offset) =>
        inner switch
        {
            IUiTextureRenderer textureRenderer
                when inner is
                    IUiTextureSamplingRenderer
                        samplingRenderer =>
                new TextureSamplingOffsetRenderer(
                    inner,
                    offset,
                    textureRenderer,
                    samplingRenderer),
            IUiTextureRenderer textureRenderer =>
                new TextureOffsetRenderer(
                    inner,
                    offset,
                    textureRenderer),
            _ => new UiOffsetRenderer(
                inner,
                offset)
        };

    public UiFont DefaultFont
    {
        get => _inner.DefaultFont;
        set => _inner.DefaultFont = value;
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        _inner.FillRect(Offset(rect), color);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        _inner.DrawRect(Offset(rect), color, thickness);
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        _inner.FillRectGradient(Offset(rect), topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        _inner.FillRectCheckerboard(Offset(rect), cellSize, colorA, colorB);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        _inner.DrawText(text, Offset(position), color, scale);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        _inner.DrawText(text, Offset(position), color, scale, font);
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return _inner.MeasureTextWidth(text, scale);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return _inner.MeasureTextWidth(text, scale, font);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return _inner.MeasureTextHeight(scale);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return _inner.MeasureTextHeight(scale, font);
    }

    public void PushClip(UiRect rect)
    {
        _inner.PushClip(Offset(rect));
    }

    public void PopClip()
    {
        _inner.PopClip();
    }

    protected UiRect Offset(UiRect rect)
    {
        return new UiRect(rect.X + _offset.X, rect.Y + _offset.Y, rect.Width, rect.Height);
    }

    private UiPoint Offset(UiPoint point)
    {
        return new UiPoint(point.X + _offset.X, point.Y + _offset.Y);
    }

    private class TextureOffsetRenderer
        : UiOffsetRenderer,
          IUiTextureRenderer,
          IUiTextureRendererResourceOwner
    {
        private readonly IUiTextureRenderer
            _textures;

        internal TextureOffsetRenderer(
            IUiRenderer inner,
            UiPoint offset,
            IUiTextureRenderer textures)
            : base(inner, offset)
        {
            _textures = textures;
            TextureRendererResourceOwner =
                inner is
                    IUiTextureRendererResourceOwner
                        resourceOwner
                    ? resourceOwner
                        .TextureRendererResourceOwner
                    : textures;
        }

        public IUiTextureRenderer
            TextureRendererResourceOwner
        {
            get;
        }

        public uint CreateRgbaTexture(
            int width,
            int height,
            ReadOnlySpan<byte> rgbaPixels) =>
            _textures.CreateRgbaTexture(
                width,
                height,
                rgbaPixels);

        public void UpdateRgbaTexture(
            uint textureId,
            int width,
            int height,
            ReadOnlySpan<byte> rgbaPixels) =>
            _textures.UpdateRgbaTexture(
                textureId,
                width,
                height,
                rgbaPixels);

        public void DrawTexture(
            uint textureId,
            UiRect rect,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight,
            bool flipVertical = false,
            UiColor? tint = null) =>
            _textures.DrawTexture(
                textureId,
                Offset(rect),
                sourceX,
                sourceY,
                sourceWidth,
                sourceHeight,
                flipVertical,
                tint);
    }

    private sealed class
        TextureSamplingOffsetRenderer
        : TextureOffsetRenderer,
          IUiTextureSamplingRenderer
    {
        private readonly
            IUiTextureSamplingRenderer
            _sampling;

        internal
            TextureSamplingOffsetRenderer(
                IUiRenderer inner,
                UiPoint offset,
                IUiTextureRenderer textures,
                IUiTextureSamplingRenderer
                    sampling)
            : base(
                inner,
                offset,
                textures)
        {
            _sampling = sampling;
        }

        public void SetTextureSampling(
            uint textureId,
            UiTextureSampling sampling) =>
            _sampling.SetTextureSampling(
                textureId,
                sampling);
    }
}
