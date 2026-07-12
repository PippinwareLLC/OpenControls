using Xunit;

namespace OpenControls.Tests;

/// <summary>
/// The Latin App Store expansion: every character of every target language's
/// alphabet renders a real glyph (no '?' boxes), and the new marks are
/// actually visible - a mark OR'd into occupied pixels would silently
/// disappear at 5x7, so the load-bearing pairs must differ pixel-for-pixel.
/// Uppercase accents merge into full-width top rows by house convention
/// (E and É share a top bar), so distinctness is asserted where the cell
/// has room: lowercase forms and the stroked hand glyphs.
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
    public void Marked_letters_differ_from_their_bases(char marked, char baseChar)
    {
        TinyBitmapFont font = new();
        Assert.False(
            font.GetGlyph(marked).AsSpan().SequenceEqual(font.GetGlyph(baseChar)),
            $"'{marked}' (U+{(int)marked:X4}) renders identically to '{baseChar}'");
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
