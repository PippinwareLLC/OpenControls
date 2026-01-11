using System.Text;

namespace OpenControls;

public enum TinyFontCodePage
{
    Latin1,
    Cp437
}

public sealed class TinyBitmapFont
{
    public const int GlyphWidth = 5;
    public const int GlyphHeight = 7;
    public const int GlyphSpacing = 1;

    private static readonly Dictionary<char, byte[]> BaseGlyphs = new()
    {
        ['A'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['B'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 },
        ['C'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
        ['D'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 },
        ['E'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
        ['F'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['G'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01110 },
        ['H'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['I'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b11111 },
        ['J'] = new byte[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b10010, 0b10010, 0b01100 },
        ['K'] = new byte[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 },
        ['L'] = new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
        ['M'] = new byte[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 },
        ['N'] = new byte[] { 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001 },
        ['O'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['P'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['Q'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 },
        ['R'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
        ['S'] = new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['T'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['U'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['V'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
        ['W'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b10101, 0b01010 },
        ['X'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 },
        ['Y'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['Z'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 },
        ['a'] = new byte[] { 0b00000, 0b00000, 0b01110, 0b00001, 0b01111, 0b10001, 0b01111 },
        ['b'] = new byte[] { 0b10000, 0b10000, 0b10110, 0b11001, 0b10001, 0b10001, 0b11110 },
        ['c'] = new byte[] { 0b00000, 0b00000, 0b01110, 0b10001, 0b10000, 0b10001, 0b01110 },
        ['d'] = new byte[] { 0b00001, 0b00001, 0b01101, 0b10011, 0b10001, 0b10001, 0b01111 },
        ['e'] = new byte[] { 0b00000, 0b00000, 0b01110, 0b10001, 0b11111, 0b10000, 0b01110 },
        ['f'] = new byte[] { 0b00110, 0b01001, 0b01000, 0b11100, 0b01000, 0b01000, 0b01000 },
        ['g'] = new byte[] { 0b00000, 0b00000, 0b01111, 0b10001, 0b10001, 0b01111, 0b00001 },
        ['h'] = new byte[] { 0b10000, 0b10000, 0b10110, 0b11001, 0b10001, 0b10001, 0b10001 },
        ['i'] = new byte[] { 0b00100, 0b00000, 0b01100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['j'] = new byte[] { 0b00010, 0b00000, 0b00110, 0b00010, 0b00010, 0b10010, 0b01100 },
        ['k'] = new byte[] { 0b10000, 0b10000, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010 },
        ['l'] = new byte[] { 0b11000, 0b01000, 0b01000, 0b01000, 0b01000, 0b01000, 0b11100 },
        ['m'] = new byte[] { 0b00000, 0b00000, 0b11010, 0b10101, 0b10101, 0b10001, 0b10001 },
        ['n'] = new byte[] { 0b00000, 0b00000, 0b10110, 0b11001, 0b10001, 0b10001, 0b10001 },
        ['o'] = new byte[] { 0b00000, 0b00000, 0b01110, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['p'] = new byte[] { 0b00000, 0b00000, 0b11110, 0b10001, 0b11110, 0b10000, 0b10000 },
        ['q'] = new byte[] { 0b00000, 0b00000, 0b01111, 0b10001, 0b01111, 0b00001, 0b00001 },
        ['r'] = new byte[] { 0b00000, 0b00000, 0b10110, 0b11001, 0b10000, 0b10000, 0b10000 },
        ['s'] = new byte[] { 0b00000, 0b00000, 0b01111, 0b10000, 0b01110, 0b00001, 0b11110 },
        ['t'] = new byte[] { 0b01000, 0b01000, 0b11100, 0b01000, 0b01000, 0b01001, 0b00110 },
        ['u'] = new byte[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10001, 0b10011, 0b01101 },
        ['v'] = new byte[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
        ['w'] = new byte[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10101, 0b10101, 0b01010 },
        ['x'] = new byte[] { 0b00000, 0b00000, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001 },
        ['y'] = new byte[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b01111, 0b00001, 0b01110 },
        ['z'] = new byte[] { 0b00000, 0b00000, 0b11111, 0b00010, 0b00100, 0b01000, 0b11111 },
        ['0'] = new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 },
        ['1'] = new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['2'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111 },
        ['3'] = new byte[] { 0b11110, 0b00001, 0b00001, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['4'] = new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 },
        ['5'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b00001, 0b00001, 0b11110 },
        ['6'] = new byte[] { 0b01110, 0b10000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 },
        ['7'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 },
        ['8'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
        ['9'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00001, 0b01110 },
        ['!'] = new byte[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00000, 0b00100 },
        [':'] = new byte[] { 0b00000, 0b00100, 0b00100, 0b00000, 0b00100, 0b00100, 0b00000 },
        [';'] = new byte[] { 0b00000, 0b00100, 0b00100, 0b00000, 0b00100, 0b00100, 0b01000 },
        ['"'] = new byte[] { 0b01010, 0b01010, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000 },
        ['\''] = new byte[] { 0b00100, 0b00100, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000 },
        ['('] = new byte[] { 0b00010, 0b00100, 0b01000, 0b01000, 0b01000, 0b00100, 0b00010 },
        [')'] = new byte[] { 0b01000, 0b00100, 0b00010, 0b00010, 0b00010, 0b00100, 0b01000 },
        ['['] = new byte[] { 0b11100, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11100 },
        [']'] = new byte[] { 0b00111, 0b00001, 0b00001, 0b00001, 0b00001, 0b00001, 0b00111 },
        ['#'] = new byte[] { 0b01010, 0b01010, 0b11111, 0b01010, 0b11111, 0b01010, 0b01010 },
        ['$'] = new byte[] { 0b00100, 0b01110, 0b10100, 0b01110, 0b00101, 0b01110, 0b00100 },
        ['%'] = new byte[] { 0b11001, 0b11010, 0b00100, 0b01011, 0b10011, 0b00000, 0b00000 },
        ['&'] = new byte[] { 0b01100, 0b10010, 0b10100, 0b01000, 0b10101, 0b10010, 0b01101 },
        ['*'] = new byte[] { 0b00000, 0b00100, 0b10101, 0b01110, 0b10101, 0b00100, 0b00000 },
        ['+'] = new byte[] { 0b00000, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0b00000 },
        ['-'] = new byte[] { 0b00000, 0b00000, 0b00000, 0b01110, 0b00000, 0b00000, 0b00000 },
        ['='] = new byte[] { 0b00000, 0b00000, 0b11111, 0b00000, 0b11111, 0b00000, 0b00000 },
        ['<'] = new byte[] { 0b00010, 0b00100, 0b01000, 0b10000, 0b01000, 0b00100, 0b00010 },
        ['>'] = new byte[] { 0b01000, 0b00100, 0b00010, 0b00001, 0b00010, 0b00100, 0b01000 },
        ['/'] = new byte[] { 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b00000, 0b00000 },
        ['\\'] = new byte[] { 0b10000, 0b01000, 0b00100, 0b00010, 0b00001, 0b00000, 0b00000 },
        ['.'] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00100, 0b00100 },
        [','] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00100, 0b01000 },
        ['^'] = new byte[] { 0b00100, 0b01010, 0b10001, 0b00000, 0b00000, 0b00000, 0b00000 },
        ['`'] = new byte[] { 0b01000, 0b00100, 0b00010, 0b00000, 0b00000, 0b00000, 0b00000 },
        ['?'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b00000, 0b00100 },
        ['@'] = new byte[] { 0b01110, 0b10001, 0b10111, 0b10101, 0b10111, 0b10000, 0b01110 },
        ['_'] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b11111 },
        ['{'] = new byte[] { 0b00010, 0b00100, 0b00100, 0b01000, 0b00100, 0b00100, 0b00010 },
        ['|'] = new byte[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['}'] = new byte[] { 0b01000, 0b00100, 0b00100, 0b00010, 0b00100, 0b00100, 0b01000 },
        ['~'] = new byte[] { 0b00000, 0b00000, 0b01001, 0b10110, 0b00000, 0b00000, 0b00000 },
        [' '] = new byte[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000 }
    };

    private static readonly Dictionary<char, byte[]> SymbolGlyphs = new()
    {
        ['\u00A1'] = G(0b00100, 0b00000, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100),
        ['\u00BF'] = G(0b00100, 0b00000, 0b00100, 0b01000, 0b10000, 0b10001, 0b01110),
        ['\u00A2'] = G(0b00100, 0b01110, 0b10000, 0b10000, 0b10000, 0b01110, 0b00100),
        ['\u00A3'] = G(0b00110, 0b01000, 0b01000, 0b11110, 0b01000, 0b01000, 0b11111),
        ['\u00A4'] = G(0b01010, 0b11111, 0b10101, 0b10101, 0b11111, 0b01010, 0b00000),
        ['\u00A5'] = G(0b10001, 0b01010, 0b00100, 0b11111, 0b00100, 0b11111, 0b00100),
        ['\u00A6'] = G(0b00100, 0b00100, 0b00000, 0b00100, 0b00100, 0b00000, 0b00000),
        ['\u00A7'] = G(0b01110, 0b10000, 0b01110, 0b00100, 0b01110, 0b00001, 0b11110),
        ['\u00A9'] = G(0b01110, 0b10001, 0b10111, 0b10101, 0b10111, 0b10001, 0b01110),
        ['\u00AE'] = G(0b01110, 0b10001, 0b10111, 0b10101, 0b10110, 0b10001, 0b01110),
        ['\u00AB'] = G(0b00000, 0b01010, 0b00100, 0b00010, 0b00100, 0b01010, 0b00000),
        ['\u00BB'] = G(0b00000, 0b01010, 0b00100, 0b01000, 0b00100, 0b01010, 0b00000),
        ['\u00AC'] = G(0b00000, 0b00000, 0b11111, 0b00001, 0b00001, 0b00000, 0b00000),
        ['\u00AF'] = G(0b11111, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000),
        ['\u00B0'] = G(0b00100, 0b01010, 0b00100, 0b00000, 0b00000, 0b00000, 0b00000),
        ['\u00B1'] = G(0b00100, 0b00100, 0b11111, 0b00100, 0b00000, 0b11111, 0b00000),
        ['\u00B6'] = G(0b11110, 0b10110, 0b10110, 0b11110, 0b10000, 0b10000, 0b10000),
        ['\u00B7'] = G(0b00000, 0b00000, 0b00000, 0b00100, 0b00000, 0b00000, 0b00000),
        ['\u00B8'] = G(0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00100, 0b01000),
        ['\u00D7'] = BaseGlyphs['x'],
        ['\u00F7'] = G(0b00100, 0b00000, 0b11111, 0b00000, 0b00100, 0b00000, 0b00000),
        ['\u00BC'] = G(0b10000, 0b10000, 0b01000, 0b00100, 0b00010, 0b01110, 0b00010),
        ['\u00BD'] = G(0b10000, 0b10000, 0b01000, 0b00100, 0b00010, 0b01110, 0b00001),
        ['\u00BE'] = G(0b11100, 0b00010, 0b01100, 0b00100, 0b00010, 0b01110, 0b00010),
        ['\u2022'] = G(0b00000, 0b00000, 0b00100, 0b01110, 0b00100, 0b00000, 0b00000),
        ['\u2302'] = G(0b00100, 0b01110, 0b11111, 0b10101, 0b10101, 0b11111, 0b00000),
        ['\u263A'] = G(0b01110, 0b10001, 0b10101, 0b10001, 0b10101, 0b10001, 0b01110),
        ['\u263B'] = G(0b01110, 0b11111, 0b11011, 0b11111, 0b11111, 0b11011, 0b01110),
        ['\u263C'] = G(0b00100, 0b10101, 0b01110, 0b11111, 0b01110, 0b10101, 0b00100),
        ['\u2660'] = G(0b00100, 0b01110, 0b11111, 0b01110, 0b00100, 0b01110, 0b00000),
        ['\u2663'] = G(0b00100, 0b01110, 0b00100, 0b11111, 0b00100, 0b01110, 0b00000),
        ['\u2665'] = G(0b01010, 0b11111, 0b11111, 0b11111, 0b01110, 0b00100, 0b00000),
        ['\u2666'] = G(0b00100, 0b01110, 0b11111, 0b11111, 0b01110, 0b00100, 0b00000),
        ['\u266A'] = G(0b00100, 0b00100, 0b00100, 0b01100, 0b10100, 0b01100, 0b00000),
        ['\u266B'] = G(0b01100, 0b01100, 0b01110, 0b01110, 0b11010, 0b11010, 0b00000),
        ['\u25D8'] = G(0b00000, 0b00100, 0b01110, 0b01110, 0b01110, 0b00100, 0b00000),
        ['\u25D9'] = G(0b00000, 0b01110, 0b11111, 0b11111, 0b11111, 0b01110, 0b00000),
        ['\u25CB'] = BaseGlyphs['O'],
        ['\u2640'] = G(0b00000, 0b01110, 0b10001, 0b01110, 0b00100, 0b01110, 0b00100),
        ['\u2642'] = G(0b00111, 0b00101, 0b00111, 0b01110, 0b10001, 0b01110, 0b00000),
        ['\u203C'] = G(0b10101, 0b10101, 0b10101, 0b10101, 0b10101, 0b00000, 0b10101),
        ['\u25AC'] = G(0b00000, 0b00000, 0b11111, 0b11111, 0b11111, 0b00000, 0b00000)
    };

    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Cp437Encoding;

    private readonly Dictionary<char, byte[]> _glyphCache = new(BaseGlyphs);
    private readonly byte[][] _latin1Glyphs;
    private readonly byte[][] _cp437Glyphs;

    public TinyFontCodePage CodePage { get; set; } = TinyFontCodePage.Latin1;

    static TinyBitmapFont()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp437Encoding = Encoding.GetEncoding(437);
    }

    public TinyBitmapFont()
    {
        _latin1Glyphs = BuildGlyphTable(Latin1Encoding);
        _cp437Glyphs = BuildGlyphTable(Cp437Encoding);
    }

    public Encoding GetEncoding()
    {
        return CodePage == TinyFontCodePage.Cp437 ? Cp437Encoding : Latin1Encoding;
    }

    public byte[] GetGlyph(byte code)
    {
        byte[][] glyphs = CodePage == TinyFontCodePage.Cp437 ? _cp437Glyphs : _latin1Glyphs;
        return glyphs[code];
    }

    public int MeasureWidth(string text, int scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int glyphWidth = (GlyphWidth + GlyphSpacing) * scale;
        int count = GetEncoding().GetByteCount(text);
        return count * glyphWidth;
    }

    public int MeasureHeight(int scale)
    {
        return GlyphHeight * scale;
    }

    private static byte[] G(params byte[] rows)
    {
        return rows;
    }

    private byte[][] BuildGlyphTable(Encoding encoding)
    {
        byte[][] table = new byte[256][];
        byte[] buffer = new byte[1];

        for (int i = 0; i < 256; i++)
        {
            buffer[0] = (byte)i;
            string text = encoding.GetString(buffer);
            char c = text.Length > 0 ? text[0] : ' ';
            table[i] = GetGlyph(c);
        }

        return table;
    }

    private byte[] GetGlyph(char c)
    {
        if (_glyphCache.TryGetValue(c, out byte[]? glyph))
        {
            return glyph;
        }

        if (TryCreateGlyph(c, out glyph))
        {
            _glyphCache[c] = glyph;
            return glyph;
        }

        glyph = BaseGlyphs['?'];
        _glyphCache[c] = glyph;
        return glyph;
    }

    private bool TryCreateGlyph(char c, out byte[] glyph)
    {
        if (SymbolGlyphs.TryGetValue(c, out glyph))
        {
            return true;
        }

        if (TryGetLineDrawingGlyph(c, out glyph))
        {
            return true;
        }

        if (TryGetBlockGlyph(c, out glyph))
        {
            return true;
        }

        if (TryGetArrowGlyph(c, out glyph))
        {
            return true;
        }

        if (TryGetGreekGlyph(c, out glyph))
        {
            return true;
        }

        if (TryCreateFromDecomposition(c, out glyph))
        {
            return true;
        }

        if (TryGetCompatibilityGlyph(c, out glyph))
        {
            return true;
        }

        if (TryGetFallbackGlyph(c, out glyph))
        {
            return true;
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    private static bool TryCreateFromDecomposition(char c, out byte[] glyph)
    {
        string decomposed = c.ToString().Normalize(NormalizationForm.FormD);
        if (decomposed.Length <= 1)
        {
            glyph = Array.Empty<byte>();
            return false;
        }

        char baseChar = decomposed[0];
        if (!BaseGlyphs.TryGetValue(baseChar, out byte[]? baseGlyph))
        {
            glyph = Array.Empty<byte>();
            return false;
        }

        byte[] rows = CloneGlyph(baseGlyph);
        bool applied = false;

        for (int i = 1; i < decomposed.Length; i++)
        {
            if (TryApplyCombiningMark(rows, decomposed[i]))
            {
                applied = true;
            }
        }

        if (!applied)
        {
            glyph = Array.Empty<byte>();
            return false;
        }

        glyph = rows;
        return true;
    }

    private static bool TryGetCompatibilityGlyph(char c, out byte[] glyph)
    {
        string compat = c.ToString().Normalize(NormalizationForm.FormKD);
        foreach (char ch in compat)
        {
            if (BaseGlyphs.TryGetValue(ch, out glyph))
            {
                return true;
            }
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    private static bool TryGetFallbackGlyph(char c, out byte[] glyph)
    {
        if (c == '\u00A0')
        {
            glyph = BaseGlyphs[' '];
            return true;
        }

        if (c == '\u0192')
        {
            glyph = BaseGlyphs['f'];
            return true;
        }

        if (c == '\u20A7')
        {
            glyph = BaseGlyphs['P'];
            return true;
        }

        if (c == '\u00DF')
        {
            glyph = BaseGlyphs['B'];
            return true;
        }

        if (c == '\u00AA')
        {
            glyph = BaseGlyphs['a'];
            return true;
        }

        if (c == '\u00BA')
        {
            glyph = BaseGlyphs['o'];
            return true;
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    private static bool TryApplyCombiningMark(byte[] rows, char mark)
    {
        switch (mark)
        {
            case '\u0300':
                ApplyAccent(rows, Accent.Grave);
                return true;
            case '\u0301':
                ApplyAccent(rows, Accent.Acute);
                return true;
            case '\u0302':
                ApplyAccent(rows, Accent.Circumflex);
                return true;
            case '\u0303':
                ApplyAccent(rows, Accent.Tilde);
                return true;
            case '\u0304':
                ApplyAccent(rows, Accent.Macron);
                return true;
            case '\u0308':
                ApplyAccent(rows, Accent.Diaeresis);
                return true;
            case '\u030A':
                ApplyAccent(rows, Accent.Ring);
                return true;
            case '\u0327':
                ApplyAccent(rows, Accent.Cedilla);
                return true;
            case '\u0338':
                ApplySlashOverlay(rows);
                return true;
            default:
                return false;
        }
    }

    private enum Accent
    {
        Grave,
        Acute,
        Circumflex,
        Tilde,
        Diaeresis,
        Ring,
        Macron,
        Cedilla
    }

    private static void ApplyAccent(byte[] rows, Accent accent)
    {
        switch (accent)
        {
            case Accent.Grave:
                rows[0] |= 0b01000;
                break;
            case Accent.Acute:
                rows[0] |= 0b00010;
                break;
            case Accent.Circumflex:
                rows[0] |= 0b01010;
                rows[1] |= 0b00100;
                break;
            case Accent.Tilde:
                rows[0] |= 0b01010;
                break;
            case Accent.Diaeresis:
                rows[0] |= 0b01010;
                break;
            case Accent.Ring:
                rows[0] |= 0b00100;
                rows[1] |= 0b01010;
                break;
            case Accent.Macron:
                rows[0] |= 0b11111;
                break;
            case Accent.Cedilla:
                rows[6] |= 0b00100;
                break;
        }
    }

    private static void ApplySlashOverlay(byte[] rows)
    {
        SetPixel(rows, 4, 0);
        SetPixel(rows, 3, 1);
        SetPixel(rows, 2, 2);
        SetPixel(rows, 2, 3);
        SetPixel(rows, 1, 4);
        SetPixel(rows, 0, 5);
    }

    private static bool TryGetGreekGlyph(char c, out byte[] glyph)
    {
        switch (c)
        {
            case '\u03B1':
                glyph = BaseGlyphs['a'];
                return true;
            case '\u03B2':
                glyph = BaseGlyphs['B'];
                return true;
            case '\u0393':
                glyph = G(0b11111, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000);
                return true;
            case '\u03C0':
                glyph = G(0b00000, 0b11111, 0b10101, 0b10101, 0b10101, 0b00000, 0b00000);
                return true;
            case '\u03A3':
                glyph = BaseGlyphs['E'];
                return true;
            case '\u03C3':
                glyph = BaseGlyphs['o'];
                return true;
            case '\u03C4':
                glyph = BaseGlyphs['t'];
                return true;
            case '\u03A6':
                glyph = G(0b01110, 0b10101, 0b10101, 0b11111, 0b10101, 0b10101, 0b01110);
                return true;
            case '\u0398':
                glyph = G(0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b01110);
                return true;
            case '\u03A9':
                glyph = G(0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b01110);
                return true;
            case '\u03B4':
                glyph = BaseGlyphs['d'];
                return true;
            case '\u03C6':
                glyph = G(0b00100, 0b01110, 0b10101, 0b01110, 0b00100, 0b00000, 0b00000);
                return true;
            case '\u03B5':
                glyph = BaseGlyphs['e'];
                return true;
            case '\u221E':
                glyph = G(0b00000, 0b01110, 0b10001, 0b01110, 0b10001, 0b01110, 0b00000);
                return true;
            case '\u2229':
                glyph = G(0b00000, 0b01110, 0b10001, 0b10001, 0b10001, 0b00000, 0b00000);
                return true;
            case '\u2261':
                glyph = G(0b00000, 0b11111, 0b00000, 0b11111, 0b00000, 0b11111, 0b00000);
                return true;
            case '\u2264':
                glyph = G(0b00010, 0b00100, 0b01000, 0b11111, 0b00000, 0b11111, 0b00000);
                return true;
            case '\u2265':
                glyph = G(0b01000, 0b00100, 0b00010, 0b11111, 0b00000, 0b11111, 0b00000);
                return true;
            case '\u2248':
                glyph = G(0b00000, 0b01010, 0b10101, 0b00000, 0b01010, 0b10101, 0b00000);
                return true;
            case '\u2219':
                glyph = SymbolGlyphs['\u00B7'];
                return true;
            case '\u221A':
                glyph = G(0b00001, 0b00010, 0b00100, 0b10100, 0b01000, 0b01000, 0b01000);
                return true;
            case '\u207F':
                glyph = BaseGlyphs['n'];
                return true;
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    private static bool TryGetArrowGlyph(char c, out byte[] glyph)
    {
        switch (c)
        {
            case '\u2191':
                glyph = G(0b00100, 0b01110, 0b10101, 0b00100, 0b00100, 0b00100, 0b00100);
                return true;
            case '\u2193':
                glyph = G(0b00100, 0b00100, 0b00100, 0b00100, 0b10101, 0b01110, 0b00100);
                return true;
            case '\u2192':
                glyph = G(0b00000, 0b00100, 0b00010, 0b11111, 0b00010, 0b00100, 0b00000);
                return true;
            case '\u2190':
                glyph = G(0b00000, 0b00100, 0b01000, 0b11111, 0b01000, 0b00100, 0b00000);
                return true;
            case '\u2195':
                glyph = G(0b00100, 0b01110, 0b00100, 0b00100, 0b00100, 0b01110, 0b00100);
                return true;
            case '\u2194':
                glyph = G(0b00000, 0b00100, 0b01010, 0b11111, 0b01010, 0b00100, 0b00000);
                return true;
            case '\u21A8':
                glyph = G(0b00100, 0b01110, 0b00100, 0b00100, 0b10101, 0b01110, 0b00100);
                return true;
            case '\u25B2':
                glyph = G(0b00100, 0b01110, 0b11111, 0b11111, 0b00000, 0b00000, 0b00000);
                return true;
            case '\u25BC':
                glyph = G(0b00000, 0b00000, 0b11111, 0b11111, 0b01110, 0b00100, 0b00000);
                return true;
            case '\u25BA':
                glyph = G(0b00100, 0b00110, 0b00111, 0b00111, 0b00110, 0b00100, 0b00000);
                return true;
            case '\u25C4':
                glyph = G(0b00100, 0b01100, 0b11100, 0b11100, 0b01100, 0b00100, 0b00000);
                return true;
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    private static bool TryGetBlockGlyph(char c, out byte[] glyph)
    {
        switch (c)
        {
            case '\u2591':
                glyph = G(0b10101, 0b01010, 0b10101, 0b01010, 0b10101, 0b01010, 0b10101);
                return true;
            case '\u2592':
                glyph = G(0b10101, 0b11111, 0b01010, 0b11111, 0b10101, 0b11111, 0b01010);
                return true;
            case '\u2593':
                glyph = G(0b11111, 0b10101, 0b11111, 0b10101, 0b11111, 0b10101, 0b11111);
                return true;
            case '\u2588':
                glyph = G(0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111);
                return true;
            case '\u2580':
                glyph = G(0b11111, 0b11111, 0b11111, 0b00000, 0b00000, 0b00000, 0b00000);
                return true;
            case '\u2584':
                glyph = G(0b00000, 0b00000, 0b00000, 0b00000, 0b11111, 0b11111, 0b11111);
                return true;
            case '\u258C':
                glyph = G(0b11100, 0b11100, 0b11100, 0b11100, 0b11100, 0b11100, 0b11100);
                return true;
            case '\u2590':
                glyph = G(0b00111, 0b00111, 0b00111, 0b00111, 0b00111, 0b00111, 0b00111);
                return true;
            case '\u25A0':
                glyph = G(0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111, 0b11111);
                return true;
        }

        glyph = Array.Empty<byte>();
        return false;
    }

    [Flags]
    private enum LineMask
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8
    }

    private static bool TryGetLineDrawingGlyph(char c, out byte[] glyph)
    {
        if (!TryGetLineMask(c, out LineMask mask))
        {
            glyph = Array.Empty<byte>();
            return false;
        }

        glyph = BuildLineGlyph(mask);
        return true;
    }

    private static bool TryGetLineMask(char c, out LineMask mask)
    {
        switch (c)
        {
            case '\u2500':
            case '\u2550':
                mask = LineMask.Left | LineMask.Right;
                return true;
            case '\u2502':
            case '\u2551':
                mask = LineMask.Up | LineMask.Down;
                return true;
            case '\u250C':
            case '\u2554':
            case '\u2552':
            case '\u2553':
                mask = LineMask.Right | LineMask.Down;
                return true;
            case '\u2510':
            case '\u2557':
            case '\u2555':
            case '\u2556':
                mask = LineMask.Left | LineMask.Down;
                return true;
            case '\u2514':
            case '\u255A':
            case '\u2558':
            case '\u2559':
                mask = LineMask.Right | LineMask.Up;
                return true;
            case '\u2518':
            case '\u255D':
            case '\u255B':
            case '\u255C':
                mask = LineMask.Left | LineMask.Up;
                return true;
            case '\u251C':
            case '\u2560':
            case '\u255E':
            case '\u255F':
                mask = LineMask.Up | LineMask.Down | LineMask.Right;
                return true;
            case '\u2524':
            case '\u2563':
            case '\u2561':
            case '\u2562':
                mask = LineMask.Up | LineMask.Down | LineMask.Left;
                return true;
            case '\u252C':
            case '\u2566':
            case '\u2564':
            case '\u2565':
                mask = LineMask.Left | LineMask.Right | LineMask.Down;
                return true;
            case '\u2534':
            case '\u2569':
            case '\u2567':
            case '\u2568':
                mask = LineMask.Left | LineMask.Right | LineMask.Up;
                return true;
            case '\u253C':
            case '\u256C':
            case '\u256A':
            case '\u256B':
                mask = LineMask.Left | LineMask.Right | LineMask.Up | LineMask.Down;
                return true;
        }

        mask = LineMask.None;
        return false;
    }

    private static byte[] BuildLineGlyph(LineMask mask)
    {
        byte[] rows = new byte[GlyphHeight];
        const int midRow = 3;
        const int midCol = 2;

        if (mask.HasFlag(LineMask.Up))
        {
            DrawVerticalLine(rows, midCol, 0, midRow);
        }

        if (mask.HasFlag(LineMask.Down))
        {
            DrawVerticalLine(rows, midCol, midRow, GlyphHeight - 1);
        }

        if (mask.HasFlag(LineMask.Left))
        {
            DrawHorizontalLine(rows, midRow, 0, midCol);
        }

        if (mask.HasFlag(LineMask.Right))
        {
            DrawHorizontalLine(rows, midRow, midCol, GlyphWidth - 1);
        }

        return rows;
    }

    private static void DrawHorizontalLine(byte[] rows, int row, int startCol, int endCol)
    {
        for (int col = startCol; col <= endCol; col++)
        {
            SetPixel(rows, col, row);
        }
    }

    private static void DrawVerticalLine(byte[] rows, int col, int startRow, int endRow)
    {
        for (int row = startRow; row <= endRow; row++)
        {
            SetPixel(rows, col, row);
        }
    }

    private static void SetPixel(byte[] rows, int col, int row)
    {
        if (row < 0 || row >= GlyphHeight || col < 0 || col >= GlyphWidth)
        {
            return;
        }

        int mask = 1 << (GlyphWidth - 1 - col);
        rows[row] = (byte)(rows[row] | mask);
    }

    private static byte[] CloneGlyph(byte[] glyph)
    {
        byte[] clone = new byte[glyph.Length];
        Array.Copy(glyph, clone, glyph.Length);
        return clone;
    }
}
