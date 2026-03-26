namespace OpenControls;

public sealed class UiDpiCompensation
{
    private float _targetDpi = 96f;
    private float _currentDpi = 96f;
    private float _minimumScale = 1f;

    public bool Enabled { get; set; } = true;

    public float TargetDpi
    {
        get => _targetDpi;
        set => _targetDpi = Math.Max(1f, value);
    }

    public float CurrentDpi
    {
        get => _currentDpi;
        set => _currentDpi = Math.Max(1f, value);
    }

    public float MinimumScale
    {
        get => _minimumScale;
        set => _minimumScale = Math.Max(0.1f, value);
    }

    public float ScaleFactor
    {
        get
        {
            if (!Enabled)
            {
                return 1f;
            }

            float scale = CurrentDpi / TargetDpi;
            return Math.Max(MinimumScale, scale);
        }
    }

    public void SetScaleFactor(float scaleFactor)
    {
        CurrentDpi = Math.Max(0.1f, scaleFactor) * TargetDpi;
    }

    public void SetScaleFromContentSize(int logicalWidth, int logicalHeight, int physicalWidth, int physicalHeight)
    {
        SetScaleFactor(ResolveScaleFactor(logicalWidth, logicalHeight, physicalWidth, physicalHeight));
    }

    public static float ResolveScaleFactor(int logicalWidth, int logicalHeight, int physicalWidth, int physicalHeight)
    {
        int safeLogicalWidth = Math.Max(1, logicalWidth);
        int safeLogicalHeight = Math.Max(1, logicalHeight);
        int safePhysicalWidth = Math.Max(1, physicalWidth);
        int safePhysicalHeight = Math.Max(1, physicalHeight);
        float scaleX = safePhysicalWidth / (float)safeLogicalWidth;
        float scaleY = safePhysicalHeight / (float)safeLogicalHeight;
        return Math.Max(1f, Math.Max(scaleX, scaleY));
    }

    public UiPoint ToLogical(UiPoint point)
    {
        return new UiPoint(ToLogicalPixels(point.X), ToLogicalPixels(point.Y));
    }

    public UiPoint? ToLogical(UiPoint? point)
    {
        return point is UiPoint value ? ToLogical(value) : null;
    }

    public UiRect ToLogical(UiRect rect)
    {
        int left = ToLogicalPixels(rect.Left);
        int top = ToLogicalPixels(rect.Top);
        int right = ToLogicalPixels(rect.Right);
        int bottom = ToLogicalPixels(rect.Bottom);
        return new UiRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public UiPoint ToPhysical(UiPoint point)
    {
        return new UiPoint(ToPhysicalPixels(point.X), ToPhysicalPixels(point.Y));
    }

    public UiPoint? ToPhysical(UiPoint? point)
    {
        return point is UiPoint value ? ToPhysical(value) : null;
    }

    public UiRect ToPhysical(UiRect rect)
    {
        int left = ToPhysicalPixels(rect.Left);
        int top = ToPhysicalPixels(rect.Top);
        int right = ToPhysicalPixels(rect.Right);
        int bottom = ToPhysicalPixels(rect.Bottom);
        return new UiRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public UiTextInputRequest ToPhysical(UiTextInputRequest request)
    {
        return new UiTextInputRequest(
            ToPhysical(request.Bounds),
            request.IsMultiLine,
            ToPhysical(request.CaretBounds),
            ToPhysical(request.CandidateBounds),
            request.SupportsComposition);
    }

    public UiInputState ToLogical(UiInputState input)
    {
        return new UiInputState
        {
            MousePosition = ToLogical(input.MousePosition),
            ScreenMousePosition = ToLogical(input.ScreenMousePosition),
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
            LeftDragOrigin = ToLogical(input.LeftDragOrigin),
            RightDragOrigin = ToLogical(input.RightDragOrigin),
            MiddleDragOrigin = ToLogical(input.MiddleDragOrigin),
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            TextInput = input.TextInput,
            Composition = input.Composition,
            KeysDown = input.KeysDown,
            KeysPressed = input.KeysPressed,
            KeysReleased = input.KeysReleased,
            Navigation = input.Navigation
        };
    }

    public int ToLogicalPixels(int pixels)
    {
        return Enabled ? (int)Math.Round(pixels / ScaleFactor) : pixels;
    }

    public int ToLogicalExtent(int extent)
    {
        if (!Enabled || extent <= 0)
        {
            return extent;
        }

        return Math.Max(1, (int)Math.Ceiling(extent / ScaleFactor));
    }

    public int ToPhysicalPixels(int pixels)
    {
        return Enabled ? (int)Math.Round(pixels * ScaleFactor) : pixels;
    }

    public int ToPhysicalExtent(int extent)
    {
        if (!Enabled || extent <= 0)
        {
            return extent;
        }

        return Math.Max(1, (int)Math.Ceiling(extent * ScaleFactor));
    }
}
