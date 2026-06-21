namespace OpenControls.Controls;

public sealed class UiSelectionMarquee : UiElement
{
    public UiColor Fill { get; set; } = new(76, 151, 255, 44);
    public UiColor Border { get; set; } = new(112, 183, 255, 220);
    public int BorderThickness { get; set; } = 1;

    public override UiElement? HitTest(UiPoint point)
    {
        return null;
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        if (Fill.A > 0)
        {
            context.Renderer.FillRect(Bounds, Fill);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            context.Renderer.DrawRect(Bounds, Border, BorderThickness);
        }
    }
}
