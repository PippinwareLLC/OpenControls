namespace OpenControls;

public static class UiInputHostHelpers
{
    public static bool DetectDoubleClick(
        bool clicked,
        UiPoint mousePosition,
        double currentTimeSeconds,
        ref double lastClickTimeSeconds,
        ref UiPoint lastClickPosition,
        double maxDelaySeconds = 0.35,
        int maxDistance = 6)
    {
        bool doubleClicked = false;
        if (clicked)
        {
            double delay = currentTimeSeconds - lastClickTimeSeconds;
            int dx = mousePosition.X - lastClickPosition.X;
            int dy = mousePosition.Y - lastClickPosition.Y;
            int distanceSquared = dx * dx + dy * dy;
            int maxDistanceSquared = maxDistance * maxDistance;
            doubleClicked = delay >= 0d && delay <= maxDelaySeconds && distanceSquared <= maxDistanceSquared;
            lastClickTimeSeconds = currentTimeSeconds;
            lastClickPosition = mousePosition;
        }

        return doubleClicked;
    }

    public static UiPoint? UpdateDragOrigin(
        bool down,
        bool clicked,
        bool released,
        UiPoint mousePosition,
        ref UiPoint? dragOrigin)
    {
        if (clicked || (down && dragOrigin == null))
        {
            dragOrigin = mousePosition;
        }

        UiPoint? currentOrigin = dragOrigin;
        if (released)
        {
            dragOrigin = null;
        }

        return currentOrigin;
    }
}
