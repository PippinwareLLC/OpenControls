namespace OpenControls.Controls;

public sealed class UiSeparatorText : UiElement
{
    public string Text { get; set; } = string.Empty;
    public UiColor LineColor { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int TextScale { get; set; } = 1;
    public int LineThickness { get; set; } = 1;
    public int TextPadding { get; set; } = 8;
    public bool Bold { get; set; }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        int thickness = Math.Max(1, LineThickness);
        int centerY = Bounds.Y + (Bounds.Height - thickness) / 2;

        int textWidth = string.IsNullOrEmpty(Text) ? 0 : context.Renderer.MeasureTextWidth(Text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = Bounds.X + (Bounds.Width - textWidth) / 2;
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;

        int padding = textWidth > 0 ? Math.Max(0, TextPadding) : 0;
        int leftEnd = textWidth > 0 ? textX - padding : Bounds.Right;
        int rightStart = textWidth > 0 ? textX + textWidth + padding : Bounds.Right;

        if (leftEnd > Bounds.X && LineColor.A > 0)
        {
            context.Renderer.FillRect(new UiRect(Bounds.X, centerY, leftEnd - Bounds.X, thickness), LineColor);
        }

        if (rightStart < Bounds.Right && LineColor.A > 0)
        {
            context.Renderer.FillRect(new UiRect(rightStart, centerY, Bounds.Right - rightStart, thickness), LineColor);
        }

        if (textWidth > 0)
        {
            UiPoint textPos = new UiPoint(textX, textY);
            if (Bold)
            {
                UiRenderHelpers.DrawTextBold(context.Renderer, Text, textPos, TextColor, TextScale);
            }
            else
            {
                context.Renderer.DrawText(Text, textPos, TextColor, TextScale);
            }
        }

        base.Render(context);
    }
}
