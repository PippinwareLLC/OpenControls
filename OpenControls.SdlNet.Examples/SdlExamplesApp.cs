using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OpenControls;
using OpenControls.Examples;
using OpenControls.SdlNet;
using OpenControls.State;
using SDL2;

namespace OpenControls.SdlNet.Examples;

public sealed class SdlExamplesApp : IDisposable
{
    private sealed class SdlClipboard : IUiClipboard
    {
        public string GetText()
        {
            return SDL.SDL_GetClipboardText() ?? string.Empty;
        }

        public void SetText(string text)
        {
            SDL.SDL_SetClipboardText(text ?? string.Empty);
        }
    }

    private const int DragThreshold = 6;

    private static readonly (SDL.SDL_Scancode Scancode, UiKey UiKey)[] KeyMap =
    [
        (SDL.SDL_Scancode.SDL_SCANCODE_A, UiKey.A),
        (SDL.SDL_Scancode.SDL_SCANCODE_B, UiKey.B),
        (SDL.SDL_Scancode.SDL_SCANCODE_C, UiKey.C),
        (SDL.SDL_Scancode.SDL_SCANCODE_D, UiKey.D),
        (SDL.SDL_Scancode.SDL_SCANCODE_E, UiKey.E),
        (SDL.SDL_Scancode.SDL_SCANCODE_F, UiKey.F),
        (SDL.SDL_Scancode.SDL_SCANCODE_G, UiKey.G),
        (SDL.SDL_Scancode.SDL_SCANCODE_H, UiKey.H),
        (SDL.SDL_Scancode.SDL_SCANCODE_I, UiKey.I),
        (SDL.SDL_Scancode.SDL_SCANCODE_J, UiKey.J),
        (SDL.SDL_Scancode.SDL_SCANCODE_K, UiKey.K),
        (SDL.SDL_Scancode.SDL_SCANCODE_L, UiKey.L),
        (SDL.SDL_Scancode.SDL_SCANCODE_M, UiKey.M),
        (SDL.SDL_Scancode.SDL_SCANCODE_N, UiKey.N),
        (SDL.SDL_Scancode.SDL_SCANCODE_O, UiKey.O),
        (SDL.SDL_Scancode.SDL_SCANCODE_P, UiKey.P),
        (SDL.SDL_Scancode.SDL_SCANCODE_Q, UiKey.Q),
        (SDL.SDL_Scancode.SDL_SCANCODE_R, UiKey.R),
        (SDL.SDL_Scancode.SDL_SCANCODE_S, UiKey.S),
        (SDL.SDL_Scancode.SDL_SCANCODE_T, UiKey.T),
        (SDL.SDL_Scancode.SDL_SCANCODE_U, UiKey.U),
        (SDL.SDL_Scancode.SDL_SCANCODE_V, UiKey.V),
        (SDL.SDL_Scancode.SDL_SCANCODE_W, UiKey.W),
        (SDL.SDL_Scancode.SDL_SCANCODE_X, UiKey.X),
        (SDL.SDL_Scancode.SDL_SCANCODE_Y, UiKey.Y),
        (SDL.SDL_Scancode.SDL_SCANCODE_Z, UiKey.Z),
        (SDL.SDL_Scancode.SDL_SCANCODE_0, UiKey.D0),
        (SDL.SDL_Scancode.SDL_SCANCODE_1, UiKey.D1),
        (SDL.SDL_Scancode.SDL_SCANCODE_2, UiKey.D2),
        (SDL.SDL_Scancode.SDL_SCANCODE_3, UiKey.D3),
        (SDL.SDL_Scancode.SDL_SCANCODE_4, UiKey.D4),
        (SDL.SDL_Scancode.SDL_SCANCODE_5, UiKey.D5),
        (SDL.SDL_Scancode.SDL_SCANCODE_6, UiKey.D6),
        (SDL.SDL_Scancode.SDL_SCANCODE_7, UiKey.D7),
        (SDL.SDL_Scancode.SDL_SCANCODE_8, UiKey.D8),
        (SDL.SDL_Scancode.SDL_SCANCODE_9, UiKey.D9),
        (SDL.SDL_Scancode.SDL_SCANCODE_F1, UiKey.F1),
        (SDL.SDL_Scancode.SDL_SCANCODE_F2, UiKey.F2),
        (SDL.SDL_Scancode.SDL_SCANCODE_F3, UiKey.F3),
        (SDL.SDL_Scancode.SDL_SCANCODE_F4, UiKey.F4),
        (SDL.SDL_Scancode.SDL_SCANCODE_F5, UiKey.F5),
        (SDL.SDL_Scancode.SDL_SCANCODE_F6, UiKey.F6),
        (SDL.SDL_Scancode.SDL_SCANCODE_F7, UiKey.F7),
        (SDL.SDL_Scancode.SDL_SCANCODE_F8, UiKey.F8),
        (SDL.SDL_Scancode.SDL_SCANCODE_F9, UiKey.F9),
        (SDL.SDL_Scancode.SDL_SCANCODE_F10, UiKey.F10),
        (SDL.SDL_Scancode.SDL_SCANCODE_F11, UiKey.F11),
        (SDL.SDL_Scancode.SDL_SCANCODE_F12, UiKey.F12),
        (SDL.SDL_Scancode.SDL_SCANCODE_LEFT, UiKey.Left),
        (SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, UiKey.Right),
        (SDL.SDL_Scancode.SDL_SCANCODE_UP, UiKey.Up),
        (SDL.SDL_Scancode.SDL_SCANCODE_DOWN, UiKey.Down),
        (SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP, UiKey.PageUp),
        (SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN, UiKey.PageDown),
        (SDL.SDL_Scancode.SDL_SCANCODE_HOME, UiKey.Home),
        (SDL.SDL_Scancode.SDL_SCANCODE_END, UiKey.End),
        (SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE, UiKey.Backspace),
        (SDL.SDL_Scancode.SDL_SCANCODE_DELETE, UiKey.Delete),
        (SDL.SDL_Scancode.SDL_SCANCODE_TAB, UiKey.Tab),
        (SDL.SDL_Scancode.SDL_SCANCODE_RETURN, UiKey.Enter),
        (SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER, UiKey.KeypadEnter),
        (SDL.SDL_Scancode.SDL_SCANCODE_SPACE, UiKey.Space),
        (SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE, UiKey.Escape)
    ];

    private static readonly Dictionary<UiMouseCursor, IntPtr> CursorCache = new();

    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    private IntPtr _window;
    private IntPtr _rendererHandle;
    private bool _textInputActive;
    private bool _quit;
    private bool _disposed;
    private UiMouseCursor _appliedCursor = UiMouseCursor.Arrow;

    private SdlUiRenderer? _renderer;
    private TinyBitmapFont? _font;
    private ExamplesUi? _ui;

    private byte[] _currentKeyStates = Array.Empty<byte>();
    private byte[] _previousKeyStates = Array.Empty<byte>();
    private int _keyCount;
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
    private readonly List<char> _textInputBuffer = new();
    private readonly UiKeyRepeatTracker _keyRepeatTracker = new();
    private UiTextCompositionState _composition = UiTextCompositionState.Empty;

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

        foreach (IntPtr cursor in CursorCache.Values)
        {
            SDL.SDL_FreeCursor(cursor);
        }
        CursorCache.Clear();

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
        _ui.Clipboard = new SdlClipboard();
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
            _elapsedSeconds += deltaSeconds;

            ProcessEvents();
            UpdateKeyStates();

            SDL.SDL_GetWindowSize(_window, out int width, out int height);

            bool saveRequested = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_F5);
            bool loadRequested = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_F9);
            UiInputState input = BuildInputState();

            _ui.Update(input, deltaSeconds, width, height, saveRequested, loadRequested);
            ApplyHostState();
            Render();
        }
    }

    private void ProcessEvents()
    {
        _scrollDelta = 0;
        _scrollDeltaX = 0;
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
                    _scrollDeltaX += evt.wheel.x * 120;
                    _scrollDelta += evt.wheel.y * 120;
                    break;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    HandleTextInput(evt.text);
                    break;
                case SDL.SDL_EventType.SDL_TEXTEDITING:
                    HandleTextEditing(evt.edit);
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

        _composition = UiTextCompositionState.Empty;
    }

    private unsafe void HandleTextEditing(SDL.SDL_TextEditingEvent textEvent)
    {
        int length = 0;
        while (length < SDL.SDL_TEXTEDITINGEVENT_TEXT_SIZE && textEvent.text[length] != 0)
        {
            length++;
        }

        if (length <= 0)
        {
            _composition = UiTextCompositionState.Empty;
            return;
        }

        byte* textPtr = textEvent.text;
        string text = Utf8Encoding.GetString(textPtr, length);
        _composition = new UiTextCompositionState(text, textEvent.start, textEvent.length, textEvent.start + textEvent.length);
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
        UiPoint mousePosition = new UiPoint(mouseX, mouseY);
        bool leftDown = (mouseState & SDL.SDL_BUTTON_LMASK) != 0;
        bool leftClicked = leftDown && !_previousLeftDown;
        bool leftReleased = !leftDown && _previousLeftDown;
        bool rightDown = (mouseState & SDL.SDL_BUTTON_RMASK) != 0;
        bool rightClicked = rightDown && !_previousRightDown;
        bool rightReleased = !rightDown && _previousRightDown;
        bool middleDown = (mouseState & SDL.SDL_BUTTON_MMASK) != 0;
        bool middleClicked = middleDown && !_previousMiddleDown;
        bool middleReleased = !middleDown && _previousMiddleDown;
        bool leftDoubleClicked = UiInputHostHelpers.DetectDoubleClick(leftClicked, mousePosition, _elapsedSeconds, ref _lastLeftClickTimeSeconds, ref _lastLeftClickPosition);
        bool rightDoubleClicked = UiInputHostHelpers.DetectDoubleClick(rightClicked, mousePosition, _elapsedSeconds, ref _lastRightClickTimeSeconds, ref _lastRightClickPosition);
        bool middleDoubleClicked = UiInputHostHelpers.DetectDoubleClick(middleClicked, mousePosition, _elapsedSeconds, ref _lastMiddleClickTimeSeconds, ref _lastMiddleClickPosition);
        UiPoint? leftDragOrigin = UiInputHostHelpers.UpdateDragOrigin(leftDown, leftClicked, leftReleased, mousePosition, ref _leftDragOrigin);
        UiPoint? rightDragOrigin = UiInputHostHelpers.UpdateDragOrigin(rightDown, rightClicked, rightReleased, mousePosition, ref _rightDragOrigin);
        UiPoint? middleDragOrigin = UiInputHostHelpers.UpdateDragOrigin(middleDown, middleClicked, middleReleased, mousePosition, ref _middleDragOrigin);
        _previousLeftDown = leftDown;
        _previousRightDown = rightDown;
        _previousMiddleDown = middleDown;

        SDL.SDL_Keymod mods = SDL.SDL_GetModState();
        bool shift = (mods & SDL.SDL_Keymod.KMOD_SHIFT) != 0;
        bool ctrl = (mods & SDL.SDL_Keymod.KMOD_CTRL) != 0;
        bool alt = (mods & SDL.SDL_Keymod.KMOD_ALT) != 0;
        bool superKey = (mods & SDL.SDL_Keymod.KMOD_GUI) != 0;

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
            ShiftDown = shift,
            CtrlDown = ctrl,
            AltDown = alt,
            SuperDown = superKey,
            ScrollDeltaX = _scrollDeltaX,
            ScrollDelta = _scrollDelta,
            TextInput = _textInputBuffer.Count > 0 ? _textInputBuffer.ToArray() : Array.Empty<char>(),
            Composition = _composition,
            KeysDown = BuildKeyList(pressedOnly: false, releasedOnly: false),
            KeysPressed = BuildKeyList(pressedOnly: true, releasedOnly: false),
            KeysReleased = BuildKeyList(pressedOnly: false, releasedOnly: true),
            Navigation = new UiNavigationInput
            {
                MoveLeft = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_LEFT, UiKey.Left),
                MoveRight = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, UiKey.Right),
                MoveUp = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_UP, UiKey.Up),
                MoveDown = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_DOWN, UiKey.Down),
                PageUp = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP, UiKey.PageUp),
                PageDown = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN, UiKey.PageDown),
                Home = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_HOME, UiKey.Home),
                End = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_END, UiKey.End),
                Backspace = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE, UiKey.Backspace),
                Delete = IsNavigationTriggered(SDL.SDL_Scancode.SDL_SCANCODE_DELETE, UiKey.Delete),
                Tab = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_TAB),
                Enter = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_RETURN),
                KeypadEnter = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER),
                Space = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_SPACE),
                Escape = IsPressed(SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
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
        ApplyTextInputRequest(_ui.TextInputRequest);
        ApplyMouseCursor(_ui.RequestedMouseCursor);
    }

    private void ApplyTextInputRequest(UiTextInputRequest? request)
    {
        if (request is not UiTextInputRequest value)
        {
            return;
        }

        SDL.SDL_Rect rect = new()
        {
            x = value.CandidateBounds.X,
            y = value.CandidateBounds.Y,
            w = Math.Max(1, value.CandidateBounds.Width),
            h = Math.Max(1, value.CandidateBounds.Height)
        };
        SDL.SDL_SetTextInputRect(ref rect);
    }

    private void SetTextInputActive(bool enabled)
    {
        if (enabled == _textInputActive)
        {
            return;
        }

        if (enabled)
        {
            SDL.SDL_StartTextInput();
        }
        else
        {
            SDL.SDL_StopTextInput();
        }

        _textInputActive = enabled;
    }

    private void ApplyMouseCursor(UiMouseCursor cursor)
    {
        if (cursor == _appliedCursor)
        {
            return;
        }

        SDL.SDL_SetCursor(GetSystemCursor(cursor));
        _appliedCursor = cursor;
    }

    private static IntPtr GetSystemCursor(UiMouseCursor cursor)
    {
        if (CursorCache.TryGetValue(cursor, out IntPtr handle))
        {
            return handle;
        }

        SDL.SDL_SystemCursor systemCursor = cursor switch
        {
            UiMouseCursor.TextInput => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM,
            UiMouseCursor.ResizeAll => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL,
            UiMouseCursor.ResizeNS => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS,
            UiMouseCursor.ResizeEW => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE,
            UiMouseCursor.ResizeNESW => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW,
            UiMouseCursor.ResizeNWSE => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE,
            UiMouseCursor.Hand => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND,
            UiMouseCursor.NotAllowed => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO,
            _ => SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW
        };

        handle = SDL.SDL_CreateSystemCursor(systemCursor);
        if (handle == IntPtr.Zero)
        {
            handle = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
        }

        CursorCache[cursor] = handle;
        return handle;
    }

    private UiKey[] BuildKeyList(bool pressedOnly, bool releasedOnly)
    {
        if (_keyCount <= 0)
        {
            return Array.Empty<UiKey>();
        }

        List<UiKey> keys = new();
        for (int index = 0; index < _keyCount; index++)
        {
            bool current = _currentKeyStates[index] != 0;
            bool previous = _previousKeyStates[index] != 0;
            bool include = pressedOnly
                ? current && !previous
                : releasedOnly
                    ? !current && previous
                    : current;

            if (!include)
            {
                continue;
            }

            if (TryMapScancode((SDL.SDL_Scancode)index, out UiKey uiKey) && !keys.Contains(uiKey))
            {
                keys.Add(uiKey);
            }
        }

        return keys.ToArray();
    }

    private static bool TryMapScancode(SDL.SDL_Scancode scancode, out UiKey uiKey)
    {
        foreach ((SDL.SDL_Scancode mappedScancode, UiKey mappedUiKey) in KeyMap)
        {
            if (mappedScancode == scancode)
            {
                uiKey = mappedUiKey;
                return true;
            }
        }

        uiKey = UiKey.Unknown;
        return false;
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

    private bool IsDown(SDL.SDL_Scancode scancode)
    {
        int index = (int)scancode;
        if (index < 0 || index >= _keyCount)
        {
            return false;
        }

        return _currentKeyStates[index] != 0;
    }

    private bool IsNavigationTriggered(SDL.SDL_Scancode scancode, UiKey uiKey)
    {
        bool justPressed = IsPressed(scancode);
        return justPressed || _keyRepeatTracker.IsRepeatDue(uiKey, IsDown(scancode), justPressed, _elapsedSeconds);
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
