namespace OpenControls.Controls;

/// <summary>
/// A clickable button carrying a 16x16 one-bit pixel icon beside optional
/// text: each icon row is a 16-bit mask drawn as filled pixels in the
/// foreground color, so icons stay crisp, palette-bound, and data-only in
/// the retro style — no textures involved.
/// </summary>
public sealed class UiIconButton : UiElement
{
    public const int IconPixels = 16;

    private ushort[] _iconRows = new ushort[IconPixels];
    private string _text = string.Empty;
    private int _pixelScale = 1;
    private UiColor _background = new(52, 58, 74);
    private UiColor _hoverBackground = new(66, 74, 94);
    private UiColor _pressedBackground = new(40, 45, 58);
    private UiColor _foreground = new(225, 230, 240);
    private bool _hovered;
    private bool _pressed;

    public event Action? Clicked;

    public string Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value ?? string.Empty, UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    /// <summary>Icon bitmap: sixteen 16-bit rows, most significant bit leftmost.</summary>
    public IReadOnlyList<ushort> IconRows => _iconRows;

    public int PixelScale
    {
        get => _pixelScale;
        set => SetInvalidatingValue(ref _pixelScale, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor Foreground
    {
        get => _foreground;
        set => SetInvalidatingValue(ref _foreground, value, UiInvalidationReason.Paint);
    }

    public UiColor Background
    {
        get => _background;
        set => SetInvalidatingValue(ref _background, value, UiInvalidationReason.Paint);
    }

    public void SetIcon(ushort[] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Length != IconPixels)
        {
            throw new ArgumentException($"Icons are {IconPixels} rows.", nameof(rows));
        }

        _iconRows = (ushort[])rows.Clone();
        Invalidate(UiInvalidationReason.Paint);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);
        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                Clicked?.Invoke();
            }

            _pressed = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor fill = _pressed ? _pressedBackground : _hovered ? _hoverBackground : _background;
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, 3, fill);

        int iconSize = IconPixels * _pixelScale;
        int iconX = Bounds.X + 4;
        int iconY = Bounds.Y + (Bounds.Height - iconSize) / 2;
        for (int row = 0; row < IconPixels; row++)
        {
            ushort mask = _iconRows[row];
            for (int column = 0; column < IconPixels; column++)
            {
                if ((mask & (1 << (IconPixels - 1 - column))) != 0)
                {
                    UiRenderHelpers.FillRectRounded(
                        context.Renderer,
                        new UiRect(
                            iconX + column * _pixelScale,
                            iconY + row * _pixelScale,
                            _pixelScale,
                            _pixelScale),
                        0,
                        _foreground);
                }
            }
        }

        if (_text.Length > 0)
        {
            UiFont font = ResolveFont(context.DefaultFont);
            context.Renderer.DrawText(
                _text,
                new UiPoint(iconX + iconSize + 5, Bounds.Y + (Bounds.Height - 10) / 2),
                _foreground,
                1,
                font);
        }

        base.Render(context);
    }
}

/// <summary>
/// A horizontal strip of icon buttons with uniform sizing: add buttons, and
/// the toolbar lays them out left to right with a fixed gap inside its own
/// bounds during layout.
/// </summary>
public sealed class UiToolbar : UiElement
{
    private readonly List<UiIconButton> _buttons = [];
    private int _buttonWidth = 96;
    private int _gap = 6;

    public IReadOnlyList<UiIconButton> Buttons => _buttons;

    public int ButtonWidth
    {
        get => _buttonWidth;
        set => SetInvalidatingValue(ref _buttonWidth, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int Gap
    {
        get => _gap;
        set => SetInvalidatingValue(ref _gap, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiIconButton AddButton(string text, ushort[] iconRows, Action? clicked = null)
    {
        var button = new UiIconButton { Text = text };
        button.SetIcon(iconRows);
        if (clicked is not null)
        {
            button.Clicked += clicked;
        }

        _buttons.Add(button);
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        return button;
    }

    private void LayoutButtons()
    {
        int x = Bounds.X;
        foreach (UiIconButton button in _buttons)
        {
            button.Bounds = new UiRect(x, Bounds.Y, _buttonWidth, Bounds.Height);
            x += _buttonWidth + _gap;
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutButtons();
        foreach (UiIconButton button in _buttons)
        {
            button.Update(context);
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutButtons();
        foreach (UiIconButton button in _buttons)
        {
            button.Render(context);
        }

        base.Render(context);
    }
}
