using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Loader;
#if OPENCONTROLS_GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenControls.SilkNet;

public sealed unsafe class SilkNetUiRenderer : IUiRenderer, IDisposable
{
    private enum MetricKind
    {
        FillRect,
        DrawRect,
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
        public float Silhouette;
        public float SkyKey;

        public UiVertex(
            float x,
            float y,
            float u,
            float v,
            UiColor color,
            UiRect clip,
            bool silhouette = false,
            bool skyKey = false,
            float skyGlow = 0f)
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
            Silhouette = silhouette ? 1f : 0f;
            SkyKey = skyKey ? 1f + Math.Clamp(skyGlow, 0f, 1f) : 0f;
        }
    }

    private sealed class AtlasPageTexture
    {
        public uint TextureId { get; set; }
        public int Version { get; set; } = -1;
    }

    private const int MaxQuadsPerFlush = 1024;
    private static readonly ushort[] QuadIndices = BuildQuadIndices(MaxQuadsPerFlush);

    private readonly GL _gl;
    private readonly Stack<UiRect> _clipStack = new();
    private readonly UiGlyphAtlas _glyphAtlas = new();
    private readonly Dictionary<int, AtlasPageTexture> _atlasTextures = new();
    private readonly UiVertex[] _vertices = new UiVertex[MaxQuadsPerFlush * 4];
    private readonly uint _program;
    private uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _whiteTexture;
    private readonly int _viewportUniformLocation;
    private readonly int _textureUniformLocation;
    private readonly int _skyTextureUniformLocation;
    private readonly int _skyDestUniformLocation;
    private readonly MetricAccumulator[] _metricAccumulators = new MetricAccumulator[Enum.GetValues<MetricKind>().Length];
    private bool _disposed;
    private bool _metricsActive;
    private bool _renderStateBound;
    private long _metricsSequence;
    private uint _batchedTextureId;
    private uint _boundTextureId;
    private uint _sharedSkyTextureId;
    private UiRect _sharedSkyDest;
    private int _batchedQuadCount;
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
        _skyTextureUniformLocation = _gl.GetUniformLocation(_program, "uSkyTexture");
        _skyDestUniformLocation = _gl.GetUniformLocation(_program, "uSkyDest");

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

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        UiRenderHelpers.FillRectGradient(this, rect, topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        UiRenderHelpers.FillRectCheckerboard(this, rect, cellSize, colorA, colorB);
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

    public void DrawTexture(uint textureId, UiRect rect, bool flipVertical = false, UiColor? tint = null, bool silhouette = false)
    {
        DrawTexture(textureId, rect, 0f, 0f, 1f, 1f, flipVertical, tint, silhouette);
    }

    public void DrawTexture(
        uint textureId,
        UiRect rect,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        bool flipVertical = false,
        UiColor? tint = null,
        bool silhouette = false)
    {
        DrawTextureCore(
            textureId,
            rect,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
            flipVertical,
            tint,
            silhouette,
            skyKey: false);
    }

    /// <summary>Draw one sprite quad whose opaque magenta texels sample the
    /// shared screen-space sky texture. The extra flag rides vertex data, so
    /// keyed and ordinary quads retain the normal texture batching rules.</summary>
    public void DrawSkyKeyedTexture(
        uint textureId,
        UiRect rect,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        bool flipVertical = false,
        UiColor? tint = null,
        bool silhouette = false,
        float skyGlow = 0f)
    {
        DrawTextureCore(
            textureId,
            rect,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
            flipVertical,
            tint,
            silhouette,
            skyKey: true,
            skyGlow: skyGlow);
    }

    private void DrawTextureCore(
        uint textureId,
        UiRect rect,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        bool flipVertical,
        UiColor? tint,
        bool silhouette,
        bool skyKey,
        float skyGlow = 0f)
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
        QueueQuad(textureId, rect.X, rect.Y, rect.Width, rect.Height, uLeft, vTop, uRight, vBottom, drawColor, silhouette, skyKey, skyGlow);
        EndMetric(MetricKind.DrawTexture, startTimestamp);
    }

    /// <summary>Bind the half-resolution sky for subsequent keyed quads. The
    /// renderer does not own this texture; the post-effect that created it
    /// retains and deletes it.</summary>
    public void SetSharedSkyTexture(uint textureId, UiRect screenDest)
    {
        FlushPending(FlushReason.RenderPassEnd);
        ResetRenderState();
        _sharedSkyTextureId = textureId;
        _sharedSkyDest = screenDest;
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
        UiColor color,
        bool silhouette = false,
        bool skyKey = false,
        float skyGlow = 0f)
    {
        if (textureId == 0 || width <= 0 || height <= 0)
        {
            return;
        }

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

        AppendQuad(textureId, x, y, width, height, u1, v1, u2, v2, color, clip, silhouette, skyKey, skyGlow, ref _batchedQuadCount);
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
        bool silhouette,
        bool skyKey,
        float skyGlow,
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

        _vertices[baseIndex + 0] = new UiVertex(left, top, u1, v1, color, clip, silhouette, skyKey, skyGlow);
        _vertices[baseIndex + 1] = new UiVertex(right, top, u2, v1, color, clip, silhouette, skyKey, skyGlow);
        _vertices[baseIndex + 2] = new UiVertex(right, bottom, u2, v2, color, clip, silhouette, skyKey, skyGlow);
        _vertices[baseIndex + 3] = new UiVertex(left, bottom, u1, v2, color, clip, silhouette, skyKey, skyGlow);
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
            _gl.Uniform1(_skyTextureUniformLocation, 1);
            _gl.Uniform4(
                _skyDestUniformLocation,
                (float)_sharedSkyDest.X,
                (float)_sharedSkyDest.Y,
                (float)_sharedSkyDest.Width,
                (float)_sharedSkyDest.Height);
            _gl.ActiveTexture(TextureUnit.Texture1);
            _gl.BindTexture(TextureTarget.Texture2D, _sharedSkyTextureId != 0 ? _sharedSkyTextureId : _whiteTexture);
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
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float) * 14), null, BufferUsageARB.StreamDraw);
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

        const uint stride = sizeof(float) * 14;
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 2));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 4));
        _gl.EnableVertexAttribArray(3);
        _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 8));
        _gl.EnableVertexAttribArray(4);
        _gl.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 12));
        _gl.EnableVertexAttribArray(5);
        _gl.VertexAttribPointer(5, 1, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 13));
    }

    private void ResetRenderState()
    {
        if (!_renderStateBound)
        {
            return;
        }

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.ActiveTexture(TextureUnit.Texture0);
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
        // Same GLSL body compiles on both profiles; only the preamble differs.
        // ES requires explicit float precision in the fragment stage.
#if OPENCONTROLS_GLES
        const string vertexHeader = "#version 300 es\n";
        const string fragmentHeader = "#version 300 es\nprecision highp float;\n";
#else
        const string vertexHeader = "#version 330 core\n";
        const string fragmentHeader = "#version 330 core\n";
#endif
        const string vertexSource =
            """
            layout(location = 0) in vec2 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            layout(location = 2) in vec4 aColor;
            layout(location = 3) in vec4 aClipRect;
            layout(location = 4) in float aSilhouette;
            layout(location = 5) in float aSkyKey;

            uniform vec2 uViewportSize;

            out vec2 vTexCoord;
            out vec4 vColor;
            out vec4 vClipRect;
            flat out float vSilhouette;
            flat out float vSkyKey;

            void main()
            {
                vec2 normalized = aPosition / uViewportSize;
                vec2 clip = vec2(normalized.x * 2.0 - 1.0, 1.0 - normalized.y * 2.0);
                gl_Position = vec4(clip, 0.0, 1.0);
                vTexCoord = aTexCoord;
                vColor = aColor;
                vClipRect = aClipRect;
                vSilhouette = aSilhouette;
                vSkyKey = aSkyKey;
            }
            """;

        const string fragmentSource =
            """
            in vec2 vTexCoord;
            in vec4 vColor;
            in vec4 vClipRect;
            flat in float vSilhouette;
            flat in float vSkyKey;

            uniform sampler2D uTexture;
            uniform sampler2D uSkyTexture;
            uniform vec2 uViewportSize;
            uniform vec4 uSkyDest;

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

                vec4 sampled = texture(uTexture, vTexCoord);
                if (vSkyKey > 0.5 && uSkyDest.z > 0.0 && uSkyDest.w > 0.0)
                {
                    float keyDistance = distance(sampled.rgb, vec3(1.0, 0.0, 1.0));
                    // Key-aware half scaling has already snapped edge texels to
                    // either exact glass or key-free art. A hard outer-radius
                    // classification prevents ASTC drift from reintroducing a
                    // pink blend beside mullions.
                    float keyCoverage = 1.0 - step(0.250980, keyDistance);
                    vec2 skyUv = clamp((fragmentPosition - uSkyDest.xy) / uSkyDest.zw, 0.0, 1.0);
                    skyUv.y = 1.0 - skyUv.y;
                    vec3 sky = texture(uSkyTexture, skyUv).rgb;
                    float glow = clamp(vSkyKey - 1.0, 0.0, 1.0);
                    vec3 litSky = mix(sky, vec3(1.0, 0.58, 0.24), glow * 0.58);
                    sampled.rgb = mix(sampled.rgb, litSky, keyCoverage);
                }
                FragColor = vSilhouette > 0.5
                    ? vec4(vColor.rgb, sampled.a * vColor.a)
                    : sampled * vColor;
            }
            """;

        uint vertexShader = CompileShader(gl, ShaderType.VertexShader, vertexHeader + vertexSource);
        uint fragmentShader = CompileShader(gl, ShaderType.FragmentShader, fragmentHeader + fragmentSource);
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
