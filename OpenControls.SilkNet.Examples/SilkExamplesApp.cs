using System.Numerics;
using OpenControls;
using OpenControls.Examples;
using OpenControls.SilkNet;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Windowing;

namespace OpenControls.SilkNet.Examples;

public sealed class SilkExamplesApp : IDisposable
{
    private readonly IWindow _window;
    private readonly Dictionary<Key, bool> _previousKeyStates = new();
    private readonly List<char> _textInputBuffer = new();

    private GL? _gl;
    private IInputContext? _input;
    private IKeyboard? _keyboard;
    private IMouse? _mouse;
    private SilkNetUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private bool _previousLeftDown;
    private int _scrollDelta;
    private bool _disposed;

    public SilkExamplesApp()
    {
        bool isMacOs = OperatingSystem.IsMacOS();
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "OpenControls Silk.NET Examples";
        options.API = isMacOs
            ? new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(2, 1))
            : new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Compatability,
                ContextFlags.Default,
                new APIVersion(3, 3));
        options.VSync = true;

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
    }

    public void Run()
    {
        ThrowIfDisposed();
        _window.Run();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _window.Load -= OnLoad;
        _window.Update -= OnUpdate;
        _window.Render -= OnRender;
        _window.FramebufferResize -= OnFramebufferResize;

        if (_keyboard is not null)
        {
            _keyboard.KeyChar -= HandleKeyChar;
            _keyboard.EndInput();
        }

        if (_mouse is not null)
        {
            _mouse.Scroll -= HandleMouseScroll;
        }

        if (_ui is not null)
        {
            _ui.ExitRequested -= CloseWindow;
        }

        if (_input is IDisposable inputDisposable)
        {
            inputDisposable.Dispose();
        }

        if (_gl is IDisposable glDisposable)
        {
            glDisposable.Dispose();
        }

        if (_window is IDisposable windowDisposable)
        {
            windowDisposable.Dispose();
        }
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_window);
        _input = _window.CreateInput();
        _keyboard = _input.Keyboards.Count > 0 ? _input.Keyboards[0] : null;
        _mouse = _input.Mice.Count > 0 ? _input.Mice[0] : null;

        if (_keyboard is not null)
        {
            _keyboard.KeyChar += HandleKeyChar;
            _keyboard.BeginInput();
        }

        if (_mouse is not null)
        {
            _mouse.Scroll += HandleMouseScroll;
        }

        _font = new TinyBitmapFont();
        _renderer = new SilkNetUiRenderer(_gl, _font);
        _ui = new ExamplesUi(_renderer, _font);
        _ui.SetTitleText("OpenControls Silk.NET Examples");
        _ui.ExitRequested += CloseWindow;

        InitializeGlState();
        UpdateProjection(_window.FramebufferSize);
    }

    private void OnUpdate(double deltaSeconds)
    {
        if (_ui is null)
        {
            return;
        }

        Vector2D<int> framebufferSize = _window.FramebufferSize;
        bool saveRequested = WasPressed(Key.F5);
        bool loadRequested = WasPressed(Key.F9);

        UiInputState input = BuildInputState();
        _ui.Update(input, (float)deltaSeconds, framebufferSize.X, framebufferSize.Y, saveRequested, loadRequested);

        _scrollDelta = 0;
        _textInputBuffer.Clear();
    }

    private void OnRender(double _)
    {
        if (_gl is null || _ui is null)
        {
            return;
        }

        _gl.ClearColor(10f / 255f, 12f / 255f, 18f / 255f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _gl.MatrixMode(MatrixMode.Modelview);
        _gl.LoadIdentity();

        _ui.Render();
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        if (_gl is null)
        {
            return;
        }

        UpdateProjection(size);
    }

    private void HandleKeyChar(IKeyboard _, char character)
    {
        if (!char.IsControl(character))
        {
            _textInputBuffer.Add(character);
        }
    }

    private void HandleMouseScroll(IMouse _, ScrollWheel wheel)
    {
        _scrollDelta += (int)Math.Round(wheel.Y * 120f);
    }

    private UiInputState BuildInputState()
    {
        Vector2 clientPosition = _mouse?.Position ?? Vector2.Zero;
        Vector2D<int> clientPoint = new(
            (int)Math.Round(clientPosition.X),
            (int)Math.Round(clientPosition.Y));
        Vector2D<int> framebufferPoint = _window.PointToFramebuffer(clientPoint);

        bool leftDown = _mouse?.IsButtonPressed(MouseButton.Left) == true;
        bool leftClicked = leftDown && !_previousLeftDown;
        bool leftReleased = !leftDown && _previousLeftDown;
        _previousLeftDown = leftDown;

        UiPoint mousePoint = new(framebufferPoint.X, framebufferPoint.Y);

        return new UiInputState
        {
            MousePosition = mousePoint,
            ScreenMousePosition = mousePoint,
            LeftDown = leftDown,
            LeftClicked = leftClicked,
            LeftReleased = leftReleased,
            ShiftDown = IsDown(Key.ShiftLeft) || IsDown(Key.ShiftRight),
            CtrlDown = IsDown(Key.ControlLeft) || IsDown(Key.ControlRight),
            ScrollDelta = _scrollDelta,
            TextInput = _textInputBuffer.Count > 0 ? _textInputBuffer.ToArray() : Array.Empty<char>(),
            Navigation = new UiNavigationInput
            {
                MoveLeft = WasPressed(Key.Left),
                MoveRight = WasPressed(Key.Right),
                MoveUp = WasPressed(Key.Up),
                MoveDown = WasPressed(Key.Down),
                Home = WasPressed(Key.Home),
                End = WasPressed(Key.End),
                Backspace = WasPressed(Key.Backspace),
                Delete = WasPressed(Key.Delete),
                Tab = WasPressed(Key.Tab),
                Enter = WasPressed(Key.Enter),
                KeypadEnter = WasPressed(Key.KeypadEnter),
                Space = WasPressed(Key.Space),
                Escape = WasPressed(Key.Escape)
            }
        };
    }

    private void InitializeGlState()
    {
        if (_gl is null)
        {
            return;
        }

        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void UpdateProjection(Vector2D<int> size)
    {
        if (_gl is null)
        {
            return;
        }

        int safeWidth = Math.Max(1, size.X);
        int safeHeight = Math.Max(1, size.Y);

        _gl.Viewport(0, 0, (uint)safeWidth, (uint)safeHeight);
        _gl.MatrixMode(MatrixMode.Projection);
        _gl.LoadIdentity();
        _gl.Ortho(0, safeWidth, safeHeight, 0, -1, 1);
        _gl.MatrixMode(MatrixMode.Modelview);
        _gl.LoadIdentity();
    }

    private bool IsDown(Key key)
    {
        return _keyboard?.IsKeyPressed(key) == true;
    }

    private bool WasPressed(Key key)
    {
        bool current = IsDown(key);
        bool previous = _previousKeyStates.TryGetValue(key, out bool wasDown) && wasDown;
        _previousKeyStates[key] = current;
        return current && !previous;
    }

    private void CloseWindow()
    {
        _window.Close();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
