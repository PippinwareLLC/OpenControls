using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiNumericAndColorDepthTests
{
    private sealed class TestRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;

        public void FillRect(UiRect rect, UiColor color)
        {
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void Slider_CtrlClickEntersInlineInputMode()
    {
        UiSlider slider = new()
        {
            Bounds = new UiRect(0, 0, 180, 28),
            Min = 0f,
            Max = 1f,
            Value = 0.5f
        };
        UiFocusManager focus = new();

        Update(slider, focus, new UiInputState
        {
            MousePosition = new UiPoint(12, 12),
            ScreenMousePosition = new UiPoint(12, 12),
            LeftClicked = true,
            LeftDown = true,
            CtrlDown = true
        });

        UiInputFloat input = Assert.IsType<UiInputFloat>(slider.Children.Single());
        Assert.Same(input.TextField, focus.Focused);
        Assert.True(input.Visible);
    }

    [Fact]
    public void DragFloat_NoRoundToFormatPreservesBackingPrecision()
    {
        UiDragFloat rounded = new()
        {
            Min = 0f,
            Max = 1f,
            ValueFormat = "0.00",
            Flags = UiDragFlags.AlwaysClamp
        };
        UiDragFloat precise = new()
        {
            Min = 0f,
            Max = 1f,
            ValueFormat = "0.00",
            Flags = UiDragFlags.AlwaysClamp | UiDragFlags.NoRoundToFormat
        };

        rounded.Value = 0.3333f;
        precise.Value = 0.3333f;

        Assert.Equal(0.33f, rounded.Value, 2);
        Assert.Equal(0.3333f, precise.Value, 4);
    }

    [Fact]
    public void DragInt_WrapAroundLoopsInsideInclusiveRange()
    {
        UiDragInt drag = new()
        {
            Min = 0,
            Max = 4,
            Flags = UiDragFlags.WrapAround
        };

        drag.Value = 5;
        Assert.Equal(0, drag.Value);

        drag.Value = -1;
        Assert.Equal(4, drag.Value);
    }

    [Fact]
    public void ColorEdit_HexModeShowsHexField()
    {
        UiColorEdit colorEdit = new()
        {
            Bounds = new UiRect(0, 0, 240, 64),
            ShowAlpha = true,
            DisplayMode = UiColorDisplayMode.Hex,
            Color = new UiColor(10, 20, 30, 40)
        };

        Update(colorEdit);

        UiTextField hexField = colorEdit.Children.OfType<UiTextField>().Single();
        Assert.True(hexField.Visible);
        Assert.Equal("#0A141E28", hexField.Text);
    }

    [Fact]
    public void ColorPicker_ShowInputFieldsControlsEmbeddedEditorVisibility()
    {
        UiColorPicker picker = new()
        {
            Bounds = new UiRect(0, 0, 280, 220),
            ShowInputFields = true,
            ShowPreview = true,
            ShowAlpha = true
        };

        Update(picker);

        UiColorEdit embeddedEditor = picker.Children.OfType<UiColorEdit>().Single();
        Assert.True(embeddedEditor.Visible);

        picker.ShowInputFields = false;
        Update(picker);
        Assert.False(embeddedEditor.Visible);
    }

    [Fact]
    public void ColorPicker_DragOriginInsideSvSurface_UpdatesColorWhileMouseIsHeld()
    {
        UiColorPicker picker = new()
        {
            Bounds = new UiRect(0, 0, 280, 220),
            ShowAlpha = false
        };

        UiColor initial = picker.Color;

        Update(picker, new UiFocusManager(), new UiInputState
        {
            MousePosition = new UiPoint(78, 78),
            ScreenMousePosition = new UiPoint(78, 78),
            LeftDown = true,
            LeftDragOrigin = new UiPoint(10, 10)
        });

        Assert.NotEqual(initial.R, picker.Color.R);
        Assert.NotEqual(initial.G, picker.Color.G);
        Assert.NotEqual(initial.B, picker.Color.B);
    }

    [Fact]
    public void ColorPicker_DragOriginInsideHueSurface_UpdatesHueWhileMouseIsHeld()
    {
        UiColorPicker picker = new()
        {
            Bounds = new UiRect(0, 0, 280, 220),
            ShowAlpha = false
        };

        UiColor initial = picker.Color;

        Update(picker, new UiFocusManager(), new UiInputState
        {
            MousePosition = new UiPoint(92, 44),
            ScreenMousePosition = new UiPoint(92, 44),
            LeftDown = true,
            LeftDragOrigin = new UiPoint(92, 10)
        });

        Assert.NotEqual(initial.R, picker.Color.R);
        Assert.NotEqual(initial.G, picker.Color.G);
        Assert.NotEqual(initial.B, picker.Color.B);
    }

    [Fact]
    public void ColorConversion_HexRoundTripsWithAlpha()
    {
        UiColor original = new(84, 146, 238, 220);
        string hex = UiColorConversion.ToHex(original, includeAlpha: true);

        Assert.True(UiColorConversion.TryParseHex(hex, out UiColor parsed));
        Assert.Equal(original.R, parsed.R);
        Assert.Equal(original.G, parsed.G);
        Assert.Equal(original.B, parsed.B);
        Assert.Equal(original.A, parsed.A);
    }

    [Fact]
    public void NumericField_RenderSynchronizesInnerTextFieldBoundsWithoutPriorUpdate()
    {
        UiInputFloat input = new()
        {
            Bounds = new UiRect(12, 18, 160, 28),
            Value = 42.5f
        };

        TestRenderer renderer = new();

        input.Render(new UiRenderContext(renderer, UiFont.Default));

        Assert.Equal(input.Bounds, input.TextField.Bounds);
        Assert.Equal("42.5", input.TextField.Text);
    }

    private static void Update(UiElement element)
    {
        Update(element, new UiFocusManager(), new UiInputState());
    }

    private static void Update(UiElement element, UiFocusManager focus, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }
}
