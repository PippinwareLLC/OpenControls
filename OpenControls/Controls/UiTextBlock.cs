namespace OpenControls.Controls;

public sealed class UiTextBlock : UiElement
{
    private string _text = string.Empty;
    private UiColor _color = UiColor.White;
    private int _scale = 1;
    private bool _bold;
    private bool _wrap = true;
    private bool _clipToBounds = true;
    private int _lineSpacing;
    private int _padding;

    public string Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor Color
    {
        get => _color;
        set => SetInvalidatingValue(ref _color, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int Scale
    {
        get => _scale;
        set => SetInvalidatingValue(ref _scale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public bool Bold
    {
        get => _bold;
        set => SetInvalidatingValue(ref _bold, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public bool Wrap
    {
        get => _wrap;
        set => SetInvalidatingValue(ref _wrap, value, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public bool ClipToBounds
    {
        get => _clipToBounds;
        set => SetInvalidatingValue(ref _clipToBounds, value, UiInvalidationReason.Clip | UiInvalidationReason.Paint);
    }

    public int LineSpacing
    {
        get => _lineSpacing;
        set => SetInvalidatingValue(ref _lineSpacing, Math.Max(0, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int Padding
    {
        get => _padding;
        set => SetInvalidatingValue(ref _padding, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public int LastLineCount { get; private set; }
    public int LastMeasuredHeight { get; private set; }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        int padding = Math.Max(0, Padding);
        int x = Bounds.X + padding;
        int y = Bounds.Y + padding;
        int width = Math.Max(0, Bounds.Width - padding * 2);
        UiFont font = ResolveFont(context.DefaultFont);

        List<string> lines = BuildLines(Text, width, context.Renderer, font);
        int lineHeight = context.Renderer.MeasureTextHeight(Scale, font);
        int lineSpacing = Math.Max(0, LineSpacing);

        LastLineCount = lines.Count;
        LastMeasuredHeight = lines.Count == 0 ? 0 : lines.Count * lineHeight + (lines.Count - 1) * lineSpacing;

        bool shouldClip = ShouldClipTextBounds(lines, width, context.Renderer, font);
        if (shouldClip)
        {
            context.Renderer.PushClip(Bounds);
        }

        int lineY = y;
        foreach (string line in lines)
        {
            UiPoint position = new UiPoint(x, lineY);
            if (Bold)
            {
                UiRenderHelpers.DrawTextBold(context.Renderer, line, position, Color, Scale, font);
            }
            else
            {
                context.Renderer.DrawText(line, position, Color, Scale, font);
            }

            lineY += lineHeight + lineSpacing;
            if (shouldClip && lineY > Bounds.Bottom)
            {
                break;
            }
        }

        if (shouldClip)
        {
            context.Renderer.PopClip();
        }

        base.Render(context);
    }

    private bool ShouldClipTextBounds(IReadOnlyList<string> lines, int availableWidth, IUiRenderer renderer, UiFont font)
    {
        if (!ClipToBounds)
        {
            return false;
        }

        int padding = Math.Max(0, Padding);
        int availableHeight = Math.Max(0, Bounds.Height - padding * 2);
        if (availableHeight <= 0)
        {
            return true;
        }

        if (LastMeasuredHeight > availableHeight)
        {
            return true;
        }

        if (availableWidth <= 0)
        {
            return true;
        }

        if (Wrap)
        {
            return false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (renderer.MeasureTextWidth(lines[i], Scale, font) > availableWidth)
            {
                return true;
            }
        }

        return false;
    }

    private List<string> BuildLines(string text, int maxWidth, IUiRenderer renderer, UiFont font)
    {
        List<string> lines = new();
        if (string.IsNullOrEmpty(text))
        {
            return lines;
        }

        string[] rawLines = text.Replace("\r", string.Empty).Split('\n');
        foreach (string raw in rawLines)
        {
            if (!Wrap || maxWidth <= 0)
            {
                lines.Add(raw);
                continue;
            }

            foreach (string line in WrapLine(raw, maxWidth, renderer, font))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private IEnumerable<string> WrapLine(string line, int maxWidth, IUiRenderer renderer, UiFont font)
    {
        if (string.IsNullOrEmpty(line))
        {
            yield return string.Empty;
            yield break;
        }

        int start = 0;
        int lastBreak = -1;
        int index = 0;
        while (index < line.Length)
        {
            if (char.IsWhiteSpace(line[index]))
            {
                lastBreak = index;
            }

            int length = index - start + 1;
            if (length > 0)
            {
                string slice = line.Substring(start, length);
                int width = renderer.MeasureTextWidth(slice, Scale, font);
                if (width > maxWidth)
                {
                    int breakIndex = lastBreak >= start ? lastBreak : index;
                    int breakLength = breakIndex - start;
                    if (breakLength <= 0)
                    {
                        breakLength = 1;
                    }

                    yield return line.Substring(start, breakLength);

                    start += breakLength;
                    while (start < line.Length && char.IsWhiteSpace(line[start]))
                    {
                        start++;
                    }

                    index = start;
                    lastBreak = -1;
                    continue;
                }
            }

            index++;
        }

        if (start <= line.Length)
        {
            yield return line.Substring(start);
        }
    }
}
