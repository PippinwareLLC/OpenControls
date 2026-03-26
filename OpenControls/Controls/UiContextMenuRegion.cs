namespace OpenControls.Controls;

public sealed class UiContextMenuRegion : UiElement
{
    public UiPopup? Popup { get; set; }
    public UiMenuBar? Menu { get; set; }
    public UiElement? Target { get; set; }
    public bool OpenOnRightClick { get; set; } = true;
    public bool OpenOnLeftClick { get; set; }
    public bool ShowWhenDisabled { get; set; }
    public bool OpenAttachedToBounds { get; set; } = true;
    public UiPopupPlacement PopupPlacement { get; set; } = UiPopupPlacement.BottomLeft;
    public UiPoint AttachedOffset { get; set; } = new UiPoint(0, 4);
    public UiPoint ContextOffset { get; set; }
    public UiPoint PopupSize { get; set; }
    public int MenuWidth { get; set; }

    public event Action? Opening;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || (!Enabled && !ShowWhenDisabled))
        {
            return;
        }

        UiInputState input = context.Input;
        bool activate = (OpenOnRightClick && input.RightClicked) || (OpenOnLeftClick && input.LeftClicked);
        UiRect targetBounds = ResolveTargetBounds();
        if (!activate || !targetBounds.Contains(input.MousePosition))
        {
            return;
        }

        Opening?.Invoke();
        Open(input.ScreenMousePosition, targetBounds);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        return null;
    }

    public override void Render(UiRenderContext context)
    {
        // Context menu regions are invisible.
    }

    private void Open(UiPoint screenPoint, UiRect targetBounds)
    {
        if (Menu != null)
        {
            if (OpenAttachedToBounds)
            {
                UiRect anchor = new UiRect(
                    targetBounds.X + AttachedOffset.X,
                    targetBounds.Bottom + AttachedOffset.Y,
                    targetBounds.Width,
                    targetBounds.Height);
                Menu.OpenAttached(anchor);
            }
            else
            {
                UiPoint point = new UiPoint(screenPoint.X + ContextOffset.X, screenPoint.Y + ContextOffset.Y);
                Menu.OpenContext(point, MenuWidth);
            }

            return;
        }

        if (Popup == null)
        {
            return;
        }

        UiPoint size = ResolvePopupSize();
        if (OpenAttachedToBounds)
        {
            UiRect anchor = new UiRect(
                targetBounds.X + AttachedOffset.X,
                targetBounds.Y + AttachedOffset.Y,
                targetBounds.Width,
                targetBounds.Height);
            Popup.OpenAttached(anchor, size, PopupPlacement);
        }
        else
        {
            UiPoint point = new UiPoint(screenPoint.X + ContextOffset.X, screenPoint.Y + ContextOffset.Y);
            Popup.OpenContext(point, size);
        }
    }

    private UiPoint ResolvePopupSize()
    {
        if (PopupSize.X > 0 && PopupSize.Y > 0)
        {
            return PopupSize;
        }

        if (Popup != null && Popup.Bounds.Width > 0 && Popup.Bounds.Height > 0)
        {
            return new UiPoint(Popup.Bounds.Width, Popup.Bounds.Height);
        }

        return new UiPoint(160, 120);
    }

    private UiRect ResolveTargetBounds()
    {
        return Target?.Bounds ?? Bounds;
    }
}
