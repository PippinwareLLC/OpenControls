namespace OpenControls.Controls;

public enum UiNodePinDirection
{
    Input,
    Output
}

public enum UiNodePinKind
{
    Data,
    Exec
}

public sealed class UiNodePin
{
    private string _id = string.Empty;
    private string _text = string.Empty;
    private string _dataType = string.Empty;

    public UiNodePin()
    {
    }

    public UiNodePin(string id, string text, UiNodePinDirection direction, UiNodePinKind kind = UiNodePinKind.Data)
    {
        Id = id;
        Text = text;
        Direction = direction;
        Kind = kind;
    }

    public string Id
    {
        get => _id;
        set => _id = value ?? string.Empty;
    }

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public UiNodePinDirection Direction { get; set; }
    public UiNodePinKind Kind { get; set; } = UiNodePinKind.Data;

    public string DataType
    {
        get => _dataType;
        set => _dataType = value ?? string.Empty;
    }

    public bool Enabled { get; set; } = true;
    public bool Selected { get; set; }
    public bool Hovered { get; internal set; }
    public UiNodePinLayout Layout { get; internal set; } = UiNodePinLayout.Empty;
}

public readonly struct UiNodePinLayout
{
    public UiNodePinLayout(UiNodePin? pin, UiRect rowBounds, UiRect labelBounds, UiRect hitBounds, UiPoint center)
    {
        Pin = pin;
        RowBounds = rowBounds;
        LabelBounds = labelBounds;
        HitBounds = hitBounds;
        Center = center;
    }

    public UiNodePin? Pin { get; }
    public UiRect RowBounds { get; }
    public UiRect LabelBounds { get; }
    public UiRect HitBounds { get; }
    public UiPoint Center { get; }
    public bool IsValid => Pin != null;

    public static UiNodePinLayout Empty => default;
}

public readonly struct UiNodeDebugLayout
{
    public UiNodeDebugLayout(
        UiRect bounds,
        UiRect headerBounds,
        UiRect bodyBounds,
        UiRect titleBounds,
        UiRect bodyTextBounds,
        IReadOnlyList<UiNodePinLayout> pins)
    {
        Bounds = bounds;
        HeaderBounds = headerBounds;
        BodyBounds = bodyBounds;
        TitleBounds = titleBounds;
        BodyTextBounds = bodyTextBounds;
        Pins = pins ?? Array.Empty<UiNodePinLayout>();
    }

    public UiRect Bounds { get; }
    public UiRect HeaderBounds { get; }
    public UiRect BodyBounds { get; }
    public UiRect TitleBounds { get; }
    public UiRect BodyTextBounds { get; }
    public IReadOnlyList<UiNodePinLayout> Pins { get; }

    public static UiNodeDebugLayout Empty => new(default, default, default, default, default, Array.Empty<UiNodePinLayout>());
}

public readonly struct UiNodeWirePreviewState
{
    public UiNodeWirePreviewState(
        bool active,
        UiNodeControl? startNode,
        UiNodePin? startPin,
        UiPoint end,
        IReadOnlyList<UiPoint> route)
    {
        Active = active;
        StartNode = startNode;
        StartPin = startPin;
        End = end;
        Route = route ?? Array.Empty<UiPoint>();
    }

    public bool Active { get; }
    public UiNodeControl? StartNode { get; }
    public UiNodePin? StartPin { get; }
    public UiPoint End { get; }
    public IReadOnlyList<UiPoint> Route { get; }

    public static UiNodeWirePreviewState Inactive => new(false, null, null, default, Array.Empty<UiPoint>());
}
