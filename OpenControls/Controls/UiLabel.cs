namespace OpenControls.Controls;

public sealed class UiLabel : UiElement
{
    public string Text { get; set; } = string.Empty;
    public UiColor Color { get; set; } = UiColor.White;
    public int Scale { get; set; } = 1;

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        context.Renderer.DrawText(Text, new UiPoint(Bounds.X, Bounds.Y), Color, Scale);
        base.Render(context);
    }
}
