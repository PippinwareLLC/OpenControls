namespace OpenControls.Controls;

public enum UiSeparatorOrientation
{
    Horizontal,
    Vertical
}

public sealed class UiSeparator : UiElement
{
    public UiSeparatorOrientation Orientation { get; set; } = UiSeparatorOrientation.Horizontal;
    public UiColor Color { get; set; } = new UiColor(70, 80, 100);
    public int Thickness { get; set; } = 1;

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        int thickness = Math.Max(1, Thickness);
        UiRect line = Orientation == UiSeparatorOrientation.Vertical
            ? new UiRect(Bounds.X + (Bounds.Width - thickness) / 2, Bounds.Y, thickness, Bounds.Height)
            : new UiRect(Bounds.X, Bounds.Y + (Bounds.Height - thickness) / 2, Bounds.Width, thickness);

        context.Renderer.FillRect(line, Color);
        base.Render(context);
    }
}
