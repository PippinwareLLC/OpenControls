using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenControls;
using OpenControls.Controls;

namespace OpenControls.Editor;

public sealed class EditorGame : Game
{
    private const int FontScale = 2;
    private const int Padding = 10;
    private const string DefaultLayoutFile = "opencontrols-editor.json";

    private readonly GraphicsDeviceManager _graphics;
    private readonly List<char> _textInputBuffer = new();
    private readonly List<EditorControlEntry> _controls = new();

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private MonoGameUiRenderer? _renderer;
    private UiContext? _context;

    private UiModalHost? _root;
    private UiPanel? _rootPanel;
    private UiPanel? _topBar;
    private UiLabel? _titleLabel;
    private UiTextField? _filePathField;
    private UiButton? _loadButton;
    private UiButton? _saveButton;
    private UiLabel? _statusLabel;

    private UiDockWorkspace? _dockWorkspace;
    private UiWindow? _paletteWindow;
    private UiWindow? _hierarchyWindow;
    private UiWindow? _canvasWindow;
    private UiWindow? _inspectorWindow;

    private UiListBox? _paletteList;
    private UiButton? _addButton;
    private UiListBox? _hierarchyList;

    private UiPanel? _designSurface;
    private UiLabel? _canvasHintLabel;
    private EditorSelectionOverlay? _selectionOverlay;

    private UiLabel? _inspectorHeader;
    private UiLabel? _typeLabel;
    private UiLabel? _idLabel;
    private UiTextField? _idField;
    private UiLabel? _xLabel;
    private UiTextField? _xField;
    private UiLabel? _yLabel;
    private UiTextField? _yField;
    private UiLabel? _widthLabel;
    private UiTextField? _widthField;
    private UiLabel? _heightLabel;
    private UiTextField? _heightField;
    private UiScrollPanel? _inspectorScrollPanel;
    private UiButton? _applyButton;
    private UiButton? _deleteButton;
    private UiButton? _alignLeftButton;
    private UiButton? _alignCenterButton;
    private UiButton? _alignRightButton;
    private UiButton? _alignTopButton;
    private UiButton? _alignMiddleButton;
    private UiButton? _alignBottomButton;

    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private EditorControlEntry? _selected;
    private string _statusMessage = "Ready";
    private int _nextControlId = 1;
    private bool _dragging;
    private UiPoint _dragOffset;
    private bool _suppressHierarchySync;

    private readonly List<Type> _paletteTypes = new();
    private readonly List<PropertyEditorEntry> _propertyEditors = new();
    private static readonly HashSet<string> ExcludedPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(UiElement.Bounds),
        nameof(UiElement.Visible),
        nameof(UiElement.Enabled),
        nameof(UiElement.Parent),
        nameof(UiElement.Children),
        nameof(UiElement.Id),
        nameof(UiElement.ClipChildren)
    };

    public EditorGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        Window.TextInput += HandleTextInput;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _renderer = new MonoGameUiRenderer(_spriteBatch, _pixel, new TinyBitmapFont());

        BuildUi();
        LoadLayoutFromFile(GetLayoutPath(), silent: true);
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_context == null || _renderer == null)
        {
            base.Update(gameTime);
            return;
        }

        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        if (IsPressed(keyboardState, _previousKeyboard, Keys.Escape))
        {
            Exit();
        }

        if (IsPressed(keyboardState, _previousKeyboard, Keys.S) && IsCtrlPressed(keyboardState))
        {
            SaveLayout();
        }

        if (IsPressed(keyboardState, _previousKeyboard, Keys.O) && IsCtrlPressed(keyboardState))
        {
            LoadLayout();
        }

        UpdateLayout(GraphicsDevice.Viewport);
        UiInputState input = BuildInputState(keyboardState, _previousKeyboard, mouseState, _previousMouse);
        HandleDesignSurfaceSelection(input);
        _context.Update(input, (float)gameTime.ElapsedGameTime.TotalSeconds);
        UpdateWindowContent();
        UpdateControlBounds();
        UpdateSelectionOverlay();
        UpdateStatusLabel();

        _previousKeyboard = keyboardState;
        _previousMouse = mouseState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_context == null || _renderer == null || _spriteBatch == null)
        {
            base.Draw(gameTime);
            return;
        }

        GraphicsDevice.Clear(new Color(12, 16, 24));
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
        _context.Render(_renderer);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void BuildUi()
    {
        _root = new UiModalHost();
        _rootPanel = new UiPanel
        {
            Background = new UiColor(16, 20, 30)
        };

        _topBar = new UiPanel
        {
            Background = new UiColor(22, 26, 38),
            Border = new UiColor(60, 70, 90)
        };

        _titleLabel = new UiLabel
        {
            Text = "OpenControls.Editor",
            Color = UiColor.White,
            Scale = FontScale
        };

        _filePathField = new UiTextField
        {
            Text = DefaultLayoutFile,
            TextScale = FontScale,
            CornerRadius = 4,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _loadButton = new UiButton
        {
            Text = "Load",
            TextScale = FontScale
        };
        _loadButton.Clicked += () => LoadLayout();

        _saveButton = new UiButton
        {
            Text = "Save",
            TextScale = FontScale
        };
        _saveButton.Clicked += () => SaveLayout();

        _statusLabel = new UiLabel
        {
            Text = _statusMessage,
            Color = new UiColor(170, 190, 220),
            Scale = FontScale
        };

        _dockWorkspace = new UiDockWorkspace
        {
            Id = "workspace-editor"
        };

        _paletteWindow = new UiWindow
        {
            Title = "Palette",
            TitleTextScale = FontScale,
            Id = "window-palette"
        };

        _hierarchyWindow = new UiWindow
        {
            Title = "Hierarchy",
            TitleTextScale = FontScale,
            Id = "window-hierarchy"
        };

        _canvasWindow = new UiWindow
        {
            Title = "Canvas",
            TitleTextScale = FontScale,
            Id = "window-canvas"
        };

        _inspectorWindow = new UiWindow
        {
            Title = "Inspector",
            TitleTextScale = FontScale,
            Id = "window-inspector"
        };
        _inspectorScrollPanel = _inspectorWindow.EnsureScrollPanel();

        _paletteTypes.Clear();
        _paletteTypes.AddRange(GetPaletteTypes());

        _paletteList = new UiListBox
        {
            Items = _paletteTypes.Select(type => type.Name).ToArray(),
            TextScale = FontScale,
            ItemHeight = 22 + FontScale * 2,
            AllowDeselect = false
        };
        _paletteList.SelectionChanged += _ => UpdateStatus("Palette selection ready.");

        _addButton = new UiButton
        {
            Text = "Add",
            TextScale = FontScale
        };
        _addButton.Clicked += AddControlFromPalette;

        _hierarchyList = new UiListBox
        {
            Items = Array.Empty<string>(),
            TextScale = FontScale,
            ItemHeight = 22 + FontScale * 2,
            AllowDeselect = true
        };
        _hierarchyList.SelectionChanged += HandleHierarchySelection;

        _designSurface = new UiPanel
        {
            Background = new UiColor(20, 26, 36),
            Border = new UiColor(70, 85, 110),
            BorderThickness = 2,
            ClipChildren = true
        };

        _canvasHintLabel = new UiLabel
        {
            Text = "Ctrl+drag to move. Use inspector to edit bounds.",
            Color = new UiColor(150, 170, 200),
            Scale = FontScale
        };

        _selectionOverlay = new EditorSelectionOverlay
        {
            Visible = false,
            Border = new UiColor(120, 200, 255),
            Thickness = 2
        };

        _inspectorHeader = new UiLabel
        {
            Text = "Selection",
            Color = UiColor.White,
            Scale = FontScale
        };

        _typeLabel = new UiLabel
        {
            Text = "Type: -",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _idLabel = new UiLabel { Text = "Id", Color = UiColor.White, Scale = FontScale };
        _idField = CreateInspectorField();

        _xLabel = new UiLabel { Text = "X", Color = UiColor.White, Scale = FontScale };
        _xField = CreateInspectorField(IsNumericCharacter);

        _yLabel = new UiLabel { Text = "Y", Color = UiColor.White, Scale = FontScale };
        _yField = CreateInspectorField(IsNumericCharacter);

        _widthLabel = new UiLabel { Text = "Width", Color = UiColor.White, Scale = FontScale };
        _widthField = CreateInspectorField(IsNumericCharacter);

        _heightLabel = new UiLabel { Text = "Height", Color = UiColor.White, Scale = FontScale };
        _heightField = CreateInspectorField(IsNumericCharacter);

        _applyButton = new UiButton
        {
            Text = "Apply",
            TextScale = FontScale
        };
        _applyButton.Clicked += ApplyInspectorChanges;

        _deleteButton = new UiButton
        {
            Text = "Delete",
            TextScale = FontScale
        };
        _deleteButton.Clicked += DeleteSelectedControl;

        _alignLeftButton = CreateAlignButton("Left", () => AlignSelection(AlignTarget.Left));
        _alignCenterButton = CreateAlignButton("Center", () => AlignSelection(AlignTarget.Center));
        _alignRightButton = CreateAlignButton("Right", () => AlignSelection(AlignTarget.Right));
        _alignTopButton = CreateAlignButton("Top", () => AlignSelection(AlignTarget.Top));
        _alignMiddleButton = CreateAlignButton("Middle", () => AlignSelection(AlignTarget.Middle));
        _alignBottomButton = CreateAlignButton("Bottom", () => AlignSelection(AlignTarget.Bottom));

        _paletteWindow.AddContentChild(_paletteList);
        _paletteWindow.AddContentChild(_addButton);

        _hierarchyWindow.AddContentChild(_hierarchyList);

        _canvasWindow.AddContentChild(_canvasHintLabel);
        _canvasWindow.AddContentChild(_designSurface);
        _designSurface.AddChild(_selectionOverlay);

        _inspectorWindow.AddContentChild(_inspectorHeader);
        _inspectorWindow.AddContentChild(_typeLabel);
        _inspectorWindow.AddContentChild(_idLabel);
        _inspectorWindow.AddContentChild(_idField);
        _inspectorWindow.AddContentChild(_xLabel);
        _inspectorWindow.AddContentChild(_xField);
        _inspectorWindow.AddContentChild(_yLabel);
        _inspectorWindow.AddContentChild(_yField);
        _inspectorWindow.AddContentChild(_widthLabel);
        _inspectorWindow.AddContentChild(_widthField);
        _inspectorWindow.AddContentChild(_heightLabel);
        _inspectorWindow.AddContentChild(_heightField);
        _inspectorWindow.AddContentChild(_alignLeftButton);
        _inspectorWindow.AddContentChild(_alignCenterButton);
        _inspectorWindow.AddContentChild(_alignRightButton);
        _inspectorWindow.AddContentChild(_alignTopButton);
        _inspectorWindow.AddContentChild(_alignMiddleButton);
        _inspectorWindow.AddContentChild(_alignBottomButton);
        _inspectorWindow.AddContentChild(_applyButton);
        _inspectorWindow.AddContentChild(_deleteButton);

        UiDockHost rootHost = _dockWorkspace.RootHost;
        UiDockHost leftHost = _dockWorkspace.SplitHost(rootHost, UiDockWorkspace.DockTarget.Left);
        UiDockHost rightHost = _dockWorkspace.SplitHost(rootHost, UiDockWorkspace.DockTarget.Right);

        leftHost.DockWindow(_paletteWindow);
        leftHost.DockWindow(_hierarchyWindow);
        rootHost.DockWindow(_canvasWindow);
        rightHost.DockWindow(_inspectorWindow);

        _topBar.AddChild(_titleLabel);
        _topBar.AddChild(_filePathField);
        _topBar.AddChild(_loadButton);
        _topBar.AddChild(_saveButton);

        _rootPanel.AddChild(_topBar);
        _rootPanel.AddChild(_dockWorkspace);
        _rootPanel.AddChild(_statusLabel);

        _root.AddChild(_rootPanel);
        _context = new UiContext(_root);
        UpdateAlignButtons();
    }

    private void UpdateLayout(Viewport viewport)
    {
        if (_renderer == null || _root == null || _rootPanel == null || _topBar == null || _dockWorkspace == null || _statusLabel == null)
        {
            return;
        }

        int fontHeight = _renderer.MeasureTextHeight(FontScale);
        int topBarHeight = fontHeight + Padding * 2;
        int statusHeight = fontHeight + Padding;

        UiRect rootBounds = new UiRect(0, 0, viewport.Width, viewport.Height);
        _root.Bounds = rootBounds;
        _rootPanel.Bounds = rootBounds;

        _topBar.Bounds = new UiRect(0, 0, viewport.Width, topBarHeight);
        _statusLabel.Bounds = new UiRect(Padding, viewport.Height - statusHeight, Math.Max(0, viewport.Width - Padding * 2), fontHeight);

        int dockTop = _topBar.Bounds.Bottom + Padding;
        int dockHeight = Math.Max(0, viewport.Height - dockTop - statusHeight - Padding);
        _dockWorkspace.Bounds = new UiRect(Padding, dockTop, Math.Max(0, viewport.Width - Padding * 2), dockHeight);

        LayoutTopBar(_topBar.Bounds);
    }

    private void LayoutTopBar(UiRect bounds)
    {
        if (_renderer == null || _titleLabel == null || _filePathField == null || _loadButton == null || _saveButton == null)
        {
            return;
        }

        int fontHeight = _renderer.MeasureTextHeight(FontScale);
        int contentY = bounds.Y + (bounds.Height - fontHeight) / 2;
        int titleWidth = _renderer.MeasureTextWidth(_titleLabel.Text, FontScale);

        _titleLabel.Bounds = new UiRect(bounds.X + Padding, contentY, titleWidth, fontHeight);

        int buttonWidth = 80;
        int buttonHeight = fontHeight + 8;
        int buttonY = bounds.Y + (bounds.Height - buttonHeight) / 2;
        int saveX = bounds.Right - Padding - buttonWidth;
        int loadX = saveX - Padding - buttonWidth;

        _loadButton.Bounds = new UiRect(loadX, buttonY, buttonWidth, buttonHeight);
        _saveButton.Bounds = new UiRect(saveX, buttonY, buttonWidth, buttonHeight);

        int fieldX = _titleLabel.Bounds.Right + Padding;
        int fieldWidth = Math.Max(120, loadX - Padding - fieldX);
        _filePathField.Bounds = new UiRect(fieldX, buttonY, fieldWidth, buttonHeight);
    }

    private void UpdateWindowContent()
    {
        if (_renderer == null)
        {
            return;
        }

        int fontHeight = _renderer.MeasureTextHeight(FontScale);

        if (_paletteWindow != null && _paletteList != null && _addButton != null)
        {
            UiRect content = _paletteWindow.ContentBounds;
            int buttonHeight = fontHeight + 8;
            _addButton.Bounds = new UiRect(content.X + Padding, content.Bottom - Padding - buttonHeight, Math.Max(0, content.Width - Padding * 2), buttonHeight);
            _paletteList.Bounds = new UiRect(content.X + Padding, content.Y + Padding, Math.Max(0, content.Width - Padding * 2), Math.Max(0, _addButton.Bounds.Y - Padding - (content.Y + Padding)));
        }

        if (_hierarchyWindow != null && _hierarchyList != null)
        {
            UiRect content = _hierarchyWindow.ContentBounds;
            _hierarchyList.Bounds = new UiRect(content.X + Padding, content.Y + Padding, Math.Max(0, content.Width - Padding * 2), Math.Max(0, content.Height - Padding * 2));
        }

        if (_canvasWindow != null && _designSurface != null && _canvasHintLabel != null)
        {
            UiRect content = _canvasWindow.ContentBounds;
            int hintHeight = fontHeight;
            _canvasHintLabel.Bounds = new UiRect(content.X + Padding, content.Y + Padding, Math.Max(0, content.Width - Padding * 2), hintHeight);
            _designSurface.Bounds = new UiRect(content.X + Padding, _canvasHintLabel.Bounds.Bottom + Padding, Math.Max(0, content.Width - Padding * 2), Math.Max(0, content.Bottom - _canvasHintLabel.Bounds.Bottom - Padding * 2));
        }

        if (_inspectorWindow != null && _inspectorHeader != null && _typeLabel != null && _idLabel != null && _idField != null
            && _xLabel != null && _xField != null && _yLabel != null && _yField != null && _widthLabel != null
            && _widthField != null && _heightLabel != null && _heightField != null && _applyButton != null && _deleteButton != null
            && _alignLeftButton != null && _alignCenterButton != null && _alignRightButton != null && _alignTopButton != null
            && _alignMiddleButton != null && _alignBottomButton != null)
        {
            UiRect content = _inspectorWindow.ContentBounds;
            int rowHeight = fontHeight + 8;
            int labelWidth = Math.Max(50, content.Width / 3);
            int x = content.X + Padding;
            int y = content.Y + Padding;
            int fieldWidth = Math.Max(0, content.Width - Padding * 2 - labelWidth - Padding);

            _inspectorHeader.Bounds = new UiRect(x, y, content.Width - Padding * 2, fontHeight);
            y = _inspectorHeader.Bounds.Bottom + Padding;

            _typeLabel.Bounds = new UiRect(x, y, content.Width - Padding * 2, fontHeight);
            y = _typeLabel.Bounds.Bottom + Padding;

            LayoutField(_idLabel, _idField, x, y, labelWidth, fieldWidth, rowHeight, fontHeight);
            y += rowHeight + Padding / 2;
            LayoutField(_xLabel, _xField, x, y, labelWidth, fieldWidth, rowHeight, fontHeight);
            y += rowHeight + Padding / 2;
            LayoutField(_yLabel, _yField, x, y, labelWidth, fieldWidth, rowHeight, fontHeight);
            y += rowHeight + Padding / 2;
            LayoutField(_widthLabel, _widthField, x, y, labelWidth, fieldWidth, rowHeight, fontHeight);
            y += rowHeight + Padding / 2;
            LayoutField(_heightLabel, _heightField, x, y, labelWidth, fieldWidth, rowHeight, fontHeight);
            y += rowHeight + Padding / 2;
            y = LayoutPropertyEditors(x, y, labelWidth, fieldWidth, rowHeight, fontHeight);

            int alignButtonWidth = (content.Width - Padding * 4) / 3;
            int alignButtonHeight = rowHeight;
            _alignLeftButton.Bounds = new UiRect(x, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            _alignCenterButton.Bounds = new UiRect(_alignLeftButton.Bounds.Right + Padding, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            _alignRightButton.Bounds = new UiRect(_alignCenterButton.Bounds.Right + Padding, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            y += alignButtonHeight + Padding / 2;

            _alignTopButton.Bounds = new UiRect(x, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            _alignMiddleButton.Bounds = new UiRect(_alignTopButton.Bounds.Right + Padding, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            _alignBottomButton.Bounds = new UiRect(_alignMiddleButton.Bounds.Right + Padding, y, Math.Max(0, alignButtonWidth), alignButtonHeight);
            y += alignButtonHeight + Padding;

            int buttonWidth = (content.Width - Padding * 3) / 2;
            int buttonHeight = rowHeight;
            _applyButton.Bounds = new UiRect(x, y, Math.Max(0, buttonWidth), buttonHeight);
            _deleteButton.Bounds = new UiRect(_applyButton.Bounds.Right + Padding, y, Math.Max(0, buttonWidth), buttonHeight);
        }
    }

    private void LayoutField(UiLabel label, UiTextField field, int x, int y, int labelWidth, int fieldWidth, int height, int labelHeight)
    {
        label.Bounds = new UiRect(x, y + (height - labelHeight) / 2, labelWidth, labelHeight);
        field.Bounds = new UiRect(x + labelWidth + Padding, y, fieldWidth, height);
    }

    private int LayoutPropertyEditors(int x, int y, int labelWidth, int fieldWidth, int height, int labelHeight)
    {
        foreach (PropertyEditorEntry entry in _propertyEditors)
        {
            switch (entry.EditorType)
            {
                case PropertyEditorType.Boolean:
                    LayoutCheckboxField(entry.Label, (UiCheckbox)entry.Editor, x, y, labelWidth, fieldWidth, height, labelHeight);
                    break;
                default:
                    LayoutField(entry.Label, (UiTextField)entry.Editor, x, y, labelWidth, fieldWidth, height, labelHeight);
                    break;
            }

            y += height + Padding / 2;
        }

        return y;
    }

    private void LayoutCheckboxField(UiLabel label, UiCheckbox checkbox, int x, int y, int labelWidth, int fieldWidth, int height, int labelHeight)
    {
        label.Bounds = new UiRect(x, y + (height - labelHeight) / 2, labelWidth, labelHeight);
        int checkboxWidth = Math.Max(0, fieldWidth);
        checkbox.Bounds = new UiRect(x + labelWidth + Padding, y, checkboxWidth, height);
    }

    private void UpdateControlBounds()
    {
        if (_designSurface == null)
        {
            return;
        }

        foreach (EditorControlEntry entry in _controls)
        {
            if (entry.Element is UiPanel panel && panel.IsResizing)
            {
                UiRect bounds = panel.Bounds;
                entry.LocalBounds = new UiRect(
                    bounds.X - _designSurface.Bounds.X,
                    bounds.Y - _designSurface.Bounds.Y,
                    Math.Max(1, bounds.Width),
                    Math.Max(1, bounds.Height));
            }

            UiRect local = entry.LocalBounds;
            entry.Element.Bounds = new UiRect(
                _designSurface.Bounds.X + local.X,
                _designSurface.Bounds.Y + local.Y,
                local.Width,
                local.Height);
        }
    }

    private void HandleDesignSurfaceSelection(UiInputState input)
    {
        if (_designSurface == null || !_designSurface.Bounds.Contains(input.MousePosition))
        {
            _dragging = false;
            return;
        }

        UiElement? hit = _designSurface.HitTest(input.MousePosition);
        bool hitSurface = hit == _designSurface || hit == _selectionOverlay;

        if (input.LeftClicked)
        {
            if (hitSurface)
            {
                SetSelection(null);
            }
            else if (hit != null)
            {
                EditorControlEntry? entry = _controls.FirstOrDefault(control => control.Element == hit);
                SetSelection(entry);

                if (entry != null && input.CtrlDown)
                {
                    _dragging = true;
                    UiRect bounds = entry.Element.Bounds;
                    _dragOffset = new UiPoint(input.MousePosition.X - bounds.X, input.MousePosition.Y - bounds.Y);
                }
            }
        }

        if (_dragging && _selected != null && input.LeftDown)
        {
            UiPoint mouse = input.MousePosition;
            int newX = mouse.X - _dragOffset.X - _designSurface.Bounds.X;
            int newY = mouse.Y - _dragOffset.Y - _designSurface.Bounds.Y;
            UiRect local = _selected.LocalBounds;
            _selected.LocalBounds = new UiRect(newX, newY, local.Width, local.Height);
            SyncInspectorFromSelection();
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }
    }

    private void UpdateSelectionOverlay()
    {
        if (_selectionOverlay == null)
        {
            return;
        }

        if (_selected == null)
        {
            _selectionOverlay.Visible = false;
            return;
        }

        _selectionOverlay.Visible = true;
        _selectionOverlay.Bounds = _selected.Element.Bounds;
    }

    private void AddControlFromPalette()
    {
        if (_paletteList == null || _designSurface == null)
        {
            return;
        }

        int index = _paletteList.SelectedIndex;
        if (index < 0 || index >= _paletteTypes.Count)
        {
            UpdateStatus("Select a control type from the palette.");
            return;
        }

        Type type = _paletteTypes[index];
        UiElement control = CreateControl(type);
        string id = $"{type.Name.ToLowerInvariant()}-{_nextControlId++}";
        control.Id = id;

        int offset = 20 + _controls.Count * 8;
        UiRect localBounds = GetDefaultBounds(type, offset);

        EditorControlEntry entry = new(control, type.Name, localBounds);
        _controls.Add(entry);
        _designSurface.AddChild(control);
        RefreshHierarchy();
        SetSelection(entry);
        UpdateStatus($"Added {type.Name}.");
    }

    private UiElement CreateControl(Type type)
    {
        UiElement element = (UiElement?)Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not create {type.Name}.");

        ApplyDefaultStyling(element);
        return element;
    }

    private UiRect GetDefaultBounds(Type type, int offset)
    {
        int width = 160;
        int height = 26;

        if (type == typeof(UiLabel))
        {
            height = _renderer?.MeasureTextHeight(FontScale) ?? 18;
            width = 120;
        }
        else if (type == typeof(UiTextField))
        {
            width = 180;
        }
        else if (type == typeof(UiCheckbox))
        {
            width = 180;
        }

        return new UiRect(offset, offset, width, height);
    }

    private void HandleHierarchySelection(int index)
    {
        if (_suppressHierarchySync)
        {
            return;
        }

        if (index < 0 || index >= _controls.Count)
        {
            SetSelection(null);
            return;
        }

        SetSelection(_controls[index]);
    }

    private void SetSelection(EditorControlEntry? entry)
    {
        _selected = entry;
        BuildPropertyEditors(entry?.Element);
        SyncInspectorFromSelection();
        SyncHierarchyFromSelection();
        UpdateAlignButtons();
    }

    private void SyncInspectorFromSelection()
    {
        if (_typeLabel == null || _idField == null || _xField == null || _yField == null || _widthField == null || _heightField == null)
        {
            return;
        }

        if (_selected == null)
        {
            _typeLabel.Text = "Type: -";
            _idField.Text = string.Empty;
            _xField.Text = string.Empty;
            _yField.Text = string.Empty;
            _widthField.Text = string.Empty;
            _heightField.Text = string.Empty;
            SyncPropertyEditors(null);
            return;
        }

        _typeLabel.Text = $"Type: {_selected.Type}";
        UiRect local = _selected.LocalBounds;
        _idField.Text = _selected.Element.Id;
        _xField.Text = local.X.ToString();
        _yField.Text = local.Y.ToString();
        _widthField.Text = local.Width.ToString();
        _heightField.Text = local.Height.ToString();
        SyncPropertyEditors(_selected.Element);
    }

    private void SyncHierarchyFromSelection()
    {
        if (_hierarchyList == null)
        {
            return;
        }

        _suppressHierarchySync = true;
        if (_selected == null)
        {
            _hierarchyList.SelectedIndex = -1;
        }
        else
        {
            int index = _controls.IndexOf(_selected);
            _hierarchyList.SelectedIndex = index;
        }

        _suppressHierarchySync = false;
    }

    private void ApplyInspectorChanges()
    {
        if (_selected == null || _idField == null || _xField == null || _yField == null || _widthField == null || _heightField == null)
        {
            UpdateStatus("Select a control to edit.");
            return;
        }

        int x = ParseInt(_xField.Text, _selected.LocalBounds.X);
        int y = ParseInt(_yField.Text, _selected.LocalBounds.Y);
        int width = Math.Max(1, ParseInt(_widthField.Text, _selected.LocalBounds.Width));
        int height = Math.Max(1, ParseInt(_heightField.Text, _selected.LocalBounds.Height));

        _selected.LocalBounds = new UiRect(x, y, width, height);
        _selected.Element.Id = _idField.Text.Trim();
        ApplyPropertyEditors(_selected.Element);

        RefreshHierarchy();
        UpdateStatus("Inspector changes applied.");
    }

    private void DeleteSelectedControl()
    {
        if (_selected == null || _designSurface == null)
        {
            return;
        }

        _designSurface.RemoveChild(_selected.Element);
        _controls.Remove(_selected);
        SetSelection(null);
        RefreshHierarchy();
        UpdateStatus("Control deleted.");
    }

    private void RefreshHierarchy()
    {
        if (_hierarchyList == null)
        {
            return;
        }

        string[] items = _controls.Select(control => control.DisplayName).ToArray();
        _hierarchyList.Items = items;
        SyncHierarchyFromSelection();
    }

    private void SaveLayout()
    {
        if (_filePathField == null)
        {
            return;
        }

        string path = GetLayoutPath();
        EditorLayout layout = BuildLayoutSnapshot();
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(layout, options);
        File.WriteAllText(path, json);
        UpdateStatus($"Saved layout to {path}.");
    }

    private void LoadLayout()
    {
        LoadLayoutFromFile(GetLayoutPath(), silent: false);
    }

    private void LoadLayoutFromFile(string path, bool silent)
    {
        if (!File.Exists(path))
        {
            if (!silent)
            {
                UpdateStatus("Layout file not found.");
            }

            return;
        }

        string json = File.ReadAllText(path);
        EditorLayout? layout = JsonSerializer.Deserialize<EditorLayout>(json);
        if (layout == null)
        {
            UpdateStatus("Failed to load layout.");
            return;
        }

        ClearControls();
        foreach (EditorLayoutControl control in layout.Controls)
        {
            UiElement element = CreateControl(ResolveControlType(control.Type));
            element.Id = control.Id;
            ApplySerializedProperties(element, control.Properties);
            ApplyLegacyProperties(element, control);

            UiRect localBounds = new UiRect(control.X, control.Y, Math.Max(1, control.Width), Math.Max(1, control.Height));
            EditorControlEntry entry = new(element, control.Type, localBounds);
            _controls.Add(entry);
            _designSurface?.AddChild(element);
        }

        RefreshHierarchy();
        SetSelection(null);
        UpdateStatus($"Loaded layout from {path}.");
    }

    private void ClearControls()
    {
        if (_designSurface == null)
        {
            return;
        }

        foreach (EditorControlEntry entry in _controls)
        {
            _designSurface.RemoveChild(entry.Element);
        }

        _controls.Clear();
    }

    private EditorLayout BuildLayoutSnapshot()
    {
        EditorLayout layout = new();

        foreach (EditorControlEntry entry in _controls)
        {
            UiRect local = entry.LocalBounds;
            EditorLayoutControl control = new()
            {
                Type = entry.Type,
                Id = entry.Element.Id,
                X = local.X,
                Y = local.Y,
                Width = local.Width,
                Height = local.Height,
                Text = GetElementText(entry.Element),
                Properties = CaptureSerializedProperties(entry.Element)
            };

            if (entry.Element is UiCheckbox checkbox)
            {
                control.Checked = checkbox.Checked;
            }

            layout.Controls.Add(control);
        }

        return layout;
    }

    private string GetLayoutPath()
    {
        string path = _filePathField?.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return DefaultLayoutFile;
        }

        return path.Trim();
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = _statusMessage;
        }
    }

    private void UpdateStatus(string message)
    {
        _statusMessage = message;
    }

    private UiTextField CreateInspectorField(Func<char, bool>? filter = null)
    {
        UiTextField field = new()
        {
            TextScale = FontScale,
            CornerRadius = 4,
            CharacterFilter = filter,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        return field;
    }

    private UiButton CreateAlignButton(string text, Action onClick)
    {
        UiButton button = new()
        {
            Text = text,
            TextScale = FontScale
        };
        button.Clicked += onClick;
        return button;
    }

    private void UpdateAlignButtons()
    {
        bool hasSelection = _selected != null;
        if (_alignLeftButton != null)
        {
            _alignLeftButton.Visible = hasSelection;
        }

        if (_alignCenterButton != null)
        {
            _alignCenterButton.Visible = hasSelection;
        }

        if (_alignRightButton != null)
        {
            _alignRightButton.Visible = hasSelection;
        }

        if (_alignTopButton != null)
        {
            _alignTopButton.Visible = hasSelection;
        }

        if (_alignMiddleButton != null)
        {
            _alignMiddleButton.Visible = hasSelection;
        }

        if (_alignBottomButton != null)
        {
            _alignBottomButton.Visible = hasSelection;
        }
    }

    private void AlignSelection(AlignTarget target)
    {
        if (_selected == null || _designSurface == null)
        {
            return;
        }

        UiRect surfaceBounds = _designSurface.Bounds;
        UiRect local = _selected.LocalBounds;
        int x = local.X;
        int y = local.Y;

        switch (target)
        {
            case AlignTarget.Left:
                x = 0;
                break;
            case AlignTarget.Center:
                x = (surfaceBounds.Width - local.Width) / 2;
                break;
            case AlignTarget.Right:
                x = surfaceBounds.Width - local.Width;
                break;
            case AlignTarget.Top:
                y = 0;
                break;
            case AlignTarget.Middle:
                y = (surfaceBounds.Height - local.Height) / 2;
                break;
            case AlignTarget.Bottom:
                y = surfaceBounds.Height - local.Height;
                break;
        }

        _selected.LocalBounds = new UiRect(x, y, local.Width, local.Height);
        SyncInspectorFromSelection();
        UpdateStatus($"Aligned {target}.");
    }

    private IReadOnlyList<Type> GetPaletteTypes()
    {
        Assembly assembly = typeof(UiElement).Assembly;
        return assembly.GetTypes()
            .Where(type => type.IsPublic
                && typeof(UiElement).IsAssignableFrom(type)
                && type.Namespace == "OpenControls.Controls"
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static Type ResolveControlType(string typeName)
    {
        Assembly assembly = typeof(UiElement).Assembly;
        return assembly.GetTypes()
            .FirstOrDefault(type => type.Name == typeName && typeof(UiElement).IsAssignableFrom(type))
            ?? typeof(UiPanel);
    }

    private void ApplyDefaultStyling(UiElement element)
    {
        if (element is UiPanel panel)
        {
            panel.Background = new UiColor(30, 36, 48);
            panel.Border = new UiColor(80, 95, 120);
            panel.BorderThickness = 2;
            panel.AllowResize = true;
        }

        ApplyTextDefaults(element);
    }

    private void ApplyTextDefaults(UiElement element)
    {
        PropertyInfo? scaleProperty = element.GetType().GetProperty("TextScale", BindingFlags.Instance | BindingFlags.Public);
        if (scaleProperty != null && scaleProperty.CanWrite && scaleProperty.PropertyType == typeof(int))
        {
            scaleProperty.SetValue(element, FontScale);
        }

        PropertyInfo? labelScaleProperty = element.GetType().GetProperty("Scale", BindingFlags.Instance | BindingFlags.Public);
        if (labelScaleProperty != null && labelScaleProperty.CanWrite && labelScaleProperty.PropertyType == typeof(int))
        {
            labelScaleProperty.SetValue(element, FontScale);
        }

        PropertyInfo? textProperty = element.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty != null && textProperty.CanWrite && textProperty.PropertyType == typeof(string))
        {
            string? text = textProperty.GetValue(element) as string;
            if (string.IsNullOrWhiteSpace(text))
            {
                textProperty.SetValue(element, element.GetType().Name);
            }
        }

        if (element is UiTextField textField)
        {
            textField.CaretIndexFromPoint = GetCaretIndexFromPoint;
        }
    }

    private void BuildPropertyEditors(UiElement? element)
    {
        ClearPropertyEditors();
        if (element == null || _inspectorScrollPanel == null)
        {
            return;
        }

        foreach (PropertyInfo property in GetEditableProperties(element.GetType()))
        {
            UiLabel label = new()
            {
                Text = property.Name,
                Color = UiColor.White,
                Scale = FontScale
            };

            if (property.PropertyType == typeof(bool))
            {
                UiCheckbox checkbox = new()
                {
                    Text = string.Empty,
                    TextScale = FontScale
                };

                _inspectorWindow?.AddContentChild(label);
                _inspectorWindow?.AddContentChild(checkbox);
                _propertyEditors.Add(new PropertyEditorEntry(property, label, checkbox, PropertyEditorType.Boolean));
            }
            else
            {
                UiTextField field = CreateInspectorField(GetNumericFilter(property.PropertyType));
                _inspectorWindow?.AddContentChild(label);
                _inspectorWindow?.AddContentChild(field);
                _propertyEditors.Add(new PropertyEditorEntry(property, label, field, GetEditorType(property.PropertyType)));
            }
        }

        SyncPropertyEditors(element);
    }

    private void ClearPropertyEditors()
    {
        if (_inspectorScrollPanel == null)
        {
            _propertyEditors.Clear();
            return;
        }

        foreach (PropertyEditorEntry entry in _propertyEditors)
        {
            _inspectorScrollPanel.RemoveChild(entry.Label);
            _inspectorScrollPanel.RemoveChild(entry.Editor);
        }

        _propertyEditors.Clear();
    }

    private void SyncPropertyEditors(UiElement? element)
    {
        foreach (PropertyEditorEntry entry in _propertyEditors)
        {
            object? value = element != null ? entry.Property.GetValue(element) : null;
            switch (entry.EditorType)
            {
                case PropertyEditorType.Boolean:
                    ((UiCheckbox)entry.Editor).Checked = value is bool boolValue && boolValue;
                    break;
                case PropertyEditorType.Float:
                    ((UiTextField)entry.Editor).Text = value is float floatValue
                        ? floatValue.ToString(CultureInfo.InvariantCulture)
                        : value is double doubleValue
                            ? doubleValue.ToString(CultureInfo.InvariantCulture)
                            : string.Empty;
                    break;
                default:
                    ((UiTextField)entry.Editor).Text = value?.ToString() ?? string.Empty;
                    break;
            }
        }
    }

    private void ApplyPropertyEditors(UiElement element)
    {
        foreach (PropertyEditorEntry entry in _propertyEditors)
        {
            switch (entry.EditorType)
            {
                case PropertyEditorType.Boolean:
                    entry.Property.SetValue(element, ((UiCheckbox)entry.Editor).Checked);
                    break;
                case PropertyEditorType.Integer:
                {
                    string text = ((UiTextField)entry.Editor).Text;
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        entry.Property.SetValue(element, intValue);
                    }
                    break;
                }
                case PropertyEditorType.Float:
                {
                    string text = ((UiTextField)entry.Editor).Text;
                    if (entry.Property.PropertyType == typeof(float)
                        && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        entry.Property.SetValue(element, floatValue);
                    }
                    else if (entry.Property.PropertyType == typeof(double)
                        && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        entry.Property.SetValue(element, doubleValue);
                    }
                    break;
                }
                default:
                    entry.Property.SetValue(element, ((UiTextField)entry.Editor).Text);
                    break;
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetEditableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead
                && property.CanWrite
                && property.GetIndexParameters().Length == 0
                && IsSupportedPropertyType(property.PropertyType)
                && !ExcludedPropertyNames.Contains(property.Name))
            .OrderBy(property => property.Name, StringComparer.Ordinal);
    }

    private static bool IsSupportedPropertyType(Type type)
    {
        return type == typeof(string)
            || type == typeof(int)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(bool);
    }

    private static PropertyEditorType GetEditorType(Type type)
    {
        if (type == typeof(bool))
        {
            return PropertyEditorType.Boolean;
        }

        if (type == typeof(int))
        {
            return PropertyEditorType.Integer;
        }

        if (type == typeof(float) || type == typeof(double))
        {
            return PropertyEditorType.Float;
        }

        return PropertyEditorType.Text;
    }

    private static Func<char, bool>? GetNumericFilter(Type type)
    {
        if (type == typeof(int))
        {
            return IsNumericCharacter;
        }

        if (type == typeof(float) || type == typeof(double))
        {
            return IsFloatCharacter;
        }

        return null;
    }

    private static bool IsFloatCharacter(char character)
    {
        return char.IsDigit(character) || character == '-' || character == '+' || character == '.';
    }

    private static Dictionary<string, JsonElement>? CaptureSerializedProperties(UiElement element)
    {
        Dictionary<string, object?> values = new();
        foreach (PropertyInfo property in GetEditableProperties(element.GetType()))
        {
            values[property.Name] = property.GetValue(element);
        }

        return values.Count == 0
            ? null
            : JsonSerializer.SerializeToElement(values).Deserialize<Dictionary<string, JsonElement>>();
    }

    private static void ApplySerializedProperties(UiElement element, Dictionary<string, JsonElement>? properties)
    {
        if (properties == null)
        {
            return;
        }

        foreach (PropertyInfo property in GetEditableProperties(element.GetType()))
        {
            if (!properties.TryGetValue(property.Name, out JsonElement value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (property.PropertyType == typeof(string) && value.ValueKind == JsonValueKind.String)
            {
                property.SetValue(element, value.GetString() ?? string.Empty);
            }
            else if (property.PropertyType == typeof(int) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int intValue))
            {
                property.SetValue(element, intValue);
            }
            else if (property.PropertyType == typeof(float) && value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float floatValue))
            {
                property.SetValue(element, floatValue);
            }
            else if (property.PropertyType == typeof(double) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double doubleValue))
            {
                property.SetValue(element, doubleValue);
            }
            else if (property.PropertyType == typeof(bool) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                property.SetValue(element, value.GetBoolean());
            }
        }
    }

    private static void ApplyLegacyProperties(UiElement element, EditorLayoutControl control)
    {
        if (!string.IsNullOrWhiteSpace(control.Text))
        {
            SetElementText(element, control.Text);
        }

        if (element is UiCheckbox checkbox && control.Checked.HasValue)
        {
            checkbox.Checked = control.Checked.Value;
        }
    }

    private int GetCaretIndexFromPoint(UiTextField field, UiPoint point)
    {
        if (_renderer == null)
        {
            return field.Text.Length;
        }

        int glyphWidth = _renderer.MeasureTextWidth("A", field.TextScale);
        int relativeX = point.X - (field.Bounds.X + field.Padding);
        if (relativeX <= 0)
        {
            return 0;
        }

        int index = relativeX / glyphWidth;
        return Math.Min(index, field.Text.Length);
    }

    private static bool IsNumericCharacter(char character)
    {
        return char.IsDigit(character) || character == '-' || character == '+';
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, out int value) ? value : fallback;
    }

    private static string? GetElementText(UiElement element)
    {
        return element switch
        {
            UiLabel label => label.Text,
            UiButton button => button.Text,
            UiTextField field => field.Text,
            UiCheckbox checkbox => checkbox.Text,
            _ => null
        };
    }

    private static void SetElementText(UiElement element, string text)
    {
        switch (element)
        {
            case UiLabel label:
                label.Text = text;
                break;
            case UiButton button:
                button.Text = text;
                break;
            case UiTextField field:
                field.Text = text;
                break;
            case UiCheckbox checkbox:
                checkbox.Text = text;
                break;
        }
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

    private enum PropertyEditorType
    {
        Text,
        Integer,
        Float,
        Boolean
    }

    private enum AlignTarget
    {
        Left,
        Center,
        Right,
        Top,
        Middle,
        Bottom
    }

    private sealed class PropertyEditorEntry
    {
        public PropertyEditorEntry(PropertyInfo property, UiLabel label, UiElement editor, PropertyEditorType editorType)
        {
            Property = property;
            Label = label;
            Editor = editor;
            EditorType = editorType;
        }

        public PropertyInfo Property { get; }
        public UiLabel Label { get; }
        public UiElement Editor { get; }
        public PropertyEditorType EditorType { get; }
    }

    private sealed class EditorControlEntry
    {
        public EditorControlEntry(UiElement element, string type, UiRect localBounds)
        {
            Element = element;
            Type = type;
            LocalBounds = localBounds;
        }

        public UiElement Element { get; }
        public string Type { get; }
        public UiRect LocalBounds { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Element.Id) ? Type : $"{Type} ({Element.Id})";
    }

    private sealed class EditorSelectionOverlay : UiElement
    {
        public UiColor Border { get; set; } = UiColor.White;
        public int Thickness { get; set; } = 1;

        public override void Render(UiRenderContext context)
        {
            if (!Visible)
            {
                return;
            }

            context.Renderer.DrawRect(Bounds, Border, Thickness);
        }

        public override UiElement? HitTest(UiPoint point)
        {
            return null;
        }
    }
}
