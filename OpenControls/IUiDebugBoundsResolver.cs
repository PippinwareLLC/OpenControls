namespace OpenControls;

internal interface IUiDebugBoundsResolver
{
    bool TryResolveDebugBounds(UiElement element, out UiRect bounds, out UiRect clipBounds);
}
