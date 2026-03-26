using OpenControls.Controls;

namespace OpenControls;

internal static class UiNumericValueHelpers
{
    public static bool HasFloatRange(float min, float max)
    {
        return max > min;
    }

    public static bool HasIntRange(int min, int max)
    {
        return max > min;
    }

    public static float ApplyFloatConstraints(
        float value,
        float min,
        float max,
        float step,
        bool wholeNumbers,
        string? valueFormat,
        bool clampByDefault,
        UiModifierFlags flags)
    {
        float next = ApplyFloatRange(value, min, max, clampByDefault, flags);

        if (wholeNumbers)
        {
            next = MathF.Round(next);
        }

        if (step > 0f)
        {
            next = SnapFloatToStep(next, min, step);
        }

        if (!wholeNumbers && !flags.HasFlag(UiModifierFlags.NoRoundToFormat))
        {
            int? precision = GetDecimalPrecision(valueFormat);
            if (precision.HasValue)
            {
                next = MathF.Round(next, precision.Value);
            }
        }

        return ApplyFloatRange(next, min, max, clampByDefault, flags);
    }

    public static int ApplyIntConstraints(int value, int min, int max, int step, bool clampByDefault, UiModifierFlags flags)
    {
        int next = ApplyIntRange(value, min, max, clampByDefault, flags);
        if (step > 1)
        {
            next = SnapIntToStep(next, min, step);
        }

        return ApplyIntRange(next, min, max, clampByDefault, flags);
    }

    public static int? GetDecimalPrecision(string? valueFormat)
    {
        if (string.IsNullOrWhiteSpace(valueFormat))
        {
            return null;
        }

        int dotIndex = valueFormat.IndexOf('.');
        if (dotIndex < 0)
        {
            return valueFormat.Contains('0') || valueFormat.Contains('#') ? 0 : null;
        }

        int precision = 0;
        for (int i = dotIndex + 1; i < valueFormat.Length; i++)
        {
            char character = valueFormat[i];
            if (character == '0' || character == '#')
            {
                precision++;
                continue;
            }

            break;
        }

        return precision;
    }

    public static float GetWrappedFloat(float value, float min, float max)
    {
        if (!HasFloatRange(min, max))
        {
            return min;
        }

        if (value >= min && value <= max)
        {
            return value;
        }

        float range = max - min;
        if (range <= 0f)
        {
            return min;
        }

        float wrapped = min + (value - min) % range;
        if (wrapped < min)
        {
            wrapped += range;
        }

        if (wrapped == min && value > max)
        {
            return max;
        }

        return wrapped;
    }

    public static int GetWrappedInt(int value, int min, int max)
    {
        if (!HasIntRange(min, max))
        {
            return min;
        }

        if (value >= min && value <= max)
        {
            return value;
        }

        int range = (max - min) + 1;
        if (range <= 0)
        {
            return min;
        }

        int wrapped = min + (value - min) % range;
        if (wrapped < min)
        {
            wrapped += range;
        }

        return wrapped;
    }

    public static UiModifierFlags ToModifierFlags(UiDragFlags flags)
    {
        UiModifierFlags result = UiModifierFlags.None;
        if (flags.HasFlag(UiDragFlags.AlwaysClamp))
        {
            result |= UiModifierFlags.AlwaysClamp;
        }

        if (flags.HasFlag(UiDragFlags.NoRoundToFormat))
        {
            result |= UiModifierFlags.NoRoundToFormat;
        }

        if (flags.HasFlag(UiDragFlags.WrapAround))
        {
            result |= UiModifierFlags.WrapAround;
        }

        return result;
    }

    public static UiModifierFlags ToModifierFlags(UiSliderFlags flags)
    {
        UiModifierFlags result = UiModifierFlags.None;
        if (flags.HasFlag(UiSliderFlags.AlwaysClamp))
        {
            result |= UiModifierFlags.AlwaysClamp;
        }

        if (flags.HasFlag(UiSliderFlags.NoRoundToFormat))
        {
            result |= UiModifierFlags.NoRoundToFormat;
        }

        if (flags.HasFlag(UiSliderFlags.WrapAround))
        {
            result |= UiModifierFlags.WrapAround;
        }

        return result;
    }

    private static float SnapFloatToStep(float value, float min, float step)
    {
        if (step <= 0f)
        {
            return value;
        }

        float steps = MathF.Round((value - min) / step);
        return min + steps * step;
    }

    private static int SnapIntToStep(int value, int min, int step)
    {
        if (step <= 1)
        {
            return value;
        }

        int steps = (int)MathF.Round((value - min) / (float)step);
        return min + steps * step;
    }

    private static float ApplyFloatRange(float value, float min, float max, bool clampByDefault, UiModifierFlags flags)
    {
        if (!HasFloatRange(min, max))
        {
            return min;
        }

        if (flags.HasFlag(UiModifierFlags.WrapAround))
        {
            return GetWrappedFloat(value, min, max);
        }

        if (clampByDefault || flags.HasFlag(UiModifierFlags.AlwaysClamp))
        {
            return Math.Clamp(value, min, max);
        }

        return value;
    }

    private static int ApplyIntRange(int value, int min, int max, bool clampByDefault, UiModifierFlags flags)
    {
        if (!HasIntRange(min, max))
        {
            return min;
        }

        if (flags.HasFlag(UiModifierFlags.WrapAround))
        {
            return GetWrappedInt(value, min, max);
        }

        if (clampByDefault || flags.HasFlag(UiModifierFlags.AlwaysClamp))
        {
            return Math.Clamp(value, min, max);
        }

        return value;
    }
}

[Flags]
internal enum UiModifierFlags
{
    None = 0,
    AlwaysClamp = 1 << 0,
    NoRoundToFormat = 1 << 1,
    WrapAround = 1 << 2
}
