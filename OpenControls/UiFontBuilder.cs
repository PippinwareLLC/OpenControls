namespace OpenControls;

public sealed class UiFontBuilder
{
    private readonly List<UiFontLayerDefinition> _layers = new();

    public UiFontBuilder(string name, int pixelSize)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A font name is required.", nameof(name));
        }

        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        Name = name;
        PixelSize = pixelSize;
    }

    public string Name { get; }
    public int PixelSize { get; }

    public UiFontBuilder AddFile(
        string path,
        IEnumerable<UiCodePointRange>? ranges = null,
        int baselineOffset = 0,
        int advanceOffset = 0,
        bool monospace = false,
        string? layerName = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A font path is required.", nameof(path));
        }

        _layers.Add(new UiFontLayerDefinition(
            layerName ?? Path.GetFileName(path),
            ranges?.ToArray(),
            baselineOffset,
            advanceOffset,
            monospace,
            new UiOutlineFontSource(path, monospace)));
        return this;
    }

    public UiFontBuilder AddBitmapFallback(
        TinyBitmapFont? font = null,
        IEnumerable<UiCodePointRange>? ranges = null,
        int baselineOffset = 0,
        int advanceOffset = 0,
        string? layerName = null)
    {
        TinyBitmapFont bitmapFont = font ?? new TinyBitmapFont();
        _layers.Add(new UiFontLayerDefinition(
            layerName ?? "TinyBitmap",
            ranges?.ToArray(),
            baselineOffset,
            advanceOffset,
            Monospace: true,
            new UiBitmapFontSource(bitmapFont)));
        return this;
    }

    public UiFont Build()
    {
        return new UiFont(Name, PixelSize, _layers);
    }
}
