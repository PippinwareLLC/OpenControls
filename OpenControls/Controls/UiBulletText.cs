namespace OpenControls.Controls;

public sealed class UiBulletText : UiElement
{
    public string Text { get; set; } = string.Empty;
    public UiColor BulletColor { get; set; } = UiColor.White;
    public UiColor TextColor { get; set; } = UiColor.White;
    public int TextScale { get; set; } = 1;
    public int BulletSize { get; set; } = 0;
    public int BulletPadding { get; set; } = 6;
    public bool Bold { get; set; }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int bulletSize = BulletSize > 0 ? BulletSize : Math.Max(2, textHeight / 3);
        int bulletY = Bounds.Y + (textHeight - bulletSize) / 2;
        UiRect bulletRect = new UiRect(Bounds.X, bulletY, bulletSize, bulletSize);
        UiRenderHelpers.FillRectRounded(context.Renderer, bulletRect, bulletSize / 2, BulletColor);

        if (!string.IsNullOrEmpty(Text))
        {
            int textX = Bounds.X + bulletSize + Math.Max(0, BulletPadding);
            UiPoint textPos = new UiPoint(textX, Bounds.Y);
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
