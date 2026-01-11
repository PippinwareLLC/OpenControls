namespace OpenControls;

public readonly struct UiNavigationInput
{
    public bool MoveLeft { get; init; }
    public bool MoveRight { get; init; }
    public bool MoveUp { get; init; }
    public bool MoveDown { get; init; }
    public bool Home { get; init; }
    public bool End { get; init; }
    public bool Backspace { get; init; }
    public bool Delete { get; init; }
    public bool Tab { get; init; }
    public bool Enter { get; init; }
    public bool Escape { get; init; }
}
