namespace OpenControls.Controls;

/// <summary>
/// A rebinding control: click to listen, and the next key pressed becomes the
/// bound key (Escape cancels). The bound key and listening state render in
/// the button chrome, and <see cref="KeyBound"/> reports each successful
/// capture so hosts can push the binding into their input maps.
/// </summary>
public sealed class UiKeyBindButton : UiElement
{
    private string _label = string.Empty;
    private UiKey _boundKey = UiKey.Unknown;
    private bool _listening;
    private bool _pressed;
    private UiColor _background = new(52, 58, 74);
    private UiColor _listeningBackground = new(96, 74, 44);
    private UiColor _foreground = new(225, 230, 240);

    public event Action<UiKey>? KeyBound;

    public string Label
    {
        get => _label;
        set => SetInvalidatingValue(ref _label, value ?? string.Empty, UiInvalidationReason.Paint);
    }

    public UiKey BoundKey
    {
        get => _boundKey;
        set => SetInvalidatingValue(ref _boundKey, value, UiInvalidationReason.Paint);
    }

    public bool Listening => _listening;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        bool hovered = Bounds.Contains(input.MousePosition);
        if (input.LeftClicked && hovered)
        {
            _pressed = true;
        }

        if (input.LeftReleased)
        {
            if (_pressed && hovered && !_listening)
            {
                _listening = true;
                Invalidate(UiInvalidationReason.Paint);
            }

            _pressed = false;
        }

        if (_listening && input.KeysPressed.Count > 0)
        {
            UiKey captured = input.KeysPressed[0];
            _listening = false;
            if (captured != UiKey.Escape && captured != UiKey.Unknown)
            {
                BoundKey = captured;
                KeyBound?.Invoke(captured);
            }

            Invalidate(UiInvalidationReason.Paint);
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(
            context.Renderer, Bounds, 3, _listening ? _listeningBackground : _background);
        UiFont font = ResolveFont(context.DefaultFont);
        string binding = _listening
            ? "press a key..."
            : _boundKey == UiKey.Unknown ? "unbound" : _boundKey.ToString();
        context.Renderer.DrawText(
            $"{_label}: {binding}",
            new UiPoint(Bounds.X + 6, Bounds.Y + (Bounds.Height - 10) / 2),
            _foreground,
            1,
            font);
        base.Render(context);
    }
}
