namespace OpenControls.Controls;

public sealed class UiInvisibleButton : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _clickedThisFrame;

    public bool IsHovered => _hovered;
    public bool IsPressed => _pressed;
    public bool IsFocused => _focused;

    public event Action? Clicked;

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
            Clicked?.Invoke();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                _clickedThisFrame = true;
                Clicked?.Invoke();
            }

            _pressed = false;
        }

        base.Update(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressed = false;
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
}
