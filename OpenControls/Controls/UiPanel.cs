namespace OpenControls.Controls;

public sealed class UiPanel : UiElement
{
    private bool _resizing;
    private UiPoint _resizeStart;
    private UiRect _resizeStartBounds;

    public UiPanel()
    {
        ClipChildren = true;
    }

    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public bool AllowResize { get; set; }
    public bool ShowResizeGrip { get; set; } = true;
    public int ResizeGripSize { get; set; } = 12;
    public UiPoint MinSize { get; set; } = new(40, 40);
    public UiPoint MaxSize { get; set; } = new(int.MaxValue, int.MaxValue);
    public bool ClampResizeToParent { get; set; } = true;
    public UiColor ResizeGripColor { get; set; } = new(90, 100, 120);

    public bool IsResizing => _resizing;

    public UiRect ResizeGripBounds
    {
        get
        {
            int size = Math.Max(1, ResizeGripSize);
            return new UiRect(Bounds.Right - size, Bounds.Bottom - size, size, size);
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        if (AllowResize)
        {
            UiInputState input = context.Input;
            if (!_resizing && input.LeftClicked && ResizeGripBounds.Contains(input.MousePosition))
            {
                _resizing = true;
                _resizeStart = input.MousePosition;
                _resizeStartBounds = Bounds;
                context.Focus.RequestFocus(null);
            }

            if (_resizing && input.LeftDown)
            {
                int deltaX = input.MousePosition.X - _resizeStart.X;
                int deltaY = input.MousePosition.Y - _resizeStart.Y;
                int width = _resizeStartBounds.Width + deltaX;
                int height = _resizeStartBounds.Height + deltaY;

                width = Math.Clamp(width, MinSize.X, MaxSize.X);
                height = Math.Clamp(height, MinSize.Y, MaxSize.Y);

                if (ClampResizeToParent && Parent != null)
                {
                    UiRect parentBounds = Parent.Bounds;
                    int maxWidth = parentBounds.Right - _resizeStartBounds.X;
                    int maxHeight = parentBounds.Bottom - _resizeStartBounds.Y;
                    width = Math.Min(width, maxWidth);
                    height = Math.Min(height, maxHeight);
                }

                Bounds = new UiRect(_resizeStartBounds.X, _resizeStartBounds.Y, width, height);
            }

            if (_resizing && input.LeftReleased)
            {
                _resizing = false;
            }
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        base.Render(context);

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        if (AllowResize && ShowResizeGrip)
        {
            context.Renderer.FillRect(ResizeGripBounds, ResizeGripColor);
        }
    }
}
