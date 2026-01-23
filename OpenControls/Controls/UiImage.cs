namespace OpenControls.Controls;

public sealed class UiImage : UiElement
{
    public Action<IUiRenderer, UiRect>? DrawImage { get; set; }

    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int Padding { get; set; } = 0;
    public int CornerRadius { get; set; }

    public bool ShowCheckerboard { get; set; }
    public int CheckerSize { get; set; } = 6;
    public UiColor CheckerColorLight { get; set; } = new UiColor(200, 200, 200);
    public UiColor CheckerColorDark { get; set; } = new UiColor(120, 120, 120);
    public UiColor PlaceholderColor { get; set; } = new UiColor(60, 70, 90);

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

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        int inset = Math.Max(0, Padding) + Math.Max(0, BorderThickness);
        UiRect inner = new UiRect(
            Bounds.X + inset,
            Bounds.Y + inset,
            Math.Max(0, Bounds.Width - inset * 2),
            Math.Max(0, Bounds.Height - inset * 2));

        if (inner.Width > 0 && inner.Height > 0)
        {
            if (ShowCheckerboard)
            {
                context.Renderer.FillRectCheckerboard(inner, CheckerSize, CheckerColorLight, CheckerColorDark);
            }

            if (DrawImage != null)
            {
                DrawImage(context.Renderer, inner);
            }
            else if (PlaceholderColor.A > 0)
            {
                int radius = Math.Max(0, CornerRadius - inset);
                UiRenderHelpers.FillRectRounded(context.Renderer, inner, radius, PlaceholderColor);
            }
        }

        base.Render(context);
    }
}
