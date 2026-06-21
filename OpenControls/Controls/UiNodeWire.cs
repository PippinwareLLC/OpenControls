namespace OpenControls.Controls;

public sealed class UiNodeWire
{
    private readonly record struct RouteKey(
        UiPoint Start,
        UiNodePinDirection StartDirection,
        UiPoint End,
        UiNodePinDirection EndDirection,
        int Thickness);

    private readonly record struct TessellationKey(
        UiPoint Start,
        UiPoint StartControl,
        UiPoint EndControl,
        UiPoint End,
        int Thickness);

    private UiPoint[] _route = Array.Empty<UiPoint>();
    private UiPoint[] _tessellatedRoute = Array.Empty<UiPoint>();
    private RouteKey _routeKey;
    private TessellationKey _tessellationKey;
    private bool _routeValid;
    private bool _tessellationValid;

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

    internal bool NeedsRouteRefresh(int thickness)
    {
        RouteKey key = BuildRouteKey(thickness);
        return !_routeValid || !_routeKey.Equals(key);
    }

    internal bool RefreshRoute(int thickness)
    {
        RouteKey key = BuildRouteKey(thickness);
        if (_routeValid && _routeKey.Equals(key))
        {
            return false;
        }

        _route = BuildRoute(key.Start, key.StartDirection, key.End, key.EndDirection);
        Bounds = CalculateBounds(_route, key.Thickness);
        _routeKey = key;
        _routeValid = true;
        _tessellationValid = false;
        return true;
    }

    internal IReadOnlyList<UiPoint> GetRenderRoute(int thickness)
    {
        int safeThickness = Math.Max(1, thickness);
        if (_route.Length < 5)
        {
            return _route;
        }

        TessellationKey key = new(
            _route[0],
            _route.Length > 1 ? _route[1] : _route[0],
            _route.Length > 2 ? _route[^2] : _route[^1],
            _route[^1],
            safeThickness);
        if (_tessellationValid && _tessellationKey.Equals(key))
        {
            return _tessellatedRoute;
        }

        _tessellatedRoute = TessellateCubic(key.Start, key.StartControl, key.EndControl, key.End);
        _tessellationKey = key;
        _tessellationValid = true;
        return _tessellatedRoute;
    }

    private RouteKey BuildRouteKey(int thickness)
    {
        return new RouteKey(
            FromPin.Layout.Center,
            FromPin.Direction,
            ToPin.Layout.Center,
            ToPin.Direction,
            Math.Max(1, thickness));
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

    internal static UiPoint[] TessellateCubic(UiPoint start, UiPoint startControl, UiPoint endControl, UiPoint end)
    {
        int steps = Math.Max(16, Math.Abs(end.X - start.X) / 12 + Math.Abs(end.Y - start.Y) / 16);
        UiPoint[] points = new UiPoint[steps + 1];
        points[0] = start;
        for (int step = 1; step <= steps; step++)
        {
            float t = step / (float)steps;
            points[step] = Cubic(start, startControl, endControl, end, t);
        }

        return points;
    }

    private static UiPoint Cubic(UiPoint a, UiPoint b, UiPoint c, UiPoint d, float t)
    {
        float inv = 1f - t;
        float x = inv * inv * inv * a.X
            + 3f * inv * inv * t * b.X
            + 3f * inv * t * t * c.X
            + t * t * t * d.X;
        float y = inv * inv * inv * a.Y
            + 3f * inv * inv * t * b.Y
            + 3f * inv * t * t * c.Y
            + t * t * t * d.Y;
        return new UiPoint((int)Math.Round(x), (int)Math.Round(y));
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
