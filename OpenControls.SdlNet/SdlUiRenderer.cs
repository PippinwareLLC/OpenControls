using OpenControls;
using SDL2;

namespace OpenControls.SdlNet;

public sealed class SdlUiRenderer : IUiRenderer
{
    private readonly IntPtr _renderer;
    private readonly Stack<SDL.SDL_Rect> _clipStack = new();

    public SdlUiRenderer(IntPtr renderer, UiFont? defaultFont = null)
    {
        _renderer = renderer;
        DefaultFont = defaultFont ?? UiFont.Default;
    }

    public SdlUiRenderer(IntPtr renderer, TinyBitmapFont font)
        : this(renderer, UiFont.FromTinyBitmap(font))
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

        SetDrawColor(color);
        SDL.SDL_Rect sdlRect = ToRect(rect);
        SDL.SDL_RenderFillRect(_renderer, ref sdlRect);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0)
        {
            return;
        }

        int maxThickness = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        SetDrawColor(color);

        SDL.SDL_Rect top = new() { x = rect.X, y = rect.Y, w = rect.Width, h = maxThickness };
        SDL.SDL_Rect bottom = new() { x = rect.X, y = rect.Bottom - maxThickness, w = rect.Width, h = maxThickness };
        SDL.SDL_Rect left = new() { x = rect.X, y = rect.Y, w = maxThickness, h = rect.Height };
        SDL.SDL_Rect right = new() { x = rect.Right - maxThickness, y = rect.Y, w = maxThickness, h = rect.Height };

        SDL.SDL_RenderFillRect(_renderer, ref top);
        SDL.SDL_RenderFillRect(_renderer, ref bottom);
        SDL.SDL_RenderFillRect(_renderer, ref left);
        SDL.SDL_RenderFillRect(_renderer, ref right);
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
        for (int i = 0; i < layout.Glyphs.Count; i++)
        {
            UiPositionedGlyph glyph = layout.Glyphs[i];
            DrawGlyph(glyph.Glyph, position.X + glyph.X, position.Y + glyph.Y, color);
        }
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return MeasureTextWidth(text, scale, null);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextWidth(text, Math.Max(1, scale));
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return MeasureTextHeight(scale, null);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextHeight(Math.Max(1, scale));
    }

    public void PushClip(UiRect rect)
    {
        SDL.SDL_Rect clip = ToRect(rect);
        SDL.SDL_Rect viewport = GetViewportRect();
        clip = Intersect(clip, viewport);
        if (_clipStack.Count > 0)
        {
            clip = Intersect(clip, _clipStack.Peek());
        }

        _clipStack.Push(clip);
        SDL.SDL_RenderSetClipRect(_renderer, ref clip);
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
            SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
            return;
        }

        SDL.SDL_Rect clip = _clipStack.Peek();
        SDL.SDL_RenderSetClipRect(_renderer, ref clip);
    }

    private void DrawGlyph(UiRasterizedGlyph glyph, int x, int y, UiColor color)
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

                SetDrawColor(ApplyAlpha(color, alpha));
                SDL.SDL_Rect pixel = new()
                {
                    x = x + col,
                    y = y + row,
                    w = 1,
                    h = 1
                };
                SDL.SDL_RenderFillRect(_renderer, ref pixel);
            }
        }
    }

    private static UiColor ApplyAlpha(UiColor color, byte alpha)
    {
        int scaledAlpha = color.A * alpha / 255;
        return new UiColor(color.R, color.G, color.B, (byte)scaledAlpha);
    }

    private void SetDrawColor(UiColor color)
    {
        SDL.SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
    }

    private static SDL.SDL_Rect ToRect(UiRect rect)
    {
        return new SDL.SDL_Rect
        {
            x = rect.X,
            y = rect.Y,
            w = Math.Max(0, rect.Width),
            h = Math.Max(0, rect.Height)
        };
    }

    private SDL.SDL_Rect GetViewportRect()
    {
        SDL.SDL_RenderGetViewport(_renderer, out SDL.SDL_Rect viewport);
        return viewport;
    }

    private static SDL.SDL_Rect Intersect(SDL.SDL_Rect a, SDL.SDL_Rect b)
    {
        int left = Math.Max(a.x, b.x);
        int top = Math.Max(a.y, b.y);
        int right = Math.Min(a.x + a.w, b.x + b.w);
        int bottom = Math.Min(a.y + a.h, b.y + b.h);

        if (right <= left || bottom <= top)
        {
            return new SDL.SDL_Rect { x = left, y = top, w = 0, h = 0 };
        }

        return new SDL.SDL_Rect { x = left, y = top, w = right - left, h = bottom - top };
    }
}
