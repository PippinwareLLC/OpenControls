using Xunit;

namespace OpenControls.Tests;

/// <summary>
/// The tiny font carries the whole EFIGS character set (no '?' boxes in
/// French/Italian/German/Spanish copy) and its descender letters keep
/// their tails inside the 7-row cell instead of clipping.
/// </summary>
public sealed class TinyBitmapFontEfigsTests
{
    [Fact]
    public void Every_efigs_character_has_a_real_glyph()
    {
        const string Efigs =
            "àâäæçéèêëîïôöœùûüÿÀÂÄÇÉÈÊËÎÏÔÖŒÙÛÜ" + // French
            "áéíóúüñÁÉÍÓÚÜÑ¡¿" +                   // Spanish
            "àèéìíîòóùúÀÈÉÌÍÎÒÓÙÚ" +               // Italian
            "äöüßÄÖÜ" +                             // German
            "€";
        TinyBitmapFont font = new();
        byte[] questionMark = font.GetGlyph('?');

        foreach (char c in Efigs.Distinct())
        {
            byte[] glyph = font.GetGlyph(c);
            Assert.False(
                ReferenceEquals(glyph, questionMark),
                $"'{c}' (U+{(int)c:X4}) fell back to the '?' box");
        }
    }

    [Fact]
    public void Descenders_keep_their_tails_inside_the_cell()
    {
        TinyBitmapFont font = new();

        // g and y end in a real hook (two or more pixels on the final row),
        // not a single floating pixel that reads as a clipped tail.
        foreach (char c in "gy")
        {
            byte lastRow = font.GetGlyph(c)[^1];
            Assert.True(
                System.Numerics.BitOperations.PopCount(lastRow) >= 2,
                $"'{c}' should end in a hook, got row {lastRow:B5}");
        }

        // p and q end in a stem that reaches the cell's bottom row.
        foreach (char c in "pq")
        {
            Assert.NotEqual(0, font.GetGlyph(c)[^1]);
        }
    }
}
