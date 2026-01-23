namespace OpenControls.Controls;

public sealed class UiBullet : UiElement
{
    public UiColor Color { get; set; } = UiColor.White;
    public int Size { get; set; } = 0;

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        int size = Size > 0 ? Size : Math.Min(Bounds.Width, Bounds.Height);
        size = Math.Max(2, size);
        int x = Bounds.X + (Bounds.Width - size) / 2;
        int y = Bounds.Y + (Bounds.Height - size) / 2;
        UiRect rect = new UiRect(x, y, size, size);
        UiRenderHelpers.FillRectRounded(context.Renderer, rect, size / 2, Color);

        base.Render(context);
    }
}
