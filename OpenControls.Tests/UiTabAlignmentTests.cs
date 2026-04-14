using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTabAlignmentTests
{
    private const string FolderIconGlyph = "\uf07b";

    [Fact]
    public void RenderHelpers_VerticallyCenterGlyphInk_ForDifferentGlyphShapes()
    {
        UiRect bounds = new(0, 0, 120, 24);
        UiFont font = CreateEditorStyleFont();

        UiPoint xPosition = UiRenderHelpers.GetVerticallyCenteredTextPosition(bounds, 0, "X", 1, font);
        UiPoint iconPosition = UiRenderHelpers.GetVerticallyCenteredTextPosition(bounds, 0, FolderIconGlyph, 1, font);

        double xCenter = GetInkVerticalCenter(font, "X", xPosition.Y);
        double iconCenter = GetInkVerticalCenter(font, FolderIconGlyph, iconPosition.Y);

        Assert.True(Math.Abs(xCenter - iconCenter) <= 0.5d, $"xCenter={xCenter}, iconCenter={iconCenter}");
    }

    [Fact]
    public void DockHost_Render_AlignsIconTitleAndCloseGlyphInkCenters()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            AllowClosingLastWindow = true
        };
        host.AddWindow(new UiWindow
        {
            Title = "Project",
            TabIconText = FolderIconGlyph,
            AllowClose = true
        });

        RecordingRenderer renderer = new()
        {
            DefaultFont = CreateEditorStyleFont()
        };
        host.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        DrawTextCall icon = renderer.TextCalls[0];
        DrawTextCall title = renderer.TextCalls[1];
        DrawTextCall close = renderer.TextCalls[2];

        Assert.Equal(FolderIconGlyph, icon.Text);
        Assert.Equal("Project", title.Text);
        Assert.Equal("X", close.Text);

        double titleCenter = GetInkVerticalCenter(title.Font, title.Text, title.Position.Y);
        double iconCenter = GetInkVerticalCenter(icon.Font, icon.Text, icon.Position.Y);
        double closeCenter = GetInkVerticalCenter(close.Font, close.Text, close.Position.Y);

        Assert.True(Math.Abs(iconCenter - titleCenter) <= 0.5d, $"iconCenter={iconCenter}, titleCenter={titleCenter}");
        Assert.True(Math.Abs(closeCenter - titleCenter) <= 0.5d, $"closeCenter={closeCenter}, titleCenter={titleCenter}");
    }

    private static double GetInkVerticalCenter(UiFont font, string text, int drawY)
    {
        UiRect inkBounds = font.MeasureTextInkBounds(text, 1);
        return drawY + inkBounds.Y + inkBounds.Height / 2d;
    }

    private static UiFont CreateEditorStyleFont()
    {
        UiFontBuilder builder = new("TabAlignmentTest", 14);
        builder.AddFile(ResolveRepoFile("third_party/BlocksEternal/assets/fonts/NotoSans-Variable.ttf"), layerName: "Body");
        builder.AddFile(
            ResolveRepoFile("src/Alliance.UI.OpenControls/Fonts/FontAwesome7ProSolid900.ttf"),
            ranges: new[] { UiCodePointRange.PrivateUseArea },
            baselineOffset: 1,
            layerName: "Icons");
        builder.AddBitmapFallback(layerName: "Fallback");
        return builder.Build();
    }

    private static string ResolveRepoFile(string relativePath)
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            string candidate = Path.Combine(current, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new Xunit.Sdk.XunitException($"Could not locate '{relativePath}' from '{AppContext.BaseDirectory}'.");
    }

    private readonly record struct DrawTextCall(string Text, UiPoint Position, int Scale, UiFont Font);

    private sealed class RecordingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;

        public List<DrawTextCall> TextCalls { get; } = new();

        public void FillRect(UiRect rect, UiColor color)
        {
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            DrawText(text, position, color, scale, DefaultFont);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            TextCalls.Add(new DrawTextCall(text, position, scale, font ?? DefaultFont));
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }
}
