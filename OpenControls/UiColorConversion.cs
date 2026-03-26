using System.Globalization;

namespace OpenControls;

public static class UiColorConversion
{
    public static void RgbToHsv(UiColor color, out float h, out float s, out float v)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        v = max;
        if (max <= 0f)
        {
            s = 0f;
            h = 0f;
            return;
        }

        s = delta <= 0f ? 0f : delta / max;
        if (delta <= 0f)
        {
            h = 0f;
            return;
        }

        if (max == r)
        {
            h = (g - b) / delta;
        }
        else if (max == g)
        {
            h = (b - r) / delta + 2f;
        }
        else
        {
            h = (r - g) / delta + 4f;
        }

        h /= 6f;
        if (h < 0f)
        {
            h += 1f;
        }
    }

    public static UiColor HsvToColor(float h, float s, float v, byte alpha = 255)
    {
        h = h - MathF.Floor(h);
        s = Math.Clamp(s, 0f, 1f);
        v = Math.Clamp(v, 0f, 1f);

        float c = v * s;
        float x = c * (1f - MathF.Abs((h * 6f) % 2f - 1f));
        float m = v - c;

        float r;
        float g;
        float b;

        int segment = (int)MathF.Floor(h * 6f);
        switch (segment)
        {
            case 0:
                r = c;
                g = x;
                b = 0f;
                break;
            case 1:
                r = x;
                g = c;
                b = 0f;
                break;
            case 2:
                r = 0f;
                g = c;
                b = x;
                break;
            case 3:
                r = 0f;
                g = x;
                b = c;
                break;
            case 4:
                r = x;
                g = 0f;
                b = c;
                break;
            default:
                r = c;
                g = 0f;
                b = x;
                break;
        }

        return new UiColor(ToByte(r + m), ToByte(g + m), ToByte(b + m), alpha);
    }

    public static string ToHex(UiColor color, bool includeAlpha)
    {
        return includeAlpha
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static bool TryParseHex(string? text, out UiColor color)
    {
        color = UiColor.White;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length != 6 && value.Length != 8)
        {
            return false;
        }

        if (!byte.TryParse(value.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
            !byte.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
            !byte.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return false;
        }

        byte a = 255;
        if (value.Length == 8 &&
            !byte.TryParse(value.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
        {
            return false;
        }

        color = new UiColor(r, g, b, a);
        return true;
    }

    public static byte ToByte(float value)
    {
        float clamped = Math.Clamp(value, 0f, 1f);
        return (byte)Math.Round(clamped * 255f);
    }

    public static float ToFloat(byte value)
    {
        return value / 255f;
    }
}
