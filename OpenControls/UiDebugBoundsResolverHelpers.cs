namespace OpenControls;

internal static class UiDebugBoundsResolverHelpers
{
    public static bool TryResolveTranslatedDescendantBounds(
        IReadOnlyList<UiElement> children,
        UiElement target,
        int offsetX,
        int offsetY,
        UiRect clipBounds,
        out UiRect bounds,
        out UiRect resolvedClipBounds)
    {
        for (int i = 0; i < children.Count; i++)
        {
            UiElement child = children[i];
            if (child is IUiDebugBoundsResolver resolver
                && resolver.TryResolveDebugBounds(target, out UiRect nestedBounds, out UiRect nestedClipBounds))
            {
                bounds = TranslateRect(nestedBounds, offsetX, offsetY);
                resolvedClipBounds = IntersectRect(TranslateRect(nestedClipBounds, offsetX, offsetY), clipBounds);
                return true;
            }

            if (!IsElementOrAncestor(child, target))
            {
                continue;
            }

            bounds = TranslateRect(target.Bounds, offsetX, offsetY);
            resolvedClipBounds = IntersectRect(TranslateRect(target.ClipBounds, offsetX, offsetY), clipBounds);
            return true;
        }

        bounds = default;
        resolvedClipBounds = default;
        return false;
    }

    public static UiRect TranslateRect(UiRect rect, int offsetX, int offsetY)
    {
        return new UiRect(
            rect.X + offsetX,
            rect.Y + offsetY,
            rect.Width,
            rect.Height);
    }

    public static UiRect IntersectRect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        return right <= left || bottom <= top
            ? new UiRect(0, 0, 0, 0)
            : new UiRect(left, top, right - left, bottom - top);
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
}
