using System.Collections.Concurrent;

namespace OpenControls;

public static class UiTextHelpers
{
    private readonly record struct ElidedTextCacheKey(string Text, int MaxWidth, int Scale, UiFont Font);

    private static readonly ConcurrentDictionary<ElidedTextCacheKey, string> ElidedTextCache = new();
    private const int MaxElidedTextCacheEntries = 8192;

    public static string BuildElidedText(string text, int maxWidth, int scale = 1, UiFont? font = null)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return string.Empty;
        }

        UiFont resolvedFont = font ?? UiFont.Default;
        int safeScale = Math.Max(1, scale);
        if (ShouldCacheElidedText(text))
        {
            if (ElidedTextCache.Count > MaxElidedTextCacheEntries)
            {
                ElidedTextCache.Clear();
            }

            ElidedTextCacheKey key = new(text, maxWidth, safeScale, resolvedFont);
            return ElidedTextCache.GetOrAdd(
                key,
                static cacheKey => BuildElidedTextCore(cacheKey.Text, cacheKey.MaxWidth, cacheKey.Scale, cacheKey.Font));
        }

        return BuildElidedTextCore(text, maxWidth, safeScale, resolvedFont);
    }

    private static string BuildElidedTextCore(string text, int maxWidth, int safeScale, UiFont resolvedFont)
    {
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

    private static bool ShouldCacheElidedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        if (text.Length > 160)
        {
            return false;
        }

        int newlineCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            newlineCount++;
            if (newlineCount > 3)
            {
                return false;
            }
        }

        return true;
    }
}
