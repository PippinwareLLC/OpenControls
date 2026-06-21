using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Loader;
using Silk.NET.OpenGL;

namespace OpenControls.SilkNet;

public sealed unsafe class SilkNetUiRenderer : IUiRenderer, IUiVectorRenderer, IUiVectorPassRenderer, IUiTransformedVectorRenderer, IUiShapeRenderer, IDisposable
{
    private enum MetricKind
    {
        FillRect,
        DrawRect,
        FillRoundedRect,
        DrawRoundedRect,
        FillCircle,
        DrawCircle,
        FillTriangle,
        DrawPolyline,
        DrawText,
        DrawTexture,
        Flush,
        FlushTextureSwitch,
        FlushCapacity,
        FlushMetricsBoundary,
        FlushRenderPassEnd,
        FlushViewportChange,
        PushClip,
        PopClip,
        MeasureTextWidth,
        MeasureTextHeight
    }

    public enum FlushReason
    {
        Default,
        TextureSwitch,
        Capacity,
        MetricsBoundary,
        RenderPassEnd,
        ViewportChange
    }

    private struct MetricAccumulator
    {
        public int Calls;
        public long Ticks;

        public void Add(long elapsedTicks)
        {
            Calls++;
            Ticks += elapsedTicks;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UiVertex
    {
        public float X;
        public float Y;
        public float U;
        public float V;
        public float R;
        public float G;
        public float B;
        public float A;
        public float ClipLeft;
        public float ClipTop;
        public float ClipRight;
        public float ClipBottom;

        public UiVertex(float x, float y, float u, float v, UiColor color, UiRect clip)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
            R = color.R / 255f;
            G = color.G / 255f;
            B = color.B / 255f;
            A = color.A / 255f;
            ClipLeft = clip.X;
            ClipTop = clip.Y;
            ClipRight = clip.Right;
            ClipBottom = clip.Bottom;
        }
    }

    private sealed class AtlasPageTexture
    {
        public uint TextureId { get; set; }
        public int Version { get; set; } = -1;
    }

    private const int MaxQuadsPerFlush = 1024;
    private const int MaxTriangleVerticesPerFlush = 4096;
    private static readonly ushort[] QuadIndices = BuildQuadIndices(MaxQuadsPerFlush);

    private readonly GL _gl;
    private readonly Stack<UiRect> _clipStack = new();
    private readonly UiGlyphAtlas _glyphAtlas = new();
    private readonly Dictionary<int, AtlasPageTexture> _atlasTextures = new();
    private readonly UiVertex[] _vertices = new UiVertex[MaxQuadsPerFlush * 4];
    private readonly UiVertex[] _triangleVertices = new UiVertex[MaxTriangleVerticesPerFlush];
    private readonly uint _program;
    private uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _whiteTexture;
    private readonly int _viewportUniformLocation;
    private readonly int _textureUniformLocation;
    private readonly MetricAccumulator[] _metricAccumulators = new MetricAccumulator[Enum.GetValues<MetricKind>().Length];
    private bool _disposed;
    private bool _metricsActive;
    private bool _renderStateBound;
    private long _metricsSequence;
    private uint _batchedTextureId;
    private uint _boundTextureId;
    private int _batchedQuadCount;
    private int _batchedTriangleVertexCount;
    private int _vectorPassDepth;
    private int _viewportWidth = 1;
    private int _viewportHeight = 1;

    public SilkNetUiRenderer(GL gl, UiFont? defaultFont = null)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        DefaultFont = defaultFont ?? UiFont.Default;

        _program = CreateProgram(_gl);
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        _whiteTexture = CreateWhiteTexture(_gl);

        _viewportUniformLocation = _gl.GetUniformLocation(_program, "uViewportSize");
        _textureUniformLocation = _gl.GetUniformLocation(_program, "uTexture");

        CreateVertexArrayForCurrentContext(uploadBufferData: true);
    }

    public SilkNetUiRenderer(GL gl, TinyBitmapFont font)
        : this(gl, UiFont.FromTinyBitmap(font))
    {
    }

    public UiFont DefaultFont { get; set; }

    public TinyFontCodePage CodePage
    {
        get
        {
            return DefaultFont.TryGetBitmapFont(out TinyBitmapFont? font) && font != null
                ? font.CodePage
                : TinyFontCodePage.Latin1;
        }
        set
        {
            if (DefaultFont.TryGetBitmapFont(out TinyBitmapFont? font) && font != null)
            {
                font.CodePage = value;
            }
        }
    }

    public bool MetricsEnabled { get; set; }

    public UiRenderMetricsSnapshot LastMetricsSnapshot { get; private set; } = UiRenderMetricsSnapshot.Empty;

    public void SetViewportSize(int width, int height)
    {
        FlushPending(FlushReason.ViewportChange);
        ResetRenderState();
        _viewportWidth = Math.Max(1, width);
        _viewportHeight = Math.Max(1, height);
        _gl.Viewport(0, 0, (uint)_viewportWidth, (uint)_viewportHeight);
    }

    public void BeginMetricsFrame()
    {
        if (!MetricsEnabled)
        {
            _metricsActive = false;
            return;
        }

        Array.Clear(_metricAccumulators, 0, _metricAccumulators.Length);
        FlushPending(FlushReason.MetricsBoundary);
        _metricsActive = true;
    }

    public UiRenderMetricsSnapshot EndMetricsFrame()
    {
        FlushPending(FlushReason.MetricsBoundary);
        ResetRenderState();
        if (!_metricsActive)
        {
            LastMetricsSnapshot = UiRenderMetricsSnapshot.Empty;
            return LastMetricsSnapshot;
        }

        _metricsActive = false;
        List<UiRenderMetric> metrics = new(_metricAccumulators.Length);
        for (int i = 0; i < _metricAccumulators.Length; i++)
        {
            MetricAccumulator accumulator = _metricAccumulators[i];
            if (accumulator.Calls <= 0)
            {
                continue;
            }

            metrics.Add(new UiRenderMetric(
                GetMetricName((MetricKind)i),
                accumulator.Calls,
                accumulator.Ticks * 1000d / Stopwatch.Frequency));
        }

        metrics.Sort((left, right) => right.DurationMs.CompareTo(left.DurationMs));
        LastMetricsSnapshot = new UiRenderMetricsSnapshot(++_metricsSequence, metrics);
        return LastMetricsSnapshot;
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            EndMetric(MetricKind.FillRect, startTimestamp);
            return;
        }

        QueueQuad(_whiteTexture, rect.X, rect.Y, rect.Width, rect.Height, 0f, 0f, 1f, 1f, color);
        EndMetric(MetricKind.FillRect, startTimestamp);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0)
        {
            EndMetric(MetricKind.DrawRect, startTimestamp);
            return;
        }

        int t = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        QueueQuad(_whiteTexture, rect.X, rect.Y, rect.Width, t, 0f, 0f, 1f, 1f, color);
        QueueQuad(_whiteTexture, rect.X, rect.Bottom - t, rect.Width, t, 0f, 0f, 1f, 1f, color);

        int middleHeight = rect.Height - t * 2;
        if (middleHeight > 0)
        {
            QueueQuad(_whiteTexture, rect.X, rect.Y + t, t, middleHeight, 0f, 0f, 1f, 1f, color);
            QueueQuad(_whiteTexture, rect.Right - t, rect.Y + t, t, middleHeight, 0f, 0f, 1f, 1f, color);
        }

        EndMetric(MetricKind.DrawRect, startTimestamp);
    }

    public void FillRoundedRect(UiRect rect, int radius, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.FillRoundedRect, startTimestamp);
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            FillRect(rect, color);
            EndMetric(MetricKind.FillRoundedRect, startTimestamp);
            return;
        }

        QueueFilledRoundedRect(rect, clampedRadius, topOnly: false, color);
        EndMetric(MetricKind.FillRoundedRect, startTimestamp);
    }

    public void FillTopRoundedRect(UiRect rect, int radius, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.FillRoundedRect, startTimestamp);
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            FillRect(rect, color);
            EndMetric(MetricKind.FillRoundedRect, startTimestamp);
            return;
        }

        QueueFilledRoundedRect(rect, clampedRadius, topOnly: true, color);
        EndMetric(MetricKind.FillRoundedRect, startTimestamp);
    }

    public void DrawRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            DrawRect(rect, color, thickness);
            EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
            return;
        }

        DrawPolyline(BuildRoundedRectOutline(rect, clampedRadius, topOnly: false), Math.Max(1, thickness), color);
        EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
    }

    public void DrawTopRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
            return;
        }

        int clampedRadius = ClampRadius(rect, radius);
        if (clampedRadius <= 0)
        {
            DrawRect(rect, color, thickness);
            EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
            return;
        }

        DrawPolyline(BuildRoundedRectOutline(rect, clampedRadius, topOnly: true), Math.Max(1, thickness), color);
        EndMetric(MetricKind.DrawRoundedRect, startTimestamp);
    }

    public void FillCircle(UiPoint center, int radius, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (radius <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.FillCircle, startTimestamp);
            return;
        }

        QueueFilledCircle(center.X, center.Y, radius, color);
        EndMetric(MetricKind.FillCircle, startTimestamp);
    }

    public void DrawCircle(UiPoint center, int radius, UiColor color, int thickness = 1)
    {
        long startTimestamp = BeginMetric();
        if (radius <= 0 || thickness <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.DrawCircle, startTimestamp);
            return;
        }

        DrawPolyline(BuildCircleOutline(center, radius), Math.Max(1, thickness), color);
        EndMetric(MetricKind.DrawCircle, startTimestamp);
    }

    public void FillTriangleRight(UiRect rect, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (rect.Width <= 0 || rect.Height <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.FillTriangle, startTimestamp);
            return;
        }

        UiRect clip = GetActiveClipBounds();
        if (!Intersects(clip, rect))
        {
            EndMetric(MetricKind.FillTriangle, startTimestamp);
            return;
        }

        FlushPendingQuads(FlushReason.Default);
        float left = rect.X;
        float top = rect.Y;
        float bottom = rect.Bottom;
        float right = rect.Right;
        float centerY = rect.Y + rect.Height * 0.5f;
        QueueTriangle(left, top, right, centerY, left, bottom, color, clip);
        EndMetric(MetricKind.FillTriangle, startTimestamp);
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        UiRenderHelpers.FillRectGradient(this, rect, topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        UiRenderHelpers.FillRectCheckerboard(this, rect, cellSize, colorA, colorB);
    }

    public void DrawPolyline(IReadOnlyList<UiPoint> points, int thickness, UiColor color)
    {
        long startTimestamp = BeginMetric();
        if (points == null || points.Count < 2 || thickness <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.DrawPolyline, startTimestamp);
            return;
        }

        if (_vectorPassDepth == 0)
        {
            FlushPendingQuads(FlushReason.Default);
        }

        UiRect clip = GetActiveClipBounds();
        float halfThickness = Math.Max(1, thickness) * 0.5f;

        for (int i = 1; i < points.Count; i++)
        {
            QueueLineSegment(points[i - 1], points[i], halfThickness, color, clip);
        }

        EndMetric(MetricKind.DrawPolyline, startTimestamp);
    }

    public void DrawPolylineTransformed(
        IReadOnlyList<UiPoint> points,
        int thickness,
        UiColor color,
        UiPoint origin,
        float zoom,
        float panX,
        float panY)
    {
        long startTimestamp = BeginMetric();
        if (points == null || points.Count < 2 || thickness <= 0 || color.A == 0)
        {
            EndMetric(MetricKind.DrawPolyline, startTimestamp);
            return;
        }

        if (_vectorPassDepth == 0)
        {
            FlushPendingQuads(FlushReason.Default);
        }

        UiRect clip = GetActiveClipBounds();
        float halfThickness = Math.Max(1, thickness) * 0.5f;
        (float X, float Y) previous = TransformPolylinePoint(points[0], origin, zoom, panX, panY);
        for (int i = 1; i < points.Count; i++)
        {
            (float X, float Y) current = TransformPolylinePoint(points[i], origin, zoom, panX, panY);
            QueueLineSegment(previous.X, previous.Y, current.X, current.Y, halfThickness, color, clip);
            previous = current;
        }

        EndMetric(MetricKind.DrawPolyline, startTimestamp);
    }

    public void BeginVectorPass()
    {
        if (_vectorPassDepth == 0)
        {
            FlushPendingQuads(FlushReason.Default);
        }

        _vectorPassDepth++;
    }

    public void EndVectorPass()
    {
        if (_vectorPassDepth <= 0)
        {
            return;
        }

        _vectorPassDepth--;
        if (_vectorPassDepth == 0)
        {
            FlushPendingTriangles(FlushReason.Default);
        }
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        DrawText(text, position, color, scale, null);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        long startTimestamp = BeginMetric();
        if (string.IsNullOrEmpty(text))
        {
            EndMetric(MetricKind.DrawText, startTimestamp);
            return;
        }

        UiFont activeFont = font ?? DefaultFont;
        UiTextLayout layout = activeFont.LayoutText(text, scale);
        if (layout.Glyphs.Count == 0)
        {
            EndMetric(MetricKind.DrawText, startTimestamp);
            return;
        }

        for (int i = 0; i < layout.Glyphs.Count; i++)
        {
            UiPositionedGlyph glyph = layout.Glyphs[i];
            UiGlyphAtlasEntry entry = _glyphAtlas.GetOrAdd(glyph.Glyph);
            if (!entry.IsValid)
            {
                continue;
            }

            uint textureId = EnsureAtlasTexture(entry.PageIndex).TextureId;
            UiGlyphAtlasPage page = _glyphAtlas.GetPage(entry.PageIndex);
            float u1 = entry.SourceRect.X / (float)page.Width;
            float v1 = entry.SourceRect.Y / (float)page.Height;
            float u2 = entry.SourceRect.Right / (float)page.Width;
            float v2 = entry.SourceRect.Bottom / (float)page.Height;

            QueueQuad(
                textureId,
                position.X + glyph.X,
                position.Y + glyph.Y,
                glyph.Glyph.Width,
                glyph.Glyph.Height,
                u1,
                v1,
                u2,
                v2,
                color);
        }

        EndMetric(MetricKind.DrawText, startTimestamp);
    }

    public void DrawTexture(uint textureId, UiRect rect, bool flipVertical = false, UiColor? tint = null)
    {
        DrawTexture(textureId, rect, 0f, 0f, 1f, 1f, flipVertical, tint);
    }

    public void DrawTexture(
        uint textureId,
        UiRect rect,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        bool flipVertical = false,
        UiColor? tint = null)
    {
        long startTimestamp = BeginMetric();
        if (textureId == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            EndMetric(MetricKind.DrawTexture, startTimestamp);
            return;
        }

        UiColor drawColor = tint ?? UiColor.White;
        float clampedSourceX = Math.Clamp(sourceX, 0f, 1f);
        float clampedSourceY = Math.Clamp(sourceY, 0f, 1f);
        float clampedSourceWidth = Math.Clamp(sourceWidth, 0f, 1f - clampedSourceX);
        float clampedSourceHeight = Math.Clamp(sourceHeight, 0f, 1f - clampedSourceY);
        if (clampedSourceWidth <= 0f || clampedSourceHeight <= 0f)
        {
            EndMetric(MetricKind.DrawTexture, startTimestamp);
            return;
        }

        float uLeft = clampedSourceX;
        float uRight = clampedSourceX + clampedSourceWidth;
        float vTop = flipVertical
            ? clampedSourceY + clampedSourceHeight
            : clampedSourceY;
        float vBottom = flipVertical
            ? clampedSourceY
            : clampedSourceY + clampedSourceHeight;
        QueueQuad(textureId, rect.X, rect.Y, rect.Width, rect.Height, uLeft, vTop, uRight, vBottom, drawColor);
        EndMetric(MetricKind.DrawTexture, startTimestamp);
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return MeasureTextWidth(text, scale, null);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        long startTimestamp = BeginMetric();
        int width = (font ?? DefaultFont).MeasureTextWidth(text, scale);
        EndMetric(MetricKind.MeasureTextWidth, startTimestamp);
        return width;
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return MeasureTextHeight(scale, null);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        long startTimestamp = BeginMetric();
        int height = (font ?? DefaultFont).MeasureTextHeight(scale);
        EndMetric(MetricKind.MeasureTextHeight, startTimestamp);
        return height;
    }

    public void PushClip(UiRect rect)
    {
        long startTimestamp = BeginMetric();
        UiRect clip = rect;
        UiRect viewport = GetViewportRect();
        clip = Intersect(clip, viewport);

        if (_clipStack.Count > 0)
        {
            clip = Intersect(clip, _clipStack.Peek());
        }

        _clipStack.Push(clip);

        EndMetric(MetricKind.PushClip, startTimestamp);
    }

    public void PopClip()
    {
        long startTimestamp = BeginMetric();
        if (_clipStack.Count == 0)
        {
            EndMetric(MetricKind.PopClip, startTimestamp);
            return;
        }

        UiRect previousClip = _clipStack.Pop();
        if (_clipStack.Count == 0)
        {
            EndMetric(MetricKind.PopClip, startTimestamp);
            return;
        }

        EndMetric(MetricKind.PopClip, startTimestamp);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (AtlasPageTexture texture in _atlasTextures.Values)
        {
            if (texture.TextureId != 0)
            {
                SafeDelete(() => _gl.DeleteTexture(texture.TextureId));
            }
        }

        SafeDelete(() => _gl.DeleteTexture(_whiteTexture));
        SafeDelete(() => _gl.DeleteBuffer(_ebo));
        SafeDelete(() => _gl.DeleteBuffer(_vbo));
        SafeDelete(() => _gl.DeleteVertexArray(_vao));
        SafeDelete(() => _gl.DeleteProgram(_program));
    }

    private static void SafeDelete(Action action)
    {
        try
        {
            action();
        }
        catch (SymbolLoadingException)
        {
            // The owning GL context may already be unavailable during process shutdown.
        }
        catch (InvalidOperationException)
        {
            // Ignore teardown-time GL state failures when the context is no longer valid.
        }
    }

    public void FlushPending()
    {
        FlushPending(FlushReason.Default);
    }

    public void FlushPending(FlushReason reason)
    {
        FlushPendingQuads(reason);
        FlushPendingTriangles(reason);
    }

    private void FlushPendingQuads(FlushReason reason)
    {
        if (_batchedTextureId == 0 || _batchedQuadCount <= 0)
        {
            _batchedTextureId = 0;
            _batchedQuadCount = 0;
            return;
        }

        Flush(_batchedTextureId, _batchedQuadCount, reason);
        _batchedTextureId = 0;
        _batchedQuadCount = 0;
    }

    private void FlushPendingTriangles(FlushReason reason)
    {
        if (_batchedTriangleVertexCount <= 0)
        {
            return;
        }

        FlushTriangles(_batchedTriangleVertexCount, reason);
        _batchedTriangleVertexCount = 0;
    }

    public void CompleteRenderPass()
    {
        FlushPending(FlushReason.RenderPassEnd);
        ResetRenderState();
    }

    private void QueueQuad(
        uint textureId,
        int x,
        int y,
        int width,
        int height,
        float u1,
        float v1,
        float u2,
        float v2,
        UiColor color)
    {
        if (textureId == 0 || width <= 0 || height <= 0)
        {
            return;
        }

        FlushPendingTriangles(FlushReason.Default);

        UiRect clip = GetActiveClipBounds();
        UiRect quadBounds = new(x, y, width, height);
        if (!Intersects(clip, quadBounds))
        {
            return;
        }

        if (_batchedQuadCount > 0 && _batchedTextureId != textureId)
        {
            FlushPending(FlushReason.TextureSwitch);
        }

        if (_batchedQuadCount == 0)
        {
            _batchedTextureId = textureId;
        }

        if (_batchedQuadCount >= MaxQuadsPerFlush)
        {
            FlushPending(FlushReason.Capacity);
            _batchedTextureId = textureId;
        }

        AppendQuad(textureId, x, y, width, height, u1, v1, u2, v2, color, clip, ref _batchedQuadCount);
    }

    private void QueueFilledRoundedRect(UiRect rect, int radius, bool topOnly, UiColor color)
    {
        UiRect clip = GetActiveClipBounds();
        if (!Intersects(clip, rect))
        {
            return;
        }

        FlushPendingQuads(FlushReason.Default);
        if (topOnly)
        {
            QueueTriangleRect(rect.X, rect.Y + radius, rect.Width, rect.Height - radius, color, clip);
            QueueTriangleRect(rect.X + radius, rect.Y, rect.Width - radius * 2, radius, color, clip);
            QueueQuarterCircleFan(rect.X + radius, rect.Y + radius, radius, MathF.PI, MathF.PI * 1.5f, color, clip);
            QueueQuarterCircleFan(rect.Right - radius, rect.Y + radius, radius, MathF.PI * 1.5f, MathF.PI * 2f, color, clip);
            return;
        }

        QueueTriangleRect(rect.X + radius, rect.Y, rect.Width - radius * 2, rect.Height, color, clip);
        QueueTriangleRect(rect.X, rect.Y + radius, radius, rect.Height - radius * 2, color, clip);
        QueueTriangleRect(rect.Right - radius, rect.Y + radius, radius, rect.Height - radius * 2, color, clip);

        QueueQuarterCircleFan(rect.X + radius, rect.Y + radius, radius, MathF.PI, MathF.PI * 1.5f, color, clip);
        QueueQuarterCircleFan(rect.Right - radius, rect.Y + radius, radius, MathF.PI * 1.5f, MathF.PI * 2f, color, clip);
        QueueQuarterCircleFan(rect.Right - radius, rect.Bottom - radius, radius, 0f, MathF.PI * 0.5f, color, clip);
        QueueQuarterCircleFan(rect.X + radius, rect.Bottom - radius, radius, MathF.PI * 0.5f, MathF.PI, color, clip);
    }

    private static (float X, float Y) TransformPolylinePoint(UiPoint point, UiPoint origin, float zoom, float panX, float panY)
    {
        float x = origin.X + (point.X - panX) * zoom;
        float y = origin.Y + (point.Y - panY) * zoom;
        return (x, y);
    }

    private void QueueTriangleRect(float x, float y, float width, float height, UiColor color, UiRect clip)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        float right = x + width;
        float bottom = y + height;
        QueueTriangle(x, y, right, y, right, bottom, color, clip);
        QueueTriangle(x, y, right, bottom, x, bottom, color, clip);
    }

    private void QueueFilledCircle(float centerX, float centerY, float radius, UiColor color)
    {
        UiRect clip = GetActiveClipBounds();
        var bounds = new UiRect(
            (int)MathF.Floor(centerX - radius),
            (int)MathF.Floor(centerY - radius),
            Math.Max(1, (int)MathF.Ceiling(radius * 2f)),
            Math.Max(1, (int)MathF.Ceiling(radius * 2f)));
        if (!Intersects(clip, bounds))
        {
            return;
        }

        FlushPendingQuads(FlushReason.Default);
        int segments = GetCircleSegments(radius);
        float previousX = centerX + radius;
        float previousY = centerY;
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathF.PI * 2f * i / segments;
            float nextX = centerX + MathF.Cos(angle) * radius;
            float nextY = centerY + MathF.Sin(angle) * radius;
            QueueTriangle(centerX, centerY, previousX, previousY, nextX, nextY, color, clip);
            previousX = nextX;
            previousY = nextY;
        }
    }

    private void QueueQuarterCircleFan(float centerX, float centerY, float radius, float startAngle, float endAngle, UiColor color, UiRect clip)
    {
        int segments = GetArcSegments(radius);
        float previousX = centerX + MathF.Cos(startAngle) * radius;
        float previousY = centerY + MathF.Sin(startAngle) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float nextX = centerX + MathF.Cos(angle) * radius;
            float nextY = centerY + MathF.Sin(angle) * radius;
            QueueTriangle(centerX, centerY, previousX, previousY, nextX, nextY, color, clip);
            previousX = nextX;
            previousY = nextY;
        }
    }

    private static IReadOnlyList<UiPoint> BuildRoundedRectOutline(UiRect rect, int radius, bool topOnly)
    {
        List<UiPoint> points = new(GetArcSegments(radius) * (topOnly ? 2 : 4) + 6);
        if (topOnly)
        {
            points.Add(new UiPoint(rect.X, rect.Bottom - 1));
            points.Add(new UiPoint(rect.X, rect.Y + radius));
            AppendArc(points, rect.X + radius, rect.Y + radius, radius, MathF.PI, MathF.PI * 1.5f);
            AppendArc(points, rect.Right - radius - 1, rect.Y + radius, radius, MathF.PI * 1.5f, MathF.PI * 2f);
            points.Add(new UiPoint(rect.Right - 1, rect.Bottom - 1));
            points.Add(points[0]);
            return points;
        }

        AppendArc(points, rect.X + radius, rect.Y + radius, radius, MathF.PI, MathF.PI * 1.5f);
        AppendArc(points, rect.Right - radius - 1, rect.Y + radius, radius, MathF.PI * 1.5f, MathF.PI * 2f);
        AppendArc(points, rect.Right - radius - 1, rect.Bottom - radius - 1, radius, 0f, MathF.PI * 0.5f);
        AppendArc(points, rect.X + radius, rect.Bottom - radius - 1, radius, MathF.PI * 0.5f, MathF.PI);
        points.Add(points[0]);
        return points;
    }

    private static IReadOnlyList<UiPoint> BuildCircleOutline(UiPoint center, int radius)
    {
        int segments = GetCircleSegments(radius);
        UiPoint[] points = new UiPoint[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = MathF.PI * 2f * i / segments;
            points[i] = new UiPoint(
                (int)MathF.Round(center.X + MathF.Cos(angle) * radius),
                (int)MathF.Round(center.Y + MathF.Sin(angle) * radius));
        }

        return points;
    }

    private static void AppendArc(List<UiPoint> points, float centerX, float centerY, float radius, float startAngle, float endAngle)
    {
        int segments = GetArcSegments(radius);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            UiPoint point = new(
                (int)MathF.Round(centerX + MathF.Cos(angle) * radius),
                (int)MathF.Round(centerY + MathF.Sin(angle) * radius));
            if (points.Count == 0 || !points[^1].Equals(point))
            {
                points.Add(point);
            }
        }
    }

    private static int GetArcSegments(float radius)
    {
        return Math.Clamp((int)MathF.Ceiling(radius / 2.5f), 4, 12);
    }

    private static int GetCircleSegments(float radius)
    {
        return Math.Clamp((int)MathF.Ceiling(radius * 1.5f), 12, 40);
    }

    private static int ClampRadius(UiRect rect, int radius)
    {
        if (radius <= 0)
        {
            return 0;
        }

        int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
        if (maxRadius <= 0)
        {
            return 0;
        }

        return Math.Clamp(radius, 0, maxRadius);
    }

    private void QueueLineSegment(UiPoint a, UiPoint b, float halfThickness, UiColor color, UiRect clip)
    {
        QueueLineSegment(a.X, a.Y, b.X, b.Y, halfThickness, color, clip);
    }

    private void QueueLineSegment(float x1, float y1, float x2, float y2, float halfThickness, UiColor color, UiRect clip)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001f)
        {
            return;
        }

        float nx = -dy / length * halfThickness;
        float ny = dx / length * halfThickness;
        float minX = MathF.Min(x1, x2) - halfThickness - 1;
        float minY = MathF.Min(y1, y2) - halfThickness - 1;
        float maxX = MathF.Max(x1, x2) + halfThickness + 1;
        float maxY = MathF.Max(y1, y2) + halfThickness + 1;
        var bounds = new UiRect(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            Math.Max(1, (int)MathF.Ceiling(maxX - minX)),
            Math.Max(1, (int)MathF.Ceiling(maxY - minY)));
        if (!Intersects(clip, bounds))
        {
            return;
        }

        QueueTriangle(x1 + nx, y1 + ny, x2 + nx, y2 + ny, x2 - nx, y2 - ny, color, clip);
        QueueTriangle(x1 + nx, y1 + ny, x2 - nx, y2 - ny, x1 - nx, y1 - ny, color, clip);
    }

    private void QueueTriangle(float x1, float y1, float x2, float y2, float x3, float y3, UiColor color, UiRect clip)
    {
        if (_batchedTriangleVertexCount + 3 > _triangleVertices.Length)
        {
            FlushPendingTriangles(FlushReason.Capacity);
        }

        int baseIndex = _batchedTriangleVertexCount;
        _triangleVertices[baseIndex + 0] = new UiVertex(x1, y1, 0f, 0f, color, clip);
        _triangleVertices[baseIndex + 1] = new UiVertex(x2, y2, 0f, 0f, color, clip);
        _triangleVertices[baseIndex + 2] = new UiVertex(x3, y3, 0f, 0f, color, clip);
        _batchedTriangleVertexCount += 3;
    }

    private void AppendQuad(
        uint expectedTexture,
        int x,
        int y,
        int width,
        int height,
        float u1,
        float v1,
        float u2,
        float v2,
        UiColor color,
        UiRect clip,
        ref int quadCount)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        int baseIndex = quadCount * 4;
        float left = x;
        float top = y;
        float right = x + width;
        float bottom = y + height;

        _vertices[baseIndex + 0] = new UiVertex(left, top, u1, v1, color, clip);
        _vertices[baseIndex + 1] = new UiVertex(right, top, u2, v1, color, clip);
        _vertices[baseIndex + 2] = new UiVertex(right, bottom, u2, v2, color, clip);
        _vertices[baseIndex + 3] = new UiVertex(left, bottom, u1, v2, color, clip);
        quadCount++;
    }

    private long BeginMetric()
    {
        return _metricsActive ? Stopwatch.GetTimestamp() : 0L;
    }

    private void EndMetric(MetricKind kind, long startTimestamp)
    {
        if (!_metricsActive || startTimestamp == 0L)
        {
            return;
        }

        _metricAccumulators[(int)kind].Add(Stopwatch.GetTimestamp() - startTimestamp);
    }

    private static string GetMetricName(MetricKind kind)
    {
        return kind switch
        {
            MetricKind.FillRect => "FillRect",
            MetricKind.DrawRect => "DrawRect",
            MetricKind.FillRoundedRect => "FillRoundedRect",
            MetricKind.DrawRoundedRect => "DrawRoundedRect",
            MetricKind.FillCircle => "FillCircle",
            MetricKind.DrawCircle => "DrawCircle",
            MetricKind.FillTriangle => "FillTriangle",
            MetricKind.DrawPolyline => "DrawPolyline",
            MetricKind.DrawText => "DrawText",
            MetricKind.DrawTexture => "DrawTexture",
            MetricKind.Flush => "Flush",
            MetricKind.FlushTextureSwitch => "Flush.TextureSwitch",
            MetricKind.FlushCapacity => "Flush.Capacity",
            MetricKind.FlushMetricsBoundary => "Flush.MetricsBoundary",
            MetricKind.FlushRenderPassEnd => "Flush.RenderPassEnd",
            MetricKind.FlushViewportChange => "Flush.ViewportChange",
            MetricKind.PushClip => "PushClip",
            MetricKind.PopClip => "PopClip",
            MetricKind.MeasureTextWidth => "MeasureTextWidth",
            MetricKind.MeasureTextHeight => "MeasureTextHeight",
            _ => kind.ToString()
        };
    }

    private static MetricKind GetFlushMetricKind(FlushReason reason)
    {
        return reason switch
        {
            FlushReason.TextureSwitch => MetricKind.FlushTextureSwitch,
            FlushReason.Capacity => MetricKind.FlushCapacity,
            FlushReason.MetricsBoundary => MetricKind.FlushMetricsBoundary,
            FlushReason.RenderPassEnd => MetricKind.FlushRenderPassEnd,
            FlushReason.ViewportChange => MetricKind.FlushViewportChange,
            _ => MetricKind.Flush
        };
    }

    private void Flush(uint textureId, int quadCount, FlushReason reason)
    {
        long startTimestamp = BeginMetric();
        if (textureId == 0 || quadCount <= 0)
        {
            EndMetric(MetricKind.Flush, startTimestamp);
            return;
        }

        EnsureRenderState(textureId);

        fixed (UiVertex* vertexPtr = _vertices)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(quadCount * sizeof(UiVertex) * 4),
                vertexPtr,
                BufferUsageARB.StreamDraw);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)(quadCount * 6), DrawElementsType.UnsignedShort, null);
        EndMetric(MetricKind.Flush, startTimestamp);
        EndMetric(GetFlushMetricKind(reason), startTimestamp);
    }

    private void FlushTriangles(int vertexCount, FlushReason reason)
    {
        long startTimestamp = BeginMetric();
        if (vertexCount <= 0)
        {
            EndMetric(MetricKind.Flush, startTimestamp);
            return;
        }

        EnsureRenderState(_whiteTexture);

        fixed (UiVertex* vertexPtr = _triangleVertices)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertexCount * sizeof(UiVertex)),
                vertexPtr,
                BufferUsageARB.StreamDraw);
        }

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)vertexCount);
        EndMetric(MetricKind.Flush, startTimestamp);
        EndMetric(GetFlushMetricKind(reason), startTimestamp);
    }

    private AtlasPageTexture EnsureAtlasTexture(int pageIndex)
    {
        UiGlyphAtlasPage page = _glyphAtlas.GetPage(pageIndex);
        if (!_atlasTextures.TryGetValue(pageIndex, out AtlasPageTexture? texture))
        {
            texture = new AtlasPageTexture
            {
                TextureId = _gl.GenTexture()
            };
            _atlasTextures[pageIndex] = texture;
        }

        if (texture.Version != page.Version)
        {
            _gl.BindTexture(TextureTarget.Texture2D, texture.TextureId);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            fixed (byte* pixels = page.Pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)page.Width,
                    (uint)page.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels);
            }

            texture.Version = page.Version;
            _boundTextureId = 0;
        }

        return texture;
    }

    private void EnsureRenderState(uint textureId)
    {
        if (!_renderStateBound)
        {
            _gl.UseProgram(_program);
            EnsureVertexArrayForCurrentContext();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.Disable(EnableCap.DepthTest);
            _gl.Disable(EnableCap.CullFace);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.Uniform2(_viewportUniformLocation, (float)_viewportWidth, (float)_viewportHeight);
            _gl.Uniform1(_textureUniformLocation, 0);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _renderStateBound = true;
            _boundTextureId = 0;
        }

        if (_boundTextureId != textureId)
        {
            _gl.BindTexture(TextureTarget.Texture2D, textureId);
            _boundTextureId = textureId;
        }
    }

    private void EnsureVertexArrayForCurrentContext()
    {
        ClearGlErrors();
        if (_vao != 0)
        {
            _gl.BindVertexArray(_vao);
            if (_gl.GetError() == GLEnum.NoError && GetInteger(GetPName.VertexArrayBinding) == _vao)
            {
                return;
            }
        }

        CreateVertexArrayForCurrentContext(uploadBufferData: false);
    }

    private void CreateVertexArrayForCurrentContext(bool uploadBufferData)
    {
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        if (uploadBufferData)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float) * 12), null, BufferUsageARB.StreamDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        if (uploadBufferData)
        {
            fixed (ushort* indexPtr = QuadIndices)
            {
                _gl.BufferData(
                    BufferTargetARB.ElementArrayBuffer,
                    (nuint)(QuadIndices.Length * sizeof(ushort)),
                    indexPtr,
                    BufferUsageARB.StaticDraw);
            }
        }

        const uint stride = sizeof(float) * 12;
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 2));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 4));
        _gl.EnableVertexAttribArray(3);
        _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 8));
    }

    private void ResetRenderState()
    {
        if (!_renderStateBound)
        {
            return;
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
        _renderStateBound = false;
        _boundTextureId = 0;
    }

    private UiRect GetViewportRect()
    {
        return new UiRect(0, 0, _viewportWidth, _viewportHeight);
    }

    private UiRect GetActiveClipBounds()
    {
        return _clipStack.Count > 0 ? _clipStack.Peek() : GetViewportRect();
    }

    private static UiRect Intersect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.X, b.X);
        int top = Math.Max(a.Y, b.Y);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        if (right <= left || bottom <= top)
        {
            return new UiRect(left, top, 0, 0);
        }

        return new UiRect(left, top, right - left, bottom - top);
    }

    private static bool Intersects(UiRect a, UiRect b)
    {
        return a.X < b.Right
            && a.Right > b.X
            && a.Y < b.Bottom
            && a.Bottom > b.Y;
    }

    private int GetInteger(GetPName parameterName)
    {
        int[] value = new int[1];
        _gl.GetInteger(parameterName, value);
        return value[0];
    }

    private void ClearGlErrors()
    {
        while (_gl.GetError() != GLEnum.NoError)
        {
        }
    }

    private static uint CreateWhiteTexture(GL gl)
    {
        uint texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        byte[] pixel = [255, 255, 255, 255];
        fixed (byte* pixelPtr = pixel)
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixelPtr);
        }

        gl.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private static ushort[] BuildQuadIndices(int maxQuads)
    {
        ushort[] indices = new ushort[maxQuads * 6];
        for (ushort i = 0; i < maxQuads; i++)
        {
            ushort baseVertex = (ushort)(i * 4);
            int baseIndex = i * 6;
            indices[baseIndex + 0] = baseVertex;
            indices[baseIndex + 1] = (ushort)(baseVertex + 1);
            indices[baseIndex + 2] = (ushort)(baseVertex + 2);
            indices[baseIndex + 3] = baseVertex;
            indices[baseIndex + 4] = (ushort)(baseVertex + 2);
            indices[baseIndex + 5] = (ushort)(baseVertex + 3);
        }

        return indices;
    }

    private static uint CreateProgram(GL gl)
    {
        const string vertexSource =
            """
            #version 330 core

            layout(location = 0) in vec2 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            layout(location = 2) in vec4 aColor;
            layout(location = 3) in vec4 aClipRect;

            uniform vec2 uViewportSize;

            out vec2 vTexCoord;
            out vec4 vColor;
            out vec4 vClipRect;

            void main()
            {
                vec2 normalized = aPosition / uViewportSize;
                vec2 clip = vec2(normalized.x * 2.0 - 1.0, 1.0 - normalized.y * 2.0);
                gl_Position = vec4(clip, 0.0, 1.0);
                vTexCoord = aTexCoord;
                vColor = aColor;
                vClipRect = aClipRect;
            }
            """;

        const string fragmentSource =
            """
            #version 330 core

            in vec2 vTexCoord;
            in vec4 vColor;
            in vec4 vClipRect;

            uniform sampler2D uTexture;
            uniform vec2 uViewportSize;

            out vec4 FragColor;

            void main()
            {
                vec2 fragmentPosition = vec2(gl_FragCoord.x, uViewportSize.y - gl_FragCoord.y);
                if (fragmentPosition.x < vClipRect.x
                    || fragmentPosition.y < vClipRect.y
                    || fragmentPosition.x >= vClipRect.z
                    || fragmentPosition.y >= vClipRect.w)
                {
                    discard;
                }

                FragColor = texture(uTexture, vTexCoord) * vColor;
            }
            """;

        uint vertexShader = CompileShader(gl, ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);
        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);
        gl.GetProgram(program, GLEnum.LinkStatus, out int linked);
        if (linked == 0)
        {
            string info = gl.GetProgramInfoLog(program);
            gl.DeleteProgram(program);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Failed to link the OpenControls Silk shader program: {info}");
        }

        gl.DetachShader(program, vertexShader);
        gl.DetachShader(program, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        return program;
    }

    private static uint CompileShader(GL gl, ShaderType shaderType, string source)
    {
        uint shader = gl.CreateShader(shaderType);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string info = gl.GetShaderInfoLog(shader);
            gl.DeleteShader(shader);
            throw new InvalidOperationException($"Failed to compile the {shaderType} for OpenControls Silk rendering: {info}");
        }

        return shader;
    }
}
