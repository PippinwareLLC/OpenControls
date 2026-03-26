namespace OpenControls;

public sealed class UiInputState
{
    public UiPoint MousePosition { get; init; }
    public UiPoint ScreenMousePosition { get; init; }
    public bool LeftDown { get; init; }
    public bool LeftClicked { get; init; }
    public bool LeftDoubleClicked { get; init; }
    public bool LeftReleased { get; init; }
    public bool RightDown { get; init; }
    public bool RightClicked { get; init; }
    public bool RightDoubleClicked { get; init; }
    public bool RightReleased { get; init; }
    public bool MiddleDown { get; init; }
    public bool MiddleClicked { get; init; }
    public bool MiddleDoubleClicked { get; init; }
    public bool MiddleReleased { get; init; }
    public UiPoint? LeftDragOrigin { get; init; }
    public UiPoint? RightDragOrigin { get; init; }
    public UiPoint? MiddleDragOrigin { get; init; }
    public int DragThreshold { get; init; } = 6;
    public bool ShiftDown { get; init; }
    public bool CtrlDown { get; init; }
    public bool AltDown { get; init; }
    public bool SuperDown { get; init; }
    public int ScrollDeltaX { get; init; }
    public int ScrollDelta { get; init; }
    public IReadOnlyList<char> TextInput { get; init; } = Array.Empty<char>();
    public UiTextCompositionState Composition { get; init; }
    public IReadOnlyList<UiKey> KeysDown { get; init; } = Array.Empty<UiKey>();
    public IReadOnlyList<UiKey> KeysPressed { get; init; } = Array.Empty<UiKey>();
    public IReadOnlyList<UiKey> KeysReleased { get; init; } = Array.Empty<UiKey>();
    public UiNavigationInput Navigation { get; init; }

    public UiModifierKeys Modifiers
    {
        get
        {
            UiModifierKeys modifiers = UiModifierKeys.None;
            if (CtrlDown)
            {
                modifiers |= UiModifierKeys.Ctrl;
            }

            if (ShiftDown)
            {
                modifiers |= UiModifierKeys.Shift;
            }

            if (AltDown)
            {
                modifiers |= UiModifierKeys.Alt;
            }

            if (SuperDown)
            {
                modifiers |= UiModifierKeys.Super;
            }

            return modifiers;
        }
    }

    public bool AnyMouseDown => LeftDown || RightDown || MiddleDown;
    public bool PrimaryShortcutDown => CtrlDown || SuperDown;
    public bool LeftDragging => LeftDown && HasExceededDragThreshold(LeftDragOrigin);
    public bool RightDragging => RightDown && HasExceededDragThreshold(RightDragOrigin);
    public bool MiddleDragging => MiddleDown && HasExceededDragThreshold(MiddleDragOrigin);

    public bool IsKeyDown(UiKey key)
    {
        if (ContainsKey(KeysDown, key))
        {
            return true;
        }

        return key switch
        {
            UiKey.Shift => ShiftDown,
            UiKey.Control => CtrlDown,
            UiKey.Alt => AltDown,
            UiKey.Super => SuperDown,
            _ => false
        };
    }

    public bool IsKeyPressed(UiKey key)
    {
        if (ContainsKey(KeysPressed, key))
        {
            return true;
        }

        return key switch
        {
            UiKey.Left => Navigation.MoveLeft,
            UiKey.Right => Navigation.MoveRight,
            UiKey.Up => Navigation.MoveUp,
            UiKey.Down => Navigation.MoveDown,
            UiKey.PageUp => Navigation.PageUp,
            UiKey.PageDown => Navigation.PageDown,
            UiKey.Home => Navigation.Home,
            UiKey.End => Navigation.End,
            UiKey.Backspace => Navigation.Backspace,
            UiKey.Delete => Navigation.Delete,
            UiKey.Tab => Navigation.Tab,
            UiKey.Enter => Navigation.Enter,
            UiKey.KeypadEnter => Navigation.KeypadEnter,
            UiKey.Space => Navigation.Space,
            UiKey.Escape => Navigation.Escape,
            _ => false
        };
    }

    public bool IsKeyReleased(UiKey key)
    {
        return ContainsKey(KeysReleased, key);
    }

    public bool IsKeyPressed(UiKey key, UiModifierKeys requiredModifiers, bool allowExtraModifiers = false)
    {
        if (!IsKeyPressed(key))
        {
            return false;
        }

        UiModifierKeys modifiers = Modifiers;
        if ((modifiers & requiredModifiers) != requiredModifiers)
        {
            return false;
        }

        if (allowExtraModifiers)
        {
            return true;
        }

        return modifiers == requiredModifiers;
    }

    public bool IsKeyChordPressed(UiKeyChord chord, bool allowExtraModifiers = false)
    {
        return IsKeyPressed(chord.Key, chord.Modifiers, allowExtraModifiers);
    }

    public bool IsPrimaryShortcutPressed(UiKey key, bool shift = false, bool alt = false, bool allowExtraModifiers = false)
    {
        if (!IsKeyPressed(key) || !PrimaryShortcutDown)
        {
            return false;
        }

        UiModifierKeys modifiers = Modifiers & ~(UiModifierKeys.Ctrl | UiModifierKeys.Super);
        UiModifierKeys requiredModifiers = UiModifierKeys.None;
        if (shift)
        {
            requiredModifiers |= UiModifierKeys.Shift;
        }

        if (alt)
        {
            requiredModifiers |= UiModifierKeys.Alt;
        }

        if ((modifiers & requiredModifiers) != requiredModifiers)
        {
            return false;
        }

        if (allowExtraModifiers)
        {
            return true;
        }

        return modifiers == requiredModifiers;
    }

    private static bool ContainsKey(IReadOnlyList<UiKey> keys, UiKey key)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == key)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasExceededDragThreshold(UiPoint? origin)
    {
        if (origin is not UiPoint dragOrigin)
        {
            return false;
        }

        int dx = MousePosition.X - dragOrigin.X;
        int dy = MousePosition.Y - dragOrigin.Y;
        int threshold = Math.Max(0, DragThreshold);
        return dx * dx + dy * dy >= threshold * threshold;
    }
}
