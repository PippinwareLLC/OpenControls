using OpenControls;
using OpenControls.Controls;

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
    public int? SelectionAnchor { get; set; }
    public int? SelectionStart { get; set; }
    public int? SelectionEnd { get; set; }
    public int? ScrollX { get; set; }
    public int? ScrollY { get; set; }
    public int? SelectedIndex { get; set; }
    public List<int> SelectedIndices { get; set; } = new();
    public string? FilterText { get; set; }
    public bool? IsOpen { get; set; }
    public UiTableElementState? Table { get; set; }
    public Dictionary<string, string> CustomState { get; set; } = new();
}

public sealed class UiTableElementState
{
    public int ScrollX { get; set; }
    public int ScrollY { get; set; }
    public List<UiTableColumnSnapshot> Columns { get; set; } = new();
}

public sealed class UiTableColumnSnapshot
{
    public int ColumnIndex { get; set; }
    public int DisplayIndex { get; set; }
    public UiTableColumnWidthMode WidthMode { get; set; }
    public int Width { get; set; }
    public float StretchWeight { get; set; } = 1f;
    public bool Visible { get; set; } = true;
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
