using System;

namespace OpenControls;

public sealed class UiDragDropContext
{
    public UiDragDropPayload? Payload { get; private set; }
    public UiElement? Source { get; private set; }
    public UiElement? HoveredTarget { get; private set; }
    public UiElement? AcceptedTarget { get; private set; }
    public UiPoint StartPosition { get; private set; }
    public UiPoint CurrentPosition { get; private set; }
    public bool IsDragging => Payload != null;
    public bool IsDropRequested { get; private set; }

    internal void BeginFrame(UiInputState input)
    {
        CurrentPosition = input.MousePosition;
        IsDropRequested = input.LeftReleased;
        HoveredTarget = null;
        AcceptedTarget = null;

        if (Payload != null && !input.LeftDown && !input.LeftReleased)
        {
            Clear();
        }
    }

    internal void EndFrame()
    {
        if (Payload != null && IsDropRequested && AcceptedTarget == null)
        {
            Clear();
        }
    }

    internal bool BeginDrag(UiElement source, UiDragDropPayload payload, UiPoint startPosition)
    {
        if (Payload != null)
        {
            return false;
        }

        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Source = source;
        StartPosition = startPosition;
        CurrentPosition = startPosition;
        payload.Source = source;
        payload.SourcePosition = startPosition;
        return true;
    }

    internal void SetHoveredTarget(UiElement target)
    {
        HoveredTarget = target;
    }

    public bool MatchesPayloadType(string? type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return true;
        }

        string payloadType = Payload?.Type ?? string.Empty;
        if (string.IsNullOrEmpty(payloadType))
        {
            return false;
        }

        return string.Equals(payloadType, type, StringComparison.OrdinalIgnoreCase);
    }

    public UiDragDropPayload? AcceptPayload(UiElement target, string? type = null)
    {
        if (Payload == null || !IsDropRequested)
        {
            return null;
        }

        if (!MatchesPayloadType(type))
        {
            return null;
        }

        AcceptedTarget = target;
        UiDragDropPayload payload = Payload;
        Clear();
        return payload;
    }

    public void Cancel()
    {
        Clear();
    }

    private void Clear()
    {
        Payload = null;
        Source = null;
        HoveredTarget = null;
        AcceptedTarget = null;
        IsDropRequested = false;
    }
}
