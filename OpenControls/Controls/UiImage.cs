namespace OpenControls.Controls;

public sealed class UiImage : UiElement
{
    private Action<IUiRenderer, UiRect>? _drawImage;
    private IUiImageSource? _imageSource;
    private UiColor _background = UiColor.Transparent;
    private UiColor _border = UiColor.Transparent;
    private int _borderThickness = 1;
    private int _padding;
    private int _cornerRadius;
    private bool _showCheckerboard;
    private int _checkerSize = 6;
    private UiColor _checkerColorLight = new UiColor(200, 200, 200);
    private UiColor _checkerColorDark = new UiColor(120, 120, 120);
    private UiColor _placeholderColor = new UiColor(60, 70, 90);

    public Action<IUiRenderer, UiRect>? DrawImage
    {
        get => _drawImage;
        set => SetInvalidatingValue(ref _drawImage, value, UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public IUiImageSource? ImageSource
    {
        get => _imageSource;
        set => SetInvalidatingValue(ref _imageSource, value, UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public UiColor Background
    {
        get => _background;
        set => SetInvalidatingValue(ref _background, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor Border
    {
        get => _border;
        set => SetInvalidatingValue(ref _border, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int BorderThickness
    {
        get => _borderThickness;
        set => SetInvalidatingValue(ref _borderThickness, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public int Padding
    {
        get => _padding;
        set => SetInvalidatingValue(ref _padding, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetInvalidatingValue(ref _cornerRadius, Math.Max(0, value), UiInvalidationReason.Style | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public bool ShowCheckerboard
    {
        get => _showCheckerboard;
        set => SetInvalidatingValue(ref _showCheckerboard, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int CheckerSize
    {
        get => _checkerSize;
        set => SetInvalidatingValue(ref _checkerSize, Math.Max(1, value), UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor CheckerColorLight
    {
        get => _checkerColorLight;
        set => SetInvalidatingValue(ref _checkerColorLight, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor CheckerColorDark
    {
        get => _checkerColorDark;
        set => SetInvalidatingValue(ref _checkerColorDark, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor PlaceholderColor
    {
        get => _placeholderColor;
        set => SetInvalidatingValue(ref _placeholderColor, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public override bool IsRenderCacheVolatile(UiContext context)
    {
        return DrawImage != null || (ImageSource?.IsRenderCacheVolatile ?? false);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        int inset = Math.Max(0, Padding) + Math.Max(0, BorderThickness);
        UiRect inner = new UiRect(
            Bounds.X + inset,
            Bounds.Y + inset,
            Math.Max(0, Bounds.Width - inset * 2),
            Math.Max(0, Bounds.Height - inset * 2));

        if (inner.Width > 0 && inner.Height > 0)
        {
            if (ShowCheckerboard)
            {
                context.Renderer.FillRectCheckerboard(inner, CheckerSize, CheckerColorLight, CheckerColorDark);
            }

            if (ImageSource != null)
            {
                ImageSource.Draw(context.Renderer, inner);
            }
            else if (DrawImage != null)
            {
                DrawImage(context.Renderer, inner);
            }
            else if (PlaceholderColor.A > 0)
            {
                int radius = Math.Max(0, CornerRadius - inset);
                UiRenderHelpers.FillRectRounded(context.Renderer, inner, radius, PlaceholderColor);
            }
        }

        base.Render(context);
    }
}
