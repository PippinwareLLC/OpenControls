namespace OpenControls.Controls;

public sealed class UiDisabledGroup : UiElement
{
    public UiColor OverlayColor { get; set; } = new UiColor(0, 0, 0, 90);
    public bool DrawOverlay { get; set; } = true;
    public bool DisableHitTestingWhenDisabled { get; set; } = true;

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        base.Render(context);

        if (!Enabled && DrawOverlay && OverlayColor.A > 0)
        {
            context.Renderer.FillRect(Bounds, OverlayColor);
        }
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible)
        {
            return null;
        }

        if (!Enabled && DisableHitTestingWhenDisabled)
        {
            return null;
        }

        return base.HitTest(point);
    }
}
