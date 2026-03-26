namespace OpenControls;

public static class UiTextHelpers
{
    public static string BuildElidedText(string text, int maxWidth, int scale = 1, UiFont? font = null)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return string.Empty;
        }

        UiFont resolvedFont = font ?? UiFont.Default;
        int safeScale = Math.Max(1, scale);
        if (resolvedFont.MeasureTextWidth(text, safeScale) <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";
        int ellipsisWidth = resolvedFont.MeasureTextWidth(ellipsis, safeScale);
        if (ellipsisWidth > maxWidth)
        {
            return string.Empty;
        }

        int low = 0;
        int high = text.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            string candidate = text.Substring(0, mid) + ellipsis;
            if (resolvedFont.MeasureTextWidth(candidate, safeScale) <= maxWidth)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low <= 0 ? ellipsis : text.Substring(0, low) + ellipsis;
    }
}
