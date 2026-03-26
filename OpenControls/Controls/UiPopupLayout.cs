namespace OpenControls.Controls;

internal static class UiPopupLayout
{
    public static UiRect BuildBounds(UiRect anchor, UiPoint size, UiPopupPlacement placement)
    {
        int width = Math.Max(0, size.X);
        int height = Math.Max(0, size.Y);

        return placement switch
        {
            UiPopupPlacement.BottomRight => new UiRect(anchor.Right - width, anchor.Bottom, width, height),
            UiPopupPlacement.TopLeft => new UiRect(anchor.X, anchor.Y - height, width, height),
            UiPopupPlacement.TopRight => new UiRect(anchor.Right - width, anchor.Y - height, width, height),
            _ => new UiRect(anchor.X, anchor.Bottom, width, height)
        };
    }

    public static UiRect BuildContextBounds(UiPoint point, UiPoint size)
    {
        return new UiRect(point.X, point.Y, Math.Max(0, size.X), Math.Max(0, size.Y));
    }

    public static UiRect Clamp(UiElement? element, UiRect bounds)
    {
        if (!TryGetClampBounds(element, out UiRect clampBounds)
            || clampBounds.Width <= 0
            || clampBounds.Height <= 0)
        {
            return bounds;
        }

        int x = bounds.X;
        int y = bounds.Y;
        if (bounds.Right > clampBounds.Right)
        {
            x = clampBounds.Right - bounds.Width;
        }

        if (bounds.Bottom > clampBounds.Bottom)
        {
            y = clampBounds.Bottom - bounds.Height;
        }

        if (x < clampBounds.X)
        {
            x = clampBounds.X;
        }

        if (y < clampBounds.Y)
        {
            y = clampBounds.Y;
        }

        return new UiRect(x, y, bounds.Width, bounds.Height);
    }

    public static bool TryGetClampBounds(UiElement? element, out UiRect bounds)
    {
        bounds = default;
        if (element == null)
        {
            return false;
        }

        int offsetX = 0;
        int offsetY = 0;
        bool hasFallback = false;
        UiRect fallback = default;

        UiElement? current = element.Parent;
        while (current != null)
        {
            if (current is UiTreeNode tree)
            {
                UiRect content = tree.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (current is UiCollapsingHeader header)
            {
                UiRect content = header.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (current is UiScrollPanel scrollPanel)
            {
                offsetX += scrollPanel.Bounds.X - scrollPanel.ScrollX;
                offsetY += scrollPanel.Bounds.Y - scrollPanel.ScrollY;
                UiRect viewport = scrollPanel.ViewportBounds;
                bounds = new UiRect(viewport.X - offsetX, viewport.Y - offsetY, viewport.Width, viewport.Height);
                return true;
            }
            else
            {
                UiRect parentBounds = current.Bounds;
                fallback = new UiRect(parentBounds.X - offsetX, parentBounds.Y - offsetY, parentBounds.Width, parentBounds.Height);
                hasFallback = true;
            }

            current = current.Parent;
        }

        if (hasFallback)
        {
            bounds = fallback;
            return true;
        }

        return false;
    }
}
