namespace OpenControls.Controls;

public sealed class UiCheckbox : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _checked;

    public string Text { get; set; } = string.Empty;
    public int TextScale { get; set; } = 1;
    public int BoxSize { get; set; } = 16;
    public int Padding { get; set; } = 6;
    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor PressedBackground { get; set; } = new UiColor(22, 26, 36);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor CheckColor { get; set; } = new UiColor(120, 140, 200);
    public UiColor TextColor { get; set; } = UiColor.White;

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

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Enter)
        {
            Toggle();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
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
        UiColor fill = _pressed ? PressedBackground : (_hovered ? HoverBackground : Background);
        context.Renderer.FillRect(box, fill);
        context.Renderer.DrawRect(box, Border, 1);

        if (Checked)
        {
            int inset = Math.Max(2, box.Width / 4);
            UiRect checkRect = new UiRect(box.X + inset, box.Y + inset, box.Width - inset * 2, box.Height - inset * 2);
            context.Renderer.FillRect(checkRect, CheckColor);
        }

        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(Text, new UiPoint(box.Right + Padding, textY), TextColor, TextScale);

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

    private UiRect GetBoxRect()
    {
        int size = Math.Max(4, BoxSize);
        int y = Bounds.Y + (Bounds.Height - size) / 2;
        return new UiRect(Bounds.X, y, size, size);
    }

    private void Toggle()
    {
        SetChecked(!_checked);
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
