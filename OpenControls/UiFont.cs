using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Buffer = HarfBuzzSharp.Buffer;
using HarfBuzzSharp;
using WaterTrans.GlyphLoader;
using WaterTrans.GlyphLoader.Geometry;

namespace OpenControls;

public sealed class UiFont
{
    private readonly UiFontLayer[] _layers;
    private readonly ConcurrentDictionary<GlyphCacheKey, UiRasterizedGlyph> _glyphCache = new();

    private static readonly Rune ReplacementRune = new('?');

    internal UiFont(string name, int pixelSize, IEnumerable<UiFontLayerDefinition> layers)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A font name is required.", nameof(name));
        }

        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        Name = name;
        PixelSize = pixelSize;
        _layers = layers?.Select((layer, index) => new UiFontLayer(index, layer)).ToArray()
            ?? throw new ArgumentNullException(nameof(layers));

        if (_layers.Length == 0)
        {
            throw new ArgumentException("At least one font source is required.", nameof(layers));
        }
    }

    public static UiFont Default { get; } = new UiFontBuilder("TinyBitmap", TinyBitmapFont.GlyphHeight)
        .AddBitmapFallback()
        .Build();

    public string Name { get; }
    public int PixelSize { get; }
    public bool IsMonospace => _layers[0].Monospace;

    public UiFontMetrics GetMetrics(int scale = 1)
    {
        int pixelSize = GetPixelSize(scale);
        return _layers[0].Source.GetMetrics(pixelSize);
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return LayoutText(text, scale).Width;
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return GetMetrics(scale).LineHeight;
    }

    public static UiFont FromTinyBitmap(TinyBitmapFont? font = null, string name = "TinyBitmap")
    {
        return new UiFontBuilder(name, TinyBitmapFont.GlyphHeight)
            .AddBitmapFallback(font)
            .Build();
    }

    public UiFont WithPixelSize(int pixelSize, string? name = null)
    {
        int resolvedPixelSize = Math.Max(1, pixelSize);
        if (resolvedPixelSize == PixelSize && string.IsNullOrWhiteSpace(name))
        {
            return this;
        }

        string fontName = string.IsNullOrWhiteSpace(name)
            ? $"{Name}@{resolvedPixelSize}"
            : name;
        return new UiFont(fontName, resolvedPixelSize, _layers.Select(static layer => layer.ToDefinition()));
    }

    internal UiTextLayout LayoutText(string text, int scale = 1)
    {
        int safeScale = Math.Max(1, scale);
        UiFontMetrics metrics = GetMetrics(safeScale);
        if (string.IsNullOrEmpty(text))
        {
            return new UiTextLayout(Array.Empty<UiPositionedGlyph>(), 0, metrics.LineHeight);
        }

        List<UiPositionedGlyph> glyphs = new();
        int lineHeight = metrics.LineHeight;
        int penY = 0;
        int width = 0;
        int lineStart = 0;
        int lines = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            bool endOfLine = i == text.Length || text[i] == '\n';
            if (!endOfLine)
            {
                continue;
            }

            string lineText = NormalizeLineText(text.AsSpan(lineStart, i - lineStart));
            width = Math.Max(width, LayoutLine(lineText, penY, safeScale, glyphs));
            penY += lineHeight;
            lines++;
            lineStart = i + 1;
        }

        int height = Math.Max(lineHeight, lines * lineHeight);
        return new UiTextLayout(glyphs, width, height);
    }

    internal bool TryGetBitmapFont(out TinyBitmapFont? font)
    {
        if (_layers.Length > 0 && _layers[0].Source is UiBitmapFontSource bitmapSource)
        {
            font = bitmapSource.Font;
            return true;
        }

        font = null;
        return false;
    }

    private UiRasterizedGlyph ResolveGlyph(Rune rune, int scale)
    {
        if (TryResolveGlyph(rune, scale, out UiRasterizedGlyph glyph))
        {
            return glyph;
        }

        if (rune != ReplacementRune && TryResolveGlyph(ReplacementRune, scale, out glyph))
        {
            return glyph;
        }

        if (TryResolveGlyph(new Rune(' '), scale, out glyph))
        {
            return glyph;
        }

        UiFontMetrics metrics = GetMetrics(scale);
        return UiRasterizedGlyph.Empty(metrics.PixelSize / 2, metrics.LineHeight);
    }

    private int LayoutLine(string text, int penY, int scale, List<UiPositionedGlyph> glyphs)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        StringBuilder segmentText = new();
        UiFontLayer? segmentLayer = null;
        bool? segmentRightToLeft = null;
        int penX = 0;

        foreach (Rune rune in text.EnumerateRunes())
        {
            UiFontLayer? layer = ResolvePreferredLayer(rune);
            bool? runeRightToLeft = GetStrongDirection(rune);

            if (segmentText.Length > 0 && !CanAppendToSegment(segmentLayer, layer, segmentRightToLeft, runeRightToLeft))
            {
                penX += EmitSegment(segmentText.ToString(), segmentLayer, segmentRightToLeft ?? false, penX, penY, scale, glyphs);
                segmentText.Clear();
                segmentRightToLeft = null;
            }

            if (segmentText.Length == 0)
            {
                segmentLayer = layer;
            }

            if (segmentRightToLeft is null && runeRightToLeft is bool direction)
            {
                segmentRightToLeft = direction;
            }

            segmentText.Append(rune.ToString());
        }

        if (segmentText.Length > 0)
        {
            penX += EmitSegment(segmentText.ToString(), segmentLayer, segmentRightToLeft ?? false, penX, penY, scale, glyphs);
        }

        return penX;
    }

    private int EmitSegment(string text, UiFontLayer? preferredLayer, bool rightToLeft, int penX, int penY, int scale, List<UiPositionedGlyph> glyphs)
    {
        int pixelSize = GetPixelSize(scale);
        if (preferredLayer?.Source is IUiShapingGlyphSource shapingSource
            && shapingSource.TryShape(text, pixelSize, rightToLeft, out UiShapedRun run))
        {
            int runAdvance = 0;
            for (int i = 0; i < run.Glyphs.Count; i++)
            {
                UiShapedGlyph shapedGlyph = run.Glyphs[i];
                UiRasterizedGlyph glyph = ResolveGlyph(preferredLayer, shapedGlyph.GlyphIndex, pixelSize);
                if (glyph.HasBitmap)
                {
                    glyphs.Add(new UiPositionedGlyph(
                        glyph,
                        penX + runAdvance + glyph.OffsetX + shapedGlyph.OffsetX,
                        penY + glyph.OffsetY + shapedGlyph.OffsetY));
                }

                runAdvance += shapedGlyph.AdvanceX;
            }

            return Math.Max(0, run.Width);
        }

        int advance = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            UiRasterizedGlyph glyph = ResolveGlyph(rune, scale);
            if (glyph.HasBitmap)
            {
                glyphs.Add(new UiPositionedGlyph(glyph, penX + advance + glyph.OffsetX, penY + glyph.OffsetY));
            }

            advance += glyph.AdvanceX;
        }

        return advance;
    }

    private UiFontLayer? ResolvePreferredLayer(Rune rune)
    {
        for (int i = 0; i < _layers.Length; i++)
        {
            UiFontLayer layer = _layers[i];
            if (layer.Matches(rune) && layer.Source.HasGlyph(rune))
            {
                return layer;
            }
        }

        return null;
    }

    private UiRasterizedGlyph ResolveGlyph(UiFontLayer layer, uint glyphIndex, int pixelSize)
    {
        GlyphCacheKey key = new(layer.Index, (int)glyphIndex, pixelSize, IsGlyphIndex: true);
        return _glyphCache.GetOrAdd(key, _ => CreateGlyph(layer, glyphIndex, pixelSize));
    }

    private static string NormalizeLineText(ReadOnlySpan<char> line)
    {
        if (line.IsEmpty)
        {
            return string.Empty;
        }

        StringBuilder builder = new(line.Length);
        foreach (char character in line)
        {
            if (character == '\r')
            {
                continue;
            }

            if (character == '\t')
            {
                builder.Append("    ");
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool CanAppendToSegment(UiFontLayer? currentLayer, UiFontLayer? nextLayer, bool? currentDirection, bool? nextDirection)
    {
        if (!ReferenceEquals(currentLayer, nextLayer))
        {
            return false;
        }

        if (nextDirection is null || currentDirection is null)
        {
            return true;
        }

        return currentDirection == nextDirection;
    }

    private static bool? GetStrongDirection(Rune rune)
    {
        if (Rune.IsWhiteSpace(rune))
        {
            return null;
        }

        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark
            or UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
            or UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.DashPunctuation
            or UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation or UnicodeCategory.InitialQuotePunctuation
            or UnicodeCategory.FinalQuotePunctuation or UnicodeCategory.OtherPunctuation or UnicodeCategory.MathSymbol
            or UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol or UnicodeCategory.OtherSymbol
            or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber)
        {
            return null;
        }

        return IsRightToLeftRune(rune);
    }

    private static bool IsRightToLeftRune(Rune rune)
    {
        int value = rune.Value;
        return value is >= 0x0590 and <= 0x08FF
            or >= 0xFB1D and <= 0xFDFF
            or >= 0xFE70 and <= 0xFEFF
            or >= 0x10800 and <= 0x10FFF
            or >= 0x1E800 and <= 0x1EEFF;
    }

    private bool TryResolveGlyph(Rune rune, int scale, out UiRasterizedGlyph glyph)
    {
        int pixelSize = GetPixelSize(scale);
        for (int i = 0; i < _layers.Length; i++)
        {
            UiFontLayer layer = _layers[i];
            if (!layer.Matches(rune))
            {
                continue;
            }

            GlyphCacheKey key = new(layer.Index, rune.Value, pixelSize, IsGlyphIndex: false);
            glyph = _glyphCache.GetOrAdd(key, _ => CreateGlyph(layer, rune, pixelSize));
            if (glyph.IsValid)
            {
                return true;
            }
        }

        glyph = UiRasterizedGlyph.Invalid;
        return false;
    }

    private static UiRasterizedGlyph CreateGlyph(UiFontLayer layer, Rune rune, int pixelSize)
    {
        if (!layer.Source.TryGetGlyph(rune, pixelSize, out UiGlyphBitmap glyph))
        {
            return UiRasterizedGlyph.Invalid;
        }

        int verticalOffset = layer.BaselineOffset * Math.Max(1, pixelSize) / Math.Max(1, layer.Source.BasePixelSize);
        int horizontalOffset = layer.AdvanceOffset * Math.Max(1, pixelSize) / Math.Max(1, layer.Source.BasePixelSize);
        return new UiRasterizedGlyph(
            glyph.Width,
            glyph.Height,
            glyph.Alpha,
            glyph.OffsetX,
            glyph.OffsetY + verticalOffset,
            glyph.AdvanceX + horizontalOffset,
            glyph.LineHeight);
    }

    private static UiRasterizedGlyph CreateGlyph(UiFontLayer layer, uint glyphIndex, int pixelSize)
    {
        if (layer.Source is not IUiShapingGlyphSource shapingSource
            || !shapingSource.TryGetGlyph(glyphIndex, pixelSize, out UiGlyphBitmap glyph))
        {
            return UiRasterizedGlyph.Invalid;
        }

        int verticalOffset = layer.BaselineOffset * Math.Max(1, pixelSize) / Math.Max(1, layer.Source.BasePixelSize);
        int horizontalOffset = layer.AdvanceOffset * Math.Max(1, pixelSize) / Math.Max(1, layer.Source.BasePixelSize);
        return new UiRasterizedGlyph(
            glyph.Width,
            glyph.Height,
            glyph.Alpha,
            glyph.OffsetX,
            glyph.OffsetY + verticalOffset,
            glyph.AdvanceX + horizontalOffset,
            glyph.LineHeight);
    }

    private int GetPixelSize(int scale)
    {
        return Math.Max(1, PixelSize * Math.Max(1, scale));
    }
}

internal readonly record struct UiFontLayerDefinition(
    string Name,
    UiCodePointRange[]? Ranges,
    int BaselineOffset,
    int AdvanceOffset,
    bool Monospace,
    IUiGlyphSource Source);

internal sealed class UiFontLayer
{
    private readonly UiCodePointRange[] _ranges;

    public UiFontLayer(int index, UiFontLayerDefinition definition)
    {
        Index = index;
        Name = definition.Name;
        BaselineOffset = definition.BaselineOffset;
        AdvanceOffset = definition.AdvanceOffset;
        Monospace = definition.Monospace;
        Source = definition.Source;
        _ranges = definition.Ranges ?? Array.Empty<UiCodePointRange>();
    }

    public int Index { get; }
    public string Name { get; }
    public int BaselineOffset { get; }
    public int AdvanceOffset { get; }
    public bool Monospace { get; }
    public IUiGlyphSource Source { get; }

    public UiFontLayerDefinition ToDefinition()
    {
        return new UiFontLayerDefinition(Name, _ranges.Length == 0 ? null : _ranges.ToArray(), BaselineOffset, AdvanceOffset, Monospace, Source);
    }

    public bool Matches(Rune rune)
    {
        if (_ranges.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < _ranges.Length; i++)
        {
            if (_ranges[i].Contains(rune))
            {
                return true;
            }
        }

        return false;
    }
}

internal interface IUiGlyphSource
{
    int BasePixelSize { get; }
    UiFontMetrics GetMetrics(int pixelSize);
    bool HasGlyph(Rune rune);
    bool TryGetGlyph(Rune rune, int pixelSize, out UiGlyphBitmap glyph);
}

internal interface IUiShapingGlyphSource : IUiGlyphSource
{
    bool TryGetGlyph(uint glyphIndex, int pixelSize, out UiGlyphBitmap glyph);
    bool TryShape(string text, int pixelSize, bool rightToLeft, out UiShapedRun run);
}

internal sealed class UiBitmapFontSource : IUiGlyphSource
{
    private readonly ConcurrentDictionary<(char Character, int PixelSize), UiGlyphBitmap> _glyphCache = new();

    public UiBitmapFontSource(TinyBitmapFont font)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
    }

    public TinyBitmapFont Font { get; }
    public int BasePixelSize => TinyBitmapFont.GlyphHeight;

    public UiFontMetrics GetMetrics(int pixelSize)
    {
        int scale = Math.Max(1, pixelSize / TinyBitmapFont.GlyphHeight);
        return new UiFontMetrics(pixelSize, 0, TinyBitmapFont.GlyphHeight * scale, TinyBitmapFont.GlyphHeight * scale);
    }

    public bool HasGlyph(Rune rune)
    {
        return true;
    }

    public bool TryGetGlyph(Rune rune, int pixelSize, out UiGlyphBitmap glyph)
    {
        char character = rune.IsBmp ? (char)rune.Value : '?';
        glyph = _glyphCache.GetOrAdd((character, pixelSize), key =>
        {
            int scale = Math.Max(1, key.PixelSize / TinyBitmapFont.GlyphHeight);
            byte[] rows = Font.GetGlyph(key.Character);
            int advance = (TinyBitmapFont.GlyphWidth + TinyBitmapFont.GlyphSpacing) * scale;
            byte[] alpha = RasterizeBitmapGlyph(rows, scale, out int width, out int height);
            return new UiGlyphBitmap(width, height, alpha, 0, 0, advance, TinyBitmapFont.GlyphHeight * scale);
        });

        return true;
    }

    private static byte[] RasterizeBitmapGlyph(byte[] rows, int scale, out int width, out int height)
    {
        width = TinyBitmapFont.GlyphWidth * scale;
        height = TinyBitmapFont.GlyphHeight * scale;
        byte[] alpha = new byte[width * height];

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

                int startX = col * scale;
                int startY = row * scale;
                for (int y = 0; y < scale; y++)
                {
                    int rowIndex = (startY + y) * width;
                    for (int x = 0; x < scale; x++)
                    {
                        alpha[rowIndex + startX + x] = 255;
                    }
                }
            }
        }

        return alpha;
    }
}

internal sealed class UiOutlineFontSource : IUiShapingGlyphSource
{
    private readonly Typeface _typeface;
    private readonly Blob _blob;
    private readonly Face _face;
    private readonly ConcurrentDictionary<int, UiFontMetrics> _metricsCache = new();
    private readonly ConcurrentDictionary<(ushort GlyphIndex, int PixelSize), UiGlyphBitmap> _glyphCache = new();
    private readonly ConcurrentDictionary<int, HarfBuzzSharp.Font> _shapingFonts = new();

    public UiOutlineFontSource(string path, bool monospace)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The font file could not be found.", path);
        }

        Path = path;
        Monospace = monospace;
        using FileStream stream = File.OpenRead(path);
        _typeface = new Typeface(stream);
        _blob = Blob.FromFile(path);
        _face = new Face(_blob, 0);
        BasePixelSize = 16;
    }

    public string Path { get; }
    public bool Monospace { get; }
    public int BasePixelSize { get; }

    public UiFontMetrics GetMetrics(int pixelSize)
    {
        return _metricsCache.GetOrAdd(pixelSize, size =>
        {
            int lineHeight = Math.Max(1, (int)Math.Ceiling(_typeface.Height * size));
            int ascent = Math.Clamp((int)Math.Ceiling(_typeface.Baseline * size), 0, lineHeight);
            int descent = Math.Max(0, lineHeight - ascent);
            return new UiFontMetrics(size, ascent, descent, lineHeight);
        });
    }

    public bool HasGlyph(Rune rune)
    {
        return _typeface.CharacterToGlyphMap.TryGetValue(rune.Value, out ushort glyphIndex) && glyphIndex != 0;
    }

    public bool TryGetGlyph(Rune rune, int pixelSize, out UiGlyphBitmap glyph)
    {
        if (!_typeface.CharacterToGlyphMap.TryGetValue(rune.Value, out ushort glyphIndex) || glyphIndex == 0)
        {
            glyph = default;
            return false;
        }

        glyph = _glyphCache.GetOrAdd((glyphIndex, pixelSize), key => RasterizeGlyph(key.GlyphIndex, key.PixelSize));
        return true;
    }

    public bool TryGetGlyph(uint glyphIndex, int pixelSize, out UiGlyphBitmap glyph)
    {
        if (glyphIndex == 0 || glyphIndex > ushort.MaxValue)
        {
            glyph = default;
            return false;
        }

        glyph = _glyphCache.GetOrAdd(((ushort)glyphIndex, pixelSize), key => RasterizeGlyph(key.GlyphIndex, key.PixelSize));
        return glyph.IsValid;
    }

    public bool TryShape(string text, int pixelSize, bool rightToLeft, out UiShapedRun run)
    {
        if (string.IsNullOrEmpty(text))
        {
            run = UiShapedRun.Empty;
            return false;
        }

        HarfBuzzSharp.Font font = _shapingFonts.GetOrAdd(pixelSize, CreateShapingFont);
        using Buffer buffer = new();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();
        buffer.Direction = rightToLeft ? Direction.RightToLeft : Direction.LeftToRight;
        font.Shape(buffer);

        GlyphInfo[] infos = buffer.GlyphInfos;
        GlyphPosition[] positions = buffer.GlyphPositions;
        if (infos.Length == 0 || infos.Length != positions.Length)
        {
            run = UiShapedRun.Empty;
            return false;
        }

        UiShapedGlyph[] glyphs = new UiShapedGlyph[infos.Length];
        int width = 0;
        for (int i = 0; i < infos.Length; i++)
        {
            GlyphInfo info = infos[i];
            GlyphPosition position = positions[i];
            int xOffset = ToPixels(position.XOffset);
            int yOffset = -ToPixels(position.YOffset);
            int advanceX = ToPixels(position.XAdvance);
            glyphs[i] = new UiShapedGlyph(info.Codepoint, xOffset, yOffset, advanceX);
            width += advanceX;
        }

        run = new UiShapedRun(glyphs, Math.Max(0, width));
        return true;
    }

    private HarfBuzzSharp.Font CreateShapingFont(int pixelSize)
    {
        HarfBuzzSharp.Font font = new(_face);
        font.SetFunctionsOpenType();
        int scale = Math.Max(1, pixelSize) * 64;
        font.SetScale(scale, scale);
        return font;
    }

    private static int ToPixels(int value)
    {
        return (int)Math.Round(value / 64f);
    }

    private UiGlyphBitmap RasterizeGlyph(ushort glyphIndex, int pixelSize)
    {
        UiFontMetrics metrics = GetMetrics(pixelSize);
        int advance = _typeface.AdvanceWidths.TryGetValue(glyphIndex, out double advanceWidth)
            ? Math.Max(1, (int)Math.Round(advanceWidth * pixelSize))
            : Math.Max(1, pixelSize / 2);

        PathGeometry geometry = _typeface.GetGlyphOutline(glyphIndex, pixelSize);
        List<List<UiGeometryPoint>> contours = FlattenContours(geometry, metrics.Ascent);
        if (contours.Count == 0)
        {
            return new UiGlyphBitmap(0, 0, Array.Empty<byte>(), 0, 0, advance, metrics.LineHeight);
        }

        if (!TryGetBounds(contours, out double minX, out double minY, out double maxX, out double maxY))
        {
            return new UiGlyphBitmap(0, 0, Array.Empty<byte>(), 0, 0, advance, metrics.LineHeight);
        }

        const int padding = 1;
        int left = (int)Math.Floor(minX);
        int top = (int)Math.Floor(minY);
        int right = (int)Math.Ceiling(maxX);
        int bottom = (int)Math.Ceiling(maxY);
        int width = Math.Max(0, right - left + padding * 2);
        int height = Math.Max(0, bottom - top + padding * 2);
        if (width == 0 || height == 0)
        {
            return new UiGlyphBitmap(0, 0, Array.Empty<byte>(), left, top, advance, metrics.LineHeight);
        }

        byte[] alpha = RasterizeContours(contours, geometry.FillRule, left - padding, top - padding, width, height);
        return new UiGlyphBitmap(width, height, alpha, left - padding, top - padding, advance, metrics.LineHeight);
    }

    private static List<List<UiGeometryPoint>> FlattenContours(PathGeometry geometry, int baseline)
    {
        List<List<UiGeometryPoint>> contours = new();
        foreach (PathFigure figure in geometry.Figures)
        {
            List<UiGeometryPoint> contour = new();
            UiGeometryPoint start = new(figure.StartPoint.X, figure.StartPoint.Y + baseline);
            UiGeometryPoint current = start;
            contour.Add(start);

            foreach (PathSegment segment in figure.Segments)
            {
                switch (segment)
                {
                    case LineSegment line:
                        current = new UiGeometryPoint(line.Point.X, line.Point.Y + baseline);
                        contour.Add(current);
                        break;
                    case QuadraticBezierSegment quadratic:
                    {
                        UiGeometryPoint control = new(quadratic.Point1.X, quadratic.Point1.Y + baseline);
                        UiGeometryPoint end = new(quadratic.Point2.X, quadratic.Point2.Y + baseline);
                        FlattenQuadratic(current, control, end, contour, depth: 0);
                        current = end;
                        break;
                    }
                    case BezierSegment cubic:
                    {
                        UiGeometryPoint control1 = new(cubic.Point1.X, cubic.Point1.Y + baseline);
                        UiGeometryPoint control2 = new(cubic.Point2.X, cubic.Point2.Y + baseline);
                        UiGeometryPoint end = new(cubic.Point3.X, cubic.Point3.Y + baseline);
                        FlattenCubic(current, control1, control2, end, contour, depth: 0);
                        current = end;
                        break;
                    }
                }
            }

            if (figure.IsClosed && contour.Count > 0)
            {
                UiGeometryPoint last = contour[^1];
                if (!UiGeometryPoint.ApproximatelyEquals(last, contour[0]))
                {
                    contour.Add(contour[0]);
                }
            }

            if (contour.Count >= 3)
            {
                contours.Add(contour);
            }
        }

        return contours;
    }

    private static void FlattenQuadratic(
        UiGeometryPoint start,
        UiGeometryPoint control,
        UiGeometryPoint end,
        List<UiGeometryPoint> points,
        int depth)
    {
        if (depth >= 10 || IsQuadraticFlatEnough(start, control, end))
        {
            points.Add(end);
            return;
        }

        UiGeometryPoint p01 = Midpoint(start, control);
        UiGeometryPoint p12 = Midpoint(control, end);
        UiGeometryPoint split = Midpoint(p01, p12);
        FlattenQuadratic(start, p01, split, points, depth + 1);
        FlattenQuadratic(split, p12, end, points, depth + 1);
    }

    private static void FlattenCubic(
        UiGeometryPoint start,
        UiGeometryPoint control1,
        UiGeometryPoint control2,
        UiGeometryPoint end,
        List<UiGeometryPoint> points,
        int depth)
    {
        if (depth >= 10 || IsCubicFlatEnough(start, control1, control2, end))
        {
            points.Add(end);
            return;
        }

        UiGeometryPoint p01 = Midpoint(start, control1);
        UiGeometryPoint p12 = Midpoint(control1, control2);
        UiGeometryPoint p23 = Midpoint(control2, end);
        UiGeometryPoint p012 = Midpoint(p01, p12);
        UiGeometryPoint p123 = Midpoint(p12, p23);
        UiGeometryPoint split = Midpoint(p012, p123);
        FlattenCubic(start, p01, p012, split, points, depth + 1);
        FlattenCubic(split, p123, p23, end, points, depth + 1);
    }

    private static bool IsQuadraticFlatEnough(UiGeometryPoint start, UiGeometryPoint control, UiGeometryPoint end)
    {
        return DistanceFromLine(control, start, end) <= 0.35;
    }

    private static bool IsCubicFlatEnough(UiGeometryPoint start, UiGeometryPoint control1, UiGeometryPoint control2, UiGeometryPoint end)
    {
        return Math.Max(DistanceFromLine(control1, start, end), DistanceFromLine(control2, start, end)) <= 0.35;
    }

    private static double DistanceFromLine(UiGeometryPoint point, UiGeometryPoint lineStart, UiGeometryPoint lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= double.Epsilon)
        {
            return 0d;
        }

        return Math.Abs(dy * point.X - dx * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X) / length;
    }

    private static UiGeometryPoint Midpoint(UiGeometryPoint a, UiGeometryPoint b)
    {
        return new UiGeometryPoint((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
    }

    private static bool TryGetBounds(List<List<UiGeometryPoint>> contours, out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = double.PositiveInfinity;
        minY = double.PositiveInfinity;
        maxX = double.NegativeInfinity;
        maxY = double.NegativeInfinity;

        for (int i = 0; i < contours.Count; i++)
        {
            List<UiGeometryPoint> contour = contours[i];
            for (int j = 0; j < contour.Count; j++)
            {
                UiGeometryPoint point = contour[j];
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        return minX <= maxX && minY <= maxY;
    }

    private static byte[] RasterizeContours(
        List<List<UiGeometryPoint>> contours,
        FillRule fillRule,
        int left,
        int top,
        int width,
        int height)
    {
        const int samplesPerAxis = 4;
        const int totalSamples = samplesPerAxis * samplesPerAxis;
        byte[] alpha = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < samplesPerAxis; sy++)
                {
                    double sampleY = top + y + (sy + 0.5) / samplesPerAxis;
                    for (int sx = 0; sx < samplesPerAxis; sx++)
                    {
                        double sampleX = left + x + (sx + 0.5) / samplesPerAxis;
                        if (ContainsPoint(contours, fillRule, sampleX, sampleY))
                        {
                            covered++;
                        }
                    }
                }

                alpha[rowStart + x] = (byte)Math.Round(covered * 255d / totalSamples);
            }
        }

        return alpha;
    }

    private static bool ContainsPoint(List<List<UiGeometryPoint>> contours, FillRule fillRule, double x, double y)
    {
        return fillRule == FillRule.Nonzero
            ? ComputeWindingNumber(contours, x, y) != 0
            : ComputeCrossings(contours, x, y) % 2 != 0;
    }

    private static int ComputeCrossings(List<List<UiGeometryPoint>> contours, double x, double y)
    {
        int crossings = 0;
        for (int i = 0; i < contours.Count; i++)
        {
            List<UiGeometryPoint> contour = contours[i];
            for (int j = 0; j < contour.Count - 1; j++)
            {
                UiGeometryPoint a = contour[j];
                UiGeometryPoint b = contour[j + 1];
                bool intersects = (a.Y > y) != (b.Y > y);
                if (!intersects)
                {
                    continue;
                }

                double intersectX = a.X + ((y - a.Y) * (b.X - a.X)) / (b.Y - a.Y);
                if (intersectX > x)
                {
                    crossings++;
                }
            }
        }

        return crossings;
    }

    private static int ComputeWindingNumber(List<List<UiGeometryPoint>> contours, double x, double y)
    {
        int winding = 0;
        for (int i = 0; i < contours.Count; i++)
        {
            List<UiGeometryPoint> contour = contours[i];
            for (int j = 0; j < contour.Count - 1; j++)
            {
                UiGeometryPoint a = contour[j];
                UiGeometryPoint b = contour[j + 1];
                if (a.Y <= y)
                {
                    if (b.Y > y && IsLeft(a, b, x, y) > 0)
                    {
                        winding++;
                    }
                }
                else if (b.Y <= y && IsLeft(a, b, x, y) < 0)
                {
                    winding--;
                }
            }
        }

        return winding;
    }

    private static double IsLeft(UiGeometryPoint a, UiGeometryPoint b, double x, double y)
    {
        return (b.X - a.X) * (y - a.Y) - (x - a.X) * (b.Y - a.Y);
    }
}

internal readonly record struct GlyphCacheKey(int LayerIndex, int GlyphValue, int PixelSize, bool IsGlyphIndex);

internal readonly record struct UiGlyphBitmap(
    int Width,
    int Height,
    byte[] Alpha,
    int OffsetX,
    int OffsetY,
    int AdvanceX,
    int LineHeight)
{
    public bool IsValid => Alpha != null;
}

internal sealed class UiRasterizedGlyph
{
    public static UiRasterizedGlyph Invalid { get; } = new(0, 0, Array.Empty<byte>(), 0, 0, 0, 0, false);

    public UiRasterizedGlyph(int width, int height, byte[] alpha, int offsetX, int offsetY, int advanceX, int lineHeight)
        : this(width, height, alpha, offsetX, offsetY, advanceX, lineHeight, true)
    {
    }

    private UiRasterizedGlyph(int width, int height, byte[] alpha, int offsetX, int offsetY, int advanceX, int lineHeight, bool isValid)
    {
        Width = width;
        Height = height;
        Alpha = alpha;
        OffsetX = offsetX;
        OffsetY = offsetY;
        AdvanceX = advanceX;
        LineHeight = lineHeight;
        IsValid = isValid;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Alpha { get; }
    public int OffsetX { get; }
    public int OffsetY { get; }
    public int AdvanceX { get; }
    public int LineHeight { get; }
    public bool IsValid { get; }
    public bool HasBitmap => Width > 0 && Height > 0 && Alpha.Length > 0;

    public static UiRasterizedGlyph Empty(int advanceX, int lineHeight)
    {
        return new UiRasterizedGlyph(0, 0, Array.Empty<byte>(), 0, 0, advanceX, lineHeight);
    }
}

internal readonly record struct UiShapedGlyph(uint GlyphIndex, int OffsetX, int OffsetY, int AdvanceX);

internal sealed class UiShapedRun
{
    public static UiShapedRun Empty { get; } = new(Array.Empty<UiShapedGlyph>(), 0);

    public UiShapedRun(IReadOnlyList<UiShapedGlyph> glyphs, int width)
    {
        Glyphs = glyphs;
        Width = width;
    }

    public IReadOnlyList<UiShapedGlyph> Glyphs { get; }
    public int Width { get; }
}

internal readonly record struct UiPositionedGlyph(UiRasterizedGlyph Glyph, int X, int Y);

internal sealed class UiTextLayout
{
    public UiTextLayout(IReadOnlyList<UiPositionedGlyph> glyphs, int width, int height)
    {
        Glyphs = glyphs;
        Width = width;
        Height = height;
    }

    public IReadOnlyList<UiPositionedGlyph> Glyphs { get; }
    public int Width { get; }
    public int Height { get; }
}

internal readonly record struct UiGeometryPoint(double X, double Y)
{
    public static bool ApproximatelyEquals(UiGeometryPoint a, UiGeometryPoint b)
    {
        return Math.Abs(a.X - b.X) <= 0.001 && Math.Abs(a.Y - b.Y) <= 0.001;
    }
}
