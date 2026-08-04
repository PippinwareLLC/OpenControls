using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiIconButtonTests
{
    private static ushort[] CrossIcon()
    {
        var rows = new ushort[UiIconButton.IconPixels];
        for (int row = 0; row < rows.Length; row++)
        {
            rows[row] = (ushort)(1 << (UiIconButton.IconPixels - 1 - row) | 0b1);
        }

        return rows;
    }

    private static UiUpdateContext IdleUpdate() => new(
        new UiInputState(),
        new UiFocusManager(),
        new UiDragDropContext(),
        1f / 60f,
        UiFont.Default,
        new UiMemoryClipboard());

    [Fact]
    public void IconsAreClonedSixteenRowMasksAndBadSizesThrow()
    {
        var button = new UiIconButton();
        ushort[] icon = CrossIcon();
        button.SetIcon(icon);
        icon[0] = 0;

        Assert.Equal((ushort)(1 << 15 | 1), button.IconRows[0]);
        Assert.Throws<ArgumentException>(() => button.SetIcon(new ushort[3]));
        Assert.Throws<ArgumentNullException>(() => button.SetIcon(null!));
    }

    [Fact]
    public void ToolbarLaysButtonsOutLeftToRightWithUniformWidthAndGap()
    {
        var toolbar = new UiToolbar
        {
            Bounds = new UiRect(10, 20, 400, 30),
            ButtonWidth = 90,
            Gap = 8,
        };
        UiIconButton first = toolbar.AddButton("Market", CrossIcon());
        UiIconButton second = toolbar.AddButton("Board", CrossIcon());
        bool clicked = false;
        UiIconButton third = toolbar.AddButton("Yard", CrossIcon(), () => clicked = true);

        toolbar.Update(IdleUpdate());

        Assert.Equal(new UiRect(10, 20, 90, 30), first.Bounds);
        Assert.Equal(new UiRect(108, 20, 90, 30), second.Bounds);
        Assert.Equal(new UiRect(206, 20, 90, 30), third.Bounds);
        Assert.False(clicked);
        Assert.Equal(3, toolbar.Buttons.Count);
    }
}
