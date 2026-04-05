using Xunit;
using OpenControls.Controls;

namespace OpenControls.Tests;

public sealed class UiRenderCacheTests
{
    private sealed class CountingRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public int FillRectCalls { get; private set; }
        public int DrawRectCalls { get; private set; }
        public int DrawTextCalls { get; private set; }
        public int PushClipCalls { get; private set; }
        public int PopClipCalls { get; private set; }

        public void FillRect(UiRect rect, UiColor color) => FillRectCalls++;
        public void DrawRect(UiRect rect, UiColor color, int thickness = 1) => DrawRectCalls++;
        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight) => FillRectCalls++;
        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB) => FillRectCalls++;
        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1) => DrawTextCalls++;
        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font) => DrawTextCalls++;
        public int MeasureTextWidth(string text, int scale = 1) => MeasureTextWidth(text, scale, DefaultFont);
        public int MeasureTextWidth(string text, int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextWidth(text, scale);
        public int MeasureTextHeight(int scale = 1) => MeasureTextHeight(scale, DefaultFont);
        public int MeasureTextHeight(int scale, UiFont? font) => (font ?? DefaultFont).MeasureTextHeight(scale);
        public void PushClip(UiRect rect) => PushClipCalls++;
        public void PopClip() => PopClipCalls++;
    }

    private class CountingElement : UiElement
    {
        public int RenderCount { get; private set; }
        public int OverlayRenderCount { get; private set; }

        public override void Render(UiRenderContext context)
        {
            RenderCount++;
            context.Renderer.FillRect(Bounds, UiColor.White);
            base.Render(context);
        }

        public override void RenderOverlay(UiRenderContext context)
        {
            OverlayRenderCount++;
            context.Renderer.DrawRect(Bounds, UiColor.White, 1);
            base.RenderOverlay(context);
        }
    }

    private sealed class VolatileElement : CountingElement
    {
        public override bool IsRenderCacheVolatile(UiContext context) => true;
    }

    private sealed class CacheRootCountingElement : CountingElement
    {
        public override bool IsRenderCacheRoot(UiContext context) => true;
    }

    [Fact]
    public void RenderCaching_ReplaysRecordedCommandsWithoutReinvokingRender()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Render(renderer);

        Assert.Equal(1, root.RenderCount);
        Assert.Equal(1, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenUiInvalidationVersionChanges()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        root.Bounds = new UiRect(0, 0, 48, 48);
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenInteractionSignatureChanges()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(12, 12),
            ScreenMousePosition = new UiPoint(12, 12)
        });
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
    }

    [Fact]
    public void RenderCaching_BypassesReplayForVolatileElements()
    {
        VolatileElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Render(renderer);

        Assert.Equal(2, root.RenderCount);
        Assert.Equal(2, root.OverlayRenderCount);
        Assert.Equal(2, renderer.FillRectCalls);
        Assert.Equal(2, renderer.DrawRectCalls);
    }

    [Fact]
    public void RenderCaching_StatisticsTrackRecordReplayAndMissReasons()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        UiRenderCacheStatisticsSnapshot firstRenderStats = context.RenderCacheStatistics;
        Assert.Equal(1, firstRenderStats.RootPass.RecordCount);
        Assert.Equal(1, firstRenderStats.RootPass.ReplayCount);
        Assert.Equal(UiRenderCachePassAction.RecordAndReplay, firstRenderStats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Empty, firstRenderStats.RootPass.LastMissReason);

        context.Render(renderer);
        UiRenderCacheStatisticsSnapshot secondRenderStats = context.RenderCacheStatistics;
        Assert.Equal(1, secondRenderStats.RootPass.RecordCount);
        Assert.Equal(2, secondRenderStats.RootPass.ReplayCount);
        Assert.Equal(UiRenderCachePassAction.Replay, secondRenderStats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.None, secondRenderStats.RootPass.LastMissReason);

        root.Bounds = new UiRect(2, 2, 24, 24);
        context.Render(renderer);
        UiRenderCacheStatisticsSnapshot invalidationStats = context.RenderCacheStatistics;
        Assert.Equal(2, invalidationStats.RootPass.RecordCount);
        Assert.Equal(3, invalidationStats.RootPass.ReplayCount);
        Assert.Equal(UiRenderCacheMissReason.Invalidation, invalidationStats.RootPass.LastMissReason);
        Assert.Equal(1, invalidationStats.RootPass.InvalidationMissCount);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(12, 12),
            ScreenMousePosition = new UiPoint(12, 12)
        });
        context.Render(renderer);
        UiRenderCacheStatisticsSnapshot interactionStats = context.RenderCacheStatistics;
        Assert.Equal(3, interactionStats.RootPass.RecordCount);
        Assert.Equal(4, interactionStats.RootPass.ReplayCount);
        Assert.Equal(UiRenderCacheMissReason.Interaction, interactionStats.RootPass.LastMissReason);
        Assert.Equal(1, interactionStats.RootPass.InteractionMissCount);
    }

    [Fact]
    public void RenderCaching_StatisticsTrackDisabledAndVolatileBypasses()
    {
        CountingElement disabledRoot = new()
        {
            Bounds = new UiRect(0, 0, 24, 24)
        };
        UiContext disabledContext = new(disabledRoot);
        disabledContext.Update(new UiInputState());
        CountingRenderer disabledRenderer = new();
        disabledContext.Render(disabledRenderer);

        UiRenderCacheStatisticsSnapshot disabledStats = disabledContext.RenderCacheStatistics;
        Assert.False(disabledStats.RenderCachingEnabled);
        Assert.Equal(UiRenderCachePassAction.Bypass, disabledStats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Disabled, disabledStats.RootPass.LastMissReason);
        Assert.Equal(1, disabledStats.RootPass.DisabledBypassCount);

        VolatileElement volatileRoot = new()
        {
            Bounds = new UiRect(0, 0, 24, 24),
            Id = "volatile-root"
        };
        UiContext volatileContext = CreateContext(volatileRoot);
        CountingRenderer volatileRenderer = new();
        volatileContext.Render(volatileRenderer);

        UiRenderCacheStatisticsSnapshot volatileStats = volatileContext.RenderCacheStatistics;
        Assert.True(volatileStats.VolatileRenderStateDetected);
        Assert.Contains("volatile-root", volatileStats.VolatileElementLabel);
        Assert.Equal(UiRenderCachePassAction.Bypass, volatileStats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Volatile, volatileStats.RootPass.LastMissReason);
        Assert.Equal(1, volatileStats.RootPass.VolatileBypassCount);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenPopupOpenStateChanges()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 80, 80)
        };
        UiPopup popup = new()
        {
            Bounds = new UiRect(8, 8, 40, 40),
            Background = UiColor.Transparent,
            Border = UiColor.Transparent
        };
        CountingElement content = new()
        {
            Bounds = new UiRect(10, 10, 16, 16)
        };
        popup.AddChild(content);
        root.AddChild(popup);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        Assert.Equal(0, content.RenderCount);
        Assert.Equal(0, content.OverlayRenderCount);

        popup.Open();
        context.Render(renderer);
        Assert.Equal(1, content.RenderCount);
        Assert.Equal(1, content.OverlayRenderCount);

        popup.Close();
        context.Render(renderer);
        Assert.Equal(1, content.RenderCount);
        Assert.Equal(1, content.OverlayRenderCount);
    }

    [Fact]
    public void RenderCaching_BypassesReplayForDelegateBackedImages()
    {
        int drawCount = 0;
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 80, 80)
        };
        UiImage image = new()
        {
            Id = "delegate-image",
            Bounds = new UiRect(8, 8, 32, 32),
            ImageSource = new UiDelegateImageSource(
                (renderer, bounds) =>
                {
                    drawCount++;
                    renderer.FillRect(bounds, UiColor.White);
                },
                debugName: "splash-logo")
        };
        root.AddChild(image);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(2, drawCount);
        Assert.True(stats.VolatileRenderStateDetected);
        Assert.Contains("delegate-image", stats.VolatileElementLabel);
        Assert.Equal(UiRenderCachePassAction.Bypass, stats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Volatile, stats.RootPass.LastMissReason);
    }

    [Fact]
    public void RenderCaching_CacheRootsReRecordIndependentlyWhenChildRootInvalidates()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 120, 80)
        };
        CacheRootCountingElement first = new()
        {
            Id = "first-window",
            Bounds = new UiRect(0, 0, 50, 40)
        };
        CacheRootCountingElement second = new()
        {
            Id = "second-window",
            Bounds = new UiRect(60, 0, 50, 40)
        };
        root.AddChild(first);
        root.AddChild(second);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        first.Bounds = new UiRect(0, 0, 52, 42);
        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(1, root.RenderCount);
        Assert.Equal(1, root.OverlayRenderCount);
        Assert.Equal(2, first.RenderCount);
        Assert.Equal(2, first.OverlayRenderCount);
        Assert.Equal(1, second.RenderCount);
        Assert.Equal(1, second.OverlayRenderCount);
        Assert.Equal(UiRenderCachePassAction.Replay, stats.RootPass.LastAction);
        Assert.Equal(1, stats.RootPass.RecordCount);
        Assert.Equal(2, stats.RootPass.ReplayCount);
    }

    [Fact]
    public void RenderCaching_CacheRootInteractionDoesNotInvalidateParentOrSiblings()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 120, 80)
        };
        CacheRootCountingElement first = new()
        {
            Id = "first-window",
            Bounds = new UiRect(0, 0, 50, 40)
        };
        CacheRootCountingElement second = new()
        {
            Id = "second-window",
            Bounds = new UiRect(60, 0, 50, 40)
        };
        root.AddChild(first);
        root.AddChild(second);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            ScreenMousePosition = new UiPoint(10, 10)
        });
        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(1, root.RenderCount);
        Assert.Equal(1, root.OverlayRenderCount);
        Assert.Equal(2, first.RenderCount);
        Assert.Equal(2, first.OverlayRenderCount);
        Assert.Equal(1, second.RenderCount);
        Assert.Equal(1, second.OverlayRenderCount);
        Assert.Equal(UiRenderCachePassAction.Replay, stats.RootPass.LastAction);
        Assert.Equal(1, stats.RootPass.RecordCount);
        Assert.Equal(2, stats.RootPass.ReplayCount);
    }

    [Fact]
    public void RenderCaching_UiWindowOnlyActsAsCacheRootWhenOptedIn()
    {
        CountingElement root = new()
        {
            Bounds = new UiRect(0, 0, 160, 120)
        };
        UiWindow cachedWindow = new()
        {
            Id = "cached-window",
            Bounds = new UiRect(0, 0, 70, 60),
            RenderCacheRootEnabled = true
        };
        CountingElement cachedContent = new()
        {
            Bounds = new UiRect(0, 24, 40, 20)
        };
        cachedWindow.AddChild(cachedContent);

        UiWindow plainWindow = new()
        {
            Id = "plain-window",
            Bounds = new UiRect(80, 0, 70, 60)
        };
        CountingElement plainContent = new()
        {
            Bounds = new UiRect(0, 24, 40, 20)
        };
        plainWindow.AddChild(plainContent);

        root.AddChild(cachedWindow);
        root.AddChild(plainWindow);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);
        cachedWindow.Bounds = new UiRect(0, 0, 72, 62);
        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(1, root.RenderCount);
        Assert.Equal(1, root.OverlayRenderCount);
        Assert.Equal(2, cachedContent.RenderCount);
        Assert.Equal(2, cachedContent.OverlayRenderCount);
        Assert.Equal(1, plainContent.RenderCount);
        Assert.Equal(1, plainContent.OverlayRenderCount);
        Assert.Equal(UiRenderCachePassAction.Replay, stats.RootPass.LastAction);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenTreeStructureChanges()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 160, 120)
        };
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 140, 100)
        };
        root.AddChild(tree);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);

        tree.RootItems.Add(new UiTreeViewItem("Scene")
        {
            IsOpen = true
        });
        tree.NotifyTreeStructureChanged();

        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(2, stats.RootPass.RecordCount);
        Assert.Equal(UiRenderCachePassAction.RecordAndReplay, stats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Invalidation, stats.RootPass.LastMissReason);
    }

    [Fact]
    public void RenderCaching_ReRecordsWhenMenuBarStateChanges()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 160, 120)
        };
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 160, 24)
        };
        UiMenuBar.MenuItem file = new() { Text = "File" };
        file.Items.Add(new UiMenuBar.MenuItem { Text = "Open", Enabled = true });
        menu.Items.Add(file);
        root.AddChild(menu);

        UiContext context = CreateContext(root);
        CountingRenderer renderer = new();

        context.Render(renderer);

        file.Items[0].Enabled = false;
        menu.NotifyMenuStateChanged();

        context.Render(renderer);

        UiRenderCacheStatisticsSnapshot stats = context.RenderCacheStatistics;
        Assert.Equal(2, stats.RootPass.RecordCount);
        Assert.Equal(UiRenderCachePassAction.RecordAndReplay, stats.RootPass.LastAction);
        Assert.Equal(UiRenderCacheMissReason.Invalidation, stats.RootPass.LastMissReason);
    }

    private static UiContext CreateContext(UiElement root)
    {
        UiContext context = new(root)
        {
            RenderCachingEnabled = true
        };

        context.Update(new UiInputState());
        return context;
    }
}
