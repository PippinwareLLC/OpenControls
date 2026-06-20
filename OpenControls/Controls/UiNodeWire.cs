namespace OpenControls.Controls;

public sealed class UiNodeWire
{
    private UiPoint[] _route = Array.Empty<UiPoint>();

    public UiNodeWire(UiNodeControl fromNode, UiNodePin fromPin, UiNodeControl toNode, UiNodePin toPin)
    {
        FromNode = fromNode ?? throw new ArgumentNullException(nameof(fromNode));
        FromPin = fromPin ?? throw new ArgumentNullException(nameof(fromPin));
        ToNode = toNode ?? throw new ArgumentNullException(nameof(toNode));
        ToPin = toPin ?? throw new ArgumentNullException(nameof(toPin));
    }

    public UiNodeControl FromNode { get; }
    public UiNodePin FromPin { get; }
    public UiNodeControl ToNode { get; }
    public UiNodePin ToPin { get; }
    public bool Selected { get; set; }
    public bool Hovered { get; internal set; }
    public int Thickness { get; set; } = 2;
    public IReadOnlyList<UiPoint> Route => _route;
    public UiRect Bounds { get; private set; }
    public UiNodePinKind Kind => FromPin.Kind == UiNodePinKind.Exec || ToPin.Kind == UiNodePinKind.Exec
        ? UiNodePinKind.Exec
        : UiNodePinKind.Data;

    internal void RefreshRoute(int thickness)
    {
        _route = BuildRoute(FromPin.Layout.Center, FromPin.Direction, ToPin.Layout.Center, ToPin.Direction);
        Bounds = CalculateBounds(_route, Math.Max(1, thickness));
    }

    internal static UiPoint[] BuildRoute(
        UiPoint start,
        UiNodePinDirection startDirection,
        UiPoint end,
        UiNodePinDirection endDirection,
        int lead = 32)
    {
        int safeLead = Math.Max(8, lead);
        int startDir = startDirection == UiNodePinDirection.Output ? 1 : -1;
        int endDir = endDirection == UiNodePinDirection.Input ? -1 : 1;
        UiPoint startLead = new(start.X + startDir * safeLead, start.Y);
        UiPoint endLead = new(end.X + endDir * safeLead, end.Y);
        int midX = (startLead.X + endLead.X) / 2;

        return new[]
        {
            start,
            startLead,
            new UiPoint(midX, startLead.Y),
            new UiPoint(midX, endLead.Y),
            endLead,
            end
        };
    }

    internal static UiPoint[] BuildPreviewRoute(UiNodePin startPin, UiPoint end, int lead = 32)
    {
        ArgumentNullException.ThrowIfNull(startPin);

        UiPoint start = startPin.Layout.Center;
        int safeLead = Math.Max(8, lead);
        int startDir = startPin.Direction == UiNodePinDirection.Output ? 1 : -1;
        UiPoint startLead = new(start.X + startDir * safeLead, start.Y);
        int midX = (startLead.X + end.X) / 2;

        return new[]
        {
            start,
            startLead,
            new UiPoint(midX, startLead.Y),
            new UiPoint(midX, end.Y),
            end
        };
    }

    internal static UiRect CalculateBounds(IReadOnlyList<UiPoint> route, int thickness)
    {
        if (route == null || route.Count == 0)
        {
            return default;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < route.Count; i++)
        {
            UiPoint point = route[i];
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        int pad = Math.Max(1, thickness);
        return new UiRect(minX - pad, minY - pad, maxX - minX + pad * 2, maxY - minY + pad * 2);
    }
}

public readonly struct UiNodeWireDebugLayout
{
    public UiNodeWireDebugLayout(
        UiNodeWire? wire,
        IReadOnlyList<UiPoint> route,
        UiRect bounds,
        UiRect hitBounds,
        UiNodePinKind kind,
        int thickness,
        UiColor color,
        bool selected,
        bool hovered)
    {
        Wire = wire;
        Route = route ?? Array.Empty<UiPoint>();
        Bounds = bounds;
        HitBounds = hitBounds;
        Kind = kind;
        Thickness = thickness;
        Color = color;
        Selected = selected;
        Hovered = hovered;
    }

    public UiNodeWire? Wire { get; }
    public IReadOnlyList<UiPoint> Route { get; }
    public UiRect Bounds { get; }
    public UiRect HitBounds { get; }
    public UiNodePinKind Kind { get; }
    public int Thickness { get; }
    public UiColor Color { get; }
    public bool Selected { get; }
    public bool Hovered { get; }
    public bool IsValid => Wire != null;
}
