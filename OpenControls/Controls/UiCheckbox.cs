namespace OpenControls.Controls;

public sealed class UiCheckbox : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _checked;
    private bool _clickedThisFrame;

    public string Text { get; set; } = string.Empty;
    public int TextScale { get; set; } = 1;
    public int BoxSize { get; set; } = 16;
    public int Padding { get; set; } = 6;
    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor PressedBackground { get; set; } = new UiColor(22, 26, 36);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor CheckedBackground { get; set; } = UiColor.Transparent;
    public UiColor CheckedBorder { get; set; } = new UiColor(120, 140, 200);
    public UiColor CheckColor { get; set; } = new UiColor(120, 140, 200);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; } = 3;
    public int CheckThickness { get; set; } = 2;

    public bool Checked
    {
        get => _checked;
        set => SetChecked(value);
    }

    public event Action<bool>? CheckedChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            _clickedThisFrame = true;
            Toggle();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                _clickedThisFrame = true;
                Toggle();
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

        UiRect box = GetBoxRect();
        UiColor stateFill = _pressed ? PressedBackground : (_hovered ? HoverBackground : Background);
        UiColor fill = Checked && CheckedBackground.A > 0 ? CheckedBackground : stateFill;
        UiColor border = Checked ? CheckedBorder : Border;
        UiRenderHelpers.FillRectRounded(context.Renderer, box, CornerRadius, fill);
        UiRenderHelpers.DrawRectRounded(context.Renderer, box, CornerRadius, border, 1);

        if (Checked)
        {
            DrawCheckmark(context.Renderer, box);
        }

        UiFont font = ResolveFont(context.DefaultFont);
        int textY = UiRenderHelpers.GetVerticallyCenteredTextY(Bounds, Text, TextScale, font);
        context.Renderer.DrawText(Text, new UiPoint(box.Right + Padding, textY), TextColor, TextScale, font);

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (_pressed)
        {
            status |= UiItemStatusFlags.Active | UiItemStatusFlags.Pressed;
        }

        if (_clickedThisFrame)
        {
            status |= UiItemStatusFlags.Clicked;
        }

        return status;
    }

    private UiRect GetBoxRect()
    {
        int size = Math.Max(4, BoxSize);
        int y = Bounds.Y + (Bounds.Height - size) / 2;
        return new UiRect(Bounds.X, y, size, size);
    }

    private void DrawCheckmark(IUiRenderer renderer, UiRect box)
    {
        int thickness = Math.Max(1, Math.Min(CheckThickness, Math.Max(1, box.Width / 4)));
        UiPoint start = new(
            box.X + Math.Max(3, box.Width * 4 / 14),
            box.Y + Math.Max(6, box.Height * 8 / 14));
        UiPoint mid = new(
            box.X + Math.Max(5, box.Width * 6 / 14),
            box.Y + Math.Max(8, box.Height * 10 / 14));
        UiPoint end = new(
            box.X + Math.Max(8, box.Width * 11 / 14),
            box.Y + Math.Max(4, box.Height * 4 / 14));

        DrawLine(renderer, start, mid, thickness, CheckColor);
        DrawLine(renderer, mid, end, thickness, CheckColor);
    }

    private static void DrawLine(IUiRenderer renderer, UiPoint start, UiPoint end, int thickness, UiColor color)
    {
        int dx = end.X - start.X;
        int dy = end.Y - start.Y;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        int size = Math.Max(1, thickness);
        int half = size / 2;

        if (steps == 0)
        {
            renderer.FillRect(new UiRect(start.X - half, start.Y - half, size, size), color);
            return;
        }

        for (int step = 0; step <= steps; step++)
        {
            int x = start.X + (dx * step) / steps;
            int y = start.Y + (dy * step) / steps;
            renderer.FillRect(new UiRect(x - half, y - half, size, size), color);
        }
    }

    private void Toggle()
    {
        SetChecked(!_checked);
    }

    public void SetCheckedWithoutNotify(bool value)
    {
        if (_checked == value)
        {
            return;
        }

        _checked = value;
    }

    private void SetChecked(bool value)
    {
        if (_checked == value)
        {
            return;
        }

        _checked = value;
        CheckedChanged?.Invoke(_checked);
    }
}
