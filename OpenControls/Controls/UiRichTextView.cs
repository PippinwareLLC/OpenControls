namespace OpenControls.Controls;

public readonly record struct UiTextRun(string Text, bool Bold, bool Accent);

/// <summary>
/// A word-wrapping paragraph flow with lightweight inline markup for retro
/// text postings: *stars* mark bold runs, _underscores_ mark accent-colored
/// runs, and newlines break paragraphs (a blank line inserts paragraph
/// spacing). Parsing and wrapping are pure static functions over an injected
/// width measurer, so layout is fully testable without a renderer.
/// </summary>
public sealed class UiRichTextView : UiElement
{
    private string _markup = string.Empty;
    private int _textScale = 1;
    private UiColor _textColor = new(215, 220, 235);
    private UiColor _accentColor = new(240, 200, 130);

    public string Markup
    {
        get => _markup;
        set => SetInvalidatingValue(ref _markup, value ?? string.Empty, UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor TextColor
    {
        get => _textColor;
        set => SetInvalidatingValue(ref _textColor, value, UiInvalidationReason.Paint);
    }

    public UiColor AccentColor
    {
        get => _accentColor;
        set => SetInvalidatingValue(ref _accentColor, value, UiInvalidationReason.Paint);
    }

    public int LineHeight => 10 * _textScale + 3;

    /// <summary>Splits markup into styled runs; line breaks become runs of "\n".</summary>
    public static IReadOnlyList<UiTextRun> ParseMarkup(string markup)
    {
        var runs = new List<UiTextRun>();
        bool bold = false;
        bool accent = false;
        var current = new System.Text.StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                runs.Add(new UiTextRun(current.ToString(), bold, accent));
                current.Clear();
            }
        }

        foreach (char character in markup ?? string.Empty)
        {
            switch (character)
            {
                case '*':
                    Flush();
                    bold = !bold;
                    break;
                case '_':
                    Flush();
                    accent = !accent;
                    break;
                case '\n':
                    Flush();
                    runs.Add(new UiTextRun("\n", false, false));
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        Flush();
        return runs;
    }

    /// <summary>
    /// Wraps runs into display lines at word boundaries within
    /// <paramref name="maxWidth"/> as reported by <paramref name="measure"/>.
    /// A word too wide for a whole line is placed alone and overflows rather
    /// than being split mid-word.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<UiTextRun>> WrapRuns(
        IReadOnlyList<UiTextRun> runs, int maxWidth, Func<string, int> measure)
    {
        var lines = new List<IReadOnlyList<UiTextRun>>();
        var line = new List<UiTextRun>();
        int lineWidth = 0;

        void BreakLine()
        {
            lines.Add(line);
            line = [];
            lineWidth = 0;
        }

        foreach (UiTextRun run in runs)
        {
            if (run.Text == "\n")
            {
                BreakLine();
                continue;
            }

            foreach (string word in run.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = lineWidth == 0 ? word : " " + word;
                int width = measure(candidate);
                if (lineWidth > 0 && lineWidth + width > maxWidth)
                {
                    BreakLine();
                    candidate = word;
                    width = measure(candidate);
                }

                if (line.Count > 0 && line[^1].Bold == run.Bold && line[^1].Accent == run.Accent)
                {
                    line[^1] = line[^1] with { Text = line[^1].Text + candidate };
                }
                else
                {
                    line.Add(run with { Text = candidate });
                }

                lineWidth += width;
            }
        }

        if (line.Count > 0)
        {
            BreakLine();
        }

        return lines;
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible || Bounds.Width <= 0)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        IReadOnlyList<IReadOnlyList<UiTextRun>> lines = WrapRuns(
            ParseMarkup(_markup),
            Bounds.Width,
            text => context.Renderer.MeasureTextWidth(text, _textScale, font));

        int y = Bounds.Y;
        foreach (IReadOnlyList<UiTextRun> line in lines)
        {
            if (y + LineHeight > Bounds.Y + Bounds.Height + LineHeight - 1)
            {
                break;
            }

            int x = Bounds.X;
            foreach (UiTextRun run in line)
            {
                UiColor color = run.Accent ? _accentColor : _textColor;
                if (run.Bold)
                {
                    UiRenderHelpers.DrawTextBold(context.Renderer, run.Text, new UiPoint(x, y), color, _textScale, font);
                }
                else
                {
                    context.Renderer.DrawText(run.Text, new UiPoint(x, y), color, _textScale, font);
                }

                x += context.Renderer.MeasureTextWidth(run.Text, _textScale, font);
            }

            y += LineHeight;
        }

        base.Render(context);
    }
}
