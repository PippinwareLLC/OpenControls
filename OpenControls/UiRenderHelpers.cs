namespace OpenControls;

public static class UiRenderHelpers
{
    public static void DrawTextBold(IUiRenderer renderer, string text, UiPoint position, UiColor color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        renderer.DrawText(text, position, color, scale);
        renderer.DrawText(text, new UiPoint(position.X + 1, position.Y), color, scale);
    }

    public static void FillRectRounded(IUiRenderer renderer, UiRect rect, int radius, UiColor color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            renderer.FillRect(rect, color);
            return;
        }

        for (int row = 0; row < rect.Height; row++)
        {
            int inset = 0;
            if (row < clampedRadius)
            {
                inset = ComputeCornerInset(clampedRadius, row);
            }
            else if (row >= rect.Height - clampedRadius)
            {
                inset = ComputeCornerInset(clampedRadius, rect.Height - row - 1);
            }

            int width = rect.Width - inset * 2;
            if (width <= 0)
            {
                continue;
            }

            renderer.FillRect(new UiRect(rect.X + inset, rect.Y + row, width, 1), color);
        }
    }

    public static void MaskRectRounded(IUiRenderer renderer, UiRect rect, int radius, UiColor maskColor)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || maskColor.A == 0)
        {
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            return;
        }

        for (int row = 0; row < rect.Height; row++)
        {
            int inset = 0;
            if (row < clampedRadius)
            {
                inset = ComputeCornerInset(clampedRadius, row);
            }
            else if (row >= rect.Height - clampedRadius)
            {
                inset = ComputeCornerInset(clampedRadius, rect.Height - row - 1);
            }

            if (inset <= 0)
            {
                continue;
            }

            int y = rect.Y + row;
            renderer.FillRect(new UiRect(rect.X, y, inset, 1), maskColor);
            renderer.FillRect(new UiRect(rect.Right - inset, y, inset, 1), maskColor);
        }
    }

    public static void DrawRectRounded(IUiRenderer renderer, UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0)
        {
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            renderer.DrawRect(rect, color, thickness);
            return;
        }

        int maxThickness = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        for (int t = 0; t < maxThickness; t++)
        {
            UiRect insetRect = new UiRect(rect.X + t, rect.Y + t, rect.Width - t * 2, rect.Height - t * 2);
            if (insetRect.Width <= 0 || insetRect.Height <= 0)
            {
                break;
            }

            int insetRadius = Math.Max(0, clampedRadius - t);
            DrawRectRoundedOutline(renderer, insetRect, insetRadius, color);
        }
    }

    public static void FillRectGradient(
        IUiRenderer renderer,
        UiRect rect,
        UiColor topLeft,
        UiColor topRight,
        UiColor bottomLeft,
        UiColor bottomRight)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (SameColor(topLeft, topRight) && SameColor(topLeft, bottomLeft) && SameColor(topLeft, bottomRight))
        {
            renderer.FillRect(rect, topLeft);
            return;
        }

        int width = rect.Width;
        int height = rect.Height;
        int lastRow = Math.Max(0, height - 1);
        int lastCol = Math.Max(0, width - 1);

        for (int row = 0; row < height; row++)
        {
            float ty = lastRow == 0 ? 0f : row / (float)lastRow;
            UiColor left = Lerp(topLeft, bottomLeft, ty);
            UiColor right = Lerp(topRight, bottomRight, ty);

            if (SameColor(left, right))
            {
                renderer.FillRect(new UiRect(rect.X, rect.Y + row, width, 1), left);
                continue;
            }

            for (int col = 0; col < width; col++)
            {
                float tx = lastCol == 0 ? 0f : col / (float)lastCol;
                UiColor color = Lerp(left, right, tx);
                renderer.FillRect(new UiRect(rect.X + col, rect.Y + row, 1, 1), color);
            }
        }
    }

    public static void FillRectCheckerboard(
        IUiRenderer renderer,
        UiRect rect,
        int cellSize,
        UiColor colorA,
        UiColor colorB)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (SameColor(colorA, colorB))
        {
            renderer.FillRect(rect, colorA);
            return;
        }

        int size = Math.Max(1, cellSize);
        int startX = rect.X;
        int startY = rect.Y;

        for (int y = startY; y < rect.Bottom; y += size)
        {
            int height = Math.Min(size, rect.Bottom - y);
            int rowIndex = (y - startY) / size;

            for (int x = startX; x < rect.Right; x += size)
            {
                int width = Math.Min(size, rect.Right - x);
                int colIndex = (x - startX) / size;
                bool useA = (rowIndex + colIndex) % 2 == 0;
                renderer.FillRect(new UiRect(x, y, width, height), useA ? colorA : colorB);
            }
        }
    }

    private static UiColor Lerp(UiColor a, UiColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new UiColor(
            LerpByte(a.R, b.R, t),
            LerpByte(a.G, b.G, t),
            LerpByte(a.B, b.B, t),
            LerpByte(a.A, b.A, t));
    }

    private static byte LerpByte(byte a, byte b, float t)
    {
        return (byte)Math.Round(a + (b - a) * t);
    }

    private static bool SameColor(UiColor a, UiColor b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    }

    private static void DrawRectRoundedOutline(IUiRenderer renderer, UiRect rect, int radius, UiColor color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (radius <= 0)
        {
            renderer.DrawRect(rect, color, 1);
            return;
        }

        int right = rect.Right - 1;
        int bottom = rect.Bottom - 1;

        for (int row = 0; row < rect.Height; row++)
        {
            int inset = 0;
            if (row < radius)
            {
                inset = ComputeCornerInset(radius, row);
            }
            else if (row >= rect.Height - radius)
            {
                inset = ComputeCornerInset(radius, rect.Height - row - 1);
            }

            int width = rect.Width - inset * 2;
            if (width <= 0)
            {
                continue;
            }

            int y = rect.Y + row;
            int leftX = rect.X + inset;
            int rightX = right - inset;

            if (row == 0 || row == rect.Height - 1)
            {
                renderer.FillRect(new UiRect(leftX, y, width, 1), color);
                continue;
            }

            if (leftX == rightX)
            {
                renderer.FillRect(new UiRect(leftX, y, 1, 1), color);
                continue;
            }

            renderer.FillRect(new UiRect(leftX, y, 1, 1), color);
            renderer.FillRect(new UiRect(rightX, y, 1, 1), color);
        }
    }

    private static int ClampRadius(UiRect rect, int radius)
    {
        if (radius <= 0)
        {
            return 0;
        }

        int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
        if (maxRadius <= 0)
        {
            return 0;
        }

        return Math.Clamp(radius, 0, maxRadius);
    }

    private static int ComputeCornerInset(int radius, int row)
    {
        if (radius <= 0 || row < 0)
        {
            return 0;
        }

        int dy = radius - row - 1;
        long r = radius;
        long dyValue = dy;
        double inside = r * r - dyValue * dyValue;
        if (inside <= 0)
        {
            return radius;
        }

        int inset = (int)Math.Ceiling(radius - Math.Sqrt(inside));
        if (inset < 0)
        {
            return 0;
        }

        return Math.Min(inset, radius);
    }
}
