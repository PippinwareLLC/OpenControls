using Xunit;

namespace OpenControls.Tests;

/// <summary>
/// The Latin App Store expansion: every character of every target language's
/// alphabet renders a real glyph (no '?' boxes), and the new marks are
/// actually visible - a mark OR'd into occupied pixels would silently
/// disappear at 5x7, so the load-bearing pairs must differ pixel-for-pixel.
/// Capitals included: the old "uppercase accents merge into full-width top
/// rows" convention was overruled (Tom, 2026-07-12 - the ALL-CAPS chrome is
/// most of the UI), so tall letterforms now squash to a 5-row body and the
/// mark rides the freed rows.
/// </summary>
public sealed class TinyBitmapFontLatinExpansionTests
{
    private static readonly (string Language, string Alphabet)[] Alphabets =
    {
        ("pt-BR/pt-PT", "áâãàçéêíóôõúÁÂÃÀÇÉÊÍÓÔÕÚ"),
        ("nl", "áéíóúëïöüÁÉÍÓÚËÏÖÜ"),
        ("sv", "åäöÅÄÖ"),
        ("da/no", "æøåÆØÅ"),
        ("fi", "äöåšžÄÖÅŠŽ"),
        ("pl", "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ"),
        ("tr", "çğıöşüÇĞİÖŞÜ"),
        ("ro", "ăâîșțĂÂÎȘȚ"),
        ("hu", "áéíóöőúüűÁÉÍÓÖŐÚÜŰ"),
        ("cs", "áčďéěíňóřšťúůýžÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ"),
        ("sk", "áäčďéíĺľňóôŕšťúýžÁÄČĎÉÍĹĽŇÓÔŔŠŤÚÝŽ"),
        ("hr", "čćđšžČĆĐŠŽ"),
        ("ca", "àçéèíïóòúü·ÀÇÉÈÍÏÓÒÚÜ"),
        // id/ms are pure ASCII - nothing beyond the base table.
        // Typographic punctuation the translations quote natively with:
        ("punctuation", "‘’‚“”„–—…«»")
    };

    [Fact]
    public void Every_target_language_character_has_a_real_glyph()
    {
        TinyBitmapFont font = new();
        byte[] questionMark = font.GetGlyph('?');

        foreach ((string language, string alphabet) in Alphabets)
        {
            foreach (char c in alphabet)
            {
                byte[] glyph = font.GetGlyph(c);
                Assert.False(
                    ReferenceEquals(glyph, questionMark),
                    $"{language}: '{c}' (U+{(int)c:X4}) fell back to the '?' box");
            }
        }
    }

    [Theory]
    // Stroked/dotless hand glyphs (no NFD decomposition exists for these).
    [InlineData('ø', 'o')]
    [InlineData('Ø', 'O')]
    [InlineData('ł', 'l')]
    [InlineData('Ł', 'L')]
    [InlineData('đ', 'd')]
    [InlineData('Đ', 'D')]
    [InlineData('ı', 'i')]
    // Caron-as-apostrophe hand glyphs for tall letters.
    [InlineData('ď', 'd')]
    [InlineData('ť', 't')]
    [InlineData('ľ', 'l')]
    // Combining marks with top-row room on lowercase.
    [InlineData('č', 'c')]
    [InlineData('š', 's')]
    [InlineData('ž', 'z')]
    [InlineData('ě', 'e')]
    [InlineData('ř', 'r')]
    [InlineData('ň', 'n')]
    [InlineData('ż', 'z')]
    [InlineData('ğ', 'g')]
    [InlineData('ă', 'a')]
    [InlineData('ů', 'u')]
    // Bottom-row marks must land on pixels the letter bottom leaves free.
    [InlineData('ș', 's')]
    [InlineData('ț', 't')]
    [InlineData('ą', 'a')]
    [InlineData('ę', 'e')]
    // Bottom marks whose pixels the letter USED to own: the letterform
    // moves out of the way now instead of swallowing the mark.
    [InlineData('ç', 'c')]
    [InlineData('Ç', 'C')]
    [InlineData('Ę', 'E')]
    [InlineData('Ą', 'A')]
    [InlineData('Ș', 'S')]
    [InlineData('Ț', 'T')]
    [InlineData('ş', 's')]
    [InlineData('Ş', 'S')]
    // Capitals: the full-height letterform squashes to make headroom, so
    // every accented capital reads as its own character on ALL-CAPS chrome.
    [InlineData('Č', 'C')]
    [InlineData('Š', 'S')]
    [InlineData('Ž', 'Z')]
    [InlineData('Ř', 'R')]
    [InlineData('Ď', 'D')]
    [InlineData('Ť', 'T')]
    [InlineData('Ň', 'N')]
    [InlineData('Ě', 'E')]
    [InlineData('É', 'E')]
    [InlineData('Á', 'A')]
    [InlineData('Í', 'I')]
    [InlineData('Ó', 'O')]
    [InlineData('Ú', 'U')]
    [InlineData('Ů', 'U')]
    [InlineData('Ý', 'Y')]
    [InlineData('Ä', 'A')]
    [InlineData('Ö', 'O')]
    [InlineData('Ü', 'U')]
    [InlineData('Å', 'A')]
    [InlineData('Ő', 'O')]
    [InlineData('Ű', 'U')]
    [InlineData('Ã', 'A')]
    [InlineData('Õ', 'O')]
    [InlineData('Ê', 'E')]
    [InlineData('Â', 'A')]
    [InlineData('Ă', 'A')]
    [InlineData('İ', 'I')]
    [InlineData('Ğ', 'G')]
    [InlineData('Ń', 'N')]
    [InlineData('Ś', 'S')]
    [InlineData('Ź', 'Z')]
    [InlineData('Ż', 'Z')]
    public void Marked_letters_differ_from_their_bases(char marked, char baseChar)
    {
        TinyBitmapFont font = new();
        Assert.False(
            font.GetGlyph(marked).AsSpan().SequenceEqual(font.GetGlyph(baseChar)),
            $"'{marked}' (U+{(int)marked:X4}) renders identically to '{baseChar}'");
    }

    [Fact]
    public void Uppercase_caron_overhangs_a_full_height_capital()
    {
        // The Czech Č keeps the COMPLETE 7-row C - a squashed capital reads
        // as lowercase (Tom, 2026-07-12) - and the caron rides two overhang
        // rows above it, the way print accents exceed cap height.
        TinyBitmapFont font = new();
        byte[] rows = font.GetGlyph('Č');
        Assert.Equal(TinyBitmapFont.GlyphHeight + TinyBitmapFont.AboveOverhangRows, rows.Length);
        Assert.Equal(0b01010, rows[0]);
        Assert.Equal(0b00100, rows[1]);
        Assert.True(rows.AsSpan(TinyBitmapFont.AboveOverhangRows).SequenceEqual(font.GetGlyph('C')),
            "the letterform under the caron must be the untouched capital C");
    }

    [Fact]
    public void Capital_cedilla_hook_hangs_below_the_full_letter()
    {
        // Ç keeps the complete C; the hook takes a bottom-extended row in
        // descender space.
        TinyBitmapFont font = new();
        byte[] rows = font.GetGlyph('Ç');
        Assert.Equal(TinyBitmapFont.GlyphHeight + TinyBitmapFont.BelowOverhangRows, rows.Length);
        Assert.True(rows.AsSpan(0, TinyBitmapFont.GlyphHeight).SequenceEqual(font.GetGlyph('C')),
            "the letterform above the hook must be the untouched capital C");
        Assert.Equal(0b00100, rows[^1]);
    }

    [Theory]
    // The Hungarian double acute must not collapse into the diaeresis...
    [InlineData('ő', 'ö')]
    [InlineData('ű', 'ü')]
    // ...and the Romanian breve must not collapse into the circumflex.
    [InlineData('ă', 'â')]
    public void Distinct_marks_stay_distinct(char left, char right)
    {
        TinyBitmapFont font = new();
        Assert.False(
            font.GetGlyph(left).AsSpan().SequenceEqual(font.GetGlyph(right)),
            $"'{left}' (U+{(int)left:X4}) renders identically to '{right}' (U+{(int)right:X4})");
    }
}
