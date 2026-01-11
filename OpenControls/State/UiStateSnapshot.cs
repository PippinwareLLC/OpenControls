using OpenControls;

namespace OpenControls.State;

public sealed class UiStateSnapshot
{
    public List<UiElementState> Elements { get; set; } = new();
    public List<UiDockWorkspaceState> DockWorkspaces { get; set; } = new();
}

public sealed class UiElementState
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public UiRect Bounds { get; set; }
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
    public string? Text { get; set; }
    public int? CaretIndex { get; set; }
}

public sealed class UiDockWorkspaceState
{
    public string Id { get; set; } = string.Empty;
    public UiDockNodeState? Root { get; set; }
    public List<UiDockHostState> Hosts { get; set; } = new();
    public List<UiFloatingWindowState> FloatingWindows { get; set; } = new();
}

public sealed class UiDockNodeState
{
    public string? HostId { get; set; }
    public UiDockNodeState? First { get; set; }
    public UiDockNodeState? Second { get; set; }
    public bool SplitHorizontal { get; set; }
    public float SplitRatio { get; set; }
}

public sealed class UiDockHostState
{
    public string HostId { get; set; } = string.Empty;
    public List<string> WindowIds { get; set; } = new();
    public int ActiveIndex { get; set; }
}

public sealed class UiFloatingWindowState
{
    public string WindowId { get; set; } = string.Empty;
    public UiRect Bounds { get; set; }
}
