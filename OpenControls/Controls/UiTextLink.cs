using System.Diagnostics;

namespace OpenControls.Controls;

public sealed class UiTextLink : UiElement
{
    private bool _hovered;
    private bool _pressed;
    private bool _focused;

    public string Text { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool OpenUrlOnClick { get; set; }
    public UiColor Color { get; set; } = new UiColor(90, 170, 250);
    public UiColor HoverColor { get; set; } = new UiColor(130, 200, 255);
    public UiColor ActiveColor { get; set; } = new UiColor(70, 140, 220);
    public UiColor UnderlineColor { get; set; } = UiColor.Transparent;
    public bool Underline { get; set; } = true;
    public int TextScale { get; set; } = 1;
    public int UnderlineThickness { get; set; } = 1;

    public event Action? Clicked;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            HandleClick();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                HandleClick();
            }

            _pressed = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor color = Color;
        if (_pressed)
        {
            color = ActiveColor;
        }
        else if (_hovered || _focused)
        {
            color = HoverColor;
        }

        UiPoint position = new UiPoint(Bounds.X, Bounds.Y);
        context.Renderer.DrawText(Text, position, color, TextScale);

        if (Underline)
        {
            int textWidth = context.Renderer.MeasureTextWidth(Text, TextScale);
            int textHeight = context.Renderer.MeasureTextHeight(TextScale);
            int underlineY = position.Y + textHeight - 1;
            int thickness = Math.Max(1, UnderlineThickness);
            UiColor underlineColor = UnderlineColor.A == 0 ? color : UnderlineColor;
            if (textWidth > 0 && thickness > 0)
            {
                context.Renderer.FillRect(new UiRect(position.X, underlineY, textWidth, thickness), underlineColor);
            }
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressed = false;
    }

    private void HandleClick()
    {
        Clicked?.Invoke();
        if (OpenUrlOnClick && !string.IsNullOrWhiteSpace(Url))
        {
            TryOpenUrl(Url!);
        }
    }

    private static void TryOpenUrl(string url)
    {
        try
        {
            ProcessStartInfo startInfo = new(url)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch
        {
        }
    }
}
