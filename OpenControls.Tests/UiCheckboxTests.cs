using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiCheckboxTests
{
    [Fact]
    public void SetCheckedWithoutNotify_UpdatesCheckedStateWithoutRaisingEvent()
    {
        UiCheckbox checkbox = new();
        int raisedCount = 0;
        checkbox.CheckedChanged += _ => raisedCount++;

        checkbox.SetCheckedWithoutNotify(true);

        Assert.True(checkbox.Checked);
        Assert.Equal(0, raisedCount);
    }

    [Fact]
    public void CheckedSetter_RaisesEventWhenValueChanges()
    {
        UiCheckbox checkbox = new();
        int raisedCount = 0;
        bool? lastValue = null;
        checkbox.CheckedChanged += value =>
        {
            raisedCount++;
            lastValue = value;
        };

        checkbox.Checked = true;

        Assert.True(checkbox.Checked);
        Assert.Equal(1, raisedCount);
        Assert.True(lastValue);
    }
}
