namespace OpenControls.SilkNet;

public readonly record struct UiRenderMetric(string Name, int Calls, double DurationMs);

public sealed class UiRenderMetricsSnapshot
{
    public static UiRenderMetricsSnapshot Empty { get; } = new(0, Array.Empty<UiRenderMetric>());

    public UiRenderMetricsSnapshot(long sequence, IReadOnlyList<UiRenderMetric> metrics)
    {
        Sequence = sequence;
        Metrics = metrics ?? Array.Empty<UiRenderMetric>();
        TotalDurationMs = Metrics.Sum(metric => metric.DurationMs);
    }

    public long Sequence { get; }

    public IReadOnlyList<UiRenderMetric> Metrics { get; }

    public double TotalDurationMs { get; }
}
