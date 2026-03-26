using OpenTK.Graphics.OpenGL;
using OpenControls;

namespace OpenControls.OpenGL;

public sealed class OpenGLUiRenderer : IUiRenderer
{
    private readonly Stack<UiRect> _clipStack = new();
    private bool _scissorEnabled;

    public OpenGLUiRenderer(UiFont? defaultFont = null)
    {
        DefaultFont = defaultFont ?? UiFont.Default;
    }

    public OpenGLUiRenderer(TinyBitmapFont font)
        : this(UiFont.FromTinyBitmap(font))
    {
    }

    public UiFont DefaultFont { get; set; }

    public TinyFontCodePage CodePage
    {
        get
        {
            return DefaultFont.TryGetBitmapFont(out TinyBitmapFont? font) && font != null
                ? font.CodePage
                : TinyFontCodePage.Latin1;
        }
        set
        {
            if (DefaultFont.TryGetBitmapFont(out TinyBitmapFont? font) && font != null)
            {
                font.CodePage = value;
            }
        }
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        SetColor(color);
        BeginQuads();
        EmitQuad(rect.X, rect.Y, rect.Width, rect.Height);
        End();
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0)
        {
            return;
        }

        int t = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        SetColor(color);
        BeginQuads();
        EmitQuad(rect.X, rect.Y, rect.Width, t);
        EmitQuad(rect.X, rect.Bottom - t, rect.Width, t);

        int middleHeight = rect.Height - t * 2;
        if (middleHeight > 0)
        {
            EmitQuad(rect.X, rect.Y + t, t, middleHeight);
            EmitQuad(rect.Right - t, rect.Y + t, t, middleHeight);
        }

        End();
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        UiRenderHelpers.FillRectGradient(this, rect, topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        UiRenderHelpers.FillRectCheckerboard(this, rect, cellSize, colorA, colorB);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        DrawText(text, position, color, scale, null);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        UiFont activeFont = font ?? DefaultFont;
        UiTextLayout layout = activeFont.LayoutText(text, scale);
        if (layout.Glyphs.Count == 0)
        {
            return;
        }

        BeginQuads();
        for (int i = 0; i < layout.Glyphs.Count; i++)
        {
            UiPositionedGlyph glyph = layout.Glyphs[i];
            EmitGlyph(glyph.Glyph, position.X + glyph.X, position.Y + glyph.Y, color);
        }
        End();
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return MeasureTextWidth(text, scale, null);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextWidth(text, scale);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return MeasureTextHeight(scale, null);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextHeight(scale);
    }

    public void PushClip(UiRect rect)
    {
        UiRect clip = rect;
        UiRect viewport = GetViewportRect();
        clip = Intersect(clip, viewport);

        if (_clipStack.Count > 0)
        {
            clip = Intersect(clip, _clipStack.Peek());
        }

        _clipStack.Push(clip);
        ApplyClip(clip, viewport.Height);
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _clipStack.Pop();
        if (_clipStack.Count == 0)
        {
            if (_scissorEnabled)
            {
                GL.Disable(EnableCap.ScissorTest);
                _scissorEnabled = false;
            }

            return;
        }

        UiRect viewport = GetViewportRect();
        ApplyClip(_clipStack.Peek(), viewport.Height);
    }

    private static void SetColor(UiColor color)
    {
        GL.Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    private static void BeginQuads()
    {
        GL.Begin(PrimitiveType.Quads);
    }

    private static void End()
    {
        GL.End();
    }

    private void EmitGlyph(UiRasterizedGlyph glyph, int x, int y, UiColor color)
    {
        for (int row = 0; row < glyph.Height; row++)
        {
            int rowStart = row * glyph.Width;
            for (int col = 0; col < glyph.Width; col++)
            {
                byte alpha = glyph.Alpha[rowStart + col];
                if (alpha == 0)
                {
                    continue;
                }

                SetColor(ApplyAlpha(color, alpha));
                EmitQuad(x + col, y + row, 1, 1);
            }
        }
    }

    private static UiColor ApplyAlpha(UiColor color, byte alpha)
    {
        int scaledAlpha = color.A * alpha / 255;
        return new UiColor(color.R, color.G, color.B, (byte)scaledAlpha);
    }

    private static void EmitQuad(int x, int y, int width, int height)
    {
        float left = x;
        float top = y;
        float right = x + width;
        float bottom = y + height;

        GL.Vertex2(left, top);
        GL.Vertex2(right, top);
        GL.Vertex2(right, bottom);
        GL.Vertex2(left, bottom);
    }

    private static UiRect Intersect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.X, b.X);
        int top = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        if (right <= left || bottom <= top)
        {
            return new UiRect(left, top, 0, 0);
        }

        return new UiRect(left, top, right - left, bottom - top);
    }

    private static UiRect GetViewportRect()
    {
        int[] viewport = new int[4];
        GL.GetInteger(GetPName.Viewport, viewport);
        return new UiRect(0, 0, viewport[2], viewport[3]);
    }

    private void ApplyClip(UiRect rect, int viewportHeight)
    {
        int width = Math.Max(0, rect.Width);
        int height = Math.Max(0, rect.Height);

        if (!_scissorEnabled)
        {
            GL.Enable(EnableCap.ScissorTest);
            _scissorEnabled = true;
        }

        // OpenGL scissor uses a bottom-left origin; convert from UI top-left coordinates.
        int y = viewportHeight - (rect.Y + height);
        GL.Scissor(rect.X, y, width, height);
    }
}
