using System;

namespace OpenControls.Controls;

public class UiPopup : UiElement
{
    private bool _suppressOutsideClick;
    private bool _suppressPointerInputOnOpen;
    private bool _dragging;
    private UiPoint _dragOffset;
    private UiElement? _pendingFocusTarget;
    private Action? _beforeDeferredFocus;

    public UiPopup()
    {
        ClipChildren = true;
    }

    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(70, 80, 100);
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public UiColor ShadowColor { get; set; } = UiColor.Transparent;
    public UiPoint ShadowOffset { get; set; }
    public int ShadowBlur { get; set; }
    public bool AllowDrag { get; set; }
    public int DragRegionHeight { get; set; }
    public bool ClampDragToParent { get; set; } = true;
    public bool ClampToParent { get; set; } = true;
    public bool CloseOnOutsideClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;

    public bool IsOpen { get; private set; }
    public bool IsDragging => _dragging;
    internal bool HasTransientInputState => IsOpen
        || _dragging
        || _pendingFocusTarget != null
        || _beforeDeferredFocus != null;
    public override bool CapturesPointerInput => IsOpen;

    public event Action? Opened;
    public event Action? Closed;

    public void QueueFocus(UiElement? focusTarget, Action? beforeDeferredFocus = null)
    {
        _pendingFocusTarget = focusTarget;
        _beforeDeferredFocus = beforeDeferredFocus;
    }

    public void Open()
    {
        if (IsOpen || UiTransientInputSuppression.IsSuppressed(this))
        {
            return;
        }

        IsOpen = true;
        _suppressOutsideClick = true;
        _suppressPointerInputOnOpen = true;
        Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
        Opened?.Invoke();
    }

    public void Open(UiRect bounds)
    {
        Bounds = ClampToParent ? UiPopupLayout.Clamp(this, bounds) : bounds;
        Open();
    }

    public void OpenAttached(UiRect anchorBounds, UiPoint size, UiPopupPlacement placement = UiPopupPlacement.BottomLeft)
    {
        UiRect bounds = UiPopupLayout.BuildBounds(anchorBounds, size, placement);
        Open(bounds);
    }

    public void OpenContext(UiPoint point, UiPoint size)
    {
        UiRect bounds = UiPopupLayout.BuildContextBounds(point, size);
        Open(bounds);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _dragging = false;
        Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
        Closed?.Invoke();
    }

    internal void DismissForAncestorSuppression()
    {
        bool wasOpen = IsOpen;
        IsOpen = false;
        _pendingFocusTarget = null;
        _beforeDeferredFocus = null;
        _suppressOutsideClick = false;
        _suppressPointerInputOnOpen = false;
        _dragging = false;
        if (wasOpen)
        {
            Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
            Delegate[] closedHandlers = Closed?.GetInvocationList() ?? [];
            foreach (Delegate closedHandler in closedHandlers)
            {
                try
                {
                    ((Action)closedHandler)();
                }
                catch
                {
                    // Ancestor suppression is cleanup. One consumer must not
                    // prevent the remaining input layers from being dismissed.
                }
            }
        }

        ForceDismissForAncestorSuppression();
    }

    internal void ForceDismissForAncestorSuppression()
    {
        bool changed = IsOpen || HasTransientInputState;
        IsOpen = false;
        _pendingFocusTarget = null;
        _beforeDeferredFocus = null;
        _suppressOutsideClick = false;
        _suppressPointerInputOnOpen = false;
        _dragging = false;
        if (changed)
        {
            Invalidate(UiInvalidationReason.Visibility | UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Layout | UiInvalidationReason.Clip);
        }
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled || !IsOpen)
        {
            return;
        }

        if (_pendingFocusTarget != null || _beforeDeferredFocus != null)
        {
            UiElement? focusTarget = _pendingFocusTarget;
            Action? beforeDeferredFocus = _beforeDeferredFocus;
            _pendingFocusTarget = null;
            _beforeDeferredFocus = null;
            beforeDeferredFocus?.Invoke();
            context.Focus.RequestFocus(focusTarget);
        }

        // A popup can contain a deeper popup (for example a combo box inside a
        // modal). Only the deepest active layer may consume Escape or outside
        // clicks; otherwise the ancestor closes first and can orphan its child.
        UiInputState selfInput = context.GetSelfInput(this);
        UiInputState input = _suppressPointerInputOnOpen
            ? SuppressPointerInput(selfInput)
            : selfInput;
        UpdateDragging(input);
        if (CloseOnEscape && input.Navigation.Escape)
        {
            Close();
            return;
        }

        if (_suppressOutsideClick)
        {
            _suppressOutsideClick = false;
        }
        else if (CloseOnOutsideClick && IsOutsideClick(input) && !ContainsOverlayPoint(input.MousePosition))
        {
            Close();
            return;
        }

        UiUpdateContext childContext = _suppressPointerInputOnOpen
            ? new UiUpdateContext(
                input,
                context.Focus,
                context.DragDrop,
                context.DeltaSeconds,
                context.DefaultFont,
                context.Clipboard,
                context.ActiveInputLayer)
            : context;

        base.Update(childContext);
        _suppressPointerInputOnOpen = false;
    }

    private void UpdateDragging(UiInputState input)
    {
        if (!AllowDrag || DragRegionHeight <= 0)
        {
            _dragging = false;
            return;
        }

        int dragHeight = Math.Min(Math.Max(1, DragRegionHeight), Math.Max(1, Bounds.Height));
        UiRect dragRegion = new(Bounds.X, Bounds.Y, Bounds.Width, dragHeight);
        if (!_dragging && input.LeftClicked && dragRegion.Contains(input.MousePosition))
        {
            _dragging = true;
            _dragOffset = new UiPoint(
                input.MousePosition.X - Bounds.X,
                input.MousePosition.Y - Bounds.Y);
        }

        if (_dragging && (input.LeftDown || input.LeftReleased))
        {
            int x = input.MousePosition.X - _dragOffset.X;
            int y = input.MousePosition.Y - _dragOffset.Y;
            if (ClampDragToParent && Parent != null)
            {
                UiRect parent = Parent.Bounds;
                int maxX = parent.Right - Bounds.Width;
                int maxY = parent.Bottom - Bounds.Height;
                x = maxX < parent.X ? parent.X : Math.Clamp(x, parent.X, maxX);
                y = maxY < parent.Y ? parent.Y : Math.Clamp(y, parent.Y, maxY);
            }

            Bounds = new UiRect(x, y, Bounds.Width, Bounds.Height);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !IsOpen)
        {
            return null;
        }

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UiElement child = Children[i];
            UiElement? childHit = child.HitTest(point);
            if (childHit != null)
            {
                return childHit;
            }
        }

        if (!Bounds.Contains(point))
        {
            return null;
        }

        return this;
    }

    public override void Render(UiRenderContext context)
    {
        // Popups render in the overlay pass only.
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !IsOpen)
        {
            return;
        }

        RenderShadow(context.Renderer);

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (ClipChildren)
        {
            context.Renderer.PushClip(ClipBounds);
        }

        foreach (UiElement child in Children)
        {
            context.RenderChild(child);
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        // Child overlays are independent surfaces and may legitimately extend
        // past popup content bounds. A combo dropdown inside a compact modal is
        // the canonical case, and it must remain above the parent border.
        foreach (UiElement child in Children)
        {
            context.RenderChildOverlay(child);
        }
    }

    private void RenderShadow(IUiRenderer renderer)
    {
        int blur = Math.Max(0, ShadowBlur);
        if (ShadowColor.A == 0 || blur == 0)
        {
            return;
        }

        byte layerAlpha = (byte)Math.Max(1, ShadowColor.A / blur);
        UiColor layerColor = new(ShadowColor.R, ShadowColor.G, ShadowColor.B, layerAlpha);
        for (int spread = blur; spread >= 1; spread--)
        {
            UiRect shadowBounds = new(
                Bounds.X + ShadowOffset.X - spread,
                Bounds.Y + ShadowOffset.Y - spread,
                Bounds.Width + spread * 2,
                Bounds.Height + spread * 2);
            UiRenderHelpers.FillRectRounded(
                renderer,
                shadowBounds,
                Math.Max(0, CornerRadius + spread),
                layerColor);
        }
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (IsOpen)
        {
            status |= UiItemStatusFlags.Active;
        }

        return status;
    }

    private bool ContainsOverlayPoint(UiPoint point)
    {
        return HitTest(point) != null;
    }

    private static bool IsOutsideClick(UiInputState input)
    {
        return input.LeftClicked || input.RightClicked || input.MiddleClicked;
    }

    private static UiInputState SuppressPointerInput(UiInputState input)
    {
        return new UiInputState
        {
            MousePosition = input.MousePosition,
            ScreenMousePosition = input.ScreenMousePosition,
            PreciseMousePosition = input.PreciseMousePosition,
            PreciseScreenMousePosition = input.PreciseScreenMousePosition,
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            PinchZoom = input.PinchZoom,
            TextInput = input.TextInput,
            Composition = input.Composition,
            KeysDown = input.KeysDown,
            KeysPressed = input.KeysPressed,
            KeysReleased = input.KeysReleased,
            Navigation = input.Navigation
        };
    }
}
