using System;

namespace OpenControls;

public interface IUiProfiler
{
    IDisposable BeginScope(string name);
}

public static class UiProfiling
{
    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    public static IUiProfiler? Current { get; set; }

    public static IDisposable Scope(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NullScope.Instance;
        }

        return Current?.BeginScope(name) ?? NullScope.Instance;
    }
}
