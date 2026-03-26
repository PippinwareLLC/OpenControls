namespace OpenControls.Controls;

public sealed class UiLabel : UiElement
{
    public string Text { get; set; } = string.Empty;
    public UiColor Color { get; set; } = UiColor.White;
    public int Scale { get; set; } = 1;
    public bool Bold { get; set; }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        UiPoint position = new UiPoint(Bounds.X, Bounds.Y);
        if (Bold)
        {
            UiRenderHelpers.DrawTextBold(context.Renderer, Text, position, Color, Scale, font);
        }
        else
        {
            context.Renderer.DrawText(Text, position, Color, Scale, font);
        }
        base.Render(context);
    }
}
