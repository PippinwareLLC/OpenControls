using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiPopupDragTests
{
    [Fact]
    public void DraggableModalMovesFromItsTitleRegionAndClampsToItsParent()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiModal modal = new()
        {
            Bounds = new UiRect(100, 80, 300, 180),
            AllowDrag = true,
            DragRegionHeight = 30,
            ClampDragToParent = true
        };
        host.AddChild(modal);
        modal.Open();
        UiContext context = new(host);
        context.Update(new UiInputState());

        context.Update(Pointer(new UiPoint(120, 90), down: true, clicked: true));
        Assert.True(modal.IsDragging);
        context.Update(Pointer(new UiPoint(250, 190), down: true));

        Assert.Equal(new UiRect(230, 180, 300, 180), modal.Bounds);

        context.Update(Pointer(new UiPoint(-500, -400), released: true));

        Assert.Equal(new UiRect(0, 0, 300, 180), modal.Bounds);
        Assert.False(modal.IsDragging);
    }

    [Fact]
    public void PointerOutsideTheDragRegionDoesNotMoveTheModal()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiModal modal = new()
        {
            Bounds = new UiRect(100, 80, 300, 180),
            AllowDrag = true,
            DragRegionHeight = 30
        };
        host.AddChild(modal);
        modal.Open();
        UiContext context = new(host);
        context.Update(new UiInputState());

        context.Update(Pointer(new UiPoint(140, 150), down: true, clicked: true));
        context.Update(Pointer(new UiPoint(260, 240), down: true));
        context.Update(Pointer(new UiPoint(260, 240), released: true));

        Assert.Equal(new UiRect(100, 80, 300, 180), modal.Bounds);
        Assert.False(modal.IsDragging);
    }

    private static UiInputState Pointer(
        UiPoint point,
        bool down = false,
        bool clicked = false,
        bool released = false) => new()
    {
        MousePosition = point,
        ScreenMousePosition = point,
        LeftDown = down,
        LeftClicked = clicked,
        LeftReleased = released
    };
}
