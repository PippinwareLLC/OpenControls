namespace OpenControls;

public sealed class UiDragDropPayload
{
    public UiDragDropPayload(string type, object? data)
    {
        Type = type ?? string.Empty;
        Data = data;
    }

    public string Type { get; }
    public object? Data { get; }
    public UiElement? Source { get; internal set; }
    public UiPoint SourcePosition { get; internal set; }
}
