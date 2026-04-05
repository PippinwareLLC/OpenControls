namespace OpenControls.Controls;

public sealed class UiModal : UiPopup
{
    public UiModal()
    {
        CloseOnOutsideClick = false;
    }

    public UiColor Backdrop { get; set; } = new UiColor(0, 0, 0, 140);
    public UiRect? BackdropBounds { get; set; }
    public override bool IsRenderCacheRoot(UiContext context) => IsOpen;

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !IsOpen)
        {
            return;
        }

        UiRect backdrop = BackdropBounds ?? Parent?.Bounds ?? Bounds;
        if (Backdrop.A > 0)
        {
            context.Renderer.FillRect(backdrop, Backdrop);
        }

        base.RenderOverlay(context);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !IsOpen)
        {
            return null;
        }

        UiRect backdrop = BackdropBounds ?? Parent?.Bounds ?? Bounds;
        if (!backdrop.Contains(point))
        {
            return null;
        }

        UiElement? hit = base.HitTest(point);
        return hit ?? this;
    }
}
