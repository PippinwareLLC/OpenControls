using OpenControls;
using OpenControls.Examples;
using OpenControls.OpenGL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenControls.OpenGL.Examples;

public sealed class OpenGlExamplesWindow : GameWindow
{
    private readonly List<char> _textInputBuffer = new();
    private OpenGLUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private KeyboardState? _previousKeyboard;
    private MouseState? _previousMouse;

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

        KeyboardState keyboard = KeyboardState;
        MouseState mouse = MouseState;
        KeyboardState previousKeyboard = _previousKeyboard ?? keyboard;
        MouseState previousMouse = _previousMouse ?? mouse;

        bool saveRequested = IsPressed(keyboard, previousKeyboard, Keys.F5);
        bool loadRequested = IsPressed(keyboard, previousKeyboard, Keys.F9);

        UiInputState input = BuildInputState(keyboard, previousKeyboard, mouse, previousMouse);
        _ui.Update(input, (float)args.Time, Size.X, Size.Y, saveRequested, loadRequested);

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
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

    private UiInputState BuildInputState(
        KeyboardState currentKeyboard,
        KeyboardState previousKeyboard,
        MouseState currentMouse,
        MouseState previousMouse)
    {
        bool leftDown = currentMouse.IsButtonDown(MouseButton.Left);
        bool leftClicked = leftDown && !previousMouse.IsButtonDown(MouseButton.Left);
        bool leftReleased = !leftDown && previousMouse.IsButtonDown(MouseButton.Left);

        Vector2 mousePosition = currentMouse.Position;
        UiPoint mousePoint = new UiPoint((int)Math.Round(mousePosition.X), (int)Math.Round(mousePosition.Y));

        return new UiInputState
        {
            MousePosition = mousePoint,
            ScreenMousePosition = mousePoint,
            LeftDown = leftDown,
            LeftClicked = leftClicked,
            LeftReleased = leftReleased,
            ShiftDown = currentKeyboard.IsKeyDown(Keys.LeftShift) || currentKeyboard.IsKeyDown(Keys.RightShift),
            CtrlDown = currentKeyboard.IsKeyDown(Keys.LeftControl) || currentKeyboard.IsKeyDown(Keys.RightControl),
            ScrollDelta = (int)Math.Round(currentMouse.ScrollDelta.Y * 120f),
            TextInput = ConsumeTextInput(),
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsPressed(currentKeyboard, previousKeyboard, Keys.Left),
                MoveRight = IsPressed(currentKeyboard, previousKeyboard, Keys.Right),
                MoveUp = IsPressed(currentKeyboard, previousKeyboard, Keys.Up),
                MoveDown = IsPressed(currentKeyboard, previousKeyboard, Keys.Down),
                Home = IsPressed(currentKeyboard, previousKeyboard, Keys.Home),
                End = IsPressed(currentKeyboard, previousKeyboard, Keys.End),
                Backspace = IsPressed(currentKeyboard, previousKeyboard, Keys.Backspace),
                Delete = IsPressed(currentKeyboard, previousKeyboard, Keys.Delete),
                Tab = IsPressed(currentKeyboard, previousKeyboard, Keys.Tab),
                Enter = IsPressed(currentKeyboard, previousKeyboard, Keys.Enter),
                Escape = IsPressed(currentKeyboard, previousKeyboard, Keys.Escape)
            }
        };
    }

    private void HandleTextInput(TextInputEventArgs args)
    {
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
