namespace OpenControls;

internal sealed class UiGlyphAtlas
{
    private readonly int _defaultPageSize;
    private readonly Dictionary<UiRasterizedGlyph, UiGlyphAtlasEntry> _entries = new(ReferenceEqualityComparer.Instance);
    private readonly List<UiGlyphAtlasPage> _pages = new();

    public UiGlyphAtlas(int defaultPageSize = 1024)
    {
        _defaultPageSize = Math.Max(128, defaultPageSize);
    }

    public IReadOnlyList<UiGlyphAtlasPage> Pages => _pages;

    public UiGlyphAtlasEntry GetOrAdd(UiRasterizedGlyph glyph)
    {
        if (glyph == null)
        {
            throw new ArgumentNullException(nameof(glyph));
        }

        if (!glyph.HasBitmap)
        {
            return UiGlyphAtlasEntry.Invalid;
        }

        if (_entries.TryGetValue(glyph, out UiGlyphAtlasEntry cached))
        {
            return cached;
        }

        UiGlyphAtlasEntry entry = PackGlyph(glyph);
        _entries[glyph] = entry;
        return entry;
    }

    public UiGlyphAtlasPage GetPage(int pageIndex)
    {
        return _pages[pageIndex];
    }

    private UiGlyphAtlasEntry PackGlyph(UiRasterizedGlyph glyph)
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i].TryAddGlyph(glyph, out UiRect sourceRect))
            {
                return new UiGlyphAtlasEntry(i, sourceRect);
            }
        }

        UiGlyphAtlasPage page = CreatePage(glyph);
        _pages.Add(page);
        if (!page.TryAddGlyph(glyph, out UiRect rect))
        {
            throw new InvalidOperationException("Failed to pack glyph into a fresh atlas page.");
        }

        return new UiGlyphAtlasEntry(_pages.Count - 1, rect);
    }

    private UiGlyphAtlasPage CreatePage(UiRasterizedGlyph glyph)
    {
        int largestSide = Math.Max(glyph.Width, glyph.Height);
        int size = _defaultPageSize;
        while (size < largestSide)
        {
            size *= 2;
        }

        return new UiGlyphAtlasPage(size, size);
    }
}

internal readonly record struct UiGlyphAtlasEntry(int PageIndex, UiRect SourceRect)
{
    public static UiGlyphAtlasEntry Invalid { get; } = new(-1, default);

    public bool IsValid => PageIndex >= 0 && SourceRect.Width > 0 && SourceRect.Height > 0;
}

internal sealed class UiGlyphAtlasPage
{
    private readonly byte[] _pixels;
    private int _cursorX;
    private int _cursorY;
    private int _rowHeight;

    public UiGlyphAtlasPage(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _pixels = new byte[Width * Height * 4];
    }

    public int Width { get; }
    public int Height { get; }
    public int Version { get; private set; }
    public byte[] Pixels => _pixels;

    public bool TryAddGlyph(UiRasterizedGlyph glyph, out UiRect sourceRect)
    {
        if (!glyph.HasBitmap || glyph.Width > Width || glyph.Height > Height)
        {
            sourceRect = default;
            return false;
        }

        if (_cursorX + glyph.Width > Width)
        {
            _cursorX = 0;
            _cursorY += _rowHeight;
            _rowHeight = 0;
        }

        if (_cursorY + glyph.Height > Height)
        {
            sourceRect = default;
            return false;
        }

        sourceRect = new UiRect(_cursorX, _cursorY, glyph.Width, glyph.Height);
        CopyGlyph(glyph, sourceRect.X, sourceRect.Y);
        _cursorX += glyph.Width + 1;
        _rowHeight = Math.Max(_rowHeight, glyph.Height + 1);
        Version++;
        return true;
    }

    private void CopyGlyph(UiRasterizedGlyph glyph, int targetX, int targetY)
    {
        for (int row = 0; row < glyph.Height; row++)
        {
            int glyphRowStart = row * glyph.Width;
            int atlasRowStart = ((targetY + row) * Width + targetX) * 4;
            for (int col = 0; col < glyph.Width; col++)
            {
                byte alpha = glyph.Alpha[glyphRowStart + col];
                if (alpha == 0)
                {
                    continue;
                }

                int pixelIndex = atlasRowStart + col * 4;
                _pixels[pixelIndex] = 255;
                _pixels[pixelIndex + 1] = 255;
                _pixels[pixelIndex + 2] = 255;
                _pixels[pixelIndex + 3] = alpha;
            }
        }
    }
}
