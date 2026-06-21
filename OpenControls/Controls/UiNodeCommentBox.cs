namespace OpenControls.Controls;

public sealed class UiNodeCommentBox : UiElement
{
    private UiNodeCommentBoxDebugLayout _debugLayout = UiNodeCommentBoxDebugLayout.Empty;
    private bool _layoutValid;
    private UiRect _layoutBounds;
    private string _layoutTitle = string.Empty;
    private string _layoutText = string.Empty;
    private string _layoutEditingTitleText = string.Empty;
    private string _layoutEditingBodyText = string.Empty;
    private string _layoutFontName = string.Empty;
    private int _layoutFontPixelSize;
    private int _layoutHeaderHeight;
    private int _layoutPadding;
    private int _layoutTextScale;
    private int _layoutCornerRadius;
    private bool _layoutTitleEditing;
    private bool _layoutBodyEditing;
    private bool _layoutEditingCaretVisible;
    private string _title = string.Empty;
    private string _text = string.Empty;
    private string _editingTitleText = string.Empty;
    private string _editingBodyText = string.Empty;
    private bool _isTitleEditing;
    private bool _isBodyEditing;
    private bool _editingCaretVisible = true;
    private int _headerHeight = 30;
    private int _padding = 18;
    private int _textScale = 1;
    private int _cornerRadius = 8;

    public string Title
    {
        get => _title;
        set => SetInvalidatingValue(ref _title, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public string Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiNodeCommentBoxDebugLayout DebugLayout => _debugLayout;

    public UiColor Background { get; set; } = new(54, 42, 78, 102);
    public UiColor HeaderBackground { get; set; } = new(92, 72, 128, 154);
    public UiColor Border { get; set; } = new(176, 148, 232, 186);
    public UiColor EditingBorder { get; set; } = new(92, 184, 255, 245);
    public UiColor TitleColor { get; set; } = new(242, 240, 255);
    public UiColor TextColor { get; set; } = new(232, 232, 242);

    public bool IsTitleEditing
    {
        get => _isTitleEditing;
        set => SetInvalidatingValue(ref _isTitleEditing, value, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public bool IsBodyEditing
    {
        get => _isBodyEditing;
        set => SetInvalidatingValue(ref _isBodyEditing, value, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public bool EditingCaretVisible
    {
        get => _editingCaretVisible;
        set => SetInvalidatingValue(ref _editingCaretVisible, value, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public string EditingTitleText
    {
        get => _editingTitleText;
        set => SetInvalidatingValue(ref _editingTitleText, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public string EditingBodyText
    {
        get => _editingBodyText;
        set => SetInvalidatingValue(ref _editingBodyText, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int HeaderHeight
    {
        get => _headerHeight;
        set => SetInvalidatingValue(ref _headerHeight, Math.Max(24, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int Padding
    {
        get => _padding;
        set => SetInvalidatingValue(ref _padding, Math.Max(8, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetInvalidatingValue(ref _cornerRadius, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UpdateLayout(ResolveFont(context.DefaultFont));
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        UpdateLayout(font);

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        UiRenderHelpers.FillRectTopRounded(context.Renderer, _debugLayout.HeaderBounds, CornerRadius, HeaderBackground);
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        if (IsTitleEditing || IsBodyEditing)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, EditingBorder, 2);
        }

        DrawTitle(context, font);
        DrawBodyText(context, font);

        base.Render(context);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        return null;
    }

    private void UpdateLayout(UiFont font)
    {
        if (_layoutValid
            && _layoutBounds.Equals(Bounds)
            && _layoutHeaderHeight == HeaderHeight
            && _layoutPadding == Padding
            && _layoutTextScale == TextScale
            && _layoutCornerRadius == CornerRadius
            && _layoutFontPixelSize == font.PixelSize
            && string.Equals(_layoutFontName, font.Name, StringComparison.Ordinal)
            && string.Equals(_layoutTitle, Title, StringComparison.Ordinal)
            && string.Equals(_layoutText, Text, StringComparison.Ordinal)
            && _layoutTitleEditing == IsTitleEditing
            && _layoutBodyEditing == IsBodyEditing
            && _layoutEditingCaretVisible == EditingCaretVisible
            && string.Equals(_layoutEditingTitleText, EditingTitleText, StringComparison.Ordinal)
            && string.Equals(_layoutEditingBodyText, EditingBodyText, StringComparison.Ordinal))
        {
            return;
        }

        int padding = Math.Max(8, Padding);
        int scale = Math.Max(1, TextScale);
        int textHeight = font.MeasureTextHeight(scale);
        string titleText = ResolveTitleRenderText();
        string bodyText = ResolveBodyRenderText();
        int headerHeight = Math.Min(
            Math.Max(0, Bounds.Height),
            Math.Max(HeaderHeight, textHeight + Math.Max(8, padding / 2)));
        UiRect headerBounds = new(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
        UiRect titleClipBounds = new(
            headerBounds.X + padding,
            headerBounds.Y,
            Math.Max(0, headerBounds.Width - padding * 2),
            headerBounds.Height);
        UiRect titleBounds = titleClipBounds.Width <= 0 || titleClipBounds.Height <= 0
            ? default
            : new UiRect(
                titleClipBounds.X,
                UiRenderHelpers.GetVerticallyCenteredTextY(titleClipBounds, titleText, scale, font),
                Math.Min(titleClipBounds.Width, font.MeasureTextWidth(titleText, scale)),
                textHeight);

        UiRect bodyBounds = new(
            Bounds.X + padding,
            Bounds.Y + headerHeight + Math.Max(8, padding / 2),
            Math.Max(0, Bounds.Width - padding * 2),
            Math.Max(0, Bounds.Height - headerHeight - padding - Math.Max(8, padding / 2)));
        UiRect[] lineBounds = BuildBodyLineBounds(bodyText, bodyBounds, font, scale, textHeight, Math.Max(3, padding / 4));
        UiRect bodyTextBounds = Union(lineBounds);

        _debugLayout = new UiNodeCommentBoxDebugLayout(Bounds, headerBounds, titleBounds, bodyBounds, bodyTextBounds, lineBounds);
        _layoutValid = true;
        _layoutBounds = Bounds;
        _layoutTitle = Title;
        _layoutText = Text;
        _layoutEditingTitleText = EditingTitleText;
        _layoutEditingBodyText = EditingBodyText;
        _layoutFontName = font.Name;
        _layoutFontPixelSize = font.PixelSize;
        _layoutHeaderHeight = HeaderHeight;
        _layoutPadding = Padding;
        _layoutTextScale = TextScale;
        _layoutCornerRadius = CornerRadius;
        _layoutTitleEditing = IsTitleEditing;
        _layoutBodyEditing = IsBodyEditing;
        _layoutEditingCaretVisible = EditingCaretVisible;
    }

    private void DrawTitle(UiRenderContext context, UiFont font)
    {
        if (_debugLayout.TitleBounds.Width <= 0 || _debugLayout.TitleBounds.Height <= 0)
        {
            return;
        }

        string drawText = UiRenderHelpers.BuildElidedText(ResolveTitleRenderText(), _debugLayout.TitleBounds.Width, TextScale, font);
        context.Renderer.PushClip(_debugLayout.HeaderBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.TitleBounds.X, _debugLayout.TitleBounds.Y), TitleColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawBodyText(UiRenderContext context, UiFont font)
    {
        if (_debugLayout.BodyBounds.Width <= 0 || _debugLayout.BodyBounds.Height <= 0 || _debugLayout.BodyLineBounds.Count == 0)
        {
            return;
        }

        string[] lines = WrapLines(ResolveBodyRenderText(), _debugLayout.BodyBounds.Width, font, TextScale, _debugLayout.BodyLineBounds.Count);
        context.Renderer.PushClip(_debugLayout.BodyBounds);
        for (int i = 0; i < lines.Length && i < _debugLayout.BodyLineBounds.Count; i++)
        {
            UiRect bounds = _debugLayout.BodyLineBounds[i];
            string drawText = UiRenderHelpers.BuildElidedText(lines[i], bounds.Width, TextScale, font);
            context.Renderer.DrawText(drawText, new UiPoint(bounds.X, bounds.Y), TextColor, TextScale, font);
        }

        context.Renderer.PopClip();
    }

    private string ResolveTitleText()
    {
        return IsTitleEditing ? EditingTitleText : Title;
    }

    private string ResolveBodyText()
    {
        return IsBodyEditing ? EditingBodyText : Text;
    }

    private string ResolveTitleRenderText()
    {
        string text = ResolveTitleText();
        return IsTitleEditing && EditingCaretVisible ? text + "|" : text;
    }

    private string ResolveBodyRenderText()
    {
        string text = ResolveBodyText();
        return IsBodyEditing && EditingCaretVisible ? text + "|" : text;
    }

    private static UiRect[] BuildBodyLineBounds(string text, UiRect bodyBounds, UiFont font, int scale, int textHeight, int lineGap)
    {
        if (string.IsNullOrWhiteSpace(text) || bodyBounds.Width <= 0 || bodyBounds.Height <= 0)
        {
            return Array.Empty<UiRect>();
        }

        int linePitch = Math.Max(textHeight + Math.Max(0, lineGap), 1);
        int maxLines = Math.Max(1, (bodyBounds.Height + Math.Max(0, lineGap)) / linePitch);
        string[] lines = WrapLines(text, bodyBounds.Width, font, scale, maxLines);
        UiRect[] bounds = new UiRect[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            int lineY = bodyBounds.Y + i * linePitch;
            int lineWidth = Math.Min(bodyBounds.Width, font.MeasureTextWidth(lines[i], scale));
            bounds[i] = new UiRect(bodyBounds.X, lineY, lineWidth, Math.Min(textHeight, Math.Max(0, bodyBounds.Bottom - lineY)));
        }

        return bounds;
    }

    private static string[] WrapLines(string text, int maxWidth, UiFont font, int scale, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0 || maxLines <= 0)
        {
            return Array.Empty<string>();
        }

        List<string> lines = new();
        foreach (string paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            AddWrappedParagraph(paragraph, maxWidth, font, scale, maxLines, lines);
            if (lines.Count >= maxLines)
            {
                break;
            }
        }

        if (lines.Count > maxLines)
        {
            lines.RemoveRange(maxLines, lines.Count - maxLines);
        }

        return lines.ToArray();
    }

    private static void AddWrappedParagraph(string paragraph, int maxWidth, UiFont font, int scale, int maxLines, List<string> lines)
    {
        string trimmed = paragraph.Trim();
        if (trimmed.Length == 0)
        {
            if (lines.Count < maxLines)
            {
                lines.Add("");
            }

            return;
        }

        string current = string.Empty;
        foreach (string word in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (font.MeasureTextWidth(candidate, scale) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
                if (lines.Count >= maxLines)
                {
                    return;
                }
            }

            current = word;
        }

        if (current.Length > 0 && lines.Count < maxLines)
        {
            lines.Add(current);
        }
    }

    private static UiRect Union(IReadOnlyList<UiRect> rects)
    {
        if (rects.Count == 0)
        {
            return default;
        }

        int left = rects[0].Left;
        int top = rects[0].Top;
        int right = rects[0].Right;
        int bottom = rects[0].Bottom;
        for (int i = 1; i < rects.Count; i++)
        {
            UiRect rect = rects[i];
            left = Math.Min(left, rect.Left);
            top = Math.Min(top, rect.Top);
            right = Math.Max(right, rect.Right);
            bottom = Math.Max(bottom, rect.Bottom);
        }

        return new UiRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}

public readonly struct UiNodeCommentBoxDebugLayout
{
    public UiNodeCommentBoxDebugLayout(
        UiRect bounds,
        UiRect headerBounds,
        UiRect titleBounds,
        UiRect bodyBounds,
        UiRect bodyTextBounds,
        IReadOnlyList<UiRect> bodyLineBounds)
    {
        Bounds = bounds;
        HeaderBounds = headerBounds;
        TitleBounds = titleBounds;
        BodyBounds = bodyBounds;
        BodyTextBounds = bodyTextBounds;
        BodyLineBounds = bodyLineBounds ?? Array.Empty<UiRect>();
    }

    public UiRect Bounds { get; }
    public UiRect HeaderBounds { get; }
    public UiRect TitleBounds { get; }
    public UiRect BodyBounds { get; }
    public UiRect BodyTextBounds { get; }
    public IReadOnlyList<UiRect> BodyLineBounds { get; }

    public static UiNodeCommentBoxDebugLayout Empty => new(default, default, default, default, default, Array.Empty<UiRect>());
}
