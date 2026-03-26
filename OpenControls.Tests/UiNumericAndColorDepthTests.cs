using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiNumericAndColorDepthTests
{
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
