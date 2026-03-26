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
    private sealed class SilkKeyboardClipboard : IUiClipboard
    {
        private readonly IKeyboard _keyboard;

        public SilkKeyboardClipboard(IKeyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public string GetText()
        {
            return _keyboard.ClipboardText ?? string.Empty;
        }

        public void SetText(string text)
        {
            _keyboard.ClipboardText = text ?? string.Empty;
        }
    }

    private const int DragThreshold = 6;

    private static readonly Dictionary<Key, UiKey> KeyMap = new()
    {
        [Key.A] = UiKey.A,
        [Key.B] = UiKey.B,
        [Key.C] = UiKey.C,
        [Key.D] = UiKey.D,
        [Key.E] = UiKey.E,
        [Key.F] = UiKey.F,
        [Key.G] = UiKey.G,
        [Key.H] = UiKey.H,
        [Key.I] = UiKey.I,
        [Key.J] = UiKey.J,
        [Key.K] = UiKey.K,
        [Key.L] = UiKey.L,
        [Key.M] = UiKey.M,
        [Key.N] = UiKey.N,
        [Key.O] = UiKey.O,
        [Key.P] = UiKey.P,
        [Key.Q] = UiKey.Q,
        [Key.R] = UiKey.R,
        [Key.S] = UiKey.S,
        [Key.T] = UiKey.T,
        [Key.U] = UiKey.U,
        [Key.V] = UiKey.V,
        [Key.W] = UiKey.W,
        [Key.X] = UiKey.X,
        [Key.Y] = UiKey.Y,
        [Key.Z] = UiKey.Z,
        [Key.Number0] = UiKey.D0,
        [Key.Number1] = UiKey.D1,
        [Key.Number2] = UiKey.D2,
        [Key.Number3] = UiKey.D3,
        [Key.Number4] = UiKey.D4,
        [Key.Number5] = UiKey.D5,
        [Key.Number6] = UiKey.D6,
        [Key.Number7] = UiKey.D7,
        [Key.Number8] = UiKey.D8,
        [Key.Number9] = UiKey.D9,
        [Key.F1] = UiKey.F1,
        [Key.F2] = UiKey.F2,
        [Key.F3] = UiKey.F3,
        [Key.F4] = UiKey.F4,
        [Key.F5] = UiKey.F5,
        [Key.F6] = UiKey.F6,
        [Key.F7] = UiKey.F7,
        [Key.F8] = UiKey.F8,
        [Key.F9] = UiKey.F9,
        [Key.F10] = UiKey.F10,
        [Key.F11] = UiKey.F11,
        [Key.F12] = UiKey.F12,
        [Key.Left] = UiKey.Left,
        [Key.Right] = UiKey.Right,
        [Key.Up] = UiKey.Up,
        [Key.Down] = UiKey.Down,
        [Key.PageUp] = UiKey.PageUp,
        [Key.PageDown] = UiKey.PageDown,
        [Key.Home] = UiKey.Home,
        [Key.End] = UiKey.End,
        [Key.Backspace] = UiKey.Backspace,
        [Key.Delete] = UiKey.Delete,
        [Key.Tab] = UiKey.Tab,
        [Key.Enter] = UiKey.Enter,
        [Key.KeypadEnter] = UiKey.KeypadEnter,
        [Key.Space] = UiKey.Space,
        [Key.Escape] = UiKey.Escape,
        [Key.ShiftLeft] = UiKey.Shift,
        [Key.ShiftRight] = UiKey.Shift,
        [Key.ControlLeft] = UiKey.Control,
        [Key.ControlRight] = UiKey.Control,
        [Key.AltLeft] = UiKey.Alt,
        [Key.AltRight] = UiKey.Alt,
        [Key.SuperLeft] = UiKey.Super,
        [Key.SuperRight] = UiKey.Super
    };

    private readonly IWindow _window;
    private readonly Dictionary<Key, bool> _previousKeyStates = new();
    private readonly Dictionary<Key, bool> _currentKeyStates = new();
    private readonly List<char> _textInputBuffer = new();

    private GL? _gl;
    private IInputContext? _input;
    private IKeyboard? _keyboard;
    private IMouse? _mouse;
    private SilkNetUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private bool _previousLeftDown;
    private bool _previousRightDown;
    private bool _previousMiddleDown;
    private UiPoint? _leftDragOrigin;
    private UiPoint? _rightDragOrigin;
    private UiPoint? _middleDragOrigin;
    private double _elapsedSeconds;
    private double _lastLeftClickTimeSeconds = double.NegativeInfinity;
    private double _lastRightClickTimeSeconds = double.NegativeInfinity;
    private double _lastMiddleClickTimeSeconds = double.NegativeInfinity;
    private UiPoint _lastLeftClickPosition;
    private UiPoint _lastRightClickPosition;
    private UiPoint _lastMiddleClickPosition;
    private int _scrollDeltaX;
    private int _scrollDelta;
    private bool _disposed;
    private bool _textInputActive = true;
    private UiMouseCursor _appliedCursor = UiMouseCursor.Arrow;

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
            SetTextInputActive(false);
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
        _ui.Clipboard = _keyboard != null ? new SilkKeyboardClipboard(_keyboard) : new UiMemoryClipboard();
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

        _elapsedSeconds += deltaSeconds;
        CaptureKeyboardState();
        Vector2D<int> framebufferSize = _window.FramebufferSize;
        bool saveRequested = WasPressed(Key.F5);
        bool loadRequested = WasPressed(Key.F9);

        UiInputState input = BuildInputState();
        _ui.Update(input, (float)deltaSeconds, framebufferSize.X, framebufferSize.Y, saveRequested, loadRequested);
        ApplyHostState();

        _scrollDeltaX = 0;
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
        _scrollDeltaX += (int)Math.Round(wheel.X * 120f);
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
        bool rightDown = _mouse?.IsButtonPressed(MouseButton.Right) == true;
        bool rightClicked = rightDown && !_previousRightDown;
        bool rightReleased = !rightDown && _previousRightDown;
        bool middleDown = _mouse?.IsButtonPressed(MouseButton.Middle) == true;
        bool middleClicked = middleDown && !_previousMiddleDown;
        bool middleReleased = !middleDown && _previousMiddleDown;

        UiPoint mousePoint = new(framebufferPoint.X, framebufferPoint.Y);
        bool leftDoubleClicked = UiInputHostHelpers.DetectDoubleClick(leftClicked, mousePoint, _elapsedSeconds, ref _lastLeftClickTimeSeconds, ref _lastLeftClickPosition);
        bool rightDoubleClicked = UiInputHostHelpers.DetectDoubleClick(rightClicked, mousePoint, _elapsedSeconds, ref _lastRightClickTimeSeconds, ref _lastRightClickPosition);
        bool middleDoubleClicked = UiInputHostHelpers.DetectDoubleClick(middleClicked, mousePoint, _elapsedSeconds, ref _lastMiddleClickTimeSeconds, ref _lastMiddleClickPosition);
        UiPoint? leftDragOrigin = UiInputHostHelpers.UpdateDragOrigin(leftDown, leftClicked, leftReleased, mousePoint, ref _leftDragOrigin);
        UiPoint? rightDragOrigin = UiInputHostHelpers.UpdateDragOrigin(rightDown, rightClicked, rightReleased, mousePoint, ref _rightDragOrigin);
        UiPoint? middleDragOrigin = UiInputHostHelpers.UpdateDragOrigin(middleDown, middleClicked, middleReleased, mousePoint, ref _middleDragOrigin);
        _previousLeftDown = leftDown;
        _previousRightDown = rightDown;
        _previousMiddleDown = middleDown;

        return new UiInputState
        {
            MousePosition = mousePoint,
            ScreenMousePosition = mousePoint,
            LeftDown = leftDown,
            LeftClicked = leftClicked,
            LeftDoubleClicked = leftDoubleClicked,
            LeftReleased = leftReleased,
            RightDown = rightDown,
            RightClicked = rightClicked,
            RightDoubleClicked = rightDoubleClicked,
            RightReleased = rightReleased,
            MiddleDown = middleDown,
            MiddleClicked = middleClicked,
            MiddleDoubleClicked = middleDoubleClicked,
            MiddleReleased = middleReleased,
            LeftDragOrigin = leftDragOrigin,
            RightDragOrigin = rightDragOrigin,
            MiddleDragOrigin = middleDragOrigin,
            DragThreshold = DragThreshold,
            ShiftDown = IsDown(Key.ShiftLeft) || IsDown(Key.ShiftRight),
            CtrlDown = IsDown(Key.ControlLeft) || IsDown(Key.ControlRight),
            AltDown = IsDown(Key.AltLeft) || IsDown(Key.AltRight),
            SuperDown = IsDown(Key.SuperLeft) || IsDown(Key.SuperRight),
            ScrollDeltaX = _scrollDeltaX,
            ScrollDelta = _scrollDelta,
            TextInput = _textInputBuffer.Count > 0 ? _textInputBuffer.ToArray() : Array.Empty<char>(),
            KeysDown = BuildKeyList(includeDown: true, includePressed: false, includeReleased: false),
            KeysPressed = BuildKeyList(includeDown: false, includePressed: true, includeReleased: false),
            KeysReleased = BuildKeyList(includeDown: false, includePressed: false, includeReleased: true),
            Navigation = new UiNavigationInput
            {
                MoveLeft = WasPressed(Key.Left),
                MoveRight = WasPressed(Key.Right),
                MoveUp = WasPressed(Key.Up),
                MoveDown = WasPressed(Key.Down),
                PageUp = WasPressed(Key.PageUp),
                PageDown = WasPressed(Key.PageDown),
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

    private void ApplyHostState()
    {
        if (_ui == null)
        {
            return;
        }

        SetTextInputActive(_ui.WantTextInput);
        ApplyMouseCursor(_ui.RequestedMouseCursor);
    }

    private void SetTextInputActive(bool enabled)
    {
        if (enabled == _textInputActive)
        {
            return;
        }

        if (_keyboard is null)
        {
            return;
        }

        if (enabled)
        {
            _keyboard.BeginInput();
        }
        else
        {
            _keyboard.EndInput();
        }

        _textInputActive = enabled;
    }

    private void ApplyMouseCursor(UiMouseCursor cursor)
    {
        if (_mouse is null || cursor == _appliedCursor)
        {
            return;
        }

        ICursor? mouseCursor = _mouse.Cursor;
        if (mouseCursor is null)
        {
            return;
        }

        mouseCursor.Type = CursorType.Standard;
        mouseCursor.StandardCursor = MapStandardCursor(cursor);
        mouseCursor.CursorMode = CursorMode.Normal;
        _appliedCursor = cursor;
    }

    private static StandardCursor MapStandardCursor(UiMouseCursor cursor)
    {
        return cursor switch
        {
            UiMouseCursor.TextInput => StandardCursor.IBeam,
            UiMouseCursor.ResizeAll => StandardCursor.ResizeAll,
            UiMouseCursor.ResizeNS => StandardCursor.VResize,
            UiMouseCursor.ResizeEW => StandardCursor.HResize,
            UiMouseCursor.ResizeNESW => StandardCursor.NeswResize,
            UiMouseCursor.ResizeNWSE => StandardCursor.NwseResize,
            UiMouseCursor.Hand => StandardCursor.Hand,
            UiMouseCursor.NotAllowed => StandardCursor.NotAllowed,
            _ => StandardCursor.Arrow
        };
    }

    private void CaptureKeyboardState()
    {
        _previousKeyStates.Clear();
        foreach ((Key key, bool isDown) in _currentKeyStates)
        {
            _previousKeyStates[key] = isDown;
        }

        _currentKeyStates.Clear();
        if (_keyboard is null)
        {
            return;
        }

        foreach (Key key in _keyboard.SupportedKeys)
        {
            _currentKeyStates[key] = _keyboard.IsKeyPressed(key);
        }
    }

    private UiKey[] BuildKeyList(bool includeDown, bool includePressed, bool includeReleased)
    {
        List<UiKey> keys = new();
        foreach ((Key key, bool currentDown) in _currentKeyStates)
        {
            bool previousDown = _previousKeyStates.TryGetValue(key, out bool wasDown) && wasDown;
            bool include = includeDown && currentDown;
            include |= includePressed && currentDown && !previousDown;
            include |= includeReleased && !currentDown && previousDown;
            if (!include || !KeyMap.TryGetValue(key, out UiKey uiKey) || keys.Contains(uiKey))
            {
                continue;
            }

            keys.Add(uiKey);
        }

        return keys.ToArray();
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
        return _currentKeyStates.TryGetValue(key, out bool isDown) && isDown;
    }

    private bool WasPressed(Key key)
    {
        bool current = _currentKeyStates.TryGetValue(key, out bool isDown) && isDown;
        bool previous = _previousKeyStates.TryGetValue(key, out bool wasDown) && wasDown;
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
