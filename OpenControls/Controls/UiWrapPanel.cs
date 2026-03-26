namespace OpenControls.Controls;

public sealed class UiWrapPanel : UiElement
{
    private UiPoint _contentSize;

    public UiThickness Padding { get; set; } = UiThickness.Uniform(0);
    public int ItemSpacing { get; set; } = 4;
    public int LineSpacing { get; set; } = 4;
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

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

    public UiPoint ContentSize => _contentSize;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutChildren();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutChildren();

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

    private void LayoutChildren()
    {
        UiRect content = ContentBounds;
        int x = content.X;
        int y = content.Y;
        int lineHeight = 0;
        int maxRight = content.X;
        int maxBottom = content.Y;
        int itemSpacing = Math.Max(0, ItemSpacing);
        int lineSpacing = Math.Max(0, LineSpacing);

        foreach (UiElement child in Children)
        {
            if (!child.Visible)
            {
                continue;
            }

            int width = Math.Max(0, child.Bounds.Width);
            int height = Math.Max(0, child.Bounds.Height);

            if (x > content.X && x + width > content.Right)
            {
                x = content.X;
                y += lineHeight + lineSpacing;
                lineHeight = 0;
            }

            child.Bounds = new UiRect(x, y, width, height);
            x += width + itemSpacing;
            lineHeight = Math.Max(lineHeight, height);
            maxRight = Math.Max(maxRight, child.Bounds.Right);
            maxBottom = Math.Max(maxBottom, child.Bounds.Bottom);
        }

        _contentSize = new UiPoint(
            Math.Max(0, maxRight - content.X),
            Math.Max(0, maxBottom - content.Y));
    }
}
