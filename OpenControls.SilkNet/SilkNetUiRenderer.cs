using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

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
    private static readonly ushort[] QuadIndices = BuildQuadIndices(MaxQuadsPerFlush);

    private readonly GL _gl;
    private readonly Stack<UiRect> _clipStack = new();
    private readonly UiGlyphAtlas _glyphAtlas = new();
    private readonly Dictionary<int, AtlasPageTexture> _atlasTextures = new();
    private readonly UiVertex[] _vertices = new UiVertex[MaxQuadsPerFlush * 4];
    private readonly uint _program;
    private readonly uint _vao;
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
    private int _viewportWidth = 1;
    private int _viewportHeight = 1;

    public SilkNetUiRenderer(GL gl, UiFont? defaultFont = null)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        DefaultFont = defaultFont ?? UiFont.Default;

        _program = CreateProgram(_gl);
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        _whiteTexture = CreateWhiteTexture(_gl);

        _viewportUniformLocation = _gl.GetUniformLocation(_program, "uViewportSize");
        _textureUniformLocation = _gl.GetUniformLocation(_program, "uTexture");

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float) * 12), null, BufferUsageARB.StreamDraw);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (ushort* indexPtr = QuadIndices)
        {
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(QuadIndices.Length * sizeof(ushort)),
                indexPtr,
                BufferUsageARB.StaticDraw);
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
        _gl.BindVertexArray(0);
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

    public void DrawTexture(uint textureId, UiRect rect, bool flipVertical = false, UiColor? tint = null)
    {
        long startTimestamp = BeginMetric();
        if (textureId == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            EndMetric(MetricKind.DrawTexture, startTimestamp);
            return;
        }

        UiColor drawColor = tint ?? UiColor.White;
        float vTop = flipVertical ? 1f : 0f;
        float vBottom = flipVertical ? 0f : 1f;
        QueueQuad(textureId, rect.X, rect.Y, rect.Width, rect.Height, 0f, vTop, 1f, vBottom, drawColor);
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
                _gl.DeleteTexture(texture.TextureId);
            }
        }

        _gl.DeleteTexture(_whiteTexture);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
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
        UiColor color)
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

        AppendQuad(textureId, x, y, width, height, u1, v1, u2, v2, color, clip, ref _batchedQuadCount);
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
            _gl.BindVertexArray(_vao);
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
