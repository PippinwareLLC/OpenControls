namespace OpenControls.Controls;

public sealed class UiModalHost : UiElement
{
    private UiModal? _trackedActiveModal;
    private UiElement? _focusBeforeModal;

    public bool BlockInputWhenModalOpen { get; set; } = true;
    public UiModal? ActiveModal => FindActiveModal();

    public override void Update(UiUpdateContext context)
    {
        UiElement? focusAtUpdateStart = context.Focus.Focused;
        UiModal? activeModal = Visible && Enabled && BlockInputWhenModalOpen ? FindActiveModal() : null;
        SynchronizeModalFocus(
            context.Focus,
            activeModal,
            focusAtUpdateStart,
            deferFinalClose: true);

        if (!Visible || !Enabled)
        {
            return;
        }

        if (activeModal == null)
        {
            foreach (UiElement child in Children)
            {
                child.Update(context.CreateChildContext(this, child));
            }

            SynchronizeModalFocus(
                context.Focus,
                Visible && Enabled && BlockInputWhenModalOpen ? FindActiveModal() : null,
                focusAtUpdateStart,
                deferFinalClose: false);
            return;
        }

        UiInputState blockedInput = BuildBlockedInput(context.Input);

        foreach (UiElement child in Children)
        {
            if (child == activeModal)
            {
                child.Update(context.CreateChildContext(this, child));
            }
            else
            {
                child.Update(context.CreateChildContext(this, child, blockedInput));
            }
        }

        SynchronizeModalFocus(
            context.Focus,
            Visible && Enabled && BlockInputWhenModalOpen ? FindActiveModal() : null,
            focusAtUpdateStart,
            deferFinalClose: false);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        foreach (UiElement child in Children)
        {
            context.RenderChild(child);
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

            context.RenderChildOverlay(child);
        }

        foreach (UiElement child in Children)
        {
            if (child is UiModal modal)
            {
                context.RenderChildOverlay(modal);
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

    private void SynchronizeModalFocus(
        UiFocusManager focus,
        UiModal? activeModal,
        UiElement? focusBeforeUpdate,
        bool deferFinalClose)
    {
        // A modal may be closed before this host's update while containers earlier
        // in the tree still have last frame's active/expanded state. Let those
        // containers update before deciding whether the saved focus target remains
        // eligible. This also gives a close handler a chance to choose an explicit
        // valid destination during the frame.
        if (deferFinalClose && _trackedActiveModal != null && activeModal == null)
        {
            return;
        }

        if (_trackedActiveModal == null && activeModal != null)
        {
            // A child control can open a later modal child and let its queued field
            // take focus in this same host update. Preserve the focus that existed
            // at the start of the update, before that handoff.
            UiElement? focused = focusBeforeUpdate ?? focus.Focused;
            _focusBeforeModal = focused != null && !IsElementOrAncestor(activeModal, focused)
                ? focused
                : null;
        }

        if (_trackedActiveModal != null && activeModal == null)
        {
            RestoreFocusAfterFinalModalCloses(focus, _trackedActiveModal);
            _focusBeforeModal = null;
        }

        _trackedActiveModal = activeModal;

        if (activeModal != null && focus.Focused != null && !IsElementOrAncestor(activeModal, focus.Focused))
        {
            // The context will apply the active modal's default focus after this update.
            // Clearing here prevents a child of a replaced or covered modal from retaining
            // text-input ownership in the meantime.
            focus.ClearFocus();
        }
    }

    private void RestoreFocusAfterFinalModalCloses(UiFocusManager focus, UiModal closingModal)
    {
        UiElement? current = focus.Focused;
        bool currentNeedsReplacement = current == null
            || IsElementOrAncestor(closingModal, current)
            || !UiContext.IsEligibleFocusTarget(current)
            || !SharesVisualTree(current, this);

        if (!currentNeedsReplacement)
        {
            return;
        }

        if (_focusBeforeModal != null
            && UiContext.IsEligibleFocusTarget(_focusBeforeModal)
            && SharesVisualTree(_focusBeforeModal, this))
        {
            focus.RequestFocus(_focusBeforeModal);
        }
        else
        {
            focus.ClearFocus();
        }
    }

    private static bool SharesVisualTree(UiElement first, UiElement second)
    {
        return ReferenceEquals(FindRoot(first), FindRoot(second));
    }

    private static UiElement FindRoot(UiElement element)
    {
        UiElement current = element;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }

    private static bool IsElementOrAncestor(UiElement ancestor, UiElement element)
    {
        UiElement? current = element;
        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
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
            LeftDoubleClicked = false,
            LeftReleased = false,
            RightDown = false,
            RightClicked = false,
            RightDoubleClicked = false,
            RightReleased = false,
            MiddleDown = false,
            MiddleClicked = false,
            MiddleDoubleClicked = false,
            MiddleReleased = false,
            LeftDragOrigin = null,
            RightDragOrigin = null,
            MiddleDragOrigin = null,
            DragThreshold = input.DragThreshold,
            ShiftDown = false,
            CtrlDown = false,
            AltDown = false,
            SuperDown = false,
            ScrollDeltaX = 0,
            ScrollDelta = 0,
            TextInput = Array.Empty<char>(),
            KeysDown = Array.Empty<UiKey>(),
            KeysPressed = Array.Empty<UiKey>(),
            KeysReleased = Array.Empty<UiKey>(),
            Navigation = default
        };
    }
}
