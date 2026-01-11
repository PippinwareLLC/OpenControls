namespace OpenControls.Controls;

public sealed class UiColorPicker : UiElement
{
    private UiColor _color = new UiColor(255, 0, 0);
    private float _h;
    private float _s = 1f;
    private float _v = 1f;
    private byte _alpha = 255;
    private bool _draggingSv;
    private bool _draggingHue;
    private bool _draggingAlpha;
    private bool _focused;

    public UiColor Color
    {
        get => _color;
        set => SetColor(value);
    }

    public int Padding { get; set; } = 4;
    public int HueBarWidth { get; set; } = 12;
    public bool ShowAlpha { get; set; }
    public int AlphaBarHeight { get; set; } = 10;
    public int CheckerSize { get; set; } = 6;
    public UiColor CheckerColorLight { get; set; } = new UiColor(80, 90, 110);
    public UiColor CheckerColorDark { get; set; } = new UiColor(50, 60, 80);
    public int GridSize { get; set; }
    public int HueSegments { get; set; }
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor SelectionBorder { get; set; } = UiColor.White;
    public UiColor SelectionShadow { get; set; } = new UiColor(0, 0, 0);
    public int CornerRadius { get; set; }

    public event Action<UiColor>? ColorChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        UiRect svRect = GetSvRect();
        UiRect hueRect = GetHueRect(svRect);
        UiRect alphaRect = ShowAlpha ? GetAlphaRect(svRect, hueRect) : default;

        if (input.LeftClicked)
        {
            if (svRect.Contains(input.MousePosition))
            {
                _draggingSv = true;
                _draggingHue = false;
                _draggingAlpha = false;
                context.Focus.RequestFocus(this);
                SetSvFromPoint(svRect, input.MousePosition);
            }
            else if (hueRect.Contains(input.MousePosition))
            {
                _draggingHue = true;
                _draggingSv = false;
                _draggingAlpha = false;
                context.Focus.RequestFocus(this);
                SetHueFromPoint(hueRect, input.MousePosition);
            }
            else if (ShowAlpha && alphaRect.Contains(input.MousePosition))
            {
                _draggingAlpha = true;
                _draggingSv = false;
                _draggingHue = false;
                context.Focus.RequestFocus(this);
                SetAlphaFromPoint(alphaRect, input.MousePosition);
            }
        }

        if (_draggingSv && input.LeftDown)
        {
            SetSvFromPoint(svRect, input.MousePosition);
        }

        if (_draggingHue && input.LeftDown)
        {
            SetHueFromPoint(hueRect, input.MousePosition);
        }

        if (_draggingAlpha && input.LeftDown)
        {
            SetAlphaFromPoint(alphaRect, input.MousePosition);
        }

        if ((_draggingSv || _draggingHue || _draggingAlpha) && input.LeftReleased)
        {
            _draggingSv = false;
            _draggingHue = false;
            _draggingAlpha = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        UiRect svRect = GetSvRect();
        UiRect hueRect = GetHueRect(svRect);
        UiRect alphaRect = ShowAlpha ? GetAlphaRect(svRect, hueRect) : default;

        if (svRect.Width <= 0 || svRect.Height <= 0)
        {
            base.Render(context);
            return;
        }

        DrawSvGrid(context, svRect);
        DrawHueBar(context, hueRect);
        if (ShowAlpha)
        {
            DrawAlphaBar(context, alphaRect);
        }
        DrawSvSelection(context, svRect);
        DrawHueSelection(context, hueRect);
        if (ShowAlpha)
        {
            DrawAlphaSelection(context, alphaRect);
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _draggingSv = false;
        _draggingHue = false;
        _draggingAlpha = false;
    }

    private UiRect GetSvRect()
    {
        int padding = Math.Max(0, Padding);
        int barWidth = Math.Max(0, HueBarWidth);
        int alphaHeight = ShowAlpha ? Math.Max(0, AlphaBarHeight) + padding : 0;
        int availableWidth = Math.Max(0, Bounds.Width - padding * 3 - barWidth);
        int availableHeight = Math.Max(0, Bounds.Height - padding * 2 - alphaHeight);
        int size = Math.Max(0, Math.Min(availableWidth, availableHeight));
        int x = Bounds.X + padding;
        int y = Bounds.Y + padding;
        return new UiRect(x, y, size, size);
    }

    private UiRect GetHueRect(UiRect svRect)
    {
        int padding = Math.Max(0, Padding);
        int width = Math.Max(0, HueBarWidth);
        int x = svRect.Right + padding;
        return new UiRect(x, svRect.Y, width, svRect.Height);
    }

    private UiRect GetAlphaRect(UiRect svRect, UiRect hueRect)
    {
        int padding = Math.Max(0, Padding);
        int height = Math.Max(0, AlphaBarHeight);
        int width = hueRect.Width > 0 ? hueRect.Right - svRect.X : svRect.Width;
        int x = svRect.X;
        int y = svRect.Bottom + padding;
        return new UiRect(x, y, Math.Max(0, width), height);
    }

    private void SetSvFromPoint(UiRect svRect, UiPoint point)
    {
        if (svRect.Width <= 1 || svRect.Height <= 1)
        {
            return;
        }

        float s = (point.X - svRect.X) / (float)(svRect.Width - 1);
        float v = 1f - (point.Y - svRect.Y) / (float)(svRect.Height - 1);
        _s = Math.Clamp(s, 0f, 1f);
        _v = Math.Clamp(v, 0f, 1f);
        UpdateColorFromHsv();
    }

    private void SetHueFromPoint(UiRect hueRect, UiPoint point)
    {
        if (hueRect.Height <= 1)
        {
            return;
        }

        float h = (point.Y - hueRect.Y) / (float)(hueRect.Height - 1);
        _h = Math.Clamp(h, 0f, 1f);
        UpdateColorFromHsv();
    }

    private void SetAlphaFromPoint(UiRect alphaRect, UiPoint point)
    {
        if (alphaRect.Width <= 1)
        {
            return;
        }

        float t = (point.X - alphaRect.X) / (float)(alphaRect.Width - 1);
        t = Math.Clamp(t, 0f, 1f);
        byte alpha = (byte)Math.Round(t * 255f);
        if (_alpha == alpha)
        {
            return;
        }

        _alpha = alpha;
        UpdateColorFromHsv();
    }

    private void DrawSvGrid(UiRenderContext context, UiRect rect)
    {
        int columns = GridSize > 1 ? Math.Min(GridSize, rect.Width) : rect.Width;
        int rows = GridSize > 1 ? Math.Min(GridSize, rect.Height) : rect.Height;
        int cellWidth = Math.Max(1, rect.Width / Math.Max(1, columns));
        int cellHeight = Math.Max(1, rect.Height / Math.Max(1, rows));
        byte drawAlpha = ShowAlpha ? (byte)255 : _alpha;

        int y = rect.Y;
        for (int row = 0; row < rows; row++)
        {
            int rowHeight = row == rows - 1 ? rect.Bottom - y : cellHeight;
            int x = rect.X;
            float v = rows == 1 ? _v : 1f - row / (float)(rows - 1);

            for (int col = 0; col < columns; col++)
            {
                int colWidth = col == columns - 1 ? rect.Right - x : cellWidth;
                float s = columns == 1 ? _s : col / (float)(columns - 1);
                UiColor color = HsvToColor(_h, s, v, drawAlpha);
                context.Renderer.FillRect(new UiRect(x, y, colWidth, rowHeight), color);
                x += colWidth;
            }

            y += rowHeight;
        }
    }

    private void DrawHueBar(UiRenderContext context, UiRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        int segments = HueSegments > 1 ? Math.Min(HueSegments, rect.Height) : rect.Height;
        int segmentHeight = Math.Max(1, rect.Height / Math.Max(1, segments));
        byte drawAlpha = ShowAlpha ? (byte)255 : _alpha;
        int y = rect.Y;

        for (int i = 0; i < segments; i++)
        {
            int height = i == segments - 1 ? rect.Bottom - y : segmentHeight;
            float h = segments == 1 ? _h : i / (float)(segments - 1);
            UiColor color = HsvToColor(h, 1f, 1f, drawAlpha);
            context.Renderer.FillRect(new UiRect(rect.X, y, rect.Width, height), color);
            y += height;
        }
    }

    private void DrawAlphaBar(UiRenderContext context, UiRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.Renderer.FillRectCheckerboard(rect, CheckerSize, CheckerColorLight, CheckerColorDark);

        UiColor baseColor = HsvToColor(_h, _s, _v, 255);
        UiColor left = new UiColor(baseColor.R, baseColor.G, baseColor.B, 0);
        UiColor right = new UiColor(baseColor.R, baseColor.G, baseColor.B, 255);
        context.Renderer.FillRectGradient(rect, left, right, left, right);

        if (Border.A > 0)
        {
            context.Renderer.DrawRect(rect, Border, 1);
        }
    }

    private void DrawSvSelection(UiRenderContext context, UiRect rect)
    {
        int x = rect.X + (int)Math.Round(_s * Math.Max(0, rect.Width - 1));
        int y = rect.Y + (int)Math.Round((1f - _v) * Math.Max(0, rect.Height - 1));
        DrawSelectionMarker(context, x, y);
    }

    private void DrawHueSelection(UiRenderContext context, UiRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        int y = rect.Y + (int)Math.Round(_h * Math.Max(0, rect.Height - 1));
        int markerHeight = 3;
        int markerY = Math.Clamp(y - markerHeight / 2, rect.Y, rect.Bottom - markerHeight);
        context.Renderer.FillRect(new UiRect(rect.X, markerY, rect.Width, markerHeight), SelectionBorder);
        if (SelectionShadow.A > 0)
        {
            context.Renderer.DrawRect(new UiRect(rect.X, markerY, rect.Width, markerHeight), SelectionShadow, 1);
        }
    }

    private void DrawAlphaSelection(UiRenderContext context, UiRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        int x = rect.X + (int)Math.Round((_alpha / 255f) * Math.Max(0, rect.Width - 1));
        int markerWidth = 3;
        int markerX = Math.Clamp(x - markerWidth / 2, rect.X, rect.Right - markerWidth);
        UiRect marker = new UiRect(markerX, rect.Y, markerWidth, rect.Height);
        context.Renderer.FillRect(marker, SelectionBorder);
        if (SelectionShadow.A > 0)
        {
            context.Renderer.DrawRect(marker, SelectionShadow, 1);
        }
    }

    private void DrawSelectionMarker(UiRenderContext context, int x, int y)
    {
        int size = 5;
        int half = size / 2;
        UiRect rect = new UiRect(x - half, y - half, size, size);
        if (SelectionShadow.A > 0)
        {
            context.Renderer.DrawRect(rect, SelectionShadow, 1);
        }
        UiRect inner = new UiRect(rect.X + 1, rect.Y + 1, Math.Max(0, rect.Width - 2), Math.Max(0, rect.Height - 2));
        context.Renderer.DrawRect(inner, SelectionBorder, 1);
    }

    private void SetColor(UiColor value)
    {
        if (_color.R == value.R && _color.G == value.G && _color.B == value.B && _color.A == value.A)
        {
            return;
        }

        _color = value;
        _alpha = value.A;
        RgbToHsv(value, out _h, out _s, out _v);
        ColorChanged?.Invoke(_color);
    }

    private void UpdateColorFromHsv()
    {
        UiColor next = HsvToColor(_h, _s, _v, _alpha);
        if (_color.R == next.R && _color.G == next.G && _color.B == next.B && _color.A == next.A)
        {
            return;
        }

        _color = next;
        ColorChanged?.Invoke(_color);
    }

    private static void RgbToHsv(UiColor color, out float h, out float s, out float v)
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

    private static UiColor HsvToColor(float h, float s, float v, byte alpha)
    {
        h = h - MathF.Floor(h);
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

        byte rb = ToByte(r + m);
        byte gb = ToByte(g + m);
        byte bb = ToByte(b + m);
        return new UiColor(rb, gb, bb, alpha);
    }

    private static byte ToByte(float value)
    {
        float clamped = Math.Clamp(value, 0f, 1f);
        return (byte)Math.Round(clamped * 255f);
    }
}
