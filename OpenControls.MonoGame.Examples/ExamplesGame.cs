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
    private readonly GraphicsDeviceManager _graphics;
    private readonly RasterizerState _uiRasterizer = new() { ScissorTestEnable = true };
    private readonly List<char> _textInputBuffer = new();
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private MonoGameUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;

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

        UiInputState input = BuildInputState(currentKeyboard, _previousKeyboard, currentMouse, _previousMouse);
        _ui.Update(
            input,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            saveRequested,
            loadRequested);

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
        MouseState previousMouse)
    {
        UiPoint mousePosition = new UiPoint(currentMouse.X, currentMouse.Y);
        IReadOnlyList<char> textInput = ConsumeTextInput();
        if (textInput.Count == 0)
        {
            textInput = GetTextInput(currentKeyboard, previousKeyboard);
        }

        return new UiInputState
        {
            MousePosition = mousePosition,
            ScreenMousePosition = mousePosition,
            LeftDown = currentMouse.LeftButton == ButtonState.Pressed,
            LeftClicked = currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released,
            LeftReleased = currentMouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed,
            ShiftDown = IsShiftPressed(currentKeyboard),
            CtrlDown = IsCtrlPressed(currentKeyboard),
            ScrollDelta = currentMouse.ScrollWheelValue - previousMouse.ScrollWheelValue,
            TextInput = textInput,
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsPressed(currentKeyboard, previousKeyboard, Keys.Left),
                MoveRight = IsPressed(currentKeyboard, previousKeyboard, Keys.Right),
                MoveUp = IsPressed(currentKeyboard, previousKeyboard, Keys.Up),
                MoveDown = IsPressed(currentKeyboard, previousKeyboard, Keys.Down),
                Home = IsPressed(currentKeyboard, previousKeyboard, Keys.Home),
                End = IsPressed(currentKeyboard, previousKeyboard, Keys.End),
                Backspace = IsPressed(currentKeyboard, previousKeyboard, Keys.Back),
                Delete = IsPressed(currentKeyboard, previousKeyboard, Keys.Delete),
                Tab = IsPressed(currentKeyboard, previousKeyboard, Keys.Tab),
                Enter = IsPressed(currentKeyboard, previousKeyboard, Keys.Enter),
                Escape = IsPressed(currentKeyboard, previousKeyboard, Keys.Escape)
            }
        };
    }

    private void HandleTextInput(object? sender, TextInputEventArgs e)
    {
        if (char.IsControl(e.Character))
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
