namespace OpenControls;

[Flags]
public enum UiModifierKeys
{
    None = 0,
    Ctrl = 1 << 0,
    Shift = 1 << 1,
    Alt = 1 << 2,
    Super = 1 << 3
}

public readonly struct UiKeyChord
{
    public UiKeyChord(UiKey key, UiModifierKeys modifiers = UiModifierKeys.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public UiKey Key { get; }
    public UiModifierKeys Modifiers { get; }
}
