namespace OpenControls.Controls;

public sealed class UiTooltipRegion : UiElement
{
    private float _hoverElapsed;
    private float _focusElapsed;

    public UiTooltip? Tooltip { get; set; }
    public string Text { get; set; } = string.Empty;
    public UiPoint? TooltipOffset { get; set; }
    public UiPoint? FocusTooltipOffset { get; set; }
    public UiElement? HoverTarget { get; set; }
    public bool ShowWhenDisabled { get; set; }
    public UiElement? FocusTarget { get; set; }
    public bool ShowOnKeyboardFocus { get; set; } = true;
    public bool SuppressWhileDragging { get; set; } = true;
    public float HoverDelaySeconds { get; set; } = 0.35f;
    public float FocusDelaySeconds { get; set; } = 0.15f;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || (!Enabled && !ShowWhenDisabled))
        {
            ResetTooltip();
            return;
        }

        if (Tooltip == null)
        {
            return;
        }

        UiInputState input = context.Input;
        bool suppress = SuppressWhileDragging && (input.LeftDragging || input.RightDragging || input.MiddleDragging);
        UiRect hoverBounds = HoverTarget?.Bounds ?? Bounds;
        bool hovered = hoverBounds.Contains(input.MousePosition);
        bool focusActive = !hovered && ShowOnKeyboardFocus && IsFocusTargetActive(context.Focus.Focused);

        if (hovered && !suppress)
        {
            _hoverElapsed += context.DeltaSeconds;
        }
        else
        {
            _hoverElapsed = 0f;
        }

        if (focusActive && !suppress)
        {
            _focusElapsed += context.DeltaSeconds;
        }
        else
        {
            _focusElapsed = 0f;
        }

        if (hovered)
        {
            if (string.IsNullOrEmpty(Text))
            {
                ResetTooltip();
                return;
            }

            if (!suppress && _hoverElapsed >= Math.Max(0f, HoverDelaySeconds))
            {
                Tooltip.Show(Text, input.ScreenMousePosition, this, TooltipOffset);
            }
            else
            {
                Tooltip.Hide(this);
            }
        }
        else if (focusActive)
        {
            if (string.IsNullOrEmpty(Text))
            {
                ResetTooltip();
                return;
            }

            if (!suppress && _focusElapsed >= Math.Max(0f, FocusDelaySeconds))
            {
                UiRect anchorBounds = FocusTarget?.Bounds ?? HoverTarget?.Bounds ?? Bounds;
                UiPoint anchor = new UiPoint(anchorBounds.X, anchorBounds.Bottom);
                Tooltip.Show(Text, anchor, this, FocusTooltipOffset ?? TooltipOffset ?? new UiPoint(0, 6));
            }
            else
            {
                Tooltip.Hide(this);
            }
        }
        else
        {
            ResetTooltip();
        }
    }

    public override UiElement? HitTest(UiPoint point)
    {
        return null;
    }

    public override void Render(UiRenderContext context)
    {
        // Tooltip regions are invisible.
    }

    private bool IsFocusTargetActive(UiElement? focused)
    {
        if (FocusTarget == null || focused == null)
        {
            return false;
        }

        UiElement? current = focused;
        while (current != null)
        {
            if (current == FocusTarget)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private void ResetTooltip()
    {
        _hoverElapsed = 0f;
        _focusElapsed = 0f;
        Tooltip?.Hide(this);
    }
}
