namespace OpenControls.Controls;

public sealed class UiColorPicker : UiElement
{
    private readonly UiColorEdit? _inputEdit;
    private UiColor _color = new(255, 0, 0);
    private float _h;
    private float _s = 1f;
    private float _v = 1f;
    private byte _alpha = 255;
    private bool _draggingSv;
    private bool _draggingHue;
    private bool _draggingAlpha;
    private bool _previewPressed;
    private UiPoint _previewPressPosition;
    private bool _syncingInputEdit;

    public UiColorPicker()
        : this(createInputEdit: true)
    {
    }

    internal UiColorPicker(bool createInputEdit)
    {
        if (createInputEdit)
        {
            _inputEdit = new UiColorEdit(createPickerPopup: false)
            {
                Visible = false,
                ShowPreview = false,
                EnablePickerPopup = false
            };
            _inputEdit.ColorChanged += HandleInputEditColorChanged;
            AddChild(_inputEdit);
        }
    }

    public UiColor Color
    {
        get => _color;
        set => SetColor(value);
    }

    public int Padding { get; set; } = 4;
    public int HueBarWidth { get; set; } = 12;
    public bool ShowAlpha { get; set; }
    public bool ShowPreview { get; set; } = true;
    public bool ShowInputFields { get; set; } = true;
    public bool AllowColorDragDrop { get; set; } = true;
    public string DragPayloadType { get; set; } = "color";
    public UiColorDisplayMode InputDisplayMode { get; set; } = UiColorDisplayMode.Rgb;
    public UiColorValueDisplayMode InputValueDisplayMode { get; set; } = UiColorValueDisplayMode.Byte;
    public int SidePanelWidth { get; set; } = 168;
    public int SidePanelGap { get; set; } = 8;
    public int AlphaBarHeight { get; set; } = 10;
    public int CheckerSize { get; set; } = 6;
    public UiColor CheckerColorLight { get; set; } = new(80, 90, 110);
    public UiColor CheckerColorDark { get; set; } = new(50, 60, 80);
    public int GridSize { get; set; }
    public int HueSegments { get; set; }
    public UiColor Background { get; set; } = new(24, 28, 38);
    public UiColor Border { get; set; } = new(60, 70, 90);
    public UiColor SelectionBorder { get; set; } = UiColor.White;
    public UiColor SelectionShadow { get; set; } = new(0, 0, 0);
    public UiColor PreviewBorder { get; set; } = new(70, 80, 100);
    public UiColor PreviewTextColor { get; set; } = UiColor.White;
    public int PreviewHeight { get; set; } = 52;
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
        UiRect sideRect = GetSidePanelRect(svRect, hueRect, alphaRect);
        UiRect previewRect = GetPreviewRect(sideRect);

        HandlePreviewDragDrop(context, input, previewRect);

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

        SyncInputEdit(sideRect, previewRect);
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
        UiRect sideRect = GetSidePanelRect(svRect, hueRect, alphaRect);
        UiRect previewRect = GetPreviewRect(sideRect);

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

        if (ShowPreview && previewRect.Width > 0 && previewRect.Height > 0)
        {
            context.Renderer.FillRectCheckerboard(previewRect, CheckerSize, CheckerColorLight, CheckerColorDark);
            context.Renderer.FillRect(previewRect, _color);
            if (PreviewBorder.A > 0)
            {
                context.Renderer.DrawRect(previewRect, PreviewBorder, 1);
            }

            string hex = UiColorConversion.ToHex(_color, ShowAlpha);
            int textHeight = context.Renderer.MeasureTextHeight(1);
            int textY = previewRect.Bottom + 4;
            if (textY + textHeight <= sideRect.Bottom)
            {
                context.Renderer.DrawText(hex, new UiPoint(sideRect.X, textY), PreviewTextColor, 1);
            }
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
    }

    protected internal override void OnFocusLost()
    {
        _draggingSv = false;
        _draggingHue = false;
        _draggingAlpha = false;
        _previewPressed = false;
    }

    private void HandlePreviewDragDrop(UiUpdateContext context, UiInputState input, UiRect previewRect)
    {
        if (!AllowColorDragDrop)
        {
            return;
        }

        if (ShowPreview && previewRect.Contains(input.MousePosition) && input.LeftClicked)
        {
            _previewPressed = true;
            _previewPressPosition = input.MousePosition;
        }

        if (_previewPressed && input.LeftDown)
        {
            int dx = Math.Abs(input.MousePosition.X - _previewPressPosition.X);
            int dy = Math.Abs(input.MousePosition.Y - _previewPressPosition.Y);
            int threshold = Math.Max(0, input.DragThreshold);
            if ((dx >= threshold || dy >= threshold) && !context.DragDrop.IsDragging)
            {
                context.DragDrop.BeginDrag(this, new UiDragDropPayload(DragPayloadType, _color), _previewPressPosition);
            }
        }

        if (context.DragDrop.IsDragging && Bounds.Contains(input.MousePosition))
        {
            context.DragDrop.SetHoveredTarget(this);
            if (context.DragDrop.IsDropRequested)
            {
                UiDragDropPayload? payload = context.DragDrop.AcceptPayload(this, DragPayloadType);
                if (payload?.Data is UiColor dropped)
                {
                    SetColor(dropped);
                }
            }
        }

        if (input.LeftReleased)
        {
            _previewPressed = false;
        }
    }

    private void SyncInputEdit(UiRect sideRect, UiRect previewRect)
    {
        if (_inputEdit == null)
        {
            return;
        }

        _inputEdit.Visible = ShowInputFields && sideRect.Width > 0;
        _inputEdit.ShowAlpha = ShowAlpha;
        _inputEdit.DisplayMode = InputDisplayMode;
        _inputEdit.ValueDisplayMode = InputValueDisplayMode;
        _inputEdit.Bounds = GetInputEditRect(sideRect, previewRect);

        if (!_syncingInputEdit)
        {
            _syncingInputEdit = true;
            _inputEdit.Color = _color;
            _syncingInputEdit = false;
        }
    }

    private void HandleInputEditColorChanged(UiColor color)
    {
        if (_syncingInputEdit)
        {
            return;
        }

        SetColor(color);
    }

    private UiRect GetSvRect()
    {
        int padding = Math.Max(0, Padding);
        int barWidth = Math.Max(0, HueBarWidth);
        int sideWidth = GetReservedSideWidth();
        int sideGap = sideWidth > 0 ? Math.Max(0, SidePanelGap) : 0;
        int alphaHeight = ShowAlpha ? Math.Max(0, AlphaBarHeight) + padding : 0;
        int availableWidth = Math.Max(0, Bounds.Width - padding * 3 - barWidth - sideGap - sideWidth);
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
        return new UiRect(svRect.Right + padding, svRect.Y, width, svRect.Height);
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

    private UiRect GetSidePanelRect(UiRect svRect, UiRect hueRect, UiRect alphaRect)
    {
        int sideWidth = GetReservedSideWidth();
        if (sideWidth <= 0)
        {
            return default;
        }

        int padding = Math.Max(0, Padding);
        int x = hueRect.Right + Math.Max(0, SidePanelGap);
        int y = Bounds.Y + padding;
        int width = Math.Max(0, Math.Min(sideWidth, Bounds.Right - x - padding));
        int bottom = alphaRect.Height > 0 ? alphaRect.Bottom : svRect.Bottom;
        return new UiRect(x, y, width, Math.Max(0, bottom - y));
    }

    private UiRect GetPreviewRect(UiRect sideRect)
    {
        if (!ShowPreview || sideRect.Width <= 0)
        {
            return default;
        }

        int height = Math.Max(32, PreviewHeight);
        return new UiRect(sideRect.X, sideRect.Y, sideRect.Width, Math.Min(height, sideRect.Height));
    }

    private UiRect GetInputEditRect(UiRect sideRect, UiRect previewRect)
    {
        if (sideRect.Width <= 0)
        {
            return default;
        }

        int y = sideRect.Y;
        if (ShowPreview && previewRect.Height > 0)
        {
            y = previewRect.Bottom + 22;
        }

        int height = sideRect.Bottom - y;
        return new UiRect(sideRect.X, y, sideRect.Width, Math.Max(0, height));
    }

    private int GetReservedSideWidth()
    {
        return (ShowPreview || ShowInputFields) ? Math.Max(120, SidePanelWidth) : 0;
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
        byte alpha = (byte)Math.Round(Math.Clamp(t, 0f, 1f) * 255f);
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
                UiColor color = UiColorConversion.HsvToColor(_h, s, v, drawAlpha);
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
            UiColor color = UiColorConversion.HsvToColor(h, 1f, 1f, drawAlpha);
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

        UiColor baseColor = UiColorConversion.HsvToColor(_h, _s, _v, 255);
        UiColor left = new(baseColor.R, baseColor.G, baseColor.B, 0);
        UiColor right = new(baseColor.R, baseColor.G, baseColor.B, 255);
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
        UiRect marker = new(markerX, rect.Y, markerWidth, rect.Height);
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
        UiRect rect = new(x - half, y - half, size, size);
        if (SelectionShadow.A > 0)
        {
            context.Renderer.DrawRect(rect, SelectionShadow, 1);
        }

        UiRect inner = new(rect.X + 1, rect.Y + 1, Math.Max(0, rect.Width - 2), Math.Max(0, rect.Height - 2));
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
        UiColorConversion.RgbToHsv(value, out _h, out _s, out _v);
        if (_inputEdit != null)
        {
            _syncingInputEdit = true;
            _inputEdit.Color = value;
            _syncingInputEdit = false;
        }

        ColorChanged?.Invoke(_color);
    }

    private void UpdateColorFromHsv()
    {
        UiColor next = UiColorConversion.HsvToColor(_h, _s, _v, _alpha);
        if (_color.R == next.R && _color.G == next.G && _color.B == next.B && _color.A == next.A)
        {
            return;
        }

        _color = next;
        if (_inputEdit != null)
        {
            _syncingInputEdit = true;
            _inputEdit.Color = next;
            _syncingInputEdit = false;
        }

        ColorChanged?.Invoke(_color);
    }
}
