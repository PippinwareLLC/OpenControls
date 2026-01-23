using System;

namespace OpenControls.Controls;

public sealed class UiCanvas : UiElement
{
    private sealed class CanvasRenderer : IUiRenderer
    {
        private readonly IUiRenderer _inner;
        private readonly UiPoint _origin;
        private readonly float _zoom;
        private readonly float _panX;
        private readonly float _panY;

        public CanvasRenderer(IUiRenderer inner, UiPoint origin, float zoom, float panX, float panY)
        {
            _inner = inner;
            _origin = origin;
            _zoom = zoom;
            _panX = panX;
            _panY = panY;
        }

        public void FillRect(UiRect rect, UiColor color)
        {
            _inner.FillRect(Transform(rect), color);
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
            int scaled = ScaleThickness(thickness);
            _inner.DrawRect(Transform(rect), color, scaled);
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            _inner.FillRectGradient(Transform(rect), topLeft, topRight, bottomLeft, bottomRight);
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            int scaled = ScaleThickness(cellSize);
            _inner.FillRectCheckerboard(Transform(rect), scaled, colorA, colorB);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            UiPoint screen = Transform(position);
            int scaled = Math.Max(1, (int)Math.Round(scale * _zoom));
            _inner.DrawText(text, screen, color, scaled);
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return _inner.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return _inner.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
            _inner.PushClip(Transform(rect));
        }

        public void PopClip()
        {
            _inner.PopClip();
        }

        private UiRect Transform(UiRect rect)
        {
            int x = _origin.X + (int)Math.Round((rect.X - _panX) * _zoom);
            int y = _origin.Y + (int)Math.Round((rect.Y - _panY) * _zoom);
            int width = (int)Math.Round(rect.Width * _zoom);
            int height = (int)Math.Round(rect.Height * _zoom);
            return new UiRect(x, y, Math.Max(0, width), Math.Max(0, height));
        }

        private UiPoint Transform(UiPoint point)
        {
            int x = _origin.X + (int)Math.Round((point.X - _panX) * _zoom);
            int y = _origin.Y + (int)Math.Round((point.Y - _panY) * _zoom);
            return new UiPoint(x, y);
        }

        private int ScaleThickness(int value)
        {
            if (value <= 0)
            {
                return 1;
            }

            int scaled = (int)Math.Round(value * _zoom);
            return Math.Max(1, scaled);
        }
    }

    private UiRect _viewportBounds;
    private bool _panning;
    private UiPoint _panStartMouse;
    private float _panStartX;
    private float _panStartY;

    public float PanX { get; set; }
    public float PanY { get; set; }
    public float Zoom { get; set; } = 1f;
    public float MinZoom { get; set; } = 0.1f;
    public float MaxZoom { get; set; } = 8f;

    public bool EnablePan { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
    public float ZoomStep { get; set; } = 0.1f;
    public bool ZoomToCursor { get; set; } = true;

    public bool ShowGrid { get; set; } = true;
    public float GridSpacing { get; set; } = 32f;
    public float MajorGridSpacing { get; set; }
    public UiColor GridColor { get; set; } = new UiColor(45, 55, 70);
    public UiColor MajorGridColor { get; set; } = new UiColor(70, 80, 100);
    public int GridThickness { get; set; } = 1;

    public bool ShowOrigin { get; set; } = true;
    public float OriginX { get; set; }
    public float OriginY { get; set; }
    public int OriginSize { get; set; } = 8;
    public UiColor OriginColor { get; set; } = new UiColor(220, 200, 120);

    public int Padding { get; set; } = 4;
    public bool ClipContent { get; set; } = true;
    public UiColor Background { get; set; } = new UiColor(16, 20, 30);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiRect ViewportBounds => _viewportBounds;
    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UpdateLayout();
        ClampZoom();

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        bool mouseInViewport = _viewportBounds.Contains(mouse);
        UiPoint worldMouse = mouseInViewport ? ScreenToWorld(mouse) : new UiPoint(int.MinValue / 4, int.MinValue / 4);
        bool overFocusableChild = mouseInViewport && IsOverFocusableChild(worldMouse);

        if (EnablePan && input.LeftClicked && mouseInViewport && !overFocusableChild)
        {
            _panning = true;
            _panStartMouse = mouse;
            _panStartX = PanX;
            _panStartY = PanY;
            context.Focus.RequestFocus(this);
        }

        if (_panning && input.LeftDown)
        {
            float deltaX = mouse.X - _panStartMouse.X;
            float deltaY = mouse.Y - _panStartMouse.Y;
            PanX = _panStartX - deltaX / Math.Max(Zoom, 0.0001f);
            PanY = _panStartY - deltaY / Math.Max(Zoom, 0.0001f);
        }

        if (_panning && input.LeftReleased)
        {
            _panning = false;
        }

        if (EnableZoom && input.ScrollDelta != 0 && mouseInViewport)
        {
            ApplyZoom(mouse, input.ScrollDelta);
        }

        UiInputState childInput = BuildChildInput(input, mouseInViewport && !_panning);
        UiUpdateContext childContext = new UiUpdateContext(childInput, context.Focus, context.DragDrop, context.DeltaSeconds);
        foreach (UiElement child in Children)
        {
            child.Update(childContext);
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UpdateLayout();

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        if (_viewportBounds.Width <= 0 || _viewportBounds.Height <= 0)
        {
            return;
        }

        if (ClipContent)
        {
            context.Renderer.PushClip(_viewportBounds);
        }

        DrawGrid(context);
        DrawOrigin(context);

        CanvasRenderer renderer = new CanvasRenderer(context.Renderer, new UiPoint(_viewportBounds.X, _viewportBounds.Y), Zoom, PanX, PanY);
        UiRenderContext childContext = new UiRenderContext(renderer);
        foreach (UiElement child in Children)
        {
            child.Render(childContext);
        }

        if (ClipContent)
        {
            context.Renderer.PopClip();
        }

        if (ClipContent && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || _viewportBounds.Width <= 0 || _viewportBounds.Height <= 0)
        {
            return;
        }

        if (ClipContent)
        {
            context.Renderer.PushClip(_viewportBounds);
        }

        CanvasRenderer renderer = new CanvasRenderer(context.Renderer, new UiPoint(_viewportBounds.X, _viewportBounds.Y), Zoom, PanX, PanY);
        UiRenderContext childContext = new UiRenderContext(renderer);
        foreach (UiElement child in Children)
        {
            child.RenderOverlay(childContext);
        }

        if (ClipContent)
        {
            context.Renderer.PopClip();
        }

        if (ClipContent && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }
    }

    protected internal override void OnFocusLost()
    {
        _panning = false;
    }

    private void UpdateLayout()
    {
        int padding = Math.Max(0, Padding);
        _viewportBounds = new UiRect(
            Bounds.X + padding,
            Bounds.Y + padding,
            Math.Max(0, Bounds.Width - padding * 2),
            Math.Max(0, Bounds.Height - padding * 2));
    }

    private UiInputState BuildChildInput(UiInputState input, bool allowMouse)
    {
        UiPoint localMouse = allowMouse
            ? ScreenToWorld(input.MousePosition)
            : new UiPoint(int.MinValue / 4, int.MinValue / 4);

        return new UiInputState
        {
            MousePosition = localMouse,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = allowMouse && input.LeftClicked,
            LeftReleased = input.LeftReleased,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            ScrollDelta = allowMouse ? input.ScrollDelta : 0,
            TextInput = input.TextInput,
            Navigation = input.Navigation
        };
    }

    private void ApplyZoom(UiPoint mouse, int scrollDelta)
    {
        int steps = (int)Math.Round(scrollDelta / 120f);
        if (steps == 0)
        {
            return;
        }

        float step = Math.Clamp(ZoomStep, 0.01f, 1f);
        float factor = MathF.Pow(1f + step, steps);
        float newZoom = Zoom * factor;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - Zoom) < 0.0001f)
        {
            return;
        }

        if (ZoomToCursor)
        {
            float worldX = PanX + (mouse.X - _viewportBounds.X) / Math.Max(Zoom, 0.0001f);
            float worldY = PanY + (mouse.Y - _viewportBounds.Y) / Math.Max(Zoom, 0.0001f);
            PanX = worldX - (mouse.X - _viewportBounds.X) / newZoom;
            PanY = worldY - (mouse.Y - _viewportBounds.Y) / newZoom;
        }

        Zoom = newZoom;
    }

    private void ClampZoom()
    {
        if (Zoom < MinZoom)
        {
            Zoom = MinZoom;
        }
        else if (Zoom > MaxZoom)
        {
            Zoom = MaxZoom;
        }
    }

    private void DrawGrid(UiRenderContext context)
    {
        if (!ShowGrid || GridSpacing <= 0f || Zoom <= 0f)
        {
            return;
        }

        float spacing = GridSpacing;
        float viewWidth = _viewportBounds.Width / Math.Max(Zoom, 0.0001f);
        float viewHeight = _viewportBounds.Height / Math.Max(Zoom, 0.0001f);
        float viewLeft = PanX;
        float viewTop = PanY;
        float viewRight = viewLeft + viewWidth;
        float viewBottom = viewTop + viewHeight;

        float startX = AlignToGrid(viewLeft, spacing, OriginX);
        float startY = AlignToGrid(viewTop, spacing, OriginY);

        int thickness = Math.Max(1, GridThickness);

        for (float x = startX; x <= viewRight; x += spacing)
        {
            int screenX = WorldToScreenX(x);
            UiColor color = IsMajorLine(x, OriginX) ? MajorGridColor : GridColor;
            context.Renderer.FillRect(new UiRect(screenX, _viewportBounds.Y, thickness, _viewportBounds.Height), color);
        }

        for (float y = startY; y <= viewBottom; y += spacing)
        {
            int screenY = WorldToScreenY(y);
            UiColor color = IsMajorLine(y, OriginY) ? MajorGridColor : GridColor;
            context.Renderer.FillRect(new UiRect(_viewportBounds.X, screenY, _viewportBounds.Width, thickness), color);
        }
    }

    private void DrawOrigin(UiRenderContext context)
    {
        if (!ShowOrigin || Zoom <= 0f)
        {
            return;
        }

        int screenX = WorldToScreenX(OriginX);
        int screenY = WorldToScreenY(OriginY);
        int size = Math.Max(2, OriginSize);
        int length = Math.Max(1, (int)Math.Round(size * Zoom));

        context.Renderer.FillRect(new UiRect(screenX - length, screenY, length * 2 + 1, 1), OriginColor);
        context.Renderer.FillRect(new UiRect(screenX, screenY - length, 1, length * 2 + 1), OriginColor);
    }

    private int WorldToScreenX(float worldX)
    {
        return _viewportBounds.X + (int)Math.Round((worldX - PanX) * Zoom);
    }

    private int WorldToScreenY(float worldY)
    {
        return _viewportBounds.Y + (int)Math.Round((worldY - PanY) * Zoom);
    }

    private UiPoint ScreenToWorld(UiPoint screen)
    {
        float invZoom = 1f / Math.Max(Zoom, 0.0001f);
        int worldX = (int)Math.Round(PanX + (screen.X - _viewportBounds.X) * invZoom);
        int worldY = (int)Math.Round(PanY + (screen.Y - _viewportBounds.Y) * invZoom);
        return new UiPoint(worldX, worldY);
    }

    private float AlignToGrid(float value, float spacing, float origin)
    {
        float offset = value - origin;
        float steps = MathF.Floor(offset / spacing);
        return origin + steps * spacing;
    }

    private bool IsMajorLine(float value, float origin)
    {
        if (MajorGridSpacing <= 0f)
        {
            return false;
        }

        float delta = (value - origin) / MajorGridSpacing;
        float nearest = MathF.Round(delta);
        return MathF.Abs(delta - nearest) < 0.001f;
    }

    private bool IsOverFocusableChild(UiPoint worldMouse)
    {
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UiElement child = Children[i];
            if (!child.Visible || !child.Enabled)
            {
                continue;
            }

            UiElement? hit = child.HitTest(worldMouse);
            if (hit != null && hit.IsFocusable)
            {
                return true;
            }
        }

        return false;
    }
}
