namespace OpenControls.Controls;

public sealed class UiLabel : UiElement
{
    private string _text = string.Empty;
    private UiColor _color = UiColor.White;
    private int _scale = 1;
    private bool _bold;

    public string Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor Color
    {
        get => _color;
        set => SetInvalidatingValue(ref _color, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int Scale
    {
        get => _scale;
        set => SetInvalidatingValue(ref _scale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public bool Bold
    {
        get => _bold;
        set => SetInvalidatingValue(ref _bold, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

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
