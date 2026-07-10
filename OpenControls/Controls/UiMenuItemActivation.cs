namespace OpenControls.Controls;

/// <summary>Describes how a menu item was activated and which modifiers were held.</summary>
public readonly record struct UiMenuItemActivation(
    UiMenuItemActivationSource Source,
    UiModifierKeys Modifiers);
