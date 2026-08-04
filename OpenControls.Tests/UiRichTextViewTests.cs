using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiRichTextViewTests
{
    private static int SixPerCharacter(string text) => text.Length * 6;

    [Fact]
    public void MarkupSplitsIntoBoldAccentAndPlainRuns()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("*URGENT* haul to _Sarpon_ today");

        Assert.Equal(
        [
            new UiTextRun("URGENT", true, false),
            new UiTextRun(" haul to ", false, false),
            new UiTextRun("Sarpon", false, true),
            new UiTextRun(" today", false, false),
        ], runs);
    }

    [Fact]
    public void NewlinesBecomeExplicitBreakRunsAndUnterminatedStylesStillFlush()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("line one\n*line two");

        Assert.Equal(
        [
            new UiTextRun("line one", false, false),
            new UiTextRun("\n", false, false),
            new UiTextRun("line two", true, false),
        ], runs);
    }

    [Fact]
    public void WrappingBreaksAtWordBoundariesWithinTheMeasuredWidth()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("cargo runs pay well here");

        // 60px at 6px/char fits 10 characters per line.
        IReadOnlyList<IReadOnlyList<UiTextRun>> lines = UiRichTextView.WrapRuns(runs, 60, SixPerCharacter);

        Assert.Equal(["cargo runs", "pay well", "here"], lines.Select(static line => line[0].Text));
    }

    [Fact]
    public void StyledRunsSurviveWrappingAndMergeWhenAdjacentWithTheSameStyle()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("*two words* plain");

        IReadOnlyList<IReadOnlyList<UiTextRun>> lines = UiRichTextView.WrapRuns(runs, 600, SixPerCharacter);

        Assert.Single(lines);
        Assert.Equal(
        [
            new UiTextRun("two words", true, false),
            new UiTextRun(" plain", false, false),
        ], lines[0]);
    }

    [Fact]
    public void AWordWiderThanTheLineStandsAloneInsteadOfSplitting()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("a extraordinarily b");

        IReadOnlyList<IReadOnlyList<UiTextRun>> lines = UiRichTextView.WrapRuns(runs, 30, SixPerCharacter);

        Assert.Equal(["a", "extraordinarily", "b"], lines.Select(static line => line[0].Text));
    }

    [Fact]
    public void ExplicitLineBreaksPreserveEmptyLinesForParagraphSpacing()
    {
        IReadOnlyList<UiTextRun> runs = UiRichTextView.ParseMarkup("head\n\nbody");

        IReadOnlyList<IReadOnlyList<UiTextRun>> lines = UiRichTextView.WrapRuns(runs, 600, SixPerCharacter);

        Assert.Equal(3, lines.Count);
        Assert.Empty(lines[1]);
    }
}
