using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OpenControls;
using OpenControls.Examples;
using OpenControls.State;
using SDL2;

namespace OpenControls.SdlNet.Examples;

public sealed class SdlExamplesApp : IDisposable
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    private IntPtr _window;
    private IntPtr _rendererHandle;
    private bool _textInputActive;
    private bool _quit;
    private bool _disposed;

    private SdlUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;

    private byte[] _currentKeyStates = Array.Empty<byte>();
    private byte[] _previousKeyStates = Array.Empty<byte>();
    private int _keyCount;
    private bool _previousLeftDown;
    private int _scrollDelta;
    private readonly List<char> _textInputBuffer = new();

    public void Run()
    {
        Initialize();
        Loop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_textInputActive)
        {
            SDL.SDL_StopTextInput();
            _textInputActive = false;
        }

        if (_rendererHandle != IntPtr.Zero)
        {
            SDL.SDL_DestroyRenderer(_rendererHandle);
            _rendererHandle = IntPtr.Zero;
        }

        if (_window != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(_window);
            _window = IntPtr.Zero;
        }

        SDL.SDL_Quit();
    }

    private void Initialize()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            throw new InvalidOperationException($"SDL_Init failed: {SDL.SDL_GetError()}");
        }

        _window = SDL.SDL_CreateWindow(
            "OpenControls SDL Examples",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            1280,
            720,
            SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.SDL_GetError()}");
        }

        _rendererHandle = SDL.SDL_CreateRenderer(
            _window,
            -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (_rendererHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateRenderer failed: {SDL.SDL_GetError()}");
        }

        SDL.SDL_SetRenderDrawBlendMode(_rendererHandle, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_StartTextInput();
        _textInputActive = true;

        InitializeKeyState();

        _font = new TinyBitmapFont();
        _renderer = new SdlUiRenderer(_rendererHandle, _font);
        _ui = new ExamplesUi(_renderer, _font);
        _ui.SetTitleText("OpenControls SDL Examples");
        _ui.ExitRequested += () => _quit = true;
    }

    private void InitializeKeyState()
    {
        SDL.SDL_PumpEvents();
        IntPtr statePtr = SDL.SDL_GetKeyboardState(out _keyCount);
        if (_keyCount <= 0)
        {
            return;
        }

        _currentKeyStates = new byte[_keyCount];
        _previousKeyStates = new byte[_keyCount];
        Marshal.Copy(statePtr, _currentKeyStates, 0, _keyCount);
    }

    private void Loop()
    {
        if (_renderer == null || _ui == null)
        {
            return;
        }

        Stopwatch timer = Stopwatch.StartNew();
        long lastTicks = timer.ElapsedMilliseconds;

        while (!_quit)
        {
            long now = timer.ElapsedMilliseconds;
            float deltaSeconds = (now - lastTicks) / 1000f;
            lastTicks = now;

            ProcessEvents();
            UpdateKeyStates();

            SDL.SDL_GetWindowSize(_window, out int width, out int height);

            bool saveRequested = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_F5);
            bool loadRequested = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_F9);
            UiInputState input = BuildInputState();

            _ui.Update(input, deltaSeconds, width, height, saveRequested, loadRequested);
            Render();
        }
    }

    private void ProcessEvents()
    {
        _scrollDelta = 0;
        _textInputBuffer.Clear();

        while (SDL.SDL_PollEvent(out SDL.SDL_Event evt) == 1)
        {
            switch (evt.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    _quit = true;
                    break;
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
                    {
                        _quit = true;
                    }
                    break;
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    _scrollDelta += evt.wheel.y * 120;
                    break;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    HandleTextInput(evt.text);
                    break;
            }
        }
    }

    private unsafe void HandleTextInput(SDL.SDL_TextInputEvent textEvent)
    {
        int length = 0;
        while (length < SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE && textEvent.text[length] != 0)
        {
            length++;
        }

        if (length <= 0)
        {
            return;
        }

        byte* textPtr = textEvent.text;
        string text = Utf8Encoding.GetString(textPtr, length);
        foreach (char character in text)
        {
            if (!char.IsControl(character))
            {
                _textInputBuffer.Add(character);
            }
        }
    }

    private void UpdateKeyStates()
    {
        if (_keyCount <= 0)
        {
            return;
        }

        Array.Copy(_currentKeyStates, _previousKeyStates, _keyCount);
        SDL.SDL_PumpEvents();

        IntPtr statePtr = SDL.SDL_GetKeyboardState(out int count);
        if (count != _keyCount)
        {
            _keyCount = count;
            _currentKeyStates = new byte[_keyCount];
            _previousKeyStates = new byte[_keyCount];
        }

        Marshal.Copy(statePtr, _currentKeyStates, 0, _keyCount);
    }

    private UiInputState BuildInputState()
    {
        uint mouseState = SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        bool leftDown = (mouseState & SDL.SDL_BUTTON_LMASK) != 0;
        bool leftClicked = leftDown && !_previousLeftDown;
        bool leftReleased = !leftDown && _previousLeftDown;
        _previousLeftDown = leftDown;

        SDL.SDL_Keymod mods = SDL.SDL_GetModState();
        bool shift = (mods & SDL.SDL_Keymod.KMOD_SHIFT) != 0;
        bool ctrl = (mods & SDL.SDL_Keymod.KMOD_CTRL) != 0;

        return new UiInputState
        {
            MousePosition = new UiPoint(mouseX, mouseY),
            ScreenMousePosition = new UiPoint(mouseX, mouseY),
            LeftDown = leftDown,
            LeftClicked = leftClicked,
            LeftReleased = leftReleased,
            ShiftDown = shift,
            CtrlDown = ctrl,
            ScrollDelta = _scrollDelta,
            TextInput = _textInputBuffer.Count > 0 ? _textInputBuffer.ToArray() : Array.Empty<char>(),
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_LEFT),
                MoveRight = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_RIGHT),
                MoveUp = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_UP),
                MoveDown = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_DOWN),
                Home = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_HOME),
                End = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_END),
                Backspace = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE),
                Delete = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_DELETE),
                Tab = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_TAB),
                Enter = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_RETURN),
                Escape = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
            }
        };
    }

    private bool IsPressed(SDL.SDL_Scancode scancode)
    {
        int index = (int)scancode;
        if (index < 0 || index >= _keyCount)
        {
            return false;
        }

        return _currentKeyStates[index] != 0 && _previousKeyStates[index] == 0;
    }

    private void Render()
    {
        if (_renderer == null || _ui == null)
        {
            return;
        }

        SDL.SDL_SetRenderDrawColor(_rendererHandle, 10, 12, 18, 255);
        SDL.SDL_RenderClear(_rendererHandle);
        _ui.Render();
        SDL.SDL_RenderPresent(_rendererHandle);
    }
}
