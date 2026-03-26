namespace OpenControls;

public sealed class UiFontRegistry
{
    private readonly Dictionary<string, UiFont> _fonts = new(StringComparer.Ordinal);

    public UiFontRegistry()
    {
        DefaultFont = UiFont.Default;
        _fonts[DefaultFont.Name] = DefaultFont;
    }

    public UiFont DefaultFont { get; private set; }
    public IReadOnlyDictionary<string, UiFont> Fonts => _fonts;

    public UiFont Register(UiFont font, bool setDefault = false)
    {
        if (font == null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        _fonts[font.Name] = font;
        if (setDefault)
        {
            DefaultFont = font;
        }

        return font;
    }

    public UiFont Register(UiFontBuilder builder, bool setDefault = false)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return Register(builder.Build(), setDefault);
    }

    public bool TryGetFont(string name, out UiFont font)
    {
        return _fonts.TryGetValue(name, out font!);
    }
}
