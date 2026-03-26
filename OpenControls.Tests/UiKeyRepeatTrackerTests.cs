using OpenControls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiKeyRepeatTrackerTests
{
    [Fact]
    public void IsRepeatDue_WaitsForDelayThenRepeatsAtInterval()
    {
        UiKeyRepeatTracker tracker = new()
        {
            InitialDelaySeconds = 0.4,
            RepeatIntervalSeconds = 0.1
        };

        Assert.False(tracker.IsRepeatDue(UiKey.Left, isDown: true, justPressed: true, currentTimeSeconds: 0.0));
        Assert.False(tracker.IsRepeatDue(UiKey.Left, isDown: true, justPressed: false, currentTimeSeconds: 0.39));
        Assert.True(tracker.IsRepeatDue(UiKey.Left, isDown: true, justPressed: false, currentTimeSeconds: 0.4));
        Assert.False(tracker.IsRepeatDue(UiKey.Left, isDown: true, justPressed: false, currentTimeSeconds: 0.45));
        Assert.True(tracker.IsRepeatDue(UiKey.Left, isDown: true, justPressed: false, currentTimeSeconds: 0.5));
    }

    [Fact]
    public void IsRepeatDue_ResetsAfterRelease()
    {
        UiKeyRepeatTracker tracker = new()
        {
            InitialDelaySeconds = 0.2,
            RepeatIntervalSeconds = 0.05
        };

        Assert.False(tracker.IsRepeatDue(UiKey.Backspace, isDown: true, justPressed: true, currentTimeSeconds: 0.0));
        Assert.True(tracker.IsRepeatDue(UiKey.Backspace, isDown: true, justPressed: false, currentTimeSeconds: 0.2));
        Assert.False(tracker.IsRepeatDue(UiKey.Backspace, isDown: false, justPressed: false, currentTimeSeconds: 0.21));
        Assert.False(tracker.IsRepeatDue(UiKey.Backspace, isDown: true, justPressed: true, currentTimeSeconds: 1.0));
        Assert.False(tracker.IsRepeatDue(UiKey.Backspace, isDown: true, justPressed: false, currentTimeSeconds: 1.19));
        Assert.True(tracker.IsRepeatDue(UiKey.Backspace, isDown: true, justPressed: false, currentTimeSeconds: 1.2));
    }
}
