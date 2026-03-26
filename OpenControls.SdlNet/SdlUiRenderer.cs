using OpenControls;
using SDL2;

namespace OpenControls.SdlNet;

public sealed class SdlUiRenderer : IUiRenderer
{
    private sealed class AtlasPageTexture
    {
        public IntPtr Texture { get; set; }
        public int Version { get; set; } = -1;
    }

    private readonly IntPtr _renderer;
    private readonly Stack<SDL.SDL_Rect> _clipStack = new();
    private readonly UiGlyphAtlas _glyphAtlas = new();
    private readonly Dictionary<int, AtlasPageTexture> _atlasTextures = new();

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
            UiGlyphAtlasEntry entry = _glyphAtlas.GetOrAdd(glyph.Glyph);
            if (!entry.IsValid)
            {
                continue;
            }

            AtlasPageTexture pageTexture = EnsureAtlasTexture(entry.PageIndex);
            SDL.SDL_SetTextureColorMod(pageTexture.Texture, color.R, color.G, color.B);
            SDL.SDL_SetTextureAlphaMod(pageTexture.Texture, color.A);
            SDL.SDL_Rect source = ToRect(entry.SourceRect);
            SDL.SDL_Rect destination = new()
            {
                x = position.X + glyph.X,
                y = position.Y + glyph.Y,
                w = glyph.Glyph.Width,
                h = glyph.Glyph.Height
            };
            SDL.SDL_RenderCopy(_renderer, pageTexture.Texture, ref source, ref destination);
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

    private AtlasPageTexture EnsureAtlasTexture(int pageIndex)
    {
        UiGlyphAtlasPage page = _glyphAtlas.GetPage(pageIndex);
        if (!_atlasTextures.TryGetValue(pageIndex, out AtlasPageTexture? texture))
        {
            texture = new AtlasPageTexture
            {
                Texture = SDL.SDL_CreateTexture(
                    _renderer,
                    SDL.SDL_PIXELFORMAT_RGBA8888,
                    (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
                    page.Width,
                    page.Height)
            };
            if (texture.Texture == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_CreateTexture failed: {SDL.SDL_GetError()}");
            }

            SDL.SDL_SetTextureBlendMode(texture.Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            _atlasTextures[pageIndex] = texture;
        }

        if (texture.Version != page.Version)
        {
            SDL.SDL_Rect pageRect = new()
            {
                x = 0,
                y = 0,
                w = page.Width,
                h = page.Height
            };
            unsafe
            {
                fixed (byte* pixels = page.Pixels)
                {
                    if (SDL.SDL_UpdateTexture(texture.Texture, ref pageRect, (IntPtr)pixels, page.Width * 4) != 0)
                    {
                        throw new InvalidOperationException($"SDL_UpdateTexture failed: {SDL.SDL_GetError()}");
                    }
                }
            }

            texture.Version = page.Version;
        }

        return texture;
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
