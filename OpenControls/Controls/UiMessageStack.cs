namespace OpenControls.Controls;

public enum UiMessageSeverity
{
    Info,
    Warning,
    Alert,
}

public readonly record struct UiMessageEntry(string Prefix, string Text, UiMessageSeverity Severity);

/// <summary>
/// A bounded rolling message log: pushed messages display oldest-first, the
/// oldest entries fall off past <see cref="MaxMessages"/>, consecutive
/// duplicates are suppressed by message text (prefixes such as timestamps are
/// stored separately so they never defeat the comparison), and each severity
/// renders in its own color. State is exposed through <see cref="Messages"/>
/// so hosts and tests can consume the log headlessly.
/// </summary>
public sealed class UiMessageStack : UiElement
{
    private readonly List<UiMessageEntry> _messages = [];
    private int _maxMessages = 3;
    private int _textScale = 1;
    private UiColor _infoColor = new(150, 165, 190);
    private UiColor _warningColor = new(235, 200, 120);
    private UiColor _alertColor = new(240, 130, 110);

    public event Action<UiMessageEntry>? MessagePushed;

    public IReadOnlyList<UiMessageEntry> Messages => _messages;

    public bool SuppressConsecutiveDuplicates { get; set; } = true;

    public int MaxMessages
    {
        get => _maxMessages;
        set
        {
            SetInvalidatingValue(ref _maxMessages, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
            TrimOverflow();
        }
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int RowHeight => 10 * _textScale + 4;

    public bool Push(string message, UiMessageSeverity severity = UiMessageSeverity.Info, string prefix = "")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (SuppressConsecutiveDuplicates
            && _messages.Count > 0
            && string.Equals(_messages[^1].Text, message, StringComparison.Ordinal))
        {
            return false;
        }

        var entry = new UiMessageEntry(prefix, message, severity);
        _messages.Add(entry);
        TrimOverflow();
        Invalidate(UiInvalidationReason.Paint);
        MessagePushed?.Invoke(entry);
        return true;
    }

    public void Clear()
    {
        _messages.Clear();
        Invalidate(UiInvalidationReason.Paint);
    }

    private void TrimOverflow()
    {
        while (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(0);
        }
    }

    private UiColor ColorFor(UiMessageSeverity severity) => severity switch
    {
        UiMessageSeverity.Warning => _warningColor,
        UiMessageSeverity.Alert => _alertColor,
        _ => _infoColor,
    };

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        int y = Bounds.Y;
        foreach (UiMessageEntry entry in _messages)
        {
            if (y + RowHeight > Bounds.Y + Bounds.Height + RowHeight - 1)
            {
                break;
            }

            context.Renderer.DrawText(
                entry.Prefix + entry.Text,
                new UiPoint(Bounds.X, y + 2),
                ColorFor(entry.Severity),
                _textScale,
                font);
            y += RowHeight;
        }

        base.Render(context);
    }
}
