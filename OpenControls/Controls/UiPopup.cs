namespace OpenControls.Controls;

public class UiPopup : UiElement
{
    private bool _suppressOutsideClick;

    public UiPopup()
    {
        ClipChildren = true;
    }

    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(70, 80, 100);
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;

    public bool IsOpen { get; private set; }

    public event Action? Opened;
    public event Action? Closed;

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        _suppressOutsideClick = true;
        Opened?.Invoke();
    }

    public void Open(UiRect bounds)
    {
        Bounds = bounds;
        Open();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Closed?.Invoke();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled || !IsOpen)
        {
            return;
        }

        UiInputState input = context.Input;
        if (CloseOnEscape && input.Navigation.Escape)
        {
            Close();
            return;
        }

        if (_suppressOutsideClick)
        {
            _suppressOutsideClick = false;
        }
        else if (CloseOnOutsideClick && input.LeftClicked && !Bounds.Contains(input.MousePosition))
        {
            Close();
            return;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        // Popups render in the overlay pass only.
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !IsOpen)
        {
            return;
        }

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (ClipChildren)
        {
            context.Renderer.PushClip(ClipBounds);
        }

        foreach (UiElement child in Children)
        {
            child.Render(context);
            child.RenderOverlay(context);
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }
    }
}
