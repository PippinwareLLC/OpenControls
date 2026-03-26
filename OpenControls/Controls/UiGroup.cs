namespace OpenControls.Controls;

public sealed class UiGroup : UiElement
{
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public UiThickness Padding { get; set; } = UiThickness.Uniform(0);

    public UiRect ContentBounds
    {
        get
        {
            UiThickness padding = Padding;
            int x = Bounds.X + padding.Left;
            int y = Bounds.Y + padding.Top;
            int width = Math.Max(0, Bounds.Width - padding.Horizontal);
            int height = Math.Max(0, Bounds.Height - padding.Vertical);
            return new UiRect(x, y, width, height);
        }
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
    }
}
