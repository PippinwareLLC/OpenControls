using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace OpenControls.SilkNet;

public sealed unsafe class SilkNetUiRenderer : IUiRenderer, IDisposable
{
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

        public UiVertex(float x, float y, float u, float v, UiColor color)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
            R = color.R / 255f;
            G = color.G / 255f;
            B = color.B / 255f;
            A = color.A / 255f;
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
    private bool _scissorEnabled;
    private bool _disposed;
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
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float) * 8), null, BufferUsageARB.StreamDraw);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (ushort* indexPtr = QuadIndices)
        {
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(QuadIndices.Length * sizeof(ushort)),
                indexPtr,
                BufferUsageARB.StaticDraw);
        }

        const uint stride = sizeof(float) * 8;
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 2));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(float) * 4));
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

    public void SetViewportSize(int width, int height)
    {
        _viewportWidth = Math.Max(1, width);
        _viewportHeight = Math.Max(1, height);
        _gl.Viewport(0, 0, (uint)_viewportWidth, (uint)_viewportHeight);
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        int quadCount = 0;
        AppendQuad(_whiteTexture, rect.X, rect.Y, rect.Width, rect.Height, 0f, 0f, 1f, 1f, color, ref quadCount);
        Flush(_whiteTexture, quadCount);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0)
        {
            return;
        }

        int t = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        int quadCount = 0;
        AppendQuad(_whiteTexture, rect.X, rect.Y, rect.Width, t, 0f, 0f, 1f, 1f, color, ref quadCount);
        AppendQuad(_whiteTexture, rect.X, rect.Bottom - t, rect.Width, t, 0f, 0f, 1f, 1f, color, ref quadCount);

        int middleHeight = rect.Height - t * 2;
        if (middleHeight > 0)
        {
            AppendQuad(_whiteTexture, rect.X, rect.Y + t, t, middleHeight, 0f, 0f, 1f, 1f, color, ref quadCount);
            AppendQuad(_whiteTexture, rect.Right - t, rect.Y + t, t, middleHeight, 0f, 0f, 1f, 1f, color, ref quadCount);
        }

        Flush(_whiteTexture, quadCount);
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
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        UiFont activeFont = font ?? DefaultFont;
        UiTextLayout layout = activeFont.LayoutText(text, scale);
        if (layout.Glyphs.Count == 0)
        {
            return;
        }

        uint activeTexture = 0;
        int quadCount = 0;

        for (int i = 0; i < layout.Glyphs.Count; i++)
        {
            UiPositionedGlyph glyph = layout.Glyphs[i];
            UiGlyphAtlasEntry entry = _glyphAtlas.GetOrAdd(glyph.Glyph);
            if (!entry.IsValid)
            {
                continue;
            }

            uint textureId = EnsureAtlasTexture(entry.PageIndex).TextureId;
            if (activeTexture != 0 && activeTexture != textureId)
            {
                Flush(activeTexture, quadCount);
                quadCount = 0;
            }

            activeTexture = textureId;
            UiGlyphAtlasPage page = _glyphAtlas.GetPage(entry.PageIndex);
            float u1 = entry.SourceRect.X / (float)page.Width;
            float v1 = entry.SourceRect.Y / (float)page.Height;
            float u2 = entry.SourceRect.Right / (float)page.Width;
            float v2 = entry.SourceRect.Bottom / (float)page.Height;

            AppendQuad(
                activeTexture,
                position.X + glyph.X,
                position.Y + glyph.Y,
                glyph.Glyph.Width,
                glyph.Glyph.Height,
                u1,
                v1,
                u2,
                v2,
                color,
                ref quadCount);
        }

        Flush(activeTexture, quadCount);
    }

    public void DrawTexture(uint textureId, UiRect rect, bool flipVertical = false, UiColor? tint = null)
    {
        if (textureId == 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        UiColor drawColor = tint ?? UiColor.White;
        float vTop = flipVertical ? 1f : 0f;
        float vBottom = flipVertical ? 0f : 1f;
        int quadCount = 0;
        AppendQuad(textureId, rect.X, rect.Y, rect.Width, rect.Height, 0f, vTop, 1f, vBottom, drawColor, ref quadCount);
        Flush(textureId, quadCount);
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return MeasureTextWidth(text, scale, null);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextWidth(text, scale);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return MeasureTextHeight(scale, null);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return (font ?? DefaultFont).MeasureTextHeight(scale);
    }

    public void PushClip(UiRect rect)
    {
        UiRect clip = rect;
        UiRect viewport = GetViewportRect();
        clip = Intersect(clip, viewport);

        if (_clipStack.Count > 0)
        {
            clip = Intersect(clip, _clipStack.Peek());
        }

        _clipStack.Push(clip);
        ApplyClip(clip);
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            return;
        }

        _clipStack.Pop();
        if (_clipStack.Count == 0)
        {
            if (_scissorEnabled)
            {
                _gl.Disable(EnableCap.ScissorTest);
                _scissorEnabled = false;
            }

            return;
        }

        ApplyClip(_clipStack.Peek());
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
        ref int quadCount)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (quadCount >= MaxQuadsPerFlush)
        {
            Flush(expectedTexture, quadCount);
            quadCount = 0;
        }

        int baseIndex = quadCount * 4;
        float left = x;
        float top = y;
        float right = x + width;
        float bottom = y + height;

        _vertices[baseIndex + 0] = new UiVertex(left, top, u1, v1, color);
        _vertices[baseIndex + 1] = new UiVertex(right, top, u2, v1, color);
        _vertices[baseIndex + 2] = new UiVertex(right, bottom, u2, v2, color);
        _vertices[baseIndex + 3] = new UiVertex(left, bottom, u1, v2, color);
        quadCount++;
    }

    private void Flush(uint textureId, int quadCount)
    {
        if (textureId == 0 || quadCount <= 0)
        {
            return;
        }

        _gl.UseProgram(_program);
        _gl.BindVertexArray(_vao);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Uniform2(_viewportUniformLocation, (float)_viewportWidth, (float)_viewportHeight);
        _gl.Uniform1(_textureUniformLocation, 0);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureId);

        fixed (UiVertex* vertexPtr = _vertices)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(quadCount * 4 * sizeof(float) * 8),
                vertexPtr,
                BufferUsageARB.StreamDraw);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)(quadCount * 6), DrawElementsType.UnsignedShort, null);
        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.UseProgram(0);
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
        }

        return texture;
    }

    private void ApplyClip(UiRect rect)
    {
        int width = Math.Max(0, rect.Width);
        int height = Math.Max(0, rect.Height);

        if (!_scissorEnabled)
        {
            _gl.Enable(EnableCap.ScissorTest);
            _scissorEnabled = true;
        }

        int y = _viewportHeight - (rect.Y + height);
        _gl.Scissor(rect.X, y, (uint)width, (uint)height);
    }

    private UiRect GetViewportRect()
    {
        return new UiRect(0, 0, _viewportWidth, _viewportHeight);
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

            uniform vec2 uViewportSize;

            out vec2 vTexCoord;
            out vec4 vColor;

            void main()
            {
                vec2 normalized = aPosition / uViewportSize;
                vec2 clip = vec2(normalized.x * 2.0 - 1.0, 1.0 - normalized.y * 2.0);
                gl_Position = vec4(clip, 0.0, 1.0);
                vTexCoord = aTexCoord;
                vColor = aColor;
            }
            """;

        const string fragmentSource =
            """
            #version 330 core

            in vec2 vTexCoord;
            in vec4 vColor;

            uniform sampler2D uTexture;

            out vec4 FragColor;

            void main()
            {
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
