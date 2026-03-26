using OpenControls;
using OpenControls.Examples;
using OpenControls.OpenGL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenControls.OpenGL.Examples;

public sealed class OpenGlExamplesWindow : GameWindow
{
    private sealed class OpenTkClipboard : IUiClipboard
    {
        private readonly OpenGlExamplesWindow _window;

        public OpenTkClipboard(OpenGlExamplesWindow window)
        {
            _window = window;
        }

        public string GetText()
        {
            return _window.ClipboardString ?? string.Empty;
        }

        public void SetText(string text)
        {
            _window.ClipboardString = text ?? string.Empty;
        }
    }

    private const int DragThreshold = 6;

    private static readonly (Keys Key, UiKey UiKey)[] KeyMap =
    [
        (Keys.A, UiKey.A),
        (Keys.B, UiKey.B),
        (Keys.C, UiKey.C),
        (Keys.D, UiKey.D),
        (Keys.E, UiKey.E),
        (Keys.F, UiKey.F),
        (Keys.G, UiKey.G),
        (Keys.H, UiKey.H),
        (Keys.I, UiKey.I),
        (Keys.J, UiKey.J),
        (Keys.K, UiKey.K),
        (Keys.L, UiKey.L),
        (Keys.M, UiKey.M),
        (Keys.N, UiKey.N),
        (Keys.O, UiKey.O),
        (Keys.P, UiKey.P),
        (Keys.Q, UiKey.Q),
        (Keys.R, UiKey.R),
        (Keys.S, UiKey.S),
        (Keys.T, UiKey.T),
        (Keys.U, UiKey.U),
        (Keys.V, UiKey.V),
        (Keys.W, UiKey.W),
        (Keys.X, UiKey.X),
        (Keys.Y, UiKey.Y),
        (Keys.Z, UiKey.Z),
        (Keys.D0, UiKey.D0),
        (Keys.D1, UiKey.D1),
        (Keys.D2, UiKey.D2),
        (Keys.D3, UiKey.D3),
        (Keys.D4, UiKey.D4),
        (Keys.D5, UiKey.D5),
        (Keys.D6, UiKey.D6),
        (Keys.D7, UiKey.D7),
        (Keys.D8, UiKey.D8),
        (Keys.D9, UiKey.D9),
        (Keys.F1, UiKey.F1),
        (Keys.F2, UiKey.F2),
        (Keys.F3, UiKey.F3),
        (Keys.F4, UiKey.F4),
        (Keys.F5, UiKey.F5),
        (Keys.F6, UiKey.F6),
        (Keys.F7, UiKey.F7),
        (Keys.F8, UiKey.F8),
        (Keys.F9, UiKey.F9),
        (Keys.F10, UiKey.F10),
        (Keys.F11, UiKey.F11),
        (Keys.F12, UiKey.F12),
        (Keys.Left, UiKey.Left),
        (Keys.Right, UiKey.Right),
        (Keys.Up, UiKey.Up),
        (Keys.Down, UiKey.Down),
        (Keys.PageUp, UiKey.PageUp),
        (Keys.PageDown, UiKey.PageDown),
        (Keys.Home, UiKey.Home),
        (Keys.End, UiKey.End),
        (Keys.Backspace, UiKey.Backspace),
        (Keys.Delete, UiKey.Delete),
        (Keys.Tab, UiKey.Tab),
        (Keys.Enter, UiKey.Enter),
        (Keys.KeyPadEnter, UiKey.KeypadEnter),
        (Keys.Space, UiKey.Space),
        (Keys.Escape, UiKey.Escape)
    ];

    private readonly List<char> _textInputBuffer = new();
    private OpenGLUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private KeyboardState? _previousKeyboard;
    private bool _leftDown;
    private bool _leftClicked;
    private bool _leftReleased;
    private bool _rightDown;
    private bool _rightClicked;
    private bool _rightReleased;
    private bool _middleDown;
    private bool _middleClicked;
    private bool _middleReleased;
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
    private UiMouseCursor _appliedCursor = UiMouseCursor.Arrow;

    public OpenGlExamplesWindow(GameWindowSettings gameSettings, NativeWindowSettings windowSettings)
        : base(gameSettings, windowSettings)
    {
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        _font = new TinyBitmapFont();
        _renderer = new OpenGLUiRenderer(_font);
        _ui = new ExamplesUi(_renderer, _font);
        _ui.Clipboard = new OpenTkClipboard(this);
        _ui.SetTitleText("OpenControls OpenGL Examples");
        _ui.ExitRequested += Close;

        TextInput += HandleTextInput;

        InitializeGlState();
        UpdateProjection(Size.X, Size.Y);
    }

    protected override void OnUnload()
    {
        TextInput -= HandleTextInput;
        base.OnUnload();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        UpdateProjection(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (_ui == null)
        {
            return;
        }

        _elapsedSeconds += args.Time;
        KeyboardState keyboard = KeyboardState;
        MouseState mouse = MouseState;
        KeyboardState previousKeyboard = _previousKeyboard ?? keyboard;

        bool saveRequested = IsPressed(keyboard, previousKeyboard, Keys.F5);
        bool loadRequested = IsPressed(keyboard, previousKeyboard, Keys.F9);

        UiInputState input = BuildInputState(keyboard, previousKeyboard, mouse);
        _ui.Update(input, (float)args.Time, Size.X, Size.Y, saveRequested, loadRequested);
        ApplyHostState();

        _previousKeyboard = keyboard;
        _leftClicked = false;
        _leftReleased = false;
        _rightClicked = false;
        _rightReleased = false;
        _middleClicked = false;
        _middleReleased = false;
        _scrollDeltaX = 0;
        _scrollDelta = 0;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (_ui == null)
        {
            SwapBuffers();
            return;
        }

        GL.ClearColor(10f / 255f, 12f / 255f, 18f / 255f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();

        _ui.Render();
        SwapBuffers();
    }

    private void InitializeGlState()
    {
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void UpdateProjection(int width, int height)
    {
        int safeWidth = Math.Max(1, width);
        int safeHeight = Math.Max(1, height);

        GL.Viewport(0, 0, safeWidth, safeHeight);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.Ortho(0, safeWidth, safeHeight, 0, -1, 1);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButton.Left)
        {
            _leftDown = true;
            _leftClicked = true;
        }
        else if (e.Button == MouseButton.Right)
        {
            _rightDown = true;
            _rightClicked = true;
        }
        else if (e.Button == MouseButton.Middle)
        {
            _middleDown = true;
            _middleClicked = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButton.Left)
        {
            _leftDown = false;
            _leftReleased = true;
        }
        else if (e.Button == MouseButton.Right)
        {
            _rightDown = false;
            _rightReleased = true;
        }
        else if (e.Button == MouseButton.Middle)
        {
            _middleDown = false;
            _middleReleased = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _scrollDeltaX += (int)Math.Round(e.OffsetX * 120f);
        _scrollDelta += (int)Math.Round(e.OffsetY * 120f);
    }

    private UiInputState BuildInputState(
        KeyboardState currentKeyboard,
        KeyboardState previousKeyboard,
        MouseState currentMouse)
    {
        Vector2 mousePosition = currentMouse.Position;
        UiPoint mousePoint = new UiPoint((int)Math.Round(mousePosition.X), (int)Math.Round(mousePosition.Y));
        IReadOnlyList<char> textInputBuffer = ConsumeTextInput();
        IReadOnlyList<char> textInput = _ui?.WantTextInput == true ? textInputBuffer : Array.Empty<char>();
        bool leftDoubleClicked = UiInputHostHelpers.DetectDoubleClick(_leftClicked, mousePoint, _elapsedSeconds, ref _lastLeftClickTimeSeconds, ref _lastLeftClickPosition);
        bool rightDoubleClicked = UiInputHostHelpers.DetectDoubleClick(_rightClicked, mousePoint, _elapsedSeconds, ref _lastRightClickTimeSeconds, ref _lastRightClickPosition);
        bool middleDoubleClicked = UiInputHostHelpers.DetectDoubleClick(_middleClicked, mousePoint, _elapsedSeconds, ref _lastMiddleClickTimeSeconds, ref _lastMiddleClickPosition);
        UiPoint? leftDragOrigin = UiInputHostHelpers.UpdateDragOrigin(_leftDown, _leftClicked, _leftReleased, mousePoint, ref _leftDragOrigin);
        UiPoint? rightDragOrigin = UiInputHostHelpers.UpdateDragOrigin(_rightDown, _rightClicked, _rightReleased, mousePoint, ref _rightDragOrigin);
        UiPoint? middleDragOrigin = UiInputHostHelpers.UpdateDragOrigin(_middleDown, _middleClicked, _middleReleased, mousePoint, ref _middleDragOrigin);

        return new UiInputState
        {
            MousePosition = mousePoint,
            ScreenMousePosition = mousePoint,
            LeftDown = _leftDown,
            LeftClicked = _leftClicked,
            LeftDoubleClicked = leftDoubleClicked,
            LeftReleased = _leftReleased,
            RightDown = _rightDown,
            RightClicked = _rightClicked,
            RightDoubleClicked = rightDoubleClicked,
            RightReleased = _rightReleased,
            MiddleDown = _middleDown,
            MiddleClicked = _middleClicked,
            MiddleDoubleClicked = middleDoubleClicked,
            MiddleReleased = _middleReleased,
            LeftDragOrigin = leftDragOrigin,
            RightDragOrigin = rightDragOrigin,
            MiddleDragOrigin = middleDragOrigin,
            DragThreshold = DragThreshold,
            ShiftDown = currentKeyboard.IsKeyDown(Keys.LeftShift) || currentKeyboard.IsKeyDown(Keys.RightShift),
            CtrlDown = currentKeyboard.IsKeyDown(Keys.LeftControl) || currentKeyboard.IsKeyDown(Keys.RightControl),
            AltDown = currentKeyboard.IsKeyDown(Keys.LeftAlt) || currentKeyboard.IsKeyDown(Keys.RightAlt),
            SuperDown = currentKeyboard.IsKeyDown(Keys.LeftSuper) || currentKeyboard.IsKeyDown(Keys.RightSuper),
            ScrollDeltaX = _scrollDeltaX,
            ScrollDelta = _scrollDelta,
            TextInput = textInput,
            KeysDown = BuildKeyList(currentKeyboard, previousKeyboard, pressedOnly: false, releasedOnly: false),
            KeysPressed = BuildKeyList(currentKeyboard, previousKeyboard, pressedOnly: true, releasedOnly: false),
            KeysReleased = BuildKeyList(currentKeyboard, previousKeyboard, pressedOnly: false, releasedOnly: true),
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsPressed(currentKeyboard, previousKeyboard, Keys.Left),
                MoveRight = IsPressed(currentKeyboard, previousKeyboard, Keys.Right),
                MoveUp = IsPressed(currentKeyboard, previousKeyboard, Keys.Up),
                MoveDown = IsPressed(currentKeyboard, previousKeyboard, Keys.Down),
                PageUp = IsPressed(currentKeyboard, previousKeyboard, Keys.PageUp),
                PageDown = IsPressed(currentKeyboard, previousKeyboard, Keys.PageDown),
                Home = IsPressed(currentKeyboard, previousKeyboard, Keys.Home),
                End = IsPressed(currentKeyboard, previousKeyboard, Keys.End),
                Backspace = IsPressed(currentKeyboard, previousKeyboard, Keys.Backspace),
                Delete = IsPressed(currentKeyboard, previousKeyboard, Keys.Delete),
                Tab = IsPressed(currentKeyboard, previousKeyboard, Keys.Tab),
                Enter = IsPressed(currentKeyboard, previousKeyboard, Keys.Enter),
                KeypadEnter = IsPressed(currentKeyboard, previousKeyboard, Keys.KeyPadEnter),
                Space = IsPressed(currentKeyboard, previousKeyboard, Keys.Space),
                Escape = IsPressed(currentKeyboard, previousKeyboard, Keys.Escape)
            }
        };
    }

    private void ApplyHostState()
    {
        if (_ui == null)
        {
            return;
        }

        ApplyMouseCursor(_ui.RequestedMouseCursor);
    }

    private void ApplyMouseCursor(UiMouseCursor cursor)
    {
        if (cursor == _appliedCursor)
        {
            return;
        }

        Cursor = MapMouseCursor(cursor);
        _appliedCursor = cursor;
    }

    private static MouseCursor MapMouseCursor(UiMouseCursor cursor)
    {
        return cursor switch
        {
            UiMouseCursor.TextInput => MouseCursor.IBeam,
            _ => MouseCursor.Default
        };
    }

    private static UiKey[] BuildKeyList(KeyboardState currentKeyboard, KeyboardState previousKeyboard, bool pressedOnly, bool releasedOnly)
    {
        List<UiKey> keys = new();
        foreach ((Keys key, UiKey uiKey) in KeyMap)
        {
            bool current = currentKeyboard.IsKeyDown(key);
            bool previous = previousKeyboard.IsKeyDown(key);
            bool include = pressedOnly
                ? current && !previous
                : releasedOnly
                    ? !current && previous
                    : current;

            if (include && !keys.Contains(uiKey))
            {
                keys.Add(uiKey);
            }
        }

        return keys.ToArray();
    }

    private void HandleTextInput(TextInputEventArgs args)
    {
        if (_ui?.WantTextInput != true)
        {
            return;
        }

        int codePoint = (int)args.Unicode;
        if (codePoint <= 0)
        {
            return;
        }

        char character = (char)codePoint;
        if (char.IsControl(character))
        {
            return;
        }

        _textInputBuffer.Add(character);
    }

    private IReadOnlyList<char> ConsumeTextInput()
    {
        if (_textInputBuffer.Count == 0)
        {
            return Array.Empty<char>();
        }

        char[] input = _textInputBuffer.ToArray();
        _textInputBuffer.Clear();
        return input;
    }

    private static bool IsPressed(KeyboardState current, KeyboardState previous, Keys key)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }
}
