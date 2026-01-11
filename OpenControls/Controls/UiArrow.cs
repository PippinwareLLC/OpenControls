namespace OpenControls.Controls;

public enum UiArrowDirection
{
    Right,
    Down,
    Left,
    Up
}

public static class UiArrow
{
    public static void DrawTriangle(IUiRenderer renderer, UiRect bounds, UiArrowDirection direction, UiColor color)
    {
        int size = Math.Min(bounds.Width, bounds.Height);
        if (size <= 0)
        {
            return;
        }

        int x = bounds.X + (bounds.Width - size) / 2;
        int y = bounds.Y + (bounds.Height - size) / 2;
        DrawTriangle(renderer, x, y, size, direction, color);
    }

    public static void DrawTriangle(IUiRenderer renderer, int x, int y, int size, UiArrowDirection direction, UiColor color)
    {
        if (size <= 0)
        {
            return;
        }

        switch (direction)
        {
            case UiArrowDirection.Down:
                for (int row = 0; row < size; row++)
                {
                    int width = size - row;
                    int offsetX = (size - width) / 2;
                    renderer.FillRect(new UiRect(x + offsetX, y + row, width, 1), color);
                }
                break;
            case UiArrowDirection.Up:
                for (int row = 0; row < size; row++)
                {
                    int width = row + 1;
                    int offsetX = (size - width) / 2;
                    renderer.FillRect(new UiRect(x + offsetX, y + row, width, 1), color);
                }
                break;
            case UiArrowDirection.Left:
                for (int col = 0; col < size; col++)
                {
                    int height = col + 1;
                    int offsetY = (size - height) / 2;
                    renderer.FillRect(new UiRect(x + col, y + offsetY, 1, height), color);
                }
                break;
            case UiArrowDirection.Right:
                for (int col = 0; col < size; col++)
                {
                    int height = size - col;
                    int offsetY = (size - height) / 2;
                    renderer.FillRect(new UiRect(x + col, y + offsetY, 1, height), color);
                }
                break;
        }
    }
}
