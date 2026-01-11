namespace OpenControls.Controls;

public sealed class UiTooltipRegion : UiElement
{
    public UiTooltip? Tooltip { get; set; }
    public string Text { get; set; } = string.Empty;
    public UiPoint? TooltipOffset { get; set; }
    public bool ShowWhenDisabled { get; set; }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || (!Enabled && !ShowWhenDisabled))
        {
            Tooltip?.Hide(this);
            return;
        }

        UiInputState input = context.Input;
        if (Tooltip == null)
        {
            return;
        }

        if (Bounds.Contains(input.MousePosition))
        {
            if (string.IsNullOrEmpty(Text))
            {
                Tooltip.Hide(this);
                return;
            }

            Tooltip.Show(Text, input.ScreenMousePosition, this, TooltipOffset);
        }
        else
        {
            Tooltip.Hide(this);
        }
    }

    public override void Render(UiRenderContext context)
    {
        // Tooltip regions are invisible.
    }
}
