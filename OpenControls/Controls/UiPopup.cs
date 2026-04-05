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
    public bool ClampToParent { get; set; } = true;
    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;

    public bool IsOpen { get; private set; }
    public override bool CapturesPointerInput => IsOpen;

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
        Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
        Opened?.Invoke();
    }

    public void Open(UiRect bounds)
    {
        Bounds = ClampToParent ? UiPopupLayout.Clamp(this, bounds) : bounds;
        Open();
    }

    public void OpenAttached(UiRect anchorBounds, UiPoint size, UiPopupPlacement placement = UiPopupPlacement.BottomLeft)
    {
        UiRect bounds = UiPopupLayout.BuildBounds(anchorBounds, size, placement);
        Open(bounds);
    }

    public void OpenContext(UiPoint point, UiPoint size)
    {
        UiRect bounds = UiPopupLayout.BuildContextBounds(point, size);
        Open(bounds);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
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
        else if (CloseOnOutsideClick && IsOutsideClick(input) && !ContainsOverlayPoint(input.MousePosition))
        {
            Close();
            return;
        }

        base.Update(context);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !IsOpen)
        {
            return null;
        }

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UiElement child = Children[i];
            UiElement? childHit = child.HitTest(point);
            if (childHit != null)
            {
                return childHit;
            }
        }

        if (!Bounds.Contains(point))
        {
            return null;
        }

        return this;
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
            context.RenderChild(child);
            context.RenderChildOverlay(child);
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

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (IsOpen)
        {
            status |= UiItemStatusFlags.Active;
        }

        return status;
    }

    private bool ContainsOverlayPoint(UiPoint point)
    {
        return HitTest(point) != null;
    }

    private static bool IsOutsideClick(UiInputState input)
    {
        return input.LeftClicked || input.RightClicked || input.MiddleClicked;
    }
}
