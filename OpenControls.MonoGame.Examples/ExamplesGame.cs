using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenControls;
using OpenControls.Examples;
using OpenControls.MonoGame;
using OpenControls.State;

namespace OpenControls.MonoGame.Examples;

public sealed class ExamplesGame : Game
{
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
        (Keys.Back, UiKey.Backspace),
        (Keys.Delete, UiKey.Delete),
        (Keys.Tab, UiKey.Tab),
        (Keys.Enter, UiKey.Enter),
        (Keys.Space, UiKey.Space),
        (Keys.Escape, UiKey.Escape)
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly IUiClipboard _clipboard = new UiMemoryClipboard();
    private readonly RasterizerState _uiRasterizer = new() { ScissorTestEnable = true };
    private readonly List<char> _textInputBuffer = new();
    private readonly UiKeyRepeatTracker _keyRepeatTracker = new();
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private MonoGameUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private bool _wantTextInput;
    private MouseCursor? _appliedCursor;
    private UiPoint? _leftDragOrigin;
    private UiPoint? _rightDragOrigin;
    private UiPoint? _middleDragOrigin;
    private double _lastLeftClickTimeSeconds = double.NegativeInfinity;
    private double _lastRightClickTimeSeconds = double.NegativeInfinity;
    private double _lastMiddleClickTimeSeconds = double.NegativeInfinity;
    private UiPoint _lastLeftClickPosition;
    private UiPoint _lastRightClickPosition;
    private UiPoint _lastMiddleClickPosition;

    public ExamplesGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        Window.Title = "OpenControls MonoGame Examples";
    }

    protected override void Initialize()
    {
        Window.TextInput += HandleTextInput;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = new TinyBitmapFont();
        _renderer = new MonoGameUiRenderer(_spriteBatch, _pixel, _font);
        _ui = new ExamplesUi(_renderer, _font);
        _ui.Clipboard = _clipboard;
        _ui.SetTitleText("OpenControls MonoGame Examples");
        _ui.ExitRequested += Exit;
    }

    protected override void Update(GameTime gameTime)
    {
        if (_ui == null)
        {
            base.Update(gameTime);
            return;
        }

        KeyboardState currentKeyboard = Keyboard.GetState();
        MouseState currentMouse = Mouse.GetState();

        bool saveRequested = IsPressed(currentKeyboard, _previousKeyboard, Keys.F5);
        bool loadRequested = IsPressed(currentKeyboard, _previousKeyboard, Keys.F9);

        UiInputState input = BuildInputState(
            currentKeyboard,
            _previousKeyboard,
            currentMouse,
            _previousMouse,
            gameTime.TotalGameTime.TotalSeconds);
        _ui.Update(
            input,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            saveRequested,
            loadRequested);

        ApplyHostState();

        _previousKeyboard = currentKeyboard;
        _previousMouse = currentMouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_ui == null || _spriteBatch == null)
        {
            base.Draw(gameTime);
            return;
        }

        GraphicsDevice.Clear(new Color(10, 12, 18));

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, _uiRasterizer);
        _ui.Render();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        Window.TextInput -= HandleTextInput;
        base.OnExiting(sender, args);
    }

    private UiInputState BuildInputState(
        KeyboardState currentKeyboard,
        KeyboardState previousKeyboard,
        MouseState currentMouse,
        MouseState previousMouse,
        double currentTimeSeconds)
    {
        UiPoint mousePosition = new UiPoint(currentMouse.X, currentMouse.Y);
        IReadOnlyList<char> textInputBuffer = ConsumeTextInput();
        IReadOnlyList<char> textInput = _wantTextInput ? textInputBuffer : Array.Empty<char>();
        if (_wantTextInput && textInput.Count == 0)
        {
            textInput = GetTextInput(currentKeyboard, previousKeyboard);
        }

        bool leftDown = currentMouse.LeftButton == ButtonState.Pressed;
        bool leftClicked = leftDown && previousMouse.LeftButton == ButtonState.Released;
        bool leftReleased = currentMouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed;
        bool rightDown = currentMouse.RightButton == ButtonState.Pressed;
        bool rightClicked = rightDown && previousMouse.RightButton == ButtonState.Released;
        bool rightReleased = currentMouse.RightButton == ButtonState.Released && previousMouse.RightButton == ButtonState.Pressed;
        bool middleDown = currentMouse.MiddleButton == ButtonState.Pressed;
        bool middleClicked = middleDown && previousMouse.MiddleButton == ButtonState.Released;
        bool middleReleased = currentMouse.MiddleButton == ButtonState.Released && previousMouse.MiddleButton == ButtonState.Pressed;

        bool leftDoubleClicked = UiInputHostHelpers.DetectDoubleClick(leftClicked, mousePosition, currentTimeSeconds, ref _lastLeftClickTimeSeconds, ref _lastLeftClickPosition);
        bool rightDoubleClicked = UiInputHostHelpers.DetectDoubleClick(rightClicked, mousePosition, currentTimeSeconds, ref _lastRightClickTimeSeconds, ref _lastRightClickPosition);
        bool middleDoubleClicked = UiInputHostHelpers.DetectDoubleClick(middleClicked, mousePosition, currentTimeSeconds, ref _lastMiddleClickTimeSeconds, ref _lastMiddleClickPosition);
        UiPoint? leftDragOrigin = UiInputHostHelpers.UpdateDragOrigin(leftDown, leftClicked, leftReleased, mousePosition, ref _leftDragOrigin);
        UiPoint? rightDragOrigin = UiInputHostHelpers.UpdateDragOrigin(rightDown, rightClicked, rightReleased, mousePosition, ref _rightDragOrigin);
        UiPoint? middleDragOrigin = UiInputHostHelpers.UpdateDragOrigin(middleDown, middleClicked, middleReleased, mousePosition, ref _middleDragOrigin);

        return new UiInputState
        {
            MousePosition = mousePosition,
            ScreenMousePosition = mousePosition,
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
            ShiftDown = IsShiftPressed(currentKeyboard),
            CtrlDown = IsCtrlPressed(currentKeyboard),
            AltDown = currentKeyboard.IsKeyDown(Keys.LeftAlt) || currentKeyboard.IsKeyDown(Keys.RightAlt),
            SuperDown = currentKeyboard.IsKeyDown(Keys.LeftWindows) || currentKeyboard.IsKeyDown(Keys.RightWindows),
            ScrollDeltaX = 0,
            ScrollDelta = currentMouse.ScrollWheelValue - previousMouse.ScrollWheelValue,
            TextInput = textInput,
            KeysDown = MapKeys(currentKeyboard.GetPressedKeys()),
            KeysPressed = MapKeys(GetPressedKeys(currentKeyboard, previousKeyboard)),
            KeysReleased = MapKeys(GetReleasedKeys(currentKeyboard, previousKeyboard)),
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Left, UiKey.Left, currentTimeSeconds),
                MoveRight = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Right, UiKey.Right, currentTimeSeconds),
                MoveUp = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Up, UiKey.Up, currentTimeSeconds),
                MoveDown = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Down, UiKey.Down, currentTimeSeconds),
                PageUp = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.PageUp, UiKey.PageUp, currentTimeSeconds),
                PageDown = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.PageDown, UiKey.PageDown, currentTimeSeconds),
                Home = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Home, UiKey.Home, currentTimeSeconds),
                End = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.End, UiKey.End, currentTimeSeconds),
                Backspace = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Back, UiKey.Backspace, currentTimeSeconds),
                Delete = IsNavigationTriggered(currentKeyboard, previousKeyboard, Keys.Delete, UiKey.Delete, currentTimeSeconds),
                Tab = IsPressed(currentKeyboard, previousKeyboard, Keys.Tab),
                Enter = IsPressed(currentKeyboard, previousKeyboard, Keys.Enter),
                KeypadEnter = IsPressed(currentKeyboard, previousKeyboard, Keys.Enter),
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

        _wantTextInput = _ui.WantTextInput;
        ApplyMouseCursor(_ui.RequestedMouseCursor);
    }

    private void ApplyMouseCursor(UiMouseCursor cursor)
    {
        MouseCursor mappedCursor = MapMouseCursor(cursor);
        if (_appliedCursor == mappedCursor)
        {
            return;
        }

        Mouse.SetCursor(mappedCursor);
        _appliedCursor = mappedCursor;
    }

    private static MouseCursor MapMouseCursor(UiMouseCursor cursor)
    {
        return cursor switch
        {
            UiMouseCursor.TextInput => MouseCursor.IBeam,
            UiMouseCursor.ResizeAll => MouseCursor.SizeAll,
            UiMouseCursor.ResizeNS => MouseCursor.SizeNS,
            UiMouseCursor.ResizeEW => MouseCursor.SizeWE,
            UiMouseCursor.ResizeNESW => MouseCursor.SizeNESW,
            UiMouseCursor.ResizeNWSE => MouseCursor.SizeNWSE,
            UiMouseCursor.Hand => MouseCursor.Hand,
            UiMouseCursor.NotAllowed => MouseCursor.No,
            _ => MouseCursor.Arrow
        };
    }

    private static UiKey[] MapKeys(IEnumerable<Keys> keys)
    {
        List<UiKey> mapped = new();
        foreach (Keys key in keys)
        {
            if (TryMapKey(key, out UiKey uiKey) && !mapped.Contains(uiKey))
            {
                mapped.Add(uiKey);
            }
        }

        return mapped.ToArray();
    }

    private static IEnumerable<Keys> GetPressedKeys(KeyboardState current, KeyboardState previous)
    {
        foreach ((Keys key, _) in KeyMap)
        {
            if (current.IsKeyDown(key) && previous.IsKeyUp(key))
            {
                yield return key;
            }
        }
    }

    private static IEnumerable<Keys> GetReleasedKeys(KeyboardState current, KeyboardState previous)
    {
        foreach ((Keys key, _) in KeyMap)
        {
            if (current.IsKeyUp(key) && previous.IsKeyDown(key))
            {
                yield return key;
            }
        }
    }

    private static bool TryMapKey(Keys key, out UiKey uiKey)
    {
        foreach ((Keys mappedKey, UiKey mappedUiKey) in KeyMap)
        {
            if (mappedKey == key)
            {
                uiKey = mappedUiKey;
                return true;
            }
        }

        uiKey = UiKey.Unknown;
        return false;
    }

    private void HandleTextInput(object? sender, TextInputEventArgs e)
    {
        if (!_wantTextInput || char.IsControl(e.Character))
        {
            return;
        }

        _textInputBuffer.Add(e.Character);
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

    private IReadOnlyList<char> GetTextInput(KeyboardState currentKeyboard, KeyboardState previousKeyboard)
    {
        List<char> input = new();
        bool shift = IsShiftPressed(currentKeyboard);

        foreach (Keys key in currentKeyboard.GetPressedKeys())
        {
            if (previousKeyboard.IsKeyUp(key) && TryGetCharacter(key, shift, out char character))
            {
                input.Add(character);
            }
        }

        return input;
    }

    private static bool IsPressed(KeyboardState current, KeyboardState previous, Keys key)
    {
        return current.IsKeyDown(key) && previous.IsKeyUp(key);
    }

    private bool IsNavigationTriggered(KeyboardState current, KeyboardState previous, Keys key, UiKey uiKey, double currentTimeSeconds)
    {
        bool justPressed = IsPressed(current, previous, key);
        return justPressed || _keyRepeatTracker.IsRepeatDue(uiKey, current.IsKeyDown(key), justPressed, currentTimeSeconds);
    }

    private static bool IsShiftPressed(KeyboardState state)
    {
        return state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
    }

    private static bool IsCtrlPressed(KeyboardState state)
    {
        return state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl);
    }

    private static bool TryGetCharacter(Keys key, bool shift, out char character)
    {
        character = '\0';

        if (key >= Keys.A && key <= Keys.Z)
        {
            character = (char)(shift ? key : key + 32);
            return true;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            int digit = key - Keys.D0;
            if (shift)
            {
                character = digit switch
                {
                    1 => '!',
                    2 => '@',
                    3 => '#',
                    9 => '(',
                    0 => ')',
                    _ => (char)('0' + digit)
                };
            }
            else
            {
                character = (char)('0' + digit);
            }
            return true;
        }

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            character = (char)('0' + (key - Keys.NumPad0));
            return true;
        }

        if (key == Keys.Space)
        {
            character = ' ';
            return true;
        }

        if (key == Keys.OemMinus)
        {
            character = shift ? '_' : '-';
            return true;
        }

        if (key == Keys.OemPlus)
        {
            character = shift ? '+' : '=';
            return true;
        }

        if (key == Keys.OemSemicolon)
        {
            character = shift ? ':' : ';';
            return true;
        }

        if (key == Keys.OemQuotes)
        {
            character = shift ? '"' : '\'';
            return true;
        }

        if (key == Keys.OemOpenBrackets)
        {
            character = '[';
            return true;
        }

        if (key == Keys.OemCloseBrackets)
        {
            character = ']';
            return true;
        }

        if (key == Keys.OemBackslash)
        {
            character = '\\';
            return true;
        }

        if (key == Keys.OemQuestion)
        {
            character = shift ? '?' : '/';
            return true;
        }

        if (key == Keys.OemComma)
        {
            character = ',';
            return true;
        }

        if (key == Keys.OemPeriod)
        {
            character = '.';
            return true;
        }

        return false;
    }
}
