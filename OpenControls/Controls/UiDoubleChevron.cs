namespace OpenControls.Controls;

/// <summary>Draws a petite, dependency-free double chevron inside an existing hit target.</summary>
public static class UiDoubleChevron
{
    public static void Draw(
        IUiRenderer renderer,
        UiRect bounds,
        UiArrowDirection direction,
        UiColor color)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (bounds.Width <= 0 || bounds.Height <= 0 || color.A <= 0)
        {
            return;
        }

        int available = Math.Min(bounds.Width, bounds.Height);
        if (available < 3)
        {
            return;
        }

        int arm = Math.Clamp((available - 1) / 2, 1, 4);
        int sourceWidth = arm * 2 + 1;
        int sourceHeight = arm * 2 - 1;
        int renderWidth = direction is UiArrowDirection.Up or UiArrowDirection.Down
            ? sourceHeight
            : sourceWidth;
        int renderHeight = direction is UiArrowDirection.Up or UiArrowDirection.Down
            ? sourceWidth
            : sourceHeight;
        int originX = bounds.X + (bounds.Width - renderWidth) / 2;
        int originY = bounds.Y + (bounds.Height - renderHeight) / 2;

        for (int chevron = 0; chevron < 2; chevron++)
        {
            int offsetX = chevron * (arm + 1);
            for (int step = 0; step < arm; step++)
            {
                DrawPoint(offsetX + step, step);
                int mirroredY = sourceHeight - 1 - step;
                if (mirroredY != step)
                {
                    DrawPoint(offsetX + step, mirroredY);
                }
            }
        }

        void DrawPoint(int sourceX, int sourceY)
        {
            (int x, int y) = direction switch
            {
                UiArrowDirection.Left => (sourceWidth - 1 - sourceX, sourceY),
                UiArrowDirection.Down => (sourceHeight - 1 - sourceY, sourceX),
                UiArrowDirection.Up => (sourceY, sourceWidth - 1 - sourceX),
                _ => (sourceX, sourceY)
            };
            renderer.FillRect(new UiRect(originX + x, originY + y, 1, 1), color);
        }
    }
}
