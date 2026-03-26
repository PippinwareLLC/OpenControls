namespace OpenControls;

internal static class UiInputTransform
{
    public static UiInputState Translate(UiInputState input, int offsetX, int offsetY)
    {
        return new UiInputState
        {
            MousePosition = new UiPoint(input.MousePosition.X - offsetX, input.MousePosition.Y - offsetY),
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftDoubleClicked = input.LeftDoubleClicked,
            LeftReleased = input.LeftReleased,
            RightDown = input.RightDown,
            RightClicked = input.RightClicked,
            RightDoubleClicked = input.RightDoubleClicked,
            RightReleased = input.RightReleased,
            MiddleDown = input.MiddleDown,
            MiddleClicked = input.MiddleClicked,
            MiddleDoubleClicked = input.MiddleDoubleClicked,
            MiddleReleased = input.MiddleReleased,
            LeftDragOrigin = TranslatePoint(input.LeftDragOrigin, offsetX, offsetY),
            RightDragOrigin = TranslatePoint(input.RightDragOrigin, offsetX, offsetY),
            MiddleDragOrigin = TranslatePoint(input.MiddleDragOrigin, offsetX, offsetY),
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            TextInput = input.TextInput,
            KeysDown = input.KeysDown,
            KeysPressed = input.KeysPressed,
            KeysReleased = input.KeysReleased,
            Navigation = input.Navigation
        };
    }

    private static UiPoint? TranslatePoint(UiPoint? point, int offsetX, int offsetY)
    {
        return point is UiPoint value
            ? new UiPoint(value.X - offsetX, value.Y - offsetY)
            : null;
    }
}
