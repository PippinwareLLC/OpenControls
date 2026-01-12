namespace OpenControls;

public sealed class HeadlessUiRenderer : IUiRenderer
{
    private readonly TinyBitmapFont _font;
    private readonly Stack<UiRect> _clipStack = new();
    private UiRect _clipRect;

    public HeadlessUiRenderer(int width, int height, TinyBitmapFont? font = null)
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
        _font = font ?? new TinyBitmapFont();
        Pixels = new byte[width * height * 4];
        _clipRect = new UiRect(0, 0, width, height);
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public TinyFontCodePage CodePage
    {
        get => _font.CodePage;
        set => _font.CodePage = value;
    }

    public void Clear(UiColor color)
    {
        for (int y = 0; y < Height; y++)
        {
            int rowStart = (y * Width) * 4;
            for (int x = 0; x < Width; x++)
            {
                int index = rowStart + x * 4;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
                Pixels[index + 3] = color.A;
            }
        }
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        if (!TryGetClippedRect(rect, out UiRect clipped))
        {
            return;
        }

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            int rowStart = (y * Width) * 4;
            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                int index = rowStart + x * 4;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
                Pixels[index + 3] = color.A;
            }
        }
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (thickness <= 0)
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
        if (!TryGetClippedRect(rect, out UiRect clipped))
        {
            return;
        }

        int lastRow = Math.Max(0, rect.Height - 1);
        int lastCol = Math.Max(0, rect.Width - 1);

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            float ty = lastRow == 0 ? 0f : (y - rect.Y) / (float)lastRow;
            UiColor left = Lerp(topLeft, bottomLeft, ty);
            UiColor right = Lerp(topRight, bottomRight, ty);
            int rowStart = (y * Width) * 4;

            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                float tx = lastCol == 0 ? 0f : (x - rect.X) / (float)lastCol;
                UiColor color = Lerp(left, right, tx);
                int index = rowStart + x * 4;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
                Pixels[index + 3] = color.A;
            }
        }
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        if (!TryGetClippedRect(rect, out UiRect clipped))
        {
            return;
        }

        int size = Math.Max(1, cellSize);

        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            int rowIndex = (y - rect.Y) / size;
            int rowStart = (y * Width) * 4;

            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                int colIndex = (x - rect.X) / size;
                UiColor color = (rowIndex + colIndex) % 2 == 0 ? colorA : colorB;
                int index = rowStart + x * 4;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
                Pixels[index + 3] = color.A;
            }
        }
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (scale <= 0)
        {
            return;
        }

        byte[] bytes = _font.GetEncoding().GetBytes(text);
        int cursorX = position.X;
        int cursorY = position.Y;

        foreach (byte code in bytes)
        {
            DrawGlyph(_font.GetGlyph(code), cursorX, cursorY, color, scale);
            cursorX += (TinyBitmapFont.GlyphWidth + TinyBitmapFont.GlyphSpacing) * scale;
        }
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return _font.MeasureWidth(text, scale);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return _font.MeasureHeight(scale);
    }

    public void PushClip(UiRect rect)
    {
        UiRect clip = new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));
        clip = Intersect(clip, new UiRect(0, 0, Width, Height));
        if (_clipStack.Count > 0)
        {
            clip = Intersect(_clipStack.Peek(), clip);
        }

        _clipStack.Push(clip);
        _clipRect = clip;
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _clipStack.Pop();
        _clipRect = _clipStack.Count > 0 ? _clipStack.Peek() : new UiRect(0, 0, Width, Height);
    }

    private void DrawGlyph(byte[] rows, int x, int y, UiColor color, int scale)
    {
        for (int row = 0; row < TinyBitmapFont.GlyphHeight; row++)
        {
            byte bits = rows[row];
            for (int col = 0; col < TinyBitmapFont.GlyphWidth; col++)
            {
                int mask = 1 << (TinyBitmapFont.GlyphWidth - 1 - col);
                if ((bits & mask) != 0)
                {
                    FillRect(new UiRect(
                        x + col * scale,
                        y + row * scale,
                        scale,
                        scale),
                        color);
                }
            }
        }
    }

    private bool TryGetClippedRect(UiRect rect, out UiRect clipped)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            clipped = default;
            return false;
        }

        UiRect normalized = new(rect.X, rect.Y, rect.Width, rect.Height);
        clipped = Intersect(normalized, _clipRect);
        return clipped.Width > 0 && clipped.Height > 0;
    }

    private static UiRect Intersect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        int width = Math.Max(0, right - left);
        int height = Math.Max(0, bottom - top);
        return new UiRect(left, top, width, height);
    }

    private static UiColor Lerp(UiColor a, UiColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new UiColor(
            LerpByte(a.R, b.R, t),
            LerpByte(a.G, b.G, t),
            LerpByte(a.B, b.B, t),
            LerpByte(a.A, b.A, t));
    }

    private static byte LerpByte(byte a, byte b, float t)
    {
        return (byte)Math.Round(a + (b - a) * t);
    }
}
