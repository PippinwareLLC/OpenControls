namespace OpenControls.Controls;

public sealed class UiModalHost : UiElement
{
    public bool BlockInputWhenModalOpen { get; set; } = true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiModal? activeModal = FindActiveModal();
        if (activeModal == null || !BlockInputWhenModalOpen)
        {
            foreach (UiElement child in Children)
            {
                child.Update(context);
            }

            return;
        }

        UiInputState blockedInput = BuildBlockedInput(context.Input);
        UiUpdateContext blockedContext = new UiUpdateContext(blockedInput, context.Focus, context.DeltaSeconds);

        foreach (UiElement child in Children)
        {
            if (child == activeModal)
            {
                child.Update(context);
            }
            else
            {
                child.Update(blockedContext);
            }
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        foreach (UiElement child in Children)
        {
            child.Render(context);
        }
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        foreach (UiElement child in Children)
        {
            if (child is UiModal)
            {
                continue;
            }

            child.RenderOverlay(context);
        }

        foreach (UiElement child in Children)
        {
            if (child is UiModal modal && modal.IsOpen)
            {
                modal.RenderOverlay(context);
            }
        }
    }

    private UiModal? FindActiveModal()
    {
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i] is UiModal modal && modal.IsOpen)
            {
                return modal;
            }
        }

        return null;
    }

    private static UiInputState BuildBlockedInput(UiInputState input)
    {
        UiPoint offScreen = new UiPoint(int.MinValue / 4, int.MinValue / 4);
        return new UiInputState
        {
            MousePosition = offScreen,
            ScreenMousePosition = offScreen,
            LeftDown = false,
            LeftClicked = false,
            LeftReleased = false,
            ShiftDown = false,
            CtrlDown = false,
            ScrollDelta = 0,
            TextInput = Array.Empty<char>(),
            Navigation = default
        };
    }
}
