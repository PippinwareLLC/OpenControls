namespace OpenControls.Controls;

public sealed class UiColorEdit : UiElement
{
    private readonly UiPopup? _pickerPopup;
    private readonly UiColorPicker? _picker;
    private readonly UiTextField _hexField;
    private UiColor _color = new(255, 255, 255);
    private bool _dragging;
    private int _dragChannel = -1;
    private int _hoverChannel = -1;
    private bool _syncingChildren;
    private UiRect _previewRect;
    private UiRect _modeButtonRect;
    private UiRect _valueModeButtonRect;
    private UiRect _pickerButtonRect;

    public UiColorEdit()
        : this(createPickerPopup: true)
    {
    }

    internal UiColorEdit(bool createPickerPopup)
    {
        if (createPickerPopup)
        {
            _pickerPopup = new UiPopup
            {
                CloseOnEscape = true,
                CloseOnOutsideClick = true
            };
            _picker = new UiColorPicker(createInputEdit: false)
            {
                ShowPreview = false,
                ShowInputFields = false,
                ShowAlpha = true
            };
            _picker.ColorChanged += HandlePickerColorChanged;
            _pickerPopup.AddChild(_picker);
            AddChild(_pickerPopup);
        }

        _hexField = new UiTextField
        {
            Visible = false
        };
        _hexField.CharacterFilter = IsHexCharacter;
        _hexField.TextChanged += _ => ApplyHexTextIfValid();
        _hexField.Submitted += ApplyHexTextIfValid;
        AddChild(_hexField);
    }

    public UiColor Color
    {
        get => _color;
        set => SetColor(value);
    }

    public bool ShowAlpha { get; set; }
    public bool ShowPreview { get; set; } = true;
    public bool ShowHex { get; set; } = true;
    public bool ShowOptionsSurface { get; set; } = true;
    public bool EnablePickerPopup { get; set; } = true;
    public UiColorDisplayMode DisplayMode { get; set; } = UiColorDisplayMode.Rgb;
    public UiColorValueDisplayMode ValueDisplayMode { get; set; } = UiColorValueDisplayMode.Byte;
    public UiPopupPlacement PickerPlacement { get; set; } = UiPopupPlacement.BottomLeft;
    public int PickerPopupWidth { get; set; } = 220;
    public int PickerPopupHeight { get; set; } = 244;
    public int TextScale { get; set; } = 1;
    public int HeaderHeight { get; set; } = 24;
    public int RowHeight { get; set; } = 22;
    public int TrackHeight { get; set; } = 6;
    public int ThumbWidth { get; set; } = 10;
    public int Padding { get; set; } = 4;
    public int LabelWidth { get; set; } = 18;
    public int ValueLabelWidth { get; set; } = 54;
    public int OptionButtonWidth { get; set; } = 48;

    public UiColor Background { get; set; } = new(24, 28, 38);
    public UiColor Border { get; set; } = new(60, 70, 90);
    public UiColor PreviewBorder { get; set; } = new(70, 80, 100);
    public UiColor TrackColor { get; set; } = new(28, 32, 44);
    public UiColor ThumbColor { get; set; } = new(200, 210, 230);
    public UiColor ThumbHoverColor { get; set; } = new(230, 240, 255);
    public UiColor LabelColor { get; set; } = UiColor.White;
    public UiColor ValueColor { get; set; } = new(170, 180, 200);
    public UiColor HexColor { get; set; } = UiColor.White;
    public UiColor OptionButtonBackground { get; set; } = new(36, 42, 58);
    public UiColor OptionButtonHoverBackground { get; set; } = new(50, 58, 76);
    public UiColor OptionButtonTextColor { get; set; } = UiColor.White;
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
        UpdateHeaderRects();
        SyncChildState(context.Focus.Focused == _hexField);

        _hoverChannel = DisplayMode == UiColorDisplayMode.Hex ? -1 : GetChannelAtPoint(input.MousePosition);

        if (input.LeftClicked)
        {
            if (ShowOptionsSurface && _modeButtonRect.Contains(input.MousePosition))
            {
                CycleDisplayMode();
                context.Focus.RequestFocus(this);
            }
            else if (ShowOptionsSurface && _valueModeButtonRect.Contains(input.MousePosition))
            {
                ToggleValueMode();
                context.Focus.RequestFocus(this);
            }
            else if (_pickerPopup != null && EnablePickerPopup && (_pickerButtonRect.Contains(input.MousePosition) || (ShowPreview && _previewRect.Contains(input.MousePosition))))
            {
                TogglePickerPopup();
                context.Focus.RequestFocus(this);
            }
            else if (_hoverChannel >= 0)
            {
                _dragging = true;
                _dragChannel = _hoverChannel;
                context.Focus.RequestFocus(this);
                UpdateChannelFromMouse(_dragChannel, input.MousePosition.X);
            }
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

        RenderHeader(context);

        if (DisplayMode == UiColorDisplayMode.Hex)
        {
            base.Render(context);
            return;
        }

        int rowCount = ShowAlpha ? 4 : 3;
        int rowStartY = Bounds.Y + Math.Max(1, HeaderHeight);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);

        for (int channel = 0; channel < rowCount; channel++)
        {
            int y = rowStartY + channel * Math.Max(1, RowHeight);
            UiRect rowRect = new(Bounds.X, y, Bounds.Width, Math.Max(1, RowHeight));
            UiRect trackRect = GetTrackRect(rowRect);
            UiRect valueRect = GetValueRect(rowRect);

            string label = GetChannelLabel(channel);
            int labelX = rowRect.X + Math.Max(0, Padding);
            int labelY = rowRect.Y + (rowRect.Height - textHeight) / 2;
            context.Renderer.DrawText(label, new UiPoint(labelX, labelY), LabelColor, TextScale);

            if (trackRect.Width > 0 && trackRect.Height > 0)
            {
                context.Renderer.FillRect(trackRect, TrackColor);
                if (Border.A > 0)
                {
                    context.Renderer.DrawRect(trackRect, Border, 1);
                }

                float t = GetChannelValueNormalized(channel);
                int fillWidth = (int)Math.Round(trackRect.Width * t);
                if (fillWidth > 0)
                {
                    UiRect fillRect = new(trackRect.X, trackRect.Y, fillWidth, trackRect.Height);
                    DrawChannelFill(context, channel, fillRect);
                }

                int thumbWidth = Math.Max(4, ThumbWidth);
                int range = Math.Max(0, trackRect.Width - thumbWidth);
                int thumbX = trackRect.X + (int)Math.Round(range * t);
                int thumbHeight = Math.Max(trackRect.Height + 6, trackRect.Height);
                int thumbY = rowRect.Y + (rowRect.Height - thumbHeight) / 2;
                UiRect thumbRect = new(thumbX, thumbY, thumbWidth, thumbHeight);
                UiColor thumbColor = (_dragChannel == channel || _hoverChannel == channel) ? ThumbHoverColor : ThumbColor;
                context.Renderer.FillRect(thumbRect, thumbColor);
                if (Border.A > 0)
                {
                    context.Renderer.DrawRect(thumbRect, Border, 1);
                }
            }

            string valueText = FormatChannelValue(channel);
            int valueWidth = context.Renderer.MeasureTextWidth(valueText, TextScale);
            int valueX = valueRect.Right - valueWidth;
            int valueY = rowRect.Y + (rowRect.Height - textHeight) / 2;
            context.Renderer.DrawText(valueText, new UiPoint(valueX, valueY), ValueColor, TextScale);
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
    }

    protected internal override void OnFocusLost()
    {
        _dragging = false;
        _dragChannel = -1;
    }

    private void RenderHeader(UiRenderContext context)
    {
        UiRect headerRect = new(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(1, HeaderHeight));
        int padding = Math.Max(0, Padding);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = headerRect.X + padding;

        if (ShowPreview && _previewRect.Width > 0 && _previewRect.Height > 0)
        {
            context.Renderer.FillRectCheckerboard(_previewRect, 6, new UiColor(90, 100, 120), new UiColor(60, 70, 90));
            context.Renderer.FillRect(_previewRect, _color);
            if (PreviewBorder.A > 0)
            {
                context.Renderer.DrawRect(_previewRect, PreviewBorder, 1);
            }

            textX = _previewRect.Right + padding;
        }

        if (ShowHex)
        {
            string hex = UiColorConversion.ToHex(_color, ShowAlpha);
            int textY = headerRect.Y + (headerRect.Height - textHeight) / 2;
            int rightLimit = ShowOptionsSurface
                ? Math.Min(_modeButtonRect.X, _valueModeButtonRect.X)
                : headerRect.Right - padding;
            int availableWidth = Math.Max(0, rightLimit - textX - padding);
            if (availableWidth > 0)
            {
                string drawHex = hex;
                int hexWidth = context.Renderer.MeasureTextWidth(drawHex, TextScale);
                while (drawHex.Length > 3 && hexWidth > availableWidth)
                {
                    drawHex = drawHex[..^1];
                    hexWidth = context.Renderer.MeasureTextWidth(drawHex + "...", TextScale);
                    if (hexWidth <= availableWidth)
                    {
                        drawHex += "...";
                        break;
                    }
                }

                context.Renderer.DrawText(drawHex, new UiPoint(textX, textY), HexColor, TextScale);
            }
        }

        if (ShowOptionsSurface)
        {
            DrawOptionButton(context, _modeButtonRect, GetModeButtonText());
            DrawOptionButton(context, _valueModeButtonRect, GetValueModeButtonText());
        }

        if (EnablePickerPopup)
        {
            DrawOptionButton(context, _pickerButtonRect, _pickerPopup != null && _pickerPopup.IsOpen ? "Hide" : "Pick");
        }
    }

    private void DrawOptionButton(UiRenderContext context, UiRect rect, string text)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, rect, 4, OptionButtonBackground);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, rect, 4, Border, 1);
        }

        int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = rect.X + (rect.Width - textWidth) / 2;
        int textY = rect.Y + (rect.Height - textHeight) / 2;
        context.Renderer.DrawText(text, new UiPoint(textX, textY), OptionButtonTextColor, TextScale);
    }

    private void UpdateHeaderRects()
    {
        int padding = Math.Max(0, Padding);
        int headerHeight = Math.Max(1, HeaderHeight);
        int buttonWidth = Math.Max(36, OptionButtonWidth);
        int buttonHeight = Math.Max(18, headerHeight - 6);
        int buttonY = Bounds.Y + (headerHeight - buttonHeight) / 2;
        int right = Bounds.Right - padding;

        _pickerButtonRect = EnablePickerPopup
            ? new UiRect(right - buttonWidth, buttonY, buttonWidth, buttonHeight)
            : new UiRect(right, buttonY, 0, buttonHeight);
        right = _pickerButtonRect.X - (EnablePickerPopup ? padding : 0);

        _valueModeButtonRect = ShowOptionsSurface
            ? new UiRect(right - buttonWidth, buttonY, buttonWidth, buttonHeight)
            : new UiRect(right, buttonY, 0, buttonHeight);
        right = _valueModeButtonRect.X - (ShowOptionsSurface ? padding : 0);

        _modeButtonRect = ShowOptionsSurface
            ? new UiRect(right - buttonWidth, buttonY, buttonWidth, buttonHeight)
            : new UiRect(right, buttonY, 0, buttonHeight);

        if (ShowPreview)
        {
            int previewSize = Math.Max(0, headerHeight - padding * 2);
            _previewRect = new UiRect(Bounds.X + padding, Bounds.Y + (headerHeight - previewSize) / 2, previewSize, previewSize);
        }
        else
        {
            _previewRect = default;
        }
    }

    private void SyncChildState(bool hexFieldFocused)
    {
        bool hexMode = DisplayMode == UiColorDisplayMode.Hex;
        _hexField.Visible = hexMode;
        _hexField.Bounds = GetHexFieldRect();
        _hexField.TextScale = TextScale;
        _hexField.Padding = Padding;
        _hexField.Placeholder = ShowAlpha ? "#RRGGBBAA" : "#RRGGBB";

        if (!_hexField.Visible || !hexFieldFocused)
        {
            string hex = UiColorConversion.ToHex(_color, ShowAlpha);
            if (_hexField.Text != hex)
            {
                _hexField.Text = hex;
                _hexField.SetCaretIndex(_hexField.Text.Length);
            }
        }

        if (_picker != null)
        {
            _picker.ShowAlpha = ShowAlpha;
            _picker.Color = _color;
        }

        if (_pickerPopup != null)
        {
            if (_pickerPopup.IsOpen)
            {
                UiRect anchor = _previewRect.Width > 0 ? _previewRect : new UiRect(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(1, HeaderHeight));
                UiRect popupBounds = UiPopupLayout.BuildBounds(anchor, new UiPoint(PickerPopupWidth, PickerPopupHeight), PickerPlacement);
                _pickerPopup.Bounds = UiPopupLayout.Clamp(_pickerPopup, popupBounds);
            }

            if (_picker != null)
            {
                _picker.Bounds = new UiRect(_pickerPopup.Bounds.X, _pickerPopup.Bounds.Y, Math.Max(0, PickerPopupWidth), Math.Max(0, PickerPopupHeight));
            }
        }
    }

    private UiRect GetHexFieldRect()
    {
        int headerHeight = Math.Max(1, HeaderHeight);
        int rowHeight = Math.Max(1, RowHeight);
        int padding = Math.Max(0, Padding);
        return new UiRect(
            Bounds.X + padding,
            Bounds.Y + headerHeight,
            Math.Max(0, Bounds.Width - padding * 2),
            rowHeight);
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

    private UiRect GetTrackRect(UiRect rowRect)
    {
        int padding = Math.Max(0, Padding);
        int labelWidth = Math.Max(0, LabelWidth);
        int trackHeight = Math.Max(1, TrackHeight);
        int valueWidth = Math.Max(0, ValueLabelWidth);
        int x = rowRect.X + padding + labelWidth;
        int width = Math.Max(0, rowRect.Width - labelWidth - valueWidth - padding * 3);
        int y = rowRect.Y + (rowRect.Height - trackHeight) / 2;
        return new UiRect(x, y, width, trackHeight);
    }

    private UiRect GetValueRect(UiRect rowRect)
    {
        int padding = Math.Max(0, Padding);
        int valueWidth = Math.Max(0, ValueLabelWidth);
        return new UiRect(rowRect.Right - padding - valueWidth, rowRect.Y, valueWidth, rowRect.Height);
    }

    private void UpdateChannelFromMouse(int channel, int mouseX)
    {
        UiRect trackRect = GetTrackRect(new UiRect(Bounds.X, Bounds.Y + Math.Max(1, HeaderHeight) + channel * Math.Max(1, RowHeight), Bounds.Width, Math.Max(1, RowHeight)));
        if (trackRect.Width <= 0)
        {
            return;
        }

        float t = (mouseX - trackRect.X) / (float)trackRect.Width;
        t = Math.Clamp(t, 0f, 1f);
        SetChannelValueNormalized(channel, t);
    }

    private float GetChannelValueNormalized(int channel)
    {
        if (DisplayMode == UiColorDisplayMode.Hsv)
        {
            UiColorConversion.RgbToHsv(_color, out float h, out float s, out float v);
            return channel switch
            {
                0 => h,
                1 => s,
                2 => v,
                _ => UiColorConversion.ToFloat(_color.A)
            };
        }

        return channel switch
        {
            0 => UiColorConversion.ToFloat(_color.R),
            1 => UiColorConversion.ToFloat(_color.G),
            2 => UiColorConversion.ToFloat(_color.B),
            _ => UiColorConversion.ToFloat(_color.A)
        };
    }

    private void SetChannelValueNormalized(int channel, float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        if (DisplayMode == UiColorDisplayMode.Hsv)
        {
            UiColorConversion.RgbToHsv(_color, out float h, out float s, out float v);
            switch (channel)
            {
                case 0:
                    h = value;
                    break;
                case 1:
                    s = value;
                    break;
                case 2:
                    v = value;
                    break;
                default:
                    SetColor(new UiColor(_color.R, _color.G, _color.B, UiColorConversion.ToByte(value)));
                    return;
            }

            SetColor(UiColorConversion.HsvToColor(h, s, v, _color.A));
            return;
        }

        byte channelByte = UiColorConversion.ToByte(value);
        UiColor next = channel switch
        {
            0 => new UiColor(channelByte, _color.G, _color.B, _color.A),
            1 => new UiColor(_color.R, channelByte, _color.B, _color.A),
            2 => new UiColor(_color.R, _color.G, channelByte, _color.A),
            _ => new UiColor(_color.R, _color.G, _color.B, channelByte)
        };
        SetColor(next);
    }

    private string GetChannelLabel(int channel)
    {
        if (DisplayMode == UiColorDisplayMode.Hsv)
        {
            return channel switch
            {
                0 => "H",
                1 => "S",
                2 => "V",
                _ => "A"
            };
        }

        return channel switch
        {
            0 => "R",
            1 => "G",
            2 => "B",
            _ => "A"
        };
    }

    private string FormatChannelValue(int channel)
    {
        float value = GetChannelValueNormalized(channel);
        bool hue = DisplayMode == UiColorDisplayMode.Hsv && channel == 0;
        if (ValueDisplayMode == UiColorValueDisplayMode.Float)
        {
            return hue ? $"{value * 360f:0.0}" : $"{value:0.000}";
        }

        return hue ? $"{Math.Round(value * 360f):0}" : $"{UiColorConversion.ToByte(value)}";
    }

    private void DrawChannelFill(UiRenderContext context, int channel, UiRect fillRect)
    {
        if (DisplayMode == UiColorDisplayMode.Hsv)
        {
            if (channel == 0)
            {
                context.Renderer.FillRect(fillRect, UiColorConversion.HsvToColor(GetChannelValueNormalized(channel), 1f, 1f, 255));
                return;
            }

            UiColorConversion.RgbToHsv(_color, out float h, out float s, out float v);
            UiColor fill = channel switch
            {
                1 => UiColorConversion.HsvToColor(h, GetChannelValueNormalized(channel), v, 255),
                2 => UiColorConversion.HsvToColor(h, s, GetChannelValueNormalized(channel), 255),
                _ => new UiColor(_color.A, _color.A, _color.A)
            };
            context.Renderer.FillRect(fillRect, fill);
            return;
        }

        byte value = UiColorConversion.ToByte(GetChannelValueNormalized(channel));
        UiColor fillColor = channel switch
        {
            0 => new UiColor(value, 0, 0),
            1 => new UiColor(0, value, 0),
            2 => new UiColor(0, 0, value),
            _ => new UiColor(value, value, value)
        };
        context.Renderer.FillRect(fillRect, fillColor);
    }

    private void CycleDisplayMode()
    {
        DisplayMode = DisplayMode switch
        {
            UiColorDisplayMode.Rgb => UiColorDisplayMode.Hsv,
            UiColorDisplayMode.Hsv => UiColorDisplayMode.Hex,
            _ => UiColorDisplayMode.Rgb
        };
    }

    private void ToggleValueMode()
    {
        ValueDisplayMode = ValueDisplayMode == UiColorValueDisplayMode.Byte
            ? UiColorValueDisplayMode.Float
            : UiColorValueDisplayMode.Byte;
    }

    private string GetModeButtonText()
    {
        return DisplayMode switch
        {
            UiColorDisplayMode.Hsv => "HSV",
            UiColorDisplayMode.Hex => "HEX",
            _ => "RGB"
        };
    }

    private string GetValueModeButtonText()
    {
        return ValueDisplayMode == UiColorValueDisplayMode.Float ? "0-1" : "255";
    }

    private void TogglePickerPopup()
    {
        if (_pickerPopup == null)
        {
            return;
        }

        if (_pickerPopup.IsOpen)
        {
            _pickerPopup.Close();
        }
        else
        {
            UiRect anchor = _previewRect.Width > 0 ? _previewRect : new UiRect(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(1, HeaderHeight));
            _pickerPopup.OpenAttached(anchor, new UiPoint(PickerPopupWidth, PickerPopupHeight), PickerPlacement);
        }
    }

    private void HandlePickerColorChanged(UiColor color)
    {
        if (_syncingChildren)
        {
            return;
        }

        SetColor(color);
    }

    private void ApplyHexTextIfValid()
    {
        if (UiColorConversion.TryParseHex(_hexField.Text, out UiColor parsed))
        {
            if (!ShowAlpha)
            {
                parsed = new UiColor(parsed.R, parsed.G, parsed.B, _color.A);
            }

            SetColor(parsed);
        }
    }

    private bool IsHexCharacter(char character)
    {
        return character == '#'
            || (character >= '0' && character <= '9')
            || (character >= 'a' && character <= 'f')
            || (character >= 'A' && character <= 'F');
    }

    private void SetColor(UiColor value)
    {
        if (_color.R == value.R && _color.G == value.G && _color.B == value.B && _color.A == value.A)
        {
            return;
        }

        _color = value;
        _syncingChildren = true;
        if (_picker != null)
        {
            _picker.Color = value;
        }

        _hexField.Text = UiColorConversion.ToHex(value, ShowAlpha);
        _syncingChildren = false;
        ColorChanged?.Invoke(_color);
    }
}
