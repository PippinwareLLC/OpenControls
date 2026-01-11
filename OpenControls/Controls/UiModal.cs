namespace OpenControls.Controls;

public sealed class UiModal : UiPopup
{
    public UiModal()
    {
        CloseOnOutsideClick = false;
    }

    public UiColor Backdrop { get; set; } = new UiColor(0, 0, 0, 140);
    public UiRect? BackdropBounds { get; set; }

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
}
