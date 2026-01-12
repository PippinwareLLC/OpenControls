namespace OpenControls;

public sealed class SoftwareUiRenderer : IUiRenderer
{
    private readonly TinyBitmapFont _font;
    private readonly Stack<UiRect> _clipStack = new();
    private UiRect _currentClip;

    public SoftwareUiRenderer(int width, int height, TinyBitmapFont? font = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        Buffer = new byte[width * height * 4];
        _font = font ?? new TinyBitmapFont();
        _currentClip = new UiRect(0, 0, width, height);
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Buffer { get; }

    public TinyFontCodePage CodePage
    {
        get => _font.CodePage;
        set => _font.CodePage = value;
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        UiRect clipped = Intersect(rect, _currentClip);
        if (clipped.Width <= 0 || clipped.Height <= 0)
        {
            return;
        }

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                BlendPixel(x, y, color);
            }
        }
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (thickness <= 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        FillRect(new UiRect(rect.X, rect.Y, rect.Width, thickness), color);
        FillRect(new UiRect(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(new UiRect(rect.X, rect.Y, thickness, rect.Height), color);
        FillRect(new UiRect(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        UiRect clipped = Intersect(rect, _currentClip);
        if (clipped.Width <= 0 || clipped.Height <= 0)
        {
            return;
        }

        int lastRow = Math.Max(0, rect.Height - 1);
        int lastCol = Math.Max(0, rect.Width - 1);

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            int localY = y - rect.Top;
            float ty = lastRow == 0 ? 0f : localY / (float)lastRow;
            UiColor left = Lerp(topLeft, bottomLeft, ty);
            UiColor right = Lerp(topRight, bottomRight, ty);

            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                int localX = x - rect.Left;
                float tx = lastCol == 0 ? 0f : localX / (float)lastCol;
                UiColor color = Lerp(left, right, tx);
                BlendPixel(x, y, color);
            }
        }
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        UiRect clipped = Intersect(rect, _currentClip);
        if (clipped.Width <= 0 || clipped.Height <= 0)
        {
            return;
        }

        int size = Math.Max(1, cellSize);

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            int rowCell = (y - rect.Top) / size;
            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                int colCell = (x - rect.Left) / size;
                UiColor color = ((rowCell + colCell) & 1) == 0 ? colorA : colorB;
                BlendPixel(x, y, color);
            }
        }
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int safeScale = Math.Max(1, scale);
        byte[] bytes = _font.GetEncoding().GetBytes(text);
        int cursorX = position.X;
        int cursorY = position.Y;

        foreach (byte code in bytes)
        {
            DrawGlyph(_font.GetGlyph(code), cursorX, cursorY, color, safeScale);
            cursorX += (TinyBitmapFont.GlyphWidth + TinyBitmapFont.GlyphSpacing) * safeScale;
        }
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return _font.MeasureWidth(text, Math.Max(1, scale));
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return _font.MeasureHeight(Math.Max(1, scale));
    }

    public void PushClip(UiRect rect)
    {
        UiRect clip = Intersect(rect, new UiRect(0, 0, Width, Height));
        if (_clipStack.Count > 0)
        {
            clip = Intersect(_clipStack.Peek(), clip);
        }

        _clipStack.Push(clip);
        _currentClip = clip;
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _clipStack.Pop();
        _currentClip = _clipStack.Count > 0 ? _clipStack.Peek() : new UiRect(0, 0, Width, Height);
    }

    private void DrawGlyph(byte[] glyph, int x, int y, UiColor color, int scale)
    {
        for (int row = 0; row < TinyBitmapFont.GlyphHeight; row++)
        {
            byte rowBits = glyph[row];
            for (int col = 0; col < TinyBitmapFont.GlyphWidth; col++)
            {
                int mask = 1 << (TinyBitmapFont.GlyphWidth - 1 - col);
                if ((rowBits & mask) == 0)
                {
                    continue;
                }

                if (scale == 1)
                {
                    BlendPixel(x + col, y + row, color);
                }
                else
                {
                    FillRect(new UiRect(x + col * scale, y + row * scale, scale, scale), color);
                }
            }
        }
    }

    private void BlendPixel(int x, int y, UiColor color)
    {
        if (x < _currentClip.Left || x >= _currentClip.Right || y < _currentClip.Top || y >= _currentClip.Bottom)
        {
            return;
        }

        int index = (y * Width + x) * 4;
        byte srcA = color.A;
        if (srcA == 0)
        {
            return;
        }

        if (srcA == 255)
        {
            Buffer[index] = color.R;
            Buffer[index + 1] = color.G;
            Buffer[index + 2] = color.B;
            Buffer[index + 3] = 255;
            return;
        }

        int invA = 255 - srcA;
        Buffer[index] = (byte)((color.R * srcA + Buffer[index] * invA) / 255);
        Buffer[index + 1] = (byte)((color.G * srcA + Buffer[index + 1] * invA) / 255);
        Buffer[index + 2] = (byte)((color.B * srcA + Buffer[index + 2] * invA) / 255);
        Buffer[index + 3] = (byte)(srcA + (Buffer[index + 3] * invA) / 255);
    }

    private static UiRect Intersect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        if (right <= left || bottom <= top)
        {
            return new UiRect(left, top, 0, 0);
        }

        return new UiRect(left, top, right - left, bottom - top);
    }

    private static UiColor Lerp(UiColor a, UiColor b, float t)
    {
        byte r = (byte)Math.Clamp(a.R + (b.R - a.R) * t, 0f, 255f);
        byte g = (byte)Math.Clamp(a.G + (b.G - a.G) * t, 0f, 255f);
        byte bChannel = (byte)Math.Clamp(a.B + (b.B - a.B) * t, 0f, 255f);
        byte aChannel = (byte)Math.Clamp(a.A + (b.A - a.A) * t, 0f, 255f);
        return new UiColor(r, g, bChannel, aChannel);
    }
}
