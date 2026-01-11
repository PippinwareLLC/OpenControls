namespace OpenControls.Controls;

public sealed class UiColorEdit : UiElement
{
    private UiColor _color = new UiColor(255, 255, 255);
    private bool _dragging;
    private int _dragChannel = -1;
    private int _hoverChannel = -1;
    private bool _focused;

    public UiColor Color
    {
        get => _color;
        set => SetColor(value);
    }

    public bool ShowAlpha { get; set; }
    public bool ShowPreview { get; set; } = true;
    public bool ShowHex { get; set; } = true;
    public int TextScale { get; set; } = 1;
    public int HeaderHeight { get; set; } = 24;
    public int RowHeight { get; set; } = 22;
    public int TrackHeight { get; set; } = 6;
    public int ThumbWidth { get; set; } = 10;
    public int Padding { get; set; } = 4;
    public int LabelWidth { get; set; } = 18;

    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor PreviewBorder { get; set; } = new UiColor(70, 80, 100);
    public UiColor TrackColor { get; set; } = new UiColor(28, 32, 44);
    public UiColor ThumbColor { get; set; } = new UiColor(200, 210, 230);
    public UiColor ThumbHoverColor { get; set; } = new UiColor(230, 240, 255);
    public UiColor LabelColor { get; set; } = UiColor.White;
    public UiColor HexColor { get; set; } = UiColor.White;
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
        _hoverChannel = GetChannelAtPoint(input.MousePosition);

        if (input.LeftClicked && _hoverChannel >= 0)
        {
            _dragging = true;
            _dragChannel = _hoverChannel;
            context.Focus.RequestFocus(this);
            UpdateChannelFromMouse(_dragChannel, input.MousePosition.X);
        }

        if (_dragging && input.LeftDown)
        {
            UpdateChannelFromMouse(_dragChannel, input.MousePosition.X);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
            _dragChannel = -1;
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

        int headerHeight = Math.Max(1, HeaderHeight);
        int rowHeight = Math.Max(1, RowHeight);
        int padding = Math.Max(0, Padding);
        int labelWidth = Math.Max(0, LabelWidth);

        if (ShowPreview || ShowHex)
        {
            UiRect headerRect = new UiRect(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
            int previewSize = Math.Max(0, headerRect.Height - padding * 2);
            int previewX = headerRect.X + padding;
            int previewY = headerRect.Y + (headerRect.Height - previewSize) / 2;

            int textX = headerRect.X + padding;
            if (ShowPreview && previewSize > 0)
            {
                UiRect previewRect = new UiRect(previewX, previewY, previewSize, previewSize);
                context.Renderer.FillRect(previewRect, _color);
                if (PreviewBorder.A > 0)
                {
                    context.Renderer.DrawRect(previewRect, PreviewBorder, 1);
                }

                textX = previewRect.Right + padding;
            }

            if (ShowHex)
            {
                string hex = FormatHex();
                int textHeight = context.Renderer.MeasureTextHeight(TextScale);
                int textY = headerRect.Y + (headerRect.Height - textHeight) / 2;
                context.Renderer.DrawText(hex, new UiPoint(textX, textY), HexColor, TextScale);
            }
        }

        int rowStartY = Bounds.Y + headerHeight;
        int rowCount = ShowAlpha ? 4 : 3;
        int trackHeight = Math.Max(1, TrackHeight);
        int thumbWidth = Math.Max(4, ThumbWidth);
        int textHeightRow = context.Renderer.MeasureTextHeight(TextScale);

        for (int channel = 0; channel < rowCount; channel++)
        {
            int y = rowStartY + channel * rowHeight;
            UiRect rowRect = new UiRect(Bounds.X, y, Bounds.Width, rowHeight);
            string label = GetChannelLabel(channel);

            int labelX = rowRect.X + padding;
            int labelY = rowRect.Y + (rowRect.Height - textHeightRow) / 2;
            context.Renderer.DrawText(label, new UiPoint(labelX, labelY), LabelColor, TextScale);

            UiRect trackRect = GetTrackRect(rowRect, labelWidth, padding, trackHeight);
            if (trackRect.Width > 0 && trackRect.Height > 0)
            {
                context.Renderer.FillRect(trackRect, TrackColor);
                if (Border.A > 0)
                {
                    context.Renderer.DrawRect(trackRect, Border, 1);
                }

                byte value = GetChannelValue(channel);
                float t = value / 255f;
                int fillWidth = (int)Math.Round(trackRect.Width * t);
                if (fillWidth > 0)
                {
                    UiRect fillRect = new UiRect(trackRect.X, trackRect.Y, fillWidth, trackRect.Height);
                    context.Renderer.FillRect(fillRect, GetChannelFillColor(channel, value));
                }

                int range = Math.Max(0, trackRect.Width - thumbWidth);
                int thumbX = trackRect.X + (int)Math.Round(range * t);
                int thumbHeight = Math.Max(trackRect.Height + 6, trackRect.Height);
                int thumbY = rowRect.Y + (rowRect.Height - thumbHeight) / 2;
                UiRect thumbRect = new UiRect(thumbX, thumbY, thumbWidth, thumbHeight);
                UiColor thumbColor = (_dragChannel == channel || _hoverChannel == channel) ? ThumbHoverColor : ThumbColor;
                context.Renderer.FillRect(thumbRect, thumbColor);
                if (Border.A > 0)
                {
                    context.Renderer.DrawRect(thumbRect, Border, 1);
                }
            }
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
        _dragging = false;
        _dragChannel = -1;
    }

    private int GetChannelAtPoint(UiPoint point)
    {
        int headerHeight = Math.Max(1, HeaderHeight);
        int rowHeight = Math.Max(1, RowHeight);
        int rowCount = ShowAlpha ? 4 : 3;
        int rowStartY = Bounds.Y + headerHeight;

        if (point.X < Bounds.X || point.X >= Bounds.Right)
        {
            return -1;
        }

        if (point.Y < rowStartY || point.Y >= rowStartY + rowCount * rowHeight)
        {
            return -1;
        }

        int index = (point.Y - rowStartY) / rowHeight;
        return index >= 0 && index < rowCount ? index : -1;
    }

    private void UpdateChannelFromMouse(int channel, int mouseX)
    {
        UiRect trackRect = GetTrackRect(channel);
        if (trackRect.Width <= 0)
        {
            return;
        }

        float t = (mouseX - trackRect.X) / (float)trackRect.Width;
        t = Math.Clamp(t, 0f, 1f);
        byte value = (byte)Math.Round(t * 255f);
        SetChannelValue(channel, value);
    }

    private UiRect GetTrackRect(int channel)
    {
        int headerHeight = Math.Max(1, HeaderHeight);
        int rowHeight = Math.Max(1, RowHeight);
        int padding = Math.Max(0, Padding);
        int labelWidth = Math.Max(0, LabelWidth);
        int trackHeight = Math.Max(1, TrackHeight);
        int rowY = Bounds.Y + headerHeight + channel * rowHeight;
        UiRect rowRect = new UiRect(Bounds.X, rowY, Bounds.Width, rowHeight);
        return GetTrackRect(rowRect, labelWidth, padding, trackHeight);
    }

    private static UiRect GetTrackRect(UiRect rowRect, int labelWidth, int padding, int trackHeight)
    {
        int x = rowRect.X + padding + labelWidth;
        int width = Math.Max(0, rowRect.Width - labelWidth - padding * 2);
        int y = rowRect.Y + (rowRect.Height - trackHeight) / 2;
        return new UiRect(x, y, width, trackHeight);
    }

    private byte GetChannelValue(int channel)
    {
        return channel switch
        {
            0 => _color.R,
            1 => _color.G,
            2 => _color.B,
            _ => _color.A
        };
    }

    private void SetChannelValue(int channel, byte value)
    {
        UiColor next = channel switch
        {
            0 => new UiColor(value, _color.G, _color.B, _color.A),
            1 => new UiColor(_color.R, value, _color.B, _color.A),
            2 => new UiColor(_color.R, _color.G, value, _color.A),
            _ => new UiColor(_color.R, _color.G, _color.B, value)
        };

        SetColor(next);
    }

    private string GetChannelLabel(int channel)
    {
        return channel switch
        {
            0 => "R",
            1 => "G",
            2 => "B",
            _ => "A"
        };
    }

    private UiColor GetChannelFillColor(int channel, byte value)
    {
        return channel switch
        {
            0 => new UiColor(value, 0, 0),
            1 => new UiColor(0, value, 0),
            2 => new UiColor(0, 0, value),
            _ => new UiColor(value, value, value)
        };
    }

    private void SetColor(UiColor value)
    {
        if (_color.R == value.R && _color.G == value.G && _color.B == value.B && _color.A == value.A)
        {
            return;
        }

        _color = value;
        ColorChanged?.Invoke(_color);
    }

    private string FormatHex()
    {
        if (ShowAlpha)
        {
            return $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}{_color.A:X2}";
        }

        return $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";
    }
}
