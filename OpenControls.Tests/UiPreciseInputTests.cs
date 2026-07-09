using System.Numerics;
using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiPreciseInputTests
{
    private sealed class InputRecorder : UiElement
    {
        public UiInputState? LastInput { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            LastInput = context.Input;
        }
    }

    [Fact]
    public void ResolvedPositions_PreferFinitePrecisionAndFallBackForNonFiniteValues()
    {
        UiInputState input = new()
        {
            MousePosition = new UiPoint(11, 12),
            ScreenMousePosition = new UiPoint(31, 32),
            PreciseMousePosition = new Vector2(11.25f, 12.75f),
            PreciseScreenMousePosition = new Vector2(float.PositiveInfinity, 32.25f)
        };

        Assert.Equal(new Vector2(11.25f, 12.75f), input.ResolvedMousePosition);
        Assert.Equal(new Vector2(31, 32), input.ResolvedScreenMousePosition);

        UiInputState noPrecision = new()
        {
            MousePosition = new UiPoint(41, 42),
            ScreenMousePosition = new UiPoint(51, 52)
        };

        Assert.Equal(new Vector2(41, 42), noPrecision.ResolvedMousePosition);
        Assert.Equal(new Vector2(51, 52), noPrecision.ResolvedScreenMousePosition);
    }

    [Fact]
    public void TranslatedChildInput_PreservesPreciseScreenPositionAndPinchZoom()
    {
        UiSelectableRow row = new()
        {
            Bounds = new UiRect(100, 50, 200, 80),
            Padding = 6
        };
        InputRecorder child = new();
        row.AddChild(child);

        Update(row, new UiInputState
        {
            MousePosition = new UiPoint(121, 72),
            ScreenMousePosition = new UiPoint(401, 202),
            PreciseMousePosition = new Vector2(121.25f, 72.75f),
            PreciseScreenMousePosition = new Vector2(401.5f, 202.25f),
            PinchZoom = 1.125f
        });

        UiInputState translated = Assert.IsType<UiInputState>(child.LastInput);
        Assert.Equal(new UiPoint(15, 16), translated.MousePosition);
        Assert.Equal(new Vector2(15.25f, 16.75f), translated.PreciseMousePosition);
        Assert.Equal(new UiPoint(401, 202), translated.ScreenMousePosition);
        Assert.Equal(new Vector2(401.5f, 202.25f), translated.PreciseScreenMousePosition);
        Assert.Equal(1.125f, translated.PinchZoom);
    }

    [Fact]
    public void PopupOpenFrame_PreservesPointerCoordinatesAndGesturesWhileSuppressingButtons()
    {
        UiPopup popup = new()
        {
            Bounds = new UiRect(10, 20, 100, 80)
        };
        InputRecorder child = new();
        popup.AddChild(child);
        popup.Open();

        Update(popup, new UiInputState
        {
            MousePosition = new UiPoint(21, 32),
            ScreenMousePosition = new UiPoint(221, 232),
            PreciseMousePosition = new Vector2(21.25f, 32.75f),
            PreciseScreenMousePosition = new Vector2(221.5f, 232.25f),
            LeftDown = true,
            LeftClicked = true,
            ScrollDeltaX = 12,
            ScrollDelta = 120,
            PinchZoom = 0.875f
        });

        UiInputState suppressed = Assert.IsType<UiInputState>(child.LastInput);
        Assert.Equal(new Vector2(21.25f, 32.75f), suppressed.PreciseMousePosition);
        Assert.Equal(new Vector2(221.5f, 232.25f), suppressed.PreciseScreenMousePosition);
        Assert.Equal(12, suppressed.ScrollDeltaX);
        Assert.Equal(120, suppressed.ScrollDelta);
        Assert.Equal(0.875f, suppressed.PinchZoom);
        Assert.False(suppressed.LeftDown);
        Assert.False(suppressed.LeftClicked);
    }

    [Fact]
    public void ScrollPanelChildInput_PropagatesPrecisionInsideAndNeutralizesGesturesOutsideViewport()
    {
        UiScrollPanel panel = new()
        {
            Bounds = new UiRect(10, 20, 100, 100)
        };
        InputRecorder child = new()
        {
            Bounds = new UiRect(0, 0, 200, 200)
        };
        panel.AddChild(child);

        Update(panel, new UiInputState
        {
            MousePosition = new UiPoint(25, 35),
            ScreenMousePosition = new UiPoint(225, 235),
            PreciseMousePosition = new Vector2(25.25f, 35.75f),
            PreciseScreenMousePosition = new Vector2(225.5f, 235.25f),
            PinchZoom = 0.9f
        });

        UiInputState inside = Assert.IsType<UiInputState>(child.LastInput);
        Assert.Equal(new Vector2(15.25f, 15.75f), inside.PreciseMousePosition);
        Assert.Equal(new Vector2(225.5f, 235.25f), inside.PreciseScreenMousePosition);
        Assert.Equal(0.9f, inside.PinchZoom);

        Update(panel, new UiInputState
        {
            MousePosition = new UiPoint(250, 250),
            ScreenMousePosition = new UiPoint(450, 450),
            PreciseMousePosition = new Vector2(250.25f, 250.75f),
            PreciseScreenMousePosition = new Vector2(450.5f, 450.25f),
            PinchZoom = 1.2f
        });

        UiInputState outside = Assert.IsType<UiInputState>(child.LastInput);
        Assert.True(outside.ResolvedMousePosition.X < -100_000_000);
        Assert.True(outside.ResolvedMousePosition.Y < -100_000_000);
        Assert.Equal(new Vector2(450.5f, 450.25f), outside.PreciseScreenMousePosition);
        Assert.Equal(1f, outside.PinchZoom);
    }

    [Fact]
    public void BlockedInput_UsesOffscreenFallbackAndNeutralizesPinchZoom()
    {
        UiPanel root = new();
        InputRecorder blockedElement = new();
        UiPopup activePopup = new();
        root.AddChild(blockedElement);
        root.AddChild(activePopup);
        activePopup.Open();

        UiInputState input = new()
        {
            MousePosition = new UiPoint(25, 35),
            ScreenMousePosition = new UiPoint(225, 235),
            PreciseMousePosition = new Vector2(25.25f, 35.75f),
            PreciseScreenMousePosition = new Vector2(225.5f, 235.25f),
            LeftDown = true,
            LeftClicked = true,
            ScrollDelta = 120,
            PinchZoom = 1.25f
        };
        UiUpdateContext context = CreateContext(input, activePopup);

        UiInputState blocked = context.GetInputFor(blockedElement);

        Assert.Null(blocked.PreciseMousePosition);
        Assert.Null(blocked.PreciseScreenMousePosition);
        Assert.Equal(new Vector2(-1_000_000, -1_000_000), blocked.ResolvedMousePosition);
        Assert.Equal(new Vector2(-1_000_000, -1_000_000), blocked.ResolvedScreenMousePosition);
        Assert.Equal(1f, blocked.PinchZoom);
        Assert.Equal(0, blocked.ScrollDelta);
        Assert.False(blocked.LeftDown);
        Assert.False(blocked.LeftClicked);
    }

    private static void Update(UiElement element, UiInputState input)
    {
        element.Update(CreateContext(input));
    }

    private static UiUpdateContext CreateContext(UiInputState input, UiElement? activeInputLayer = null)
    {
        return new UiUpdateContext(
            input,
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard(),
            activeInputLayer);
    }
}
