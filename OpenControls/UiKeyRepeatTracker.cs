namespace OpenControls;

public sealed class UiKeyRepeatTracker
{
    private readonly Dictionary<UiKey, double> _nextRepeatTimes = new();

    public double InitialDelaySeconds { get; set; } = 0.45;
    public double RepeatIntervalSeconds { get; set; } = 0.05;

    public bool IsRepeatDue(UiKey key, bool isDown, bool justPressed, double currentTimeSeconds)
    {
        if (justPressed)
        {
            _nextRepeatTimes[key] = currentTimeSeconds + Math.Max(0d, InitialDelaySeconds);
            return false;
        }

        if (!isDown)
        {
            _nextRepeatTimes.Remove(key);
            return false;
        }

        if (!_nextRepeatTimes.TryGetValue(key, out double nextRepeatTime))
        {
            _nextRepeatTimes[key] = currentTimeSeconds + Math.Max(0d, InitialDelaySeconds);
            return false;
        }

        if (currentTimeSeconds < nextRepeatTime)
        {
            return false;
        }

        double interval = Math.Max(0.01d, RepeatIntervalSeconds);
        do
        {
            nextRepeatTime += interval;
        }
        while (nextRepeatTime <= currentTimeSeconds);

        _nextRepeatTimes[key] = nextRepeatTime;
        return true;
    }

    public void Reset()
    {
        _nextRepeatTimes.Clear();
    }
}
