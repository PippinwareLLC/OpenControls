using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenControls;

namespace OpenControls.Editor;

public sealed class MonoGameUiRenderer : IUiRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly TinyBitmapFont _font;
    private readonly Stack<Rectangle> _clipStack = new();
    private Texture2D? _gradientTexture;
    private int _gradientWidth;
    private int _gradientHeight;
    private UiColor _gradientTopLeft;
    private UiColor _gradientTopRight;
    private UiColor _gradientBottomLeft;
    private UiColor _gradientBottomRight;
    private Color[]? _gradientData;
    private Texture2D? _checkerTexture;
    private int _checkerWidth;
    private int _checkerHeight;
    private int _checkerCellSize;
    private UiColor _checkerColorA;
    private UiColor _checkerColorB;
    private Color[]? _checkerData;

    public MonoGameUiRenderer(SpriteBatch spriteBatch, Texture2D pixel, TinyBitmapFont font)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _font = font;
    }

    public TinyFontCodePage CodePage
    {
        get => _font.CodePage;
        set => _font.CodePage = value;
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), ToColor(color));
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (thickness <= 0)
        {
            return;
        }

        Color drawColor = ToColor(color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), drawColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), drawColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), drawColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), drawColor);
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (SameColor(topLeft, topRight) && SameColor(topLeft, bottomLeft) && SameColor(topLeft, bottomRight))
        {
            FillRect(rect, topLeft);
            return;
        }

        EnsureGradientTexture(rect.Width, rect.Height, topLeft, topRight, bottomLeft, bottomRight);
        _spriteBatch.Draw(_gradientTexture!, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), Color.White);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (SameColor(colorA, colorB))
        {
            FillRect(rect, colorA);
            return;
        }

        EnsureCheckerTexture(rect.Width, rect.Height, cellSize, colorA, colorB);
        _spriteBatch.Draw(_checkerTexture!, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), Color.White);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        byte[] bytes = _font.GetEncoding().GetBytes(text);
        Color drawColor = ToColor(color);
        int cursorX = position.X;
        int cursorY = position.Y;

        foreach (byte code in bytes)
        {
            DrawGlyph(_font.GetGlyph(code), cursorX, cursorY, drawColor, scale);
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
        Rectangle clip = new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));
        Rectangle viewport = GetViewportBounds();
        clip = Rectangle.Intersect(viewport, clip);
        if (_clipStack.Count > 0)
        {
            clip = Rectangle.Intersect(_clipStack.Peek(), clip);
        }

        _clipStack.Push(clip);
        ApplyClip(clip);
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _clipStack.Pop();
        Rectangle clip = _clipStack.Count > 0 ? _clipStack.Peek() : GetViewportBounds();
        ApplyClip(clip);
    }

    private static Color ToColor(UiColor color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }

    private void EnsureGradientTexture(int width, int height, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        bool sizeChanged = _gradientTexture == null || _gradientWidth != width || _gradientHeight != height;
        if (sizeChanged)
        {
            _gradientTexture?.Dispose();
            _gradientTexture = new Texture2D(_spriteBatch.GraphicsDevice, width, height);
            _gradientWidth = width;
            _gradientHeight = height;
            _gradientData = new Color[width * height];
        }

        bool colorsChanged = !SameColor(topLeft, _gradientTopLeft)
            || !SameColor(topRight, _gradientTopRight)
            || !SameColor(bottomLeft, _gradientBottomLeft)
            || !SameColor(bottomRight, _gradientBottomRight);

        if (sizeChanged || colorsChanged)
        {
            UpdateGradientData(width, height, topLeft, topRight, bottomLeft, bottomRight, _gradientData!);
            _gradientTexture!.SetData(_gradientData!);
            _gradientTopLeft = topLeft;
            _gradientTopRight = topRight;
            _gradientBottomLeft = bottomLeft;
            _gradientBottomRight = bottomRight;
        }
    }

    private void EnsureCheckerTexture(int width, int height, int cellSize, UiColor colorA, UiColor colorB)
    {
        int size = Math.Max(1, cellSize);
        bool sizeChanged = _checkerTexture == null || _checkerWidth != width || _checkerHeight != height;
        if (sizeChanged)
        {
            _checkerTexture?.Dispose();
            _checkerTexture = new Texture2D(_spriteBatch.GraphicsDevice, width, height);
            _checkerWidth = width;
            _checkerHeight = height;
            _checkerData = new Color[width * height];
        }

        bool settingsChanged = sizeChanged || size != _checkerCellSize
            || !SameColor(colorA, _checkerColorA)
            || !SameColor(colorB, _checkerColorB);

        if (settingsChanged)
        {
            UpdateCheckerData(width, height, size, colorA, colorB, _checkerData!);
            _checkerTexture!.SetData(_checkerData!);
            _checkerCellSize = size;
            _checkerColorA = colorA;
            _checkerColorB = colorB;
        }
    }

    private void UpdateGradientData(int width, int height, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight, Color[] data)
    {
        for (int y = 0; y < height; y++)
        {
            float v = height <= 1 ? 0f : y / (float)(height - 1);
            UiColor left = LerpColor(topLeft, bottomLeft, v);
            UiColor right = LerpColor(topRight, bottomRight, v);
            for (int x = 0; x < width; x++)
            {
                float u = width <= 1 ? 0f : x / (float)(width - 1);
                UiColor color = LerpColor(left, right, u);
                data[y * width + x] = ToColor(color);
            }
        }
    }

    private void UpdateCheckerData(int width, int height, int cellSize, UiColor colorA, UiColor colorB, Color[] data)
    {
        int size = Math.Max(1, cellSize);
        for (int y = 0; y < height; y++)
        {
            int cellY = (y / size) % 2;
            for (int x = 0; x < width; x++)
            {
                int cellX = (x / size) % 2;
                UiColor color = cellX == cellY ? colorA : colorB;
                data[y * width + x] = ToColor(color);
            }
        }
    }

    private static UiColor LerpColor(UiColor a, UiColor b, float t)
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

    private static bool SameColor(UiColor a, UiColor b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    }

    private Rectangle GetViewportBounds()
    {
        Viewport viewport = _spriteBatch.GraphicsDevice.Viewport;
        return new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
    }

    private void ApplyClip(Rectangle clip)
    {
        RasterizerState rasterizer = new()
        {
            ScissorTestEnable = true
        };

        _spriteBatch.End();
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizer);
        _spriteBatch.GraphicsDevice.ScissorRectangle = clip;
    }

    private void DrawGlyph(byte[] rows, int x, int y, Color color, int scale)
    {
        for (int row = 0; row < TinyBitmapFont.GlyphHeight; row++)
        {
            byte bits = rows[row];
            for (int col = 0; col < TinyBitmapFont.GlyphWidth; col++)
            {
                int mask = 1 << (TinyBitmapFont.GlyphWidth - 1 - col);
                if ((bits & mask) == 0)
                {
                    continue;
                }

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        x + col * scale,
                        y + row * scale,
                        scale,
                        scale),
                    color);
            }
        }
    }
}
