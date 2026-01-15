using OpenControls.Controls;

namespace OpenControls;

public sealed class UiContext
{
    public UiContext(UiElement root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public UiElement Root { get; }
    public UiFocusManager Focus { get; } = new();

    public void Update(UiInputState input, float deltaSeconds = 0f)
    {
        UiInputState effectiveInput = input;
        if (input.Navigation.Tab && !IsTabHandled())
        {
            MoveFocus(input.ShiftDown);
            effectiveInput = ConsumeTabInput(input);
        }

        if (Focus.Focused != null && (!Focus.Focused.Visible || !Focus.Focused.Enabled))
        {
            Focus.ClearFocus();
        }

        Root.Update(new UiUpdateContext(effectiveInput, Focus, deltaSeconds));
    }

    private bool IsTabHandled()
    {
        if (Focus.Focused is not { Visible: true, Enabled: true } focused)
        {
            return false;
        }

        return focused.HandlesTabInput;
    }

    public void Render(IUiRenderer renderer)
    {
        UiRenderContext context = new(renderer);
        Root.Render(context);
        Root.RenderOverlay(context);
    }

    private void MoveFocus(bool reverse)
    {
        List<UiElement> focusables = new();
        CollectFocusable(Root, focusables);

        if (focusables.Count == 0)
        {
            Focus.ClearFocus();
            return;
        }

        int currentIndex = Focus.Focused == null ? -1 : focusables.IndexOf(Focus.Focused);
        if (currentIndex == -1)
        {
            Focus.RequestFocus(focusables[reverse ? focusables.Count - 1 : 0]);
            return;
        }

        int nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;
        if (nextIndex < 0)
        {
            nextIndex = focusables.Count - 1;
        }
        else if (nextIndex >= focusables.Count)
        {
            nextIndex = 0;
        }

        Focus.RequestFocus(focusables[nextIndex]);
    }

    private static void CollectFocusable(UiElement element, List<UiElement> focusables)
    {
        if (!element.Visible || !element.Enabled)
        {
            return;
        }

        if (element is UiTabItem tabItem && !tabItem.IsActive)
        {
            return;
        }

        if (element.IsFocusable)
        {
            focusables.Add(element);
        }

        if (!ShouldTraverseChildren(element))
        {
            return;
        }

        if (element is UiModalHost modalHost && modalHost.BlockInputWhenModalOpen)
        {
            UiModal? activeModal = FindActiveModal(modalHost);
            if (activeModal != null)
            {
                CollectFocusable(activeModal, focusables);
                return;
            }
        }

        foreach (UiElement child in element.Children)
        {
            CollectFocusable(child, focusables);
        }
    }

    private static bool ShouldTraverseChildren(UiElement element)
    {
        if (element is UiPopup popup)
        {
            return popup.IsOpen;
        }

        if (element is UiTreeNode tree)
        {
            return tree.IsOpen;
        }

        if (element is UiCollapsingHeader header)
        {
            return header.IsOpen;
        }

        return true;
    }

    private static UiModal? FindActiveModal(UiModalHost host)
    {
        for (int i = host.Children.Count - 1; i >= 0; i--)
        {
            if (host.Children[i] is UiModal modal && modal.IsOpen)
            {
                return modal;
            }
        }

        return null;
    }

    private static UiInputState ConsumeTabInput(UiInputState input)
    {
        UiNavigationInput navigation = new UiNavigationInput
        {
            MoveLeft = input.Navigation.MoveLeft,
            MoveRight = input.Navigation.MoveRight,
            MoveUp = input.Navigation.MoveUp,
            MoveDown = input.Navigation.MoveDown,
            Home = input.Navigation.Home,
            End = input.Navigation.End,
            Backspace = input.Navigation.Backspace,
            Delete = input.Navigation.Delete,
            Tab = false,
            Enter = input.Navigation.Enter,
            KeypadEnter = input.Navigation.KeypadEnter,
            Space = input.Navigation.Space,
            Escape = input.Navigation.Escape
        };

        IReadOnlyList<char> textInput = input.TextInput;
        if (textInput.Count > 0)
        {
            List<char>? filtered = null;
            for (int i = 0; i < textInput.Count; i++)
            {
                char character = textInput[i];
                if (character == '\t')
                {
                    if (filtered == null)
                    {
                        filtered = new List<char>(textInput.Count);
                        for (int j = 0; j < i; j++)
                        {
                            filtered.Add(textInput[j]);
                        }
                    }
                    continue;
                }

                filtered?.Add(character);
            }

            if (filtered != null)
            {
                textInput = filtered;
            }
        }

        return new UiInputState
        {
            MousePosition = input.MousePosition,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftReleased = input.LeftReleased,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            ScrollDelta = input.ScrollDelta,
            TextInput = textInput,
            Navigation = navigation
        };
    }
}
