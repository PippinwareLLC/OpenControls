using System.Text.Json;

namespace OpenControls.Editor;

public sealed class EditorLayout
{
    public List<EditorLayoutControl> Controls { get; set; } = new();
}

public sealed class EditorLayoutControl
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Text { get; set; }
    public bool? Checked { get; set; }
    public Dictionary<string, JsonElement>? Properties { get; set; }
}
