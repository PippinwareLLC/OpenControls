using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenControls;
using OpenControls.Controls;
using OpenControls.State;

namespace OpenControls.Examples;

public sealed class ExamplesGame : Game
{
    private const int FontScale = 2;
    private const int Padding = 12;

    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Cp437Encoding;
    private static readonly string[] ControlNames =
    {
        "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
        "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
        "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
        "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US"
    };

    private readonly GraphicsDeviceManager _graphics;
    private readonly RasterizerState _uiRasterizer = new() { ScissorTestEnable = true };
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private MonoGameUiRenderer? _renderer;
    private UiContext? _context;

    private UiModalHost? _root;
    private UiPanel? _rootPanel;
    private UiLabel? _titleLabel;
    private UiLabel? _helpLabel;
    private UiLabel? _statusLabel;
    private UiButton? _button;
    private UiTextField? _textField;
    private UiMenuBar? _menuBar;
    private string _menuStatus = string.Empty;
    private UiPanel? _clipPanel;
    private UiLabel? _clipLabel;
    private UiWindow? _basicsWindow;
    private UiWindow? _widgetsWindow;
    private UiWindow? _clippingWindow;
    private UiWindow? _serializationWindow;
    private UiPanel? _serializationPanel;
    private UiLabel? _serializationLabel;
    private UiLabel? _widgetsTitleLabel;
    private UiTreeNode? _widgetsTree;
    private UiTreeNode? _widgetsBasicTree;
    private UiTreeNode? _widgetsSliderTree;
    private UiTreeNode? _widgetsStyleTree;
    private UiTreeNode? _widgetsDragTree;
    private UiTreeNode? _widgetsListTree;
    private UiTreeNode? _widgetsMultiSelectTree;
    private UiTreeNode? _widgetsComboTree;
    private UiTreeNode? _widgetsTableTree;
    private UiTreeNode? _widgetsPlotTree;
    private UiTreeNode? _widgetsColorTree;
    private UiTreeNode? _widgetsAsciiTree;
    private UiTreeNode? _widgetsSelectableTree;
    private UiTreeNode? _widgetsLayoutTree;
    private UiTreeNode? _widgetsTooltipTree;
    private UiTreeNode? _widgetsPopupTree;
    private UiTreeNode? _widgetsTreeHeaderTree;
    private UiCheckbox? _snapCheckbox;
    private UiCheckbox? _gizmoCheckbox;
    private UiSeparator? _widgetsSeparator;
    private UiRadioButton? _qualityLow;
    private UiRadioButton? _qualityMedium;
    private UiRadioButton? _qualityHigh;
    private UiSlider? _volumeSlider;
    private UiProgressBar? _volumeProgress;
    private UiLabel? _roundingLabel;
    private UiSlider? _roundingSlider;
    private UiLabel? _roundingPreviewLabel;
    private UiButton? _roundingButton;
    private UiTextField? _roundingField;
    private UiPanel? _roundingPanel;
    private UiLabel? _dragFloatLabel;
    private UiDragFloat? _dragFloat;
    private UiLabel? _dragIntLabel;
    private UiDragInt? _dragInt;
    private UiLabel? _dragRangeLabel;
    private UiDragFloatRange? _dragFloatRange;
    private UiDragIntRange? _dragIntRange;
    private UiLabel? _dragVectorLabel;
    private UiDragFloat2? _dragFloat2;
    private UiDragFloat3? _dragFloat3;
    private UiDragFloat4? _dragFloat4;
    private UiLabel? _dragVectorIntLabel;
    private UiDragInt2? _dragInt2;
    private UiDragInt3? _dragInt3;
    private UiDragInt4? _dragInt4;
    private UiLabel? _dragFlagsLabel;
    private UiCheckbox? _dragClampCheckbox;
    private UiCheckbox? _dragNoSlowFastCheckbox;
    private UiCheckbox? _dragLogCheckbox;
    private UiLabel? _dragHintLabel;
    private UiListBox? _sceneList;
    private UiLabel? _sceneLabel;
    private UiLabel? _sceneSelectionLabel;
    private UiLabel? _multiSelectLabel;
    private UiLabel? _multiSelectHintLabel;
    private UiListBox? _multiSelectList;
    private UiLabel? _multiSelectSelectionLabel;
    private UiSelectionModel? _multiSelectModel;
    private UiLabel? _comboLabel;
    private UiComboBox? _sceneComboBox;
    private UiLabel? _comboSelectionLabel;
    private UiLabel? _tableLabel;
    private UiTable? _sceneTable;
    private UiLabel? _tableSelectionLabel;
    private readonly List<UiTableRow> _tableRows = new();
    private UiLabel? _plotLabel;
    private UiPlotPanel? _plotPanel;
    private UiLabel? _colorEditLabel;
    private UiColorEdit? _colorEdit;
    private UiLabel? _colorPickerLabel;
    private UiColorPicker? _colorPicker;
    private UiLabel? _colorSelectionLabel;
    private UiLabel? _colorButtonLabel;
    private readonly List<UiColorButton> _colorButtons = new();
    private UiColor _sharedColor = new UiColor(120, 180, 220);
    private bool _syncingColor;
    private UiLabel? _asciiLabel;
    private UiLabel? _asciiPageLabel;
    private UiComboBox? _asciiPageCombo;
    private UiTable? _asciiTable;
    private readonly List<UiTableRow> _asciiRows = new();
    private int _asciiPageIndex;
    private readonly List<string> _asciiPageItems = new()
    {
        "ISO-8859-1 (Latin-1)",
        "CP437 (DOS)"
    };
    private UiLabel? _selectablesLabel;
    private UiLabel? _selectablesHintLabel;
    private UiSelectionModel? _selectableSelection;
    private UiSelectable? _selectableLighting;
    private UiSelectable? _selectableNavigation;
    private UiSelectable? _selectableAudio;
    private UiLabel? _scrollPanelLabel;
    private UiLabel? _gridLabel;
    private UiGrid? _grid;
    private UiButton? _gridPrimaryButton;
    private UiButton? _gridSecondaryButton;
    private UiLabel? _gridInfoLabel;
    private UiLabel? _gridStatusLabel;
    private UiLabel? _canvasLabel;
    private UiCanvas? _canvas;
    private UiButton? _canvasNodeA;
    private UiButton? _canvasNodeB;
    private UiButton? _canvasNodeC;
    private UiScrollPanel? _scrollPanel;
    private readonly List<UiLabel> _scrollPanelItems = new();
    private UiButton? _popupButton;
    private UiButton? _modalButton;
    private UiLabel? _menuPopupLabel;
    private UiButton? _menuPopupButton;
    private UiLabel? _menuPopupStatus;
    private UiMenuBar? _menuPopup;
    private UiTable? _menuPopupTable;
    private UiMenuBar.MenuItem? _menuPopupContentItem;
    private UiLabel? _tooltipLabel;
    private UiTooltipRegion? _tooltipRegion;
    private UiTooltip? _tooltip;
    private UiPopup? _popup;
    private UiLabel? _popupLabel;
    private UiButton? _popupCloseButton;
    private UiModal? _modal;
    private UiLabel? _modalLabel;
    private UiButton? _modalCloseButton;
    private UiLabel? _hierarchyLabel;
    private UiTreeNode? _treeNode;
    private readonly List<UiLabel> _treeNodeItems = new();
    private UiCollapsingHeader? _collapsingHeader;
    private readonly List<UiLabel> _collapsingItems = new();

    private UiMenuBar.MenuItem? _examplesMenuAllItem;
    private UiMenuBar.MenuItem? _examplesMenuBasicsItem;
    private UiMenuBar.MenuItem? _examplesMenuWidgetsItem;
    private UiMenuBar.MenuItem? _examplesMenuDockingItem;
    private UiMenuBar.MenuItem? _examplesMenuClippingItem;
    private UiMenuBar.MenuItem? _examplesMenuSerializationItem;

    private UiWindow? _standaloneWindow;
    private UiScrollPanel? _standaloneScrollPanel;
    private readonly List<UiLabel> _standaloneScrollItems = new();

    private UiDockWorkspace? _dockWorkspace;
    private UiDockHost? _dockLeft;
    private UiDockHost? _dockRight;
    private UiWindow? _assetsWindow;
    private UiWindow? _consoleWindow;
    private UiWindow? _inspectorWindow;
    private UiLabel? _assetsLabel;
    private UiLabel? _consoleLabel;
    private UiLabel? _inspectorLabel;
    private bool _standaloneInitialized;
    private bool _clipPanelInitialized;
    private string? _statePath;
    private ExamplePanel _activeExample = ExamplePanel.Custom;
    private readonly List<string> _sceneItems = new()
    {
        "Intro Scene",
        "Town Square",
        "Workshop",
        "Courtyard",
        "Library",
        "Catacombs"
    };

    private enum ExamplePanel
    {
        All,
        Basics,
        Widgets,
        Docking,
        Clipping,
        Serialization,
        Custom
    }

    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private int _buttonClicks;
    private readonly List<char> _textInputBuffer = new();

    static ExamplesGame()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp437Encoding = Encoding.GetEncoding(437);
    }

    public ExamplesGame()
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

        if (IsPressed(keyboardState, _previousKeyboard, Keys.F5))
        {
            SaveUiState();
        }

        if (IsPressed(keyboardState, _previousKeyboard, Keys.F9))
        {
            LoadUiState();
        }

        UpdateLayout(GraphicsDevice.Viewport);
        UiInputState input = BuildInputState(keyboardState, _previousKeyboard, mouseState, _previousMouse);
        _context.Update(input, (float)gameTime.ElapsedGameTime.TotalSeconds);
        UpdateWindowContent();
        UpdateStatusLabel();
        UpdateWidgetPanel();

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

        GraphicsDevice.Clear(new Color(10, 12, 18));
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: _uiRasterizer);
        _context.Render(_renderer);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void BuildUi()
    {
        _root = new UiModalHost();
        _rootPanel = new UiPanel
        {
            Background = new UiColor(12, 14, 20)
        };
        _root.AddChild(_rootPanel);

        _titleLabel = new UiLabel
        {
            Text = "OpenControls MonoGame Examples",
            Color = UiColor.White,
            Scale = FontScale
        };

        _helpLabel = new UiLabel
        {
            Text = "Drag tabs to reorder; drag outside to float; use targets to dock/split; drag windows by title bar.",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _statusLabel = new UiLabel
        {
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _serializationPanel = new UiPanel
        {
            Id = "panel-serialization",
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100)
        };

        _serializationLabel = new UiLabel
        {
            Text = "UI State: Press F5 to save, F9 to load.",
            Color = UiColor.White,
            Scale = FontScale
        };
        _serializationPanel.AddChild(_serializationLabel);

        _button = new UiButton
        {
            Text = "Click Me",
            TextScale = FontScale
        };
        _button.Clicked += () => _buttonClicks++;

        _textField = new UiTextField
        {
            Id = "text-field",
            TextScale = FontScale,
            MaxLength = 24,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _widgetsTitleLabel = new UiLabel
        {
            Text = "Widget Sampler",
            Color = UiColor.White,
            Scale = FontScale
        };

        _snapCheckbox = new UiCheckbox
        {
            Text = "Snap to Grid",
            TextScale = FontScale,
            Checked = true
        };

        _gizmoCheckbox = new UiCheckbox
        {
            Text = "Show Gizmos",
            TextScale = FontScale,
            Checked = true
        };

        _widgetsSeparator = new UiSeparator
        {
            Color = new UiColor(70, 80, 100),
            Thickness = 2
        };

        _qualityLow = new UiRadioButton
        {
            Text = "Quality: Low",
            TextScale = FontScale,
            GroupId = "quality"
        };

        _qualityMedium = new UiRadioButton
        {
            Text = "Quality: Medium",
            TextScale = FontScale,
            GroupId = "quality",
            Checked = true
        };

        _qualityHigh = new UiRadioButton
        {
            Text = "Quality: High",
            TextScale = FontScale,
            GroupId = "quality"
        };

        _volumeSlider = new UiSlider
        {
            Min = 0f,
            Max = 1f,
            Value = 0.4f,
            TextScale = FontScale,
            ValueFormat = "0.00",
            ShowValue = true
        };

        _volumeProgress = new UiProgressBar
        {
            Min = 0f,
            Max = 1f,
            Value = _volumeSlider.Value,
            TextScale = FontScale
        };

        _roundingLabel = new UiLabel
        {
            Text = "Corner Radius",
            Color = UiColor.White,
            Scale = FontScale
        };

        _roundingSlider = new UiSlider
        {
            Min = 0f,
            Max = 12f,
            Value = 4f,
            TextScale = FontScale,
            WholeNumbers = true,
            ShowValue = true
        };

        _roundingPreviewLabel = new UiLabel
        {
            Text = "Preview Controls",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _roundingButton = new UiButton
        {
            Text = "Rounded Button",
            TextScale = FontScale
        };

        _roundingField = new UiTextField
        {
            Text = "Rounded Field",
            TextScale = FontScale,
            MaxLength = 24,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _roundingPanel = new UiPanel
        {
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100),
            BorderThickness = 1
        };

        _dragFloatLabel = new UiLabel
        {
            Text = "Drag Float",
            Color = UiColor.White,
            Scale = FontScale
        };

        _dragFloat = new UiDragFloat
        {
            Min = 0.01f,
            Max = 1f,
            Value = 0.25f,
            Speed = 0.005f,
            ValueFormat = "0.00",
            TextScale = FontScale
        };

        _dragIntLabel = new UiLabel
        {
            Text = "Drag Int",
            Color = UiColor.White,
            Scale = FontScale
        };

        _dragInt = new UiDragInt
        {
            Min = 1,
            Max = 100,
            Value = 42,
            Speed = 1f,
            TextScale = FontScale
        };

        _dragRangeLabel = new UiLabel
        {
            Text = "Drag Range",
            Color = UiColor.White,
            Scale = FontScale
        };

        _dragFloatRange = new UiDragFloatRange
        {
            Min = 0.01f,
            Max = 1f,
            ValueMin = 0.2f,
            ValueMax = 0.8f,
            Speed = 0.005f,
            ValueFormat = "0.00",
            TextScale = FontScale
        };

        _dragIntRange = new UiDragIntRange
        {
            Min = 1,
            Max = 100,
            ValueMin = 20,
            ValueMax = 80,
            Speed = 1f,
            TextScale = FontScale
        };

        _dragVectorLabel = new UiLabel
        {
            Text = "Drag Float Vectors",
            Color = UiColor.White,
            Scale = FontScale
        };

        _dragFloat2 = new UiDragFloat2
        {
            Min = 0.01f,
            Max = 1f,
            ValueX = 0.25f,
            ValueY = 0.5f,
            Speed = 0.005f,
            ValueFormat = "0.00",
            TextScale = FontScale
        };

        _dragFloat3 = new UiDragFloat3
        {
            Min = 0.01f,
            Max = 1f,
            ValueX = 0.15f,
            ValueY = 0.6f,
            ValueZ = 0.9f,
            Speed = 0.005f,
            ValueFormat = "0.00",
            TextScale = FontScale
        };

        _dragFloat4 = new UiDragFloat4
        {
            Min = 0.01f,
            Max = 1f,
            ValueX = 0.2f,
            ValueY = 0.4f,
            ValueZ = 0.6f,
            ValueW = 0.8f,
            Speed = 0.005f,
            ValueFormat = "0.00",
            TextScale = FontScale
        };

        _dragVectorIntLabel = new UiLabel
        {
            Text = "Drag Int Vectors",
            Color = UiColor.White,
            Scale = FontScale
        };

        _dragInt2 = new UiDragInt2
        {
            Min = 1,
            Max = 100,
            ValueX = 12,
            ValueY = 24,
            Speed = 1f,
            TextScale = FontScale
        };

        _dragInt3 = new UiDragInt3
        {
            Min = 1,
            Max = 100,
            ValueX = 5,
            ValueY = 15,
            ValueZ = 30,
            Speed = 1f,
            TextScale = FontScale
        };

        _dragInt4 = new UiDragInt4
        {
            Min = 1,
            Max = 100,
            ValueX = 8,
            ValueY = 16,
            ValueZ = 32,
            ValueW = 64,
            Speed = 1f,
            TextScale = FontScale
        };

        _dragFlagsLabel = new UiLabel
        {
            Text = "Drag Flags",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _dragClampCheckbox = new UiCheckbox
        {
            Text = "Clamp to Range",
            TextScale = FontScale,
            Checked = true
        };
        _dragClampCheckbox.CheckedChanged += _ => UpdateDragFlags();

        _dragNoSlowFastCheckbox = new UiCheckbox
        {
            Text = "Disable Ctrl and Shift Speed",
            TextScale = FontScale
        };
        _dragNoSlowFastCheckbox.CheckedChanged += _ => UpdateDragFlags();

        _dragLogCheckbox = new UiCheckbox
        {
            Text = "Logarithmic Scaling (Min > 0)",
            TextScale = FontScale
        };
        _dragLogCheckbox.CheckedChanged += _ => UpdateDragFlags();

        _dragHintLabel = new UiLabel
        {
            Text = "Ctrl slows, Shift speeds. Log uses exponential scaling.",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };
        UpdateDragFlags();

        _sceneLabel = new UiLabel
        {
            Text = "Scenes",
            Color = UiColor.White,
            Scale = FontScale
        };

        _sceneList = new UiListBox
        {
            Items = _sceneItems,
            TextScale = FontScale,
            SelectedIndex = 0
        };

        _sceneSelectionLabel = new UiLabel
        {
            Text = "List: Intro Scene",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _multiSelectModel = new UiSelectionModel();
        _multiSelectModel.SetSelected(1, true);
        _multiSelectModel.SetSelected(3, true);

        _multiSelectLabel = new UiLabel
        {
            Text = "Multi-Select Scenes",
            Color = UiColor.White,
            Scale = FontScale
        };

        _multiSelectHintLabel = new UiLabel
        {
            Text = "Ctrl/Shift to multi-select",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _multiSelectList = new UiListBox
        {
            Items = _sceneItems,
            TextScale = FontScale,
            SelectionModel = _multiSelectModel,
            AllowDeselect = true
        };

        _multiSelectSelectionLabel = new UiLabel
        {
            Text = "Multi: Town Square, Courtyard",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _comboLabel = new UiLabel
        {
            Text = "Scene Combo",
            Color = UiColor.White,
            Scale = FontScale
        };

        _sceneComboBox = new UiComboBox
        {
            Items = _sceneItems,
            TextScale = FontScale,
            SelectedIndex = 0,
            Placeholder = "Select scene",
            MaxVisibleItems = 4
        };

        _comboSelectionLabel = new UiLabel
        {
            Text = "Combo: Intro Scene",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tableLabel = new UiLabel
        {
            Text = "Scene Table",
            Color = UiColor.White,
            Scale = FontScale
        };

        _sceneTable = new UiTable
        {
            TextScale = FontScale,
            HeaderTextScale = FontScale,
            ShowHeader = true,
            ShowGrid = true,
            AlternatingRowBackgrounds = true
        };
        _sceneTable.Columns.Add(new UiTableColumn("Scene", weight: 2f));
        _sceneTable.Columns.Add(new UiTableColumn("Status", weight: 1f));
        _sceneTable.Columns.Add(new UiTableColumn("Size", weight: 1f));

        _tableRows.Add(new UiTableRow("Intro Scene", "Loaded", "320x180"));
        _tableRows.Add(new UiTableRow("Town Square", "Unloaded", "640x360"));
        _tableRows.Add(new UiTableRow("Workshop", "Loaded", "640x360"));
        _tableRows.Add(new UiTableRow("Courtyard", "Unloaded", "640x360"));
        _tableRows.Add(new UiTableRow("Library", "Loaded", "640x360"));
        _tableRows.Add(new UiTableRow("Catacombs", "Unloaded", "800x450"));
        _sceneTable.Rows = _tableRows;

        _tableSelectionLabel = new UiLabel
        {
            Text = "Table: None",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _plotLabel = new UiLabel
        {
            Text = "Plotting: Scroll to zoom, drag to pan",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _plotPanel = new UiPlotPanel
        {
            TextScale = FontScale,
            Title = "Scene Performance",
            XAxisLabel = "Time (s)",
            YAxisLabel = "Frame Time (ms)",
            XTickCount = 6,
            YTickCount = 5,
            MeasureTextWidth = (text, scale) => _renderer?.MeasureTextWidth(text, scale) ?? text.Length * 6 * scale,
            MeasureTextHeight = scale => _renderer?.MeasureTextHeight(scale) ?? 7 * scale
        };

        List<UiPlotPoint> frameSeries = new();
        List<UiPlotPoint> gpuSeries = new();
        for (int i = 0; i <= 120; i++)
        {
            float x = i * 0.1f;
            float frame = 16f + MathF.Sin(x * 0.6f) * 3f + MathF.Cos(x * 0.2f) * 2f;
            float gpu = 12f + MathF.Sin(x * 0.4f + 1f) * 2f;
            frameSeries.Add(new UiPlotPoint(x, frame));
            gpuSeries.Add(new UiPlotPoint(x, gpu));
        }

        _plotPanel.Series.Add(new UiPlotSeries
        {
            Label = "Frame",
            LineColor = new UiColor(120, 180, 220),
            LineThickness = 2,
            Points = frameSeries
        });
        _plotPanel.Series.Add(new UiPlotSeries
        {
            Label = "GPU",
            LineColor = new UiColor(220, 140, 90),
            LineThickness = 2,
            Points = gpuSeries
        });
        _plotPanel.AutoFit();

        _colorEditLabel = new UiLabel
        {
            Text = "Color Edit",
            Color = UiColor.White,
            Scale = FontScale
        };

        _colorEdit = new UiColorEdit
        {
            TextScale = FontScale,
            Color = _sharedColor,
            ShowAlpha = true
        };
        _colorEdit.ColorChanged += color => SyncColorFromEdit(color);

        _colorPickerLabel = new UiLabel
        {
            Text = "Color Picker",
            Color = UiColor.White,
            Scale = FontScale
        };

        _colorPicker = new UiColorPicker
        {
            Color = _sharedColor,
            HueBarWidth = 14,
            ShowAlpha = true
        };
        _colorPicker.ColorChanged += color => SyncColorFromPicker(color);

        _colorSelectionLabel = new UiLabel
        {
            Text = $"Color: {FormatHex(_sharedColor, true)}",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _colorButtonLabel = new UiLabel
        {
            Text = "Color Buttons: None",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        UiColorButton primaryButton = new UiColorButton { Color = new UiColor(72, 140, 220), ShowAlpha = false };
        primaryButton.Clicked += () => _colorButtonLabel.Text = "Color Buttons: Primary";
        _colorButtons.Add(primaryButton);

        UiColorButton accentButton = new UiColorButton { Color = new UiColor(220, 120, 90), ShowAlpha = false };
        accentButton.Clicked += () => _colorButtonLabel.Text = "Color Buttons: Accent";
        _colorButtons.Add(accentButton);

        UiColorButton neutralButton = new UiColorButton { Color = new UiColor(120, 160, 120), ShowAlpha = false };
        neutralButton.Clicked += () => _colorButtonLabel.Text = "Color Buttons: Neutral";
        _colorButtons.Add(neutralButton);

        UiColorButton alphaButton = new UiColorButton { Color = new UiColor(140, 200, 240, 120), ShowAlpha = true };
        alphaButton.Clicked += () => _colorButtonLabel.Text = "Color Buttons: Alpha";
        _colorButtons.Add(alphaButton);

        _asciiLabel = new UiLabel
        {
            Text = "Codes and Characters",
            Color = UiColor.White,
            Scale = FontScale
        };

        _asciiPageLabel = new UiLabel
        {
            Text = "Code Page",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _asciiPageCombo = new UiComboBox
        {
            Items = _asciiPageItems,
            TextScale = FontScale,
            SelectedIndex = 0,
            MaxVisibleItems = 2
        };
        _asciiPageCombo.SelectionChanged += index => SetAsciiPageIndex(index);

        _asciiTable = new UiTable
        {
            TextScale = FontScale,
            HeaderTextScale = FontScale,
            ShowHeader = true,
            ShowGrid = true,
            AlternatingRowBackgrounds = true
        };
        _asciiTable.Columns.Add(new UiTableColumn("Dec", weight: 1f));
        _asciiTable.Columns.Add(new UiTableColumn("Hex", weight: 1f));
        _asciiTable.Columns.Add(new UiTableColumn("Char", weight: 1f));
        SetAsciiPageIndex(0);

        _selectablesLabel = new UiLabel
        {
            Text = "Selectable Filters",
            Color = UiColor.White,
            Scale = FontScale
        };

        _selectablesHintLabel = new UiLabel
        {
            Text = "Ctrl/Shift to multi-select",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _selectableSelection = new UiSelectionModel();
        _selectableSelection.SetSelected(0, true);
        _selectableSelection.SetSelected(2, true);

        _selectableLighting = new UiSelectable
        {
            Text = "Lighting",
            TextScale = FontScale,
            SelectionModel = _selectableSelection,
            SelectionIndex = 0
        };

        _selectableNavigation = new UiSelectable
        {
            Text = "Navigation",
            TextScale = FontScale,
            SelectionModel = _selectableSelection,
            SelectionIndex = 1
        };

        _selectableAudio = new UiSelectable
        {
            Text = "Audio",
            TextScale = FontScale,
            SelectionModel = _selectableSelection,
            SelectionIndex = 2
        };

        _scrollPanelLabel = new UiLabel
        {
            Text = "Scroll Panel",
            Color = UiColor.White,
            Scale = FontScale
        };

        _gridLabel = new UiLabel
        {
            Text = "Grid Layout",
            Color = UiColor.White,
            Scale = FontScale
        };

        _canvasLabel = new UiLabel
        {
            Text = "Canvas: drag to pan, scroll to zoom",
            Color = UiColor.White,
            Scale = FontScale
        };

        _canvas = new UiCanvas
        {
            PanX = -80f,
            PanY = -60f,
            Zoom = 1f,
            GridSpacing = 32f,
            MajorGridSpacing = 128f,
            ShowGrid = true,
            ShowOrigin = true,
            OriginColor = new UiColor(200, 180, 120),
            Background = new UiColor(16, 20, 30),
            Border = new UiColor(60, 70, 90)
        };

        _canvasNodeA = new UiButton
        {
            Text = "Node A",
            TextScale = FontScale,
            Bounds = new UiRect(-40, -10, 120, 28)
        };

        _canvasNodeB = new UiButton
        {
            Text = "Node B",
            TextScale = FontScale,
            Bounds = new UiRect(120, 60, 120, 28)
        };

        _canvasNodeC = new UiButton
        {
            Text = "Backdrop",
            TextScale = FontScale,
            Bounds = new UiRect(-140, 100, 160, 28)
        };

        _canvas.AddChild(_canvasNodeA);
        _canvas.AddChild(_canvasNodeB);
        _canvas.AddChild(_canvasNodeC);

        _gridPrimaryButton = new UiButton
        {
            Text = "Primary",
            TextScale = FontScale
        };

        _gridSecondaryButton = new UiButton
        {
            Text = "Secondary",
            TextScale = FontScale
        };

        _gridInfoLabel = new UiLabel
        {
            Text = "Cell: Info",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _gridStatusLabel = new UiLabel
        {
            Text = "Cell: Status",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _grid = new UiGrid
        {
            Padding = 4,
            ColumnSpacing = 6,
            RowSpacing = 6,
            CellPadding = 4,
            ShowGridLines = true,
            Background = new UiColor(16, 20, 30),
            Border = new UiColor(60, 70, 90),
            GridLineColor = new UiColor(40, 50, 70)
        };
        _grid.SetColumnCount(2);
        _grid.SetRowCount(2);
        _grid.AddChild(_gridPrimaryButton, 0, 0);
        _grid.AddChild(_gridSecondaryButton, 0, 1);
        _grid.AddChild(_gridInfoLabel, 1, 0);
        _grid.AddChild(_gridStatusLabel, 1, 1);

        string[] scrollItems =
        {
            "Entry 01: Boot sequence complete",
            "Entry 02: Asset scan complete",
            "Entry 03: Dock workspace ready",
            "Entry 04: Shader warmup finished",
            "Entry 05: Scene graph rebuilt",
            "Entry 06: Audio mixer online",
            "Entry 07: Navigation mesh baked",
            "Entry 08: Lighting probes updated"
        };

        foreach (string itemText in scrollItems)
        {
            UiLabel item = new UiLabel
            {
                Text = itemText,
                Color = new UiColor(200, 210, 230),
                Scale = FontScale
            };
            _scrollPanelItems.Add(item);
        }

        _popupButton = new UiButton
        {
            Text = "Open Popup",
            TextScale = FontScale
        };

        _modalButton = new UiButton
        {
            Text = "Open Modal",
            TextScale = FontScale
        };

        _menuPopupLabel = new UiLabel
        {
            Text = "Popup Menu",
            Color = UiColor.White,
            Scale = FontScale
        };

        _menuPopupButton = new UiButton
        {
            Text = "Open Menu",
            TextScale = FontScale
        };

        _menuPopupStatus = new UiLabel
        {
            Text = "Menu: None",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _menuPopupTable = new UiTable
        {
            TextScale = FontScale,
            HeaderTextScale = FontScale,
            ShowHeader = true,
            ShowGrid = true,
            AlternatingRowBackgrounds = true
        };
        _menuPopupTable.Columns.Add(new UiTableColumn("Action", weight: 2f));
        _menuPopupTable.Columns.Add(new UiTableColumn("Key", weight: 1f));
        _menuPopupTable.Rows = new[]
        {
            new UiTableRow("Quick Save", "F5"),
            new UiTableRow("Quick Load", "F9"),
            new UiTableRow("Screenshot", "F12")
        };
        _menuPopupTable.SelectionChanged += index =>
        {
            if (_menuPopupStatus == null || _menuPopupTable == null)
            {
                return;
            }

            if (index < 0 || index >= _menuPopupTable.Rows.Count)
            {
                _menuPopupStatus.Text = "Menu: None";
                return;
            }

            IReadOnlyList<string> cells = _menuPopupTable.Rows[index].Cells;
            string label = cells.Count > 0 ? cells[0] : "Action";
            _menuPopupStatus.Text = $"Menu: {label}";
        };

        _menuPopup = new UiMenuBar
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            TextScale = FontScale,
            DropdownMinWidth = 180,
            MeasureTextWidth = (text, scale) => _renderer?.MeasureTextWidth(text, scale) ?? text.Length * 6 * scale,
            MeasureTextHeight = scale => _renderer?.MeasureTextHeight(scale) ?? 7 * scale
        };

        Action<UiMenuBar.MenuItem> popupStatus = item =>
        {
            if (_menuPopupStatus != null)
            {
                _menuPopupStatus.Text = $"Menu: {item.Text}";
            }
        };

        UiMenuBar.MenuItem popupNew = new() { Text = "New", Clicked = popupStatus };
        UiMenuBar.MenuItem popupOpen = new() { Text = "Open", Clicked = popupStatus };
        UiMenuBar.MenuItem popupSave = new() { Text = "Save", Shortcut = "Ctrl+S", Clicked = popupStatus };
        UiMenuBar.MenuItem popupPin = new() { Text = "Pin to Toolbar", IsCheckable = true, Checked = true };
        popupPin.Clicked = item =>
        {
            if (_menuPopupStatus != null)
            {
                _menuPopupStatus.Text = $"Menu: {item.Text} {(item.Checked ? "On" : "Off")}";
            }
        };

        UiMenuBar.MenuItem popupRecent = new() { Text = "Recent" };
        popupRecent.Items.Add(new UiMenuBar.MenuItem { Text = "Intro Scene", Clicked = popupStatus });
        popupRecent.Items.Add(new UiMenuBar.MenuItem { Text = "Town Square", Clicked = popupStatus });
        popupRecent.Items.Add(new UiMenuBar.MenuItem { Text = "Courtyard", Clicked = popupStatus });

        _menuPopupContentItem = new UiMenuBar.MenuItem
        {
            Content = _menuPopupTable,
            ContentWidth = 200,
            ContentHeight = 120,
            ContentPadding = 4
        };

        _menuPopup.Items.Add(popupNew);
        _menuPopup.Items.Add(popupOpen);
        _menuPopup.Items.Add(popupSave);
        _menuPopup.Items.Add(UiMenuBar.MenuItem.Separator());
        _menuPopup.Items.Add(popupPin);
        _menuPopup.Items.Add(popupRecent);
        _menuPopup.Items.Add(UiMenuBar.MenuItem.Separator());
        _menuPopup.Items.Add(_menuPopupContentItem);

        _tooltipLabel = new UiLabel
        {
            Text = "Hover for tooltip",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tooltip = new UiTooltip
        {
            TextScale = FontScale,
            Background = new UiColor(20, 24, 34),
            Border = new UiColor(70, 80, 100),
            TextColor = UiColor.White
        };

        _tooltipRegion = new UiTooltipRegion
        {
            Text = "Tooltip: Overlay hints can live anywhere.",
            Tooltip = _tooltip
        };

        _popup = new UiPopup
        {
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100)
        };

        _popupLabel = new UiLabel
        {
            Text = "Popup content",
            Color = UiColor.White,
            Scale = FontScale
        };

        _popupCloseButton = new UiButton
        {
            Text = "Close",
            TextScale = FontScale
        };
        _popupCloseButton.Clicked += () => _popup?.Close();

        _popup.AddChild(_popupLabel);
        _popup.AddChild(_popupCloseButton);

        _modal = new UiModal
        {
            Background = new UiColor(24, 28, 38),
            Border = new UiColor(90, 100, 120),
            Backdrop = new UiColor(0, 0, 0, 160)
        };

        _modalLabel = new UiLabel
        {
            Text = "Modal dialog",
            Color = UiColor.White,
            Scale = FontScale
        };

        _modalCloseButton = new UiButton
        {
            Text = "Close",
            TextScale = FontScale
        };
        _modalCloseButton.Clicked += () => _modal?.Close();

        _modal.AddChild(_modalLabel);
        _modal.AddChild(_modalCloseButton);

        _popupButton.Clicked += () => _popup?.Open();
        _modalButton.Clicked += () => _modal?.Open();
        _menuPopupButton.Clicked += () => _menuPopup?.TogglePopup();

        _hierarchyLabel = new UiLabel
        {
            Text = "Hierarchy",
            Color = UiColor.White,
            Scale = FontScale
        };

        _treeNode = new UiTreeNode
        {
            Text = "Scene Root",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4
        };

        string[] treeItems =
        {
            "Camera",
            "Lighting",
            "Gameplay"
        };

        foreach (string treeItem in treeItems)
        {
            UiLabel label = new UiLabel
            {
                Text = treeItem,
                Color = new UiColor(200, 210, 230),
                Scale = FontScale
            };
            _treeNode.AddChild(label);
            _treeNodeItems.Add(label);
        }

        _collapsingHeader = new UiCollapsingHeader
        {
            Text = "Advanced Settings",
            TextScale = FontScale,
            ContentPadding = 4
        };

        string[] collapsingItems =
        {
            "Baked Lighting",
            "Static Colliders"
        };

        foreach (string itemText in collapsingItems)
        {
            UiLabel label = new UiLabel
            {
                Text = itemText,
                Color = new UiColor(200, 210, 230),
                Scale = FontScale
            };
            _collapsingHeader.AddChild(label);
            _collapsingItems.Add(label);
        }

        _widgetsTree = new UiTreeNode
        {
            Text = "Widgets",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsBasicTree = new UiTreeNode
        {
            Text = "Basic",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsSliderTree = new UiTreeNode
        {
            Text = "Sliders and Progress Bars",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsStyleTree = new UiTreeNode
        {
            Text = "Style and Rounding",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsDragTree = new UiTreeNode
        {
            Text = "Drag Widgets",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsListTree = new UiTreeNode
        {
            Text = "List Boxes",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsMultiSelectTree = new UiTreeNode
        {
            Text = "Selection State and Multi-Select",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsComboTree = new UiTreeNode
        {
            Text = "Combo",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTableTree = new UiTreeNode
        {
            Text = "Tables and Columns",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsPlotTree = new UiTreeNode
        {
            Text = "Plotting",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsColorTree = new UiTreeNode
        {
            Text = "Color and Picker Widgets",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsAsciiTree = new UiTreeNode
        {
            Text = "ASCII and Code Pages",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsSelectableTree = new UiTreeNode
        {
            Text = "Selectables",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsLayoutTree = new UiTreeNode
        {
            Text = "Layout and Scrolling",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTooltipTree = new UiTreeNode
        {
            Text = "Tooltips",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsPopupTree = new UiTreeNode
        {
            Text = "Popups and Modal Windows",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTreeHeaderTree = new UiTreeNode
        {
            Text = "Tree Nodes and Collapsing Headers",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsBasicTree.AddChild(_snapCheckbox);
        _widgetsBasicTree.AddChild(_gizmoCheckbox);
        _widgetsBasicTree.AddChild(_widgetsSeparator);
        _widgetsBasicTree.AddChild(_qualityLow);
        _widgetsBasicTree.AddChild(_qualityMedium);
        _widgetsBasicTree.AddChild(_qualityHigh);

        _widgetsSliderTree.AddChild(_volumeSlider);
        _widgetsSliderTree.AddChild(_volumeProgress);

        _widgetsStyleTree.AddChild(_roundingLabel);
        _widgetsStyleTree.AddChild(_roundingSlider);
        _widgetsStyleTree.AddChild(_roundingPreviewLabel);
        _widgetsStyleTree.AddChild(_roundingButton);
        _widgetsStyleTree.AddChild(_roundingField);
        _widgetsStyleTree.AddChild(_roundingPanel);

        _widgetsDragTree.AddChild(_dragFloatLabel);
        _widgetsDragTree.AddChild(_dragFloat);
        _widgetsDragTree.AddChild(_dragIntLabel);
        _widgetsDragTree.AddChild(_dragInt);
        _widgetsDragTree.AddChild(_dragRangeLabel);
        _widgetsDragTree.AddChild(_dragFloatRange);
        _widgetsDragTree.AddChild(_dragIntRange);
        _widgetsDragTree.AddChild(_dragVectorLabel);
        _widgetsDragTree.AddChild(_dragFloat2);
        _widgetsDragTree.AddChild(_dragFloat3);
        _widgetsDragTree.AddChild(_dragFloat4);
        _widgetsDragTree.AddChild(_dragVectorIntLabel);
        _widgetsDragTree.AddChild(_dragInt2);
        _widgetsDragTree.AddChild(_dragInt3);
        _widgetsDragTree.AddChild(_dragInt4);
        _widgetsDragTree.AddChild(_dragFlagsLabel);
        _widgetsDragTree.AddChild(_dragClampCheckbox);
        _widgetsDragTree.AddChild(_dragNoSlowFastCheckbox);
        _widgetsDragTree.AddChild(_dragLogCheckbox);
        _widgetsDragTree.AddChild(_dragHintLabel);

        _widgetsListTree.AddChild(_sceneLabel);
        _widgetsListTree.AddChild(_sceneList);
        _widgetsListTree.AddChild(_sceneSelectionLabel);

        _widgetsMultiSelectTree.AddChild(_multiSelectLabel);
        _widgetsMultiSelectTree.AddChild(_multiSelectHintLabel);
        _widgetsMultiSelectTree.AddChild(_multiSelectList);
        _widgetsMultiSelectTree.AddChild(_multiSelectSelectionLabel);

        _widgetsComboTree.AddChild(_comboLabel);
        _widgetsComboTree.AddChild(_sceneComboBox);
        _widgetsComboTree.AddChild(_comboSelectionLabel);

        _widgetsTableTree.AddChild(_tableLabel);
        _widgetsTableTree.AddChild(_sceneTable);
        _widgetsTableTree.AddChild(_tableSelectionLabel);

        _widgetsPlotTree.AddChild(_plotLabel);
        _widgetsPlotTree.AddChild(_plotPanel);

        _widgetsColorTree.AddChild(_colorEditLabel);
        _widgetsColorTree.AddChild(_colorEdit);
        _widgetsColorTree.AddChild(_colorPickerLabel);
        _widgetsColorTree.AddChild(_colorPicker);
        _widgetsColorTree.AddChild(_colorSelectionLabel);
        _widgetsColorTree.AddChild(_colorButtonLabel);
        foreach (UiColorButton button in _colorButtons)
        {
            _widgetsColorTree.AddChild(button);
        }

        _widgetsAsciiTree.AddChild(_asciiLabel);
        _widgetsAsciiTree.AddChild(_asciiPageLabel);
        _widgetsAsciiTree.AddChild(_asciiPageCombo);
        _widgetsAsciiTree.AddChild(_asciiTable);

        _widgetsSelectableTree.AddChild(_selectablesLabel);
        _widgetsSelectableTree.AddChild(_selectablesHintLabel);
        _widgetsSelectableTree.AddChild(_selectableLighting);
        _widgetsSelectableTree.AddChild(_selectableNavigation);
        _widgetsSelectableTree.AddChild(_selectableAudio);

        _widgetsLayoutTree.AddChild(_canvasLabel);
        _widgetsLayoutTree.AddChild(_canvas);
        _widgetsLayoutTree.AddChild(_gridLabel);
        _widgetsLayoutTree.AddChild(_grid);
        _widgetsLayoutTree.AddChild(_scrollPanelLabel);
        foreach (UiLabel item in _scrollPanelItems)
        {
            _widgetsLayoutTree.AddChild(item);
        }

        _widgetsTooltipTree.AddChild(_tooltipLabel);
        _widgetsTooltipTree.AddChild(_tooltipRegion);

        _widgetsPopupTree.AddChild(_menuPopupLabel);
        _widgetsPopupTree.AddChild(_menuPopupButton);
        _widgetsPopupTree.AddChild(_menuPopupStatus);
        _widgetsPopupTree.AddChild(_menuPopup);
        _widgetsPopupTree.AddChild(_popupButton);
        _widgetsPopupTree.AddChild(_modalButton);

        _widgetsTreeHeaderTree.AddChild(_hierarchyLabel);
        _widgetsTreeHeaderTree.AddChild(_treeNode);
        _widgetsTreeHeaderTree.AddChild(_collapsingHeader);

        _widgetsTree.AddChild(_widgetsBasicTree);
        _widgetsTree.AddChild(_widgetsSliderTree);
        _widgetsTree.AddChild(_widgetsStyleTree);
        _widgetsTree.AddChild(_widgetsDragTree);
        _widgetsTree.AddChild(_widgetsListTree);
        _widgetsTree.AddChild(_widgetsMultiSelectTree);
        _widgetsTree.AddChild(_widgetsComboTree);
        _widgetsTree.AddChild(_widgetsTableTree);
        _widgetsTree.AddChild(_widgetsPlotTree);
        _widgetsTree.AddChild(_widgetsColorTree);
        _widgetsTree.AddChild(_widgetsAsciiTree);
        _widgetsTree.AddChild(_widgetsSelectableTree);
        _widgetsTree.AddChild(_widgetsLayoutTree);
        _widgetsTree.AddChild(_widgetsTooltipTree);
        _widgetsTree.AddChild(_widgetsPopupTree);
        _widgetsTree.AddChild(_widgetsTreeHeaderTree);

        _menuBar = new UiMenuBar
        {
            TextScale = FontScale,
            BarHeight = 24,
            MeasureTextWidth = (text, scale) => _renderer?.MeasureTextWidth(text, scale) ?? text.Length * 6 * scale,
            MeasureTextHeight = scale => _renderer?.MeasureTextHeight(scale) ?? 7 * scale
        };

        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "New", Shortcut = "Ctrl+N", Clicked = item => _menuStatus = $"Menu: {item.Text}" });
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Open", Shortcut = "Ctrl+O", Clicked = item => _menuStatus = $"Menu: {item.Text}" });
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Save", Shortcut = "Ctrl+S", Clicked = item => _menuStatus = $"Menu: {item.Text}" });
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Save As", Shortcut = "Ctrl+Shift+S", Clicked = item => _menuStatus = $"Menu: {item.Text}" });
        fileMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Exit", Clicked = _ => Exit() });

        UiMenuBar.MenuItem viewMenu = new() { Text = "View" };
        UiMenuBar.MenuItem showHelp = new() { Text = "Show Help", IsCheckable = true, Checked = true };
        showHelp.Clicked = item =>
        {
            if (_helpLabel != null)
            {
                _helpLabel.Visible = item.Checked;
            }

            _menuStatus = $"Menu: {item.Text} {(item.Checked ? "On" : "Off")}";
        };

        UiMenuBar.MenuItem showStatus = new() { Text = "Show Status Bar", IsCheckable = true, Checked = true };
        showStatus.Clicked = item =>
        {
            if (_statusLabel != null)
            {
                _statusLabel.Visible = item.Checked;
            }

            _menuStatus = $"Menu: {item.Text} {(item.Checked ? "On" : "Off")}";
        };

        UiMenuBar.MenuItem themeMenu = new() { Text = "Theme" };
        UiMenuBar.MenuItem themeDark = new() { Text = "Dark", IsCheckable = true, Checked = true };
        UiMenuBar.MenuItem themeLight = new() { Text = "Light", IsCheckable = true };
        themeDark.Clicked = _ =>
        {
            themeDark.Checked = true;
            themeLight.Checked = false;
            if (_rootPanel != null)
            {
                _rootPanel.Background = new UiColor(12, 14, 20);
            }

            _menuStatus = "Menu: Theme Dark";
        };
        themeLight.Clicked = _ =>
        {
            themeLight.Checked = true;
            themeDark.Checked = false;
            if (_rootPanel != null)
            {
                _rootPanel.Background = new UiColor(24, 26, 32);
            }

            _menuStatus = "Menu: Theme Light";
        };
        themeMenu.Items.Add(themeDark);
        themeMenu.Items.Add(themeLight);

        viewMenu.Items.Add(showHelp);
        viewMenu.Items.Add(showStatus);
        viewMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        viewMenu.Items.Add(themeMenu);

        UiMenuBar.MenuItem helpMenu = new() { Text = "Help" };
        helpMenu.Items.Add(new UiMenuBar.MenuItem { Text = "About", Clicked = item => _menuStatus = $"Menu: {item.Text}" });

        UiMenuBar.MenuItem examplesMenu = new() { Text = "Examples" };
        _examplesMenuAllItem = new UiMenuBar.MenuItem { Text = "All", IsCheckable = true, Checked = true };
        _examplesMenuBasicsItem = new UiMenuBar.MenuItem { Text = "Basics", IsCheckable = true };
        _examplesMenuWidgetsItem = new UiMenuBar.MenuItem { Text = "Widgets", IsCheckable = true };
        _examplesMenuDockingItem = new UiMenuBar.MenuItem { Text = "Docking", IsCheckable = true };
        _examplesMenuClippingItem = new UiMenuBar.MenuItem { Text = "Clipping", IsCheckable = true };
        _examplesMenuSerializationItem = new UiMenuBar.MenuItem { Text = "Serialization", IsCheckable = true };

        _examplesMenuAllItem.Clicked = _ => SetActiveExample(ExamplePanel.All);
        _examplesMenuBasicsItem.Clicked = _ => SetActiveExample(ExamplePanel.Basics);
        _examplesMenuWidgetsItem.Clicked = _ => SetActiveExample(ExamplePanel.Widgets);
        _examplesMenuDockingItem.Clicked = _ => SetActiveExample(ExamplePanel.Docking);
        _examplesMenuClippingItem.Clicked = _ => SetActiveExample(ExamplePanel.Clipping);
        _examplesMenuSerializationItem.Clicked = _ => SetActiveExample(ExamplePanel.Serialization);

        examplesMenu.Items.Add(_examplesMenuAllItem);
        examplesMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        examplesMenu.Items.Add(_examplesMenuBasicsItem);
        examplesMenu.Items.Add(_examplesMenuWidgetsItem);
        examplesMenu.Items.Add(_examplesMenuDockingItem);
        examplesMenu.Items.Add(_examplesMenuClippingItem);
        examplesMenu.Items.Add(_examplesMenuSerializationItem);

        _menuBar.Items.Add(fileMenu);
        _menuBar.Items.Add(viewMenu);
        _menuBar.Items.Add(examplesMenu);
        _menuBar.Items.Add(helpMenu);

        _clipPanel = new UiPanel
        {
            Id = "clip-panel",
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100),
            AllowResize = true,
            ShowResizeGrip = true
        };

        _clipLabel = new UiLabel
        {
            Text = "Clipped text: The quick brown fox jumps over the lazy dog.",
            Color = UiColor.White,
            Scale = FontScale
        };
        _clipPanel.AddChild(_clipLabel);

        _basicsWindow = new UiWindow
        {
            Id = "window-basics",
            Title = "Basics",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        _basicsWindow.AddChild(_titleLabel);
        _basicsWindow.AddChild(_helpLabel);
        _basicsWindow.AddChild(_button);
        _basicsWindow.AddChild(_textField);

        _widgetsWindow = new UiWindow
        {
            Id = "window-widgets",
            Title = "Widgets",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        _scrollPanel = _widgetsWindow.EnsureScrollPanel();
        _scrollPanel.HorizontalScrollbar = UiScrollbarVisibility.Always;
        _scrollPanel.VerticalScrollbar = UiScrollbarVisibility.Always;

        _widgetsWindow.AddContentChild(_widgetsTitleLabel);
        _widgetsWindow.AddContentChild(_widgetsTree);

        _clippingWindow = new UiWindow
        {
            Id = "window-clipping",
            Title = "Clipping",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        _clippingWindow.AddChild(_clipPanel);

        _serializationWindow = new UiWindow
        {
            Id = "window-serialization",
            Title = "Serialization",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        _serializationWindow.AddChild(_serializationPanel);

        _standaloneWindow = new UiWindow
        {
            Id = "window-standalone",
            Title = "Window",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        _standaloneScrollPanel = _standaloneWindow.EnsureScrollPanel();
        _standaloneScrollPanel.HorizontalScrollbar = UiScrollbarVisibility.Auto;
        _standaloneScrollPanel.VerticalScrollbar = UiScrollbarVisibility.Auto;

        string[] standaloneItems =
        {
            "Line 01: Floating log entry with a wider width for horizontal scroll.",
            "Line 02: Another event flowed into the log window.",
            "Line 03: Debug marker reached.",
            "Line 04: Streaming assets complete.",
            "Line 05: Diagnostics updated.",
            "Line 06: Network ready.",
            "Line 07: Script reload completed.",
            "Line 08: Timeline refreshed.",
            "Line 09: Scene travel initiated.",
            "Line 10: Autosave completed."
        };

        foreach (string text in standaloneItems)
        {
            UiLabel item = new UiLabel
            {
                Text = text,
                Color = new UiColor(200, 210, 230),
                Scale = FontScale
            };
            _standaloneScrollPanel.AddChild(item);
            _standaloneScrollItems.Add(item);
        }

        _dockWorkspace = new UiDockWorkspace
        {
            Id = "workspace-main"
        };
        _dockLeft = _dockWorkspace.RootHost;
        _dockLeft.Id = "dock-left";
        _dockLeft.TabWidth = 160;
        _dockLeft.TabTextScale = FontScale;

        _assetsWindow = new UiWindow { Id = "window-assets", Title = "Assets", TitleTextScale = FontScale };
        _consoleWindow = new UiWindow { Id = "window-console", Title = "Console", TitleTextScale = FontScale };
        _inspectorWindow = new UiWindow { Id = "window-inspector", Title = "Inspector", TitleTextScale = FontScale };

        _assetsLabel = new UiLabel { Text = "Assets tab", Color = UiColor.White, Scale = FontScale };
        _consoleLabel = new UiLabel { Text = "Console tab", Color = UiColor.White, Scale = FontScale };
        _inspectorLabel = new UiLabel { Text = "Inspector tab", Color = UiColor.White, Scale = FontScale };

        _assetsWindow.AddChild(_assetsLabel);
        _consoleWindow.AddChild(_consoleLabel);
        _inspectorWindow.AddChild(_inspectorLabel);

        _rootPanel.AddChild(_menuBar);
        _rootPanel.AddChild(_dockWorkspace);
        _rootPanel.AddChild(_statusLabel);

        if (_popup != null)
        {
            _root.AddChild(_popup);
        }

        if (_tooltip != null)
        {
            _root.AddChild(_tooltip);
        }

        if (_modal != null)
        {
            _root.AddChild(_modal);
        }

        _context = new UiContext(_root);
        SetActiveExample(ExamplePanel.Custom);
    }

    private void UpdateLayout(Viewport viewport)
    {
        if (_root == null || _renderer == null || _dockWorkspace == null)
        {
            return;
        }

        int fontHeight = _renderer.MeasureTextHeight(FontScale);

        UiRect rootBounds = new UiRect(0, 0, viewport.Width, viewport.Height);
        _root.Bounds = rootBounds;
        if (_rootPanel != null)
        {
            _rootPanel.Bounds = rootBounds;
        }

        int contentTop = Padding;
        if (_menuBar != null)
        {
            _menuBar.Bounds = new UiRect(0, 0, viewport.Width, _menuBar.BarHeight);
            contentTop = _menuBar.Bounds.Bottom + Padding;
        }

        int statusY = viewport.Height - Padding - fontHeight;
        if (_statusLabel != null)
        {
            _statusLabel.Bounds = new UiRect(Padding, statusY, Math.Max(0, viewport.Width - Padding * 2), fontHeight);
        }

        int dockHeight = Math.Max(0, statusY - contentTop - Padding);
        int dockWidth = Math.Max(0, viewport.Width - Padding * 2);
        _dockWorkspace.Bounds = new UiRect(Padding, contentTop, dockWidth, dockHeight);

        if (_standaloneWindow != null && !_standaloneInitialized)
        {
            UiRect dockBounds = _dockWorkspace.Bounds;
            int windowWidth = Math.Min(280, dockBounds.Width);
            int windowHeight = Math.Min(160, dockBounds.Height);
            int windowX = Math.Max(dockBounds.X, dockBounds.Right - windowWidth - Padding);
            int windowY = dockBounds.Y + Padding;
            _standaloneWindow.Bounds = new UiRect(windowX, windowY, windowWidth, windowHeight);
            _standaloneInitialized = true;
        }
    }

    private void UpdateWindowContent()
    {
        if (_renderer == null)
        {
            return;
        }

        int labelHeight = _renderer.MeasureTextHeight(FontScale);

        if (_basicsWindow != null && _titleLabel != null && _helpLabel != null && _button != null && _textField != null)
        {
            UiRect content = _basicsWindow.ContentBounds;
            int contentWidth = Math.Max(0, content.Width - Padding * 2);
            int x = content.X + Padding;
            int y = content.Y + Padding;

            _titleLabel.Bounds = new UiRect(x, y, contentWidth, labelHeight);
            _helpLabel.Bounds = new UiRect(x, y + labelHeight + 6, contentWidth, labelHeight);

            int buttonWidth = Math.Min(140, contentWidth);
            int fieldWidth = Math.Min(240, contentWidth);
            int buttonY = _helpLabel.Bounds.Bottom + 10;
            _button.Bounds = new UiRect(x, buttonY, Math.Max(0, buttonWidth), 28);
            _textField.Bounds = new UiRect(x, buttonY + 38, Math.Max(0, fieldWidth), 22);
        }

        if (_widgetsWindow != null && _widgetsTitleLabel != null && _widgetsTree != null && _widgetsBasicTree != null && _widgetsSliderTree != null
            && _widgetsStyleTree != null && _widgetsDragTree != null && _widgetsListTree != null && _widgetsMultiSelectTree != null && _widgetsComboTree != null && _widgetsTableTree != null
            && _widgetsPlotTree != null && _widgetsColorTree != null && _widgetsAsciiTree != null && _widgetsSelectableTree != null && _widgetsLayoutTree != null && _widgetsTooltipTree != null
            && _widgetsPopupTree != null && _widgetsTreeHeaderTree != null && _snapCheckbox != null && _gizmoCheckbox != null
            && _widgetsSeparator != null
            && _qualityLow != null && _qualityMedium != null && _qualityHigh != null && _volumeSlider != null && _volumeProgress != null
            && _roundingLabel != null && _roundingSlider != null && _roundingPreviewLabel != null && _roundingButton != null
            && _roundingField != null && _roundingPanel != null
            && _dragFloatLabel != null && _dragFloat != null && _dragIntLabel != null && _dragInt != null && _dragRangeLabel != null
            && _dragFloatRange != null && _dragIntRange != null && _dragVectorLabel != null && _dragFloat2 != null
            && _dragFloat3 != null && _dragFloat4 != null && _dragVectorIntLabel != null && _dragInt2 != null
            && _dragInt3 != null && _dragInt4 != null && _dragFlagsLabel != null && _dragClampCheckbox != null
            && _dragNoSlowFastCheckbox != null && _dragLogCheckbox != null && _dragHintLabel != null
            && _sceneLabel != null && _sceneList != null && _sceneSelectionLabel != null && _multiSelectLabel != null
            && _multiSelectHintLabel != null && _multiSelectList != null && _multiSelectSelectionLabel != null && _comboLabel != null
            && _sceneComboBox != null && _comboSelectionLabel != null && _tableLabel != null && _sceneTable != null
            && _tableSelectionLabel != null && _plotLabel != null && _plotPanel != null && _colorEditLabel != null && _colorEdit != null && _colorPickerLabel != null
            && _colorPicker != null && _colorSelectionLabel != null && _colorButtonLabel != null && _asciiLabel != null && _asciiPageLabel != null && _asciiPageCombo != null
            && _asciiTable != null && _selectablesLabel != null && _selectablesHintLabel != null
            && _selectableLighting != null && _selectableNavigation != null && _selectableAudio != null && _scrollPanelLabel != null
            && _gridLabel != null && _grid != null && _gridPrimaryButton != null && _gridSecondaryButton != null && _gridInfoLabel != null && _gridStatusLabel != null
            && _canvasLabel != null && _canvas != null && _canvasNodeA != null && _canvasNodeB != null && _canvasNodeC != null
            && _scrollPanel != null && _menuPopupLabel != null && _menuPopupButton != null && _menuPopupStatus != null
            && _menuPopup != null && _menuPopupTable != null && _menuPopupContentItem != null
            && _popupButton != null && _modalButton != null && _tooltipLabel != null && _tooltipRegion != null
            && _hierarchyLabel != null && _treeNode != null && _collapsingHeader != null && _popup != null && _popupLabel != null
            && _popupCloseButton != null && _modal != null && _modalLabel != null && _modalCloseButton != null && _root != null)
        {
            UiRect content = _widgetsWindow.ContentBounds;
            int contentWidth = Math.Max(0, content.Width - Padding * 2);
            int widgetX = Padding;
            int widgetY = Padding;

            _widgetsTitleLabel.Bounds = new UiRect(widgetX, widgetY, contentWidth, labelHeight);
            widgetY = _widgetsTitleLabel.Bounds.Bottom + 8;

            int treeHeaderHeight = labelHeight + 6;
            int rowHeight = labelHeight + 6;
            int treeWidth = Math.Max(0, contentWidth);
            int categorySpacing = 8;

            int rootContentWidth = Math.Max(0, treeWidth - _widgetsTree.Indent);
            int rootPadding = Math.Max(0, _widgetsTree.ContentPadding);
            int categoryY = 0;

            int basicContentWidth = Math.Max(0, rootContentWidth - _widgetsBasicTree.Indent);
            int basicPadding = Math.Max(0, _widgetsBasicTree.ContentPadding);
            int basicY = 0;
            _snapCheckbox.Bounds = new UiRect(0, basicY, basicContentWidth, rowHeight);
            basicY += rowHeight + 4;
            _gizmoCheckbox.Bounds = new UiRect(0, basicY, basicContentWidth, rowHeight);
            basicY += rowHeight + 10;
            _widgetsSeparator.Bounds = new UiRect(0, basicY, Math.Max(0, Math.Min(260, basicContentWidth)), 4);
            basicY += 12;
            _qualityLow.Bounds = new UiRect(0, basicY, basicContentWidth, rowHeight);
            basicY += rowHeight + 4;
            _qualityMedium.Bounds = new UiRect(0, basicY, basicContentWidth, rowHeight);
            basicY += rowHeight + 4;
            _qualityHigh.Bounds = new UiRect(0, basicY, basicContentWidth, rowHeight);
            basicY += rowHeight + 4;
            int basicHeight = treeHeaderHeight + (_widgetsBasicTree.IsOpen ? basicPadding + basicY : 0);
            _widgetsBasicTree.HeaderHeight = treeHeaderHeight;
            _widgetsBasicTree.Bounds = new UiRect(0, categoryY, rootContentWidth, basicHeight);
            categoryY += basicHeight + categorySpacing;

            int sliderContentWidth = Math.Max(0, rootContentWidth - _widgetsSliderTree.Indent);
            int sliderPadding = Math.Max(0, _widgetsSliderTree.ContentPadding);
            int sliderY = 0;
            _volumeSlider.Bounds = new UiRect(0, sliderY, sliderContentWidth, 24);
            sliderY += 32;
            _volumeProgress.Bounds = new UiRect(0, sliderY, sliderContentWidth, 20);
            sliderY += 30;
            int sliderHeight = treeHeaderHeight + (_widgetsSliderTree.IsOpen ? sliderPadding + sliderY : 0);
            _widgetsSliderTree.HeaderHeight = treeHeaderHeight;
            _widgetsSliderTree.Bounds = new UiRect(0, categoryY, rootContentWidth, sliderHeight);
            categoryY += sliderHeight + categorySpacing;

            int styleContentWidth = Math.Max(0, rootContentWidth - _widgetsStyleTree.Indent);
            int stylePadding = Math.Max(0, _widgetsStyleTree.ContentPadding);
            int styleY = 0;
            _roundingLabel.Bounds = new UiRect(0, styleY, styleContentWidth, labelHeight);
            styleY += labelHeight + 6;
            int roundingSliderWidth = Math.Min(240, styleContentWidth);
            _roundingSlider.Bounds = new UiRect(0, styleY, Math.Max(0, roundingSliderWidth), 24);
            styleY += 32;
            _roundingPreviewLabel.Bounds = new UiRect(0, styleY, styleContentWidth, labelHeight);
            styleY += labelHeight + 6;
            int roundingButtonWidth = Math.Min(220, styleContentWidth);
            _roundingButton.Bounds = new UiRect(0, styleY, Math.Max(0, roundingButtonWidth), 24);
            styleY += 32;
            int roundingFieldWidth = Math.Min(240, styleContentWidth);
            _roundingField.Bounds = new UiRect(0, styleY, Math.Max(0, roundingFieldWidth), 22);
            styleY += 30;
            int roundingPanelWidth = Math.Min(240, styleContentWidth);
            _roundingPanel.Bounds = new UiRect(0, styleY, Math.Max(0, roundingPanelWidth), 36);
            styleY += _roundingPanel.Bounds.Height + 4;

            int roundingRadius = (int)Math.Round(_roundingSlider.Value);
            _roundingButton.CornerRadius = roundingRadius;
            _roundingField.CornerRadius = roundingRadius;
            _roundingPanel.CornerRadius = roundingRadius;

            int styleHeight = treeHeaderHeight + (_widgetsStyleTree.IsOpen ? stylePadding + styleY : 0);
            _widgetsStyleTree.HeaderHeight = treeHeaderHeight;
            _widgetsStyleTree.Bounds = new UiRect(0, categoryY, rootContentWidth, styleHeight);
            categoryY += styleHeight + categorySpacing;

            int dragContentWidth = Math.Max(0, rootContentWidth - _widgetsDragTree.Indent);
            int dragPadding = Math.Max(0, _widgetsDragTree.ContentPadding);
            int dragY = 0;
            _dragFloatLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            int dragControlWidth = Math.Min(240, dragContentWidth);
            _dragFloat.Bounds = new UiRect(0, dragY, Math.Max(0, dragControlWidth), 24);
            dragY += 30;
            _dragIntLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            _dragInt.Bounds = new UiRect(0, dragY, Math.Max(0, dragControlWidth), 24);
            dragY += 30;
            _dragRangeLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            int dragRangeWidth = Math.Min(280, dragContentWidth);
            _dragFloatRange.Bounds = new UiRect(0, dragY, Math.Max(0, dragRangeWidth), 24);
            dragY += 30;
            _dragIntRange.Bounds = new UiRect(0, dragY, Math.Max(0, dragRangeWidth), 24);
            dragY += 30;
            _dragVectorLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            int dragVectorWidth = Math.Min(320, dragContentWidth);
            _dragFloat2.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragFloat3.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragFloat4.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragVectorIntLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            _dragInt2.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragInt3.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragInt4.Bounds = new UiRect(0, dragY, Math.Max(0, dragVectorWidth), 24);
            dragY += 30;
            _dragFlagsLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            _dragClampCheckbox.Bounds = new UiRect(0, dragY, dragContentWidth, rowHeight);
            dragY += rowHeight + 4;
            _dragNoSlowFastCheckbox.Bounds = new UiRect(0, dragY, dragContentWidth, rowHeight);
            dragY += rowHeight + 4;
            _dragLogCheckbox.Bounds = new UiRect(0, dragY, dragContentWidth, rowHeight);
            dragY += rowHeight + 4;
            _dragHintLabel.Bounds = new UiRect(0, dragY, dragContentWidth, labelHeight);
            dragY += labelHeight + 4;
            int dragHeight = treeHeaderHeight + (_widgetsDragTree.IsOpen ? dragPadding + dragY : 0);
            _widgetsDragTree.HeaderHeight = treeHeaderHeight;
            _widgetsDragTree.Bounds = new UiRect(0, categoryY, rootContentWidth, dragHeight);
            categoryY += dragHeight + categorySpacing;

            int listContentWidth = Math.Max(0, rootContentWidth - _widgetsListTree.Indent);
            int listPadding = Math.Max(0, _widgetsListTree.ContentPadding);
            int listY = 0;
            _sceneLabel.Bounds = new UiRect(0, listY, listContentWidth, labelHeight);
            listY += labelHeight + 6;
            _sceneList.Bounds = new UiRect(0, listY, listContentWidth, 120);
            _sceneList.ItemHeight = labelHeight + 6;
            listY += _sceneList.Bounds.Height + 6;
            _sceneSelectionLabel.Bounds = new UiRect(0, listY, listContentWidth, labelHeight);
            listY += labelHeight + 4;
            int listHeight = treeHeaderHeight + (_widgetsListTree.IsOpen ? listPadding + listY : 0);
            _widgetsListTree.HeaderHeight = treeHeaderHeight;
            _widgetsListTree.Bounds = new UiRect(0, categoryY, rootContentWidth, listHeight);
            categoryY += listHeight + categorySpacing;

            int multiContentWidth = Math.Max(0, rootContentWidth - _widgetsMultiSelectTree.Indent);
            int multiPadding = Math.Max(0, _widgetsMultiSelectTree.ContentPadding);
            int multiY = 0;
            _multiSelectLabel.Bounds = new UiRect(0, multiY, multiContentWidth, labelHeight);
            multiY += labelHeight + 4;
            _multiSelectHintLabel.Bounds = new UiRect(0, multiY, multiContentWidth, labelHeight);
            multiY += labelHeight + 6;
            _multiSelectList.Bounds = new UiRect(0, multiY, multiContentWidth, 120);
            _multiSelectList.ItemHeight = labelHeight + 6;
            multiY += _multiSelectList.Bounds.Height + 6;
            _multiSelectSelectionLabel.Bounds = new UiRect(0, multiY, multiContentWidth, labelHeight);
            multiY += labelHeight + 4;
            int multiHeight = treeHeaderHeight + (_widgetsMultiSelectTree.IsOpen ? multiPadding + multiY : 0);
            _widgetsMultiSelectTree.HeaderHeight = treeHeaderHeight;
            _widgetsMultiSelectTree.Bounds = new UiRect(0, categoryY, rootContentWidth, multiHeight);
            categoryY += multiHeight + categorySpacing;

            int comboContentWidth = Math.Max(0, rootContentWidth - _widgetsComboTree.Indent);
            int comboPadding = Math.Max(0, _widgetsComboTree.ContentPadding);
            int comboY = 0;
            _comboLabel.Bounds = new UiRect(0, comboY, comboContentWidth, labelHeight);
            comboY += labelHeight + 6;
            _sceneComboBox.Bounds = new UiRect(0, comboY, comboContentWidth, 24);
            comboY += 30;
            _comboSelectionLabel.Bounds = new UiRect(0, comboY, comboContentWidth, labelHeight);
            comboY += labelHeight + 4;
            int comboHeight = treeHeaderHeight + (_widgetsComboTree.IsOpen ? comboPadding + comboY : 0);
            _widgetsComboTree.HeaderHeight = treeHeaderHeight;
            _widgetsComboTree.Bounds = new UiRect(0, categoryY, rootContentWidth, comboHeight);
            categoryY += comboHeight + categorySpacing;

            int tableContentWidth = Math.Max(0, rootContentWidth - _widgetsTableTree.Indent);
            int tablePadding = Math.Max(0, _widgetsTableTree.ContentPadding);
            int tableY = 0;
            _tableLabel.Bounds = new UiRect(0, tableY, tableContentWidth, labelHeight);
            tableY += labelHeight + 6;
            int tableRowHeight = labelHeight + 6;
            _sceneTable.RowHeight = tableRowHeight;
            _sceneTable.HeaderHeight = labelHeight + 8;
            int tableHeight = Math.Max(0, tableRowHeight * _sceneTable.Rows.Count + _sceneTable.HeaderHeight);
            _sceneTable.Bounds = new UiRect(0, tableY, tableContentWidth, tableHeight);
            tableY += _sceneTable.Bounds.Height + 6;
            _tableSelectionLabel.Bounds = new UiRect(0, tableY, tableContentWidth, labelHeight);
            tableY += labelHeight + 4;
            int tableTreeHeight = treeHeaderHeight + (_widgetsTableTree.IsOpen ? tablePadding + tableY : 0);
            _widgetsTableTree.HeaderHeight = treeHeaderHeight;
            _widgetsTableTree.Bounds = new UiRect(0, categoryY, rootContentWidth, tableTreeHeight);
            categoryY += tableTreeHeight + categorySpacing;

            int plotContentWidth = Math.Max(0, rootContentWidth - _widgetsPlotTree.Indent);
            int plotPadding = Math.Max(0, _widgetsPlotTree.ContentPadding);
            int plotY = 0;
            _plotLabel.Bounds = new UiRect(0, plotY, plotContentWidth, labelHeight);
            plotY += labelHeight + 6;
            int plotPanelHeight = Math.Max(140, Math.Min(220, plotContentWidth / 2));
            _plotPanel.Bounds = new UiRect(0, plotY, plotContentWidth, plotPanelHeight);
            plotY += plotPanelHeight + 4;
            int plotTreeHeight = treeHeaderHeight + (_widgetsPlotTree.IsOpen ? plotPadding + plotY : 0);
            _widgetsPlotTree.HeaderHeight = treeHeaderHeight;
            _widgetsPlotTree.Bounds = new UiRect(0, categoryY, rootContentWidth, plotTreeHeight);
            categoryY += plotTreeHeight + categorySpacing;

            int colorContentWidth = Math.Max(0, rootContentWidth - _widgetsColorTree.Indent);
            int colorPadding = Math.Max(0, _widgetsColorTree.ContentPadding);
            int colorY = 0;
            _colorEditLabel.Bounds = new UiRect(0, colorY, colorContentWidth, labelHeight);
            colorY += labelHeight + 6;
            int colorHeaderHeight = labelHeight + 8;
            int colorRowHeight = labelHeight + 6;
            _colorEdit.HeaderHeight = colorHeaderHeight;
            _colorEdit.RowHeight = colorRowHeight;
            int colorEditHeight = colorHeaderHeight + colorRowHeight * (_colorEdit.ShowAlpha ? 4 : 3);
            _colorEdit.Bounds = new UiRect(0, colorY, colorContentWidth, colorEditHeight);
            colorY += _colorEdit.Bounds.Height + 6;
            _colorPickerLabel.Bounds = new UiRect(0, colorY, colorContentWidth, labelHeight);
            colorY += labelHeight + 6;
            int colorPickerHeight = Math.Max(80, Math.Min(colorContentWidth, labelHeight * 10));
            if (_colorPicker.ShowAlpha)
            {
                colorPickerHeight += _colorPicker.AlphaBarHeight + Math.Max(0, _colorPicker.Padding);
            }
            _colorPicker.Bounds = new UiRect(0, colorY, colorContentWidth, colorPickerHeight);
            colorY += _colorPicker.Bounds.Height + 6;
            _colorSelectionLabel.Bounds = new UiRect(0, colorY, colorContentWidth, labelHeight);
            colorY += labelHeight + 6;
            _colorButtonLabel.Bounds = new UiRect(0, colorY, colorContentWidth, labelHeight);
            colorY += labelHeight + 6;
            int buttonSize = Math.Max(18, labelHeight + 4);
            int buttonSpacing = 8;
            int buttonX = 0;
            foreach (UiColorButton button in _colorButtons)
            {
                button.Bounds = new UiRect(buttonX, colorY, buttonSize, buttonSize);
                buttonX += buttonSize + buttonSpacing;
            }
            colorY += buttonSize + 4;
            int colorTreeHeight = treeHeaderHeight + (_widgetsColorTree.IsOpen ? colorPadding + colorY : 0);
            _widgetsColorTree.HeaderHeight = treeHeaderHeight;
            _widgetsColorTree.Bounds = new UiRect(0, categoryY, rootContentWidth, colorTreeHeight);
            categoryY += colorTreeHeight + categorySpacing;

            int asciiContentWidth = Math.Max(0, rootContentWidth - _widgetsAsciiTree.Indent);
            int asciiPadding = Math.Max(0, _widgetsAsciiTree.ContentPadding);
            int asciiY = 0;
            _asciiLabel.Bounds = new UiRect(0, asciiY, asciiContentWidth, labelHeight);
            asciiY += labelHeight + 4;
            _asciiPageLabel.Bounds = new UiRect(0, asciiY, asciiContentWidth, labelHeight);
            asciiY += labelHeight + 4;
            int asciiComboWidth = Math.Min(260, asciiContentWidth);
            _asciiPageCombo.Bounds = new UiRect(0, asciiY, Math.Max(0, asciiComboWidth), 24);
            asciiY += 30;
            int asciiRowHeight = labelHeight + 4;
            _asciiTable.RowHeight = asciiRowHeight;
            _asciiTable.HeaderHeight = labelHeight + 8;
            int asciiTableHeight = _asciiTable.HeaderHeight + asciiRowHeight * _asciiTable.Rows.Count;
            _asciiTable.Bounds = new UiRect(0, asciiY, asciiContentWidth, asciiTableHeight);
            asciiY += _asciiTable.Bounds.Height + 4;
            int asciiHeight = treeHeaderHeight + (_widgetsAsciiTree.IsOpen ? asciiPadding + asciiY : 0);
            _widgetsAsciiTree.HeaderHeight = treeHeaderHeight;
            _widgetsAsciiTree.Bounds = new UiRect(0, categoryY, rootContentWidth, asciiHeight);
            categoryY += asciiHeight + categorySpacing;

            int selectableContentWidth = Math.Max(0, rootContentWidth - _widgetsSelectableTree.Indent);
            int selectablePadding = Math.Max(0, _widgetsSelectableTree.ContentPadding);
            int selectableY = 0;
            _selectablesLabel.Bounds = new UiRect(0, selectableY, selectableContentWidth, labelHeight);
            selectableY += labelHeight + 4;
            _selectablesHintLabel.Bounds = new UiRect(0, selectableY, selectableContentWidth, labelHeight);
            selectableY += labelHeight + 6;
            _selectableLighting.Bounds = new UiRect(0, selectableY, selectableContentWidth, rowHeight);
            selectableY += rowHeight + 4;
            _selectableNavigation.Bounds = new UiRect(0, selectableY, selectableContentWidth, rowHeight);
            selectableY += rowHeight + 4;
            _selectableAudio.Bounds = new UiRect(0, selectableY, selectableContentWidth, rowHeight);
            selectableY += rowHeight + 4;
            int selectableHeight = treeHeaderHeight + (_widgetsSelectableTree.IsOpen ? selectablePadding + selectableY : 0);
            _widgetsSelectableTree.HeaderHeight = treeHeaderHeight;
            _widgetsSelectableTree.Bounds = new UiRect(0, categoryY, rootContentWidth, selectableHeight);
            categoryY += selectableHeight + categorySpacing;

            int layoutContentWidth = Math.Max(0, rootContentWidth - _widgetsLayoutTree.Indent);
            int layoutPadding = Math.Max(0, _widgetsLayoutTree.ContentPadding);
            int layoutY = 0;
            _canvasLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int canvasHeight = Math.Max(140, Math.Min(220, layoutContentWidth / 2));
            _canvas.Bounds = new UiRect(0, layoutY, layoutContentWidth, canvasHeight);
            layoutY += canvasHeight + 8;

            _gridLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int gridHeight = Math.Max(80, Math.Min(140, layoutContentWidth / 2));
            _grid.Bounds = new UiRect(0, layoutY, layoutContentWidth, gridHeight);
            layoutY += gridHeight + 8;

            _scrollPanelLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int logWidth = Math.Max(layoutContentWidth, 360);
            int logY = layoutY;
            foreach (UiLabel item in _scrollPanelItems)
            {
                item.Bounds = new UiRect(0, logY, logWidth, labelHeight);
                logY += labelHeight + 4;
            }

            layoutY = logY + 2;
            int layoutHeight = treeHeaderHeight + (_widgetsLayoutTree.IsOpen ? layoutPadding + layoutY : 0);
            _widgetsLayoutTree.HeaderHeight = treeHeaderHeight;
            _widgetsLayoutTree.Bounds = new UiRect(0, categoryY, rootContentWidth, layoutHeight);
            categoryY += layoutHeight + categorySpacing;

            int tooltipContentWidth = Math.Max(0, rootContentWidth - _widgetsTooltipTree.Indent);
            int tooltipPadding = Math.Max(0, _widgetsTooltipTree.ContentPadding);
            int tooltipY = 0;
            _tooltipLabel.Bounds = new UiRect(0, tooltipY, tooltipContentWidth, labelHeight);
            _tooltipRegion.Bounds = _tooltipLabel.Bounds;
            tooltipY += labelHeight + 4;
            int tooltipHeight = treeHeaderHeight + (_widgetsTooltipTree.IsOpen ? tooltipPadding + tooltipY : 0);
            _widgetsTooltipTree.HeaderHeight = treeHeaderHeight;
            _widgetsTooltipTree.Bounds = new UiRect(0, categoryY, rootContentWidth, tooltipHeight);
            categoryY += tooltipHeight + categorySpacing;

            int popupContentWidth = Math.Max(0, rootContentWidth - _widgetsPopupTree.Indent);
            int popupPadding = Math.Max(0, _widgetsPopupTree.ContentPadding);
            int popupCategoryY = 0;
            int buttonHeight = 24;
            int popupButtonWidth = Math.Min(220, popupContentWidth);
            _menuPopupLabel.Bounds = new UiRect(0, popupCategoryY, popupContentWidth, labelHeight);
            popupCategoryY += labelHeight + 6;
            _menuPopupButton.Bounds = new UiRect(0, popupCategoryY, Math.Max(0, popupButtonWidth), buttonHeight);
            popupCategoryY += buttonHeight + 4;
            _menuPopupStatus.Bounds = new UiRect(0, popupCategoryY, popupContentWidth, labelHeight);
            popupCategoryY += labelHeight + 8;

            int menuTableRowHeight = labelHeight + 4;
            int menuTableHeaderHeight = labelHeight + 6;
            _menuPopupTable.RowHeight = menuTableRowHeight;
            _menuPopupTable.HeaderHeight = menuTableHeaderHeight;
            int menuTableHeight = menuTableHeaderHeight + menuTableRowHeight * _menuPopupTable.Rows.Count;
            int menuTableWidth = Math.Max(0, Math.Min(260, popupContentWidth));
            _menuPopupContentItem.ContentWidth = menuTableWidth;
            _menuPopupContentItem.ContentHeight = menuTableHeight;

            _menuPopup.Bounds = new UiRect(_menuPopupButton.Bounds.X, _menuPopupButton.Bounds.Bottom + 4, _menuPopupButton.Bounds.Width, 0);

            _popupButton.Bounds = new UiRect(0, popupCategoryY, Math.Max(0, popupButtonWidth), buttonHeight);
            popupCategoryY += buttonHeight + 6;
            _modalButton.Bounds = new UiRect(0, popupCategoryY, Math.Max(0, popupButtonWidth), buttonHeight);
            popupCategoryY += buttonHeight + 4;
            int popupTreeHeight = treeHeaderHeight + (_widgetsPopupTree.IsOpen ? popupPadding + popupCategoryY : 0);
            _widgetsPopupTree.HeaderHeight = treeHeaderHeight;
            _widgetsPopupTree.Bounds = new UiRect(0, categoryY, rootContentWidth, popupTreeHeight);
            categoryY += popupTreeHeight + categorySpacing;

            int treeContentWidth = Math.Max(0, rootContentWidth - _widgetsTreeHeaderTree.Indent);
            int treePadding = Math.Max(0, _widgetsTreeHeaderTree.ContentPadding);
            int treeY = 0;
            _hierarchyLabel.Bounds = new UiRect(0, treeY, treeContentWidth, labelHeight);
            treeY += labelHeight + 6;

            int demoHeaderHeight = labelHeight + 6;
            int treeItemHeight = labelHeight + 4;
            int demoTreePadding = Math.Max(0, _treeNode.ContentPadding);
            int treeItemY = 0;
            int treeItemWidth = Math.Max(0, treeContentWidth - _treeNode.Indent);
            foreach (UiLabel child in _treeNodeItems)
            {
                child.Bounds = new UiRect(0, treeItemY, treeItemWidth, labelHeight);
                treeItemY += treeItemHeight;
            }

            int demoTreeHeight = demoHeaderHeight + (_treeNode.IsOpen ? demoTreePadding + treeItemY : 0);
            _treeNode.HeaderHeight = demoHeaderHeight;
            _treeNode.Bounds = new UiRect(0, treeY, treeContentWidth, demoTreeHeight);
            treeY += demoTreeHeight + 10;

            int collapseHeaderHeight = labelHeight + 6;
            int collapsePadding = Math.Max(0, _collapsingHeader.ContentPadding);
            int collapseItemY = 0;
            foreach (UiLabel child in _collapsingItems)
            {
                child.Bounds = new UiRect(0, collapseItemY, treeContentWidth, labelHeight);
                collapseItemY += treeItemHeight;
            }

            int collapseHeight = collapseHeaderHeight + (_collapsingHeader.IsOpen ? collapsePadding + collapseItemY : 0);
            _collapsingHeader.HeaderHeight = collapseHeaderHeight;
            _collapsingHeader.Bounds = new UiRect(0, treeY, treeContentWidth, collapseHeight);
            treeY += collapseHeight + 4;
            int treeCategoryHeight = treeHeaderHeight + (_widgetsTreeHeaderTree.IsOpen ? treePadding + treeY : 0);
            _widgetsTreeHeaderTree.HeaderHeight = treeHeaderHeight;
            _widgetsTreeHeaderTree.Bounds = new UiRect(0, categoryY, rootContentWidth, treeCategoryHeight);
            categoryY += treeCategoryHeight + categorySpacing;

            int widgetsHeight = treeHeaderHeight + (_widgetsTree.IsOpen ? rootPadding + categoryY : 0);
            _widgetsTree.HeaderHeight = treeHeaderHeight;
            _widgetsTree.Bounds = new UiRect(widgetX, widgetY, treeWidth, widgetsHeight);

            UiRect widgetsContent = _widgetsTree.ContentBounds;
            UiRect popupContent = _widgetsPopupTree.ContentBounds;
            int popupAnchorX = widgetsContent.X + popupContent.X + _popupButton.Bounds.X;
            int popupAnchorY = widgetsContent.Y + popupContent.Y + _popupButton.Bounds.Y;

            int popupWidth = Math.Min(220, Math.Max(140, _popupButton.Bounds.Width));
            int popupHeight = labelHeight + buttonHeight + 24;
            int popupX = content.X + popupAnchorX - _scrollPanel.ScrollX;
            int popupY = content.Y + popupAnchorY - _scrollPanel.ScrollY + _popupButton.Bounds.Height + 6;
            _popup.Bounds = new UiRect(popupX, popupY, popupWidth, popupHeight);

            int popupInnerPadding = 8;
            _popupLabel.Bounds = new UiRect(popupX + popupInnerPadding, popupY + popupInnerPadding, Math.Max(0, popupWidth - popupInnerPadding * 2), labelHeight);
            _popupCloseButton.Bounds = new UiRect(popupX + popupWidth - 80 - popupInnerPadding, popupY + popupHeight - buttonHeight - popupInnerPadding, 80, buttonHeight);

            UiRect rootBounds = _root.Bounds;
            int modalWidth = Math.Min(280, Math.Max(160, rootBounds.Width - Padding * 2));
            int modalHeight = labelHeight + buttonHeight + 36;
            int modalX = rootBounds.X + (rootBounds.Width - modalWidth) / 2;
            int modalY = rootBounds.Y + (rootBounds.Height - modalHeight) / 2;
            _modal.Bounds = new UiRect(modalX, modalY, modalWidth, modalHeight);
            _modalLabel.Bounds = new UiRect(modalX + Padding, modalY + Padding, Math.Max(0, modalWidth - Padding * 2), labelHeight);
            _modalCloseButton.Bounds = new UiRect(modalX + modalWidth - 90 - Padding, modalY + modalHeight - buttonHeight - Padding, 90, buttonHeight);
        }

        if (_clippingWindow != null && _clipPanel != null && _clipLabel != null)
        {
            UiRect content = _clippingWindow.ContentBounds;
            int maxWidth = Math.Max(0, content.Width - Padding * 2);
            int maxHeight = Math.Max(0, content.Height - Padding * 2);
            int width = _clipPanelInitialized ? Math.Min(_clipPanel.Bounds.Width, maxWidth) : Math.Min(220, maxWidth);
            int height = _clipPanelInitialized ? Math.Min(_clipPanel.Bounds.Height, maxHeight) : Math.Min(40, maxHeight);

            _clipPanel.Bounds = new UiRect(content.X + Padding, content.Y + Padding, Math.Max(0, width), Math.Max(0, height));
            _clipPanelInitialized = true;
            _clipLabel.Bounds = new UiRect(_clipPanel.Bounds.X + 6, _clipPanel.Bounds.Y + 6, Math.Max(0, _clipPanel.Bounds.Width - 12), labelHeight);
        }

        if (_serializationWindow != null && _serializationPanel != null && _serializationLabel != null)
        {
            UiRect content = _serializationWindow.ContentBounds;
            int panelWidth = Math.Max(0, content.Width - Padding * 2);
            int panelHeight = Math.Max(0, content.Height - Padding * 2);
            _serializationPanel.Bounds = new UiRect(content.X + Padding, content.Y + Padding, panelWidth, panelHeight);
            _serializationLabel.Bounds = new UiRect(_serializationPanel.Bounds.X + Padding, _serializationPanel.Bounds.Y + Padding / 2, Math.Max(0, panelWidth - Padding * 2), labelHeight);
        }

        if (_standaloneWindow != null && _standaloneScrollPanel != null)
        {
            UiRect content = _standaloneWindow.ContentBounds;
            int itemWidth = Math.Max(360, content.Width);
            int itemY = 0;
            foreach (UiLabel item in _standaloneScrollItems)
            {
                item.Bounds = new UiRect(0, itemY, itemWidth, labelHeight);
                itemY += labelHeight + 4;
            }
        }

        if (_assetsWindow != null && _assetsLabel != null)
        {
            UiRect content = _assetsWindow.ContentBounds;
            _assetsLabel.Bounds = new UiRect(content.X + 8, content.Y + 8, Math.Max(0, content.Width - 16), labelHeight);
        }

        if (_consoleWindow != null && _consoleLabel != null)
        {
            UiRect content = _consoleWindow.ContentBounds;
            _consoleLabel.Bounds = new UiRect(content.X + 8, content.Y + 8, Math.Max(0, content.Width - 16), labelHeight);
        }

        if (_inspectorWindow != null && _inspectorLabel != null)
        {
            UiRect content = _inspectorWindow.ContentBounds;
            _inspectorLabel.Bounds = new UiRect(content.X + 8, content.Y + 8, Math.Max(0, content.Width - 16), labelHeight);
        }
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null || _textField == null)
        {
            return;
        }

        string menuStatus = string.IsNullOrWhiteSpace(_menuStatus) ? string.Empty : $"  {_menuStatus}";
        _statusLabel.Text = $"Clicks: {_buttonClicks}  Text: {_textField.Text}{menuStatus}  F5 Save UI  F9 Load UI";

        if (_serializationLabel != null)
        {
            _serializationLabel.Text = $"UI State: Press F5 to save, F9 to load. ({GetStatePath()})";
        }
    }

    private void UpdateWidgetPanel()
    {
        if (_volumeSlider != null && _volumeProgress != null)
        {
            _volumeProgress.Value = _volumeSlider.Value;
            _volumeProgress.Text = $"Volume {(int)Math.Round(_volumeSlider.Value * 100f)}%";
        }

        if (_sceneSelectionLabel != null && _sceneList != null)
        {
            string selection = _sceneList.SelectedIndex >= 0 && _sceneList.SelectedIndex < _sceneItems.Count
                ? _sceneItems[_sceneList.SelectedIndex]
                : "None";
            _sceneSelectionLabel.Text = $"List: {selection}";
        }

        if (_multiSelectSelectionLabel != null && _multiSelectModel != null)
        {
            if (_multiSelectModel.SelectedIndices.Count == 0)
            {
                _multiSelectSelectionLabel.Text = "Multi: None";
            }
            else
            {
                List<string> selections = new();
                foreach (int index in _multiSelectModel.SelectedIndices)
                {
                    if (index >= 0 && index < _sceneItems.Count)
                    {
                        selections.Add(_sceneItems[index]);
                    }
                }

                string selection = selections.Count > 0 ? string.Join(", ", selections) : "None";
                _multiSelectSelectionLabel.Text = $"Multi: {selection}";
            }
        }

        if (_comboSelectionLabel != null && _sceneComboBox != null)
        {
            string selection = _sceneComboBox.SelectedIndex >= 0 && _sceneComboBox.SelectedIndex < _sceneItems.Count
                ? _sceneItems[_sceneComboBox.SelectedIndex]
                : "None";
            _comboSelectionLabel.Text = $"Combo: {selection}";
        }

        if (_tableSelectionLabel != null && _sceneTable != null)
        {
            int index = _sceneTable.SelectedIndex;
            string selection = "None";
            if (index >= 0 && index < _tableRows.Count)
            {
                IReadOnlyList<string> cells = _tableRows[index].Cells;
                if (cells.Count > 0 && !string.IsNullOrWhiteSpace(cells[0]))
                {
                    selection = cells[0];
                }
            }

            _tableSelectionLabel.Text = $"Table: {selection}";
        }

        if (_colorSelectionLabel != null)
        {
            bool includeAlpha = (_colorEdit?.ShowAlpha ?? false) || (_colorPicker?.ShowAlpha ?? false);
            _colorSelectionLabel.Text = $"Color: {FormatHex(_sharedColor, includeAlpha)}";
        }
    }

    private void UpdateDragFlags()
    {
        UiDragFlags flags = UiDragFlags.None;
        if (_dragClampCheckbox?.Checked ?? false)
        {
            flags |= UiDragFlags.Clamp;
        }

        if (_dragNoSlowFastCheckbox?.Checked ?? false)
        {
            flags |= UiDragFlags.NoSlowFast;
        }

        if (_dragLogCheckbox?.Checked ?? false)
        {
            flags |= UiDragFlags.Logarithmic;
        }

        if (_dragFloat != null)
        {
            _dragFloat.Flags = flags;
        }

        if (_dragInt != null)
        {
            _dragInt.Flags = flags;
        }

        if (_dragFloatRange != null)
        {
            _dragFloatRange.Flags = flags;
        }

        if (_dragIntRange != null)
        {
            _dragIntRange.Flags = flags;
        }

        if (_dragFloat2 != null)
        {
            _dragFloat2.Flags = flags;
        }

        if (_dragFloat3 != null)
        {
            _dragFloat3.Flags = flags;
        }

        if (_dragFloat4 != null)
        {
            _dragFloat4.Flags = flags;
        }

        if (_dragInt2 != null)
        {
            _dragInt2.Flags = flags;
        }

        if (_dragInt3 != null)
        {
            _dragInt3.Flags = flags;
        }

        if (_dragInt4 != null)
        {
            _dragInt4.Flags = flags;
        }

        if (_dragHintLabel != null)
        {
            _dragHintLabel.Visible = !flags.HasFlag(UiDragFlags.NoSlowFast);
        }
    }

    private void SyncColorFromEdit(UiColor color)
    {
        if (_syncingColor)
        {
            return;
        }

        _syncingColor = true;
        _sharedColor = color;
        if (_colorPicker != null)
        {
            _colorPicker.Color = color;
        }
        _syncingColor = false;
    }

    private void SyncColorFromPicker(UiColor color)
    {
        if (_syncingColor)
        {
            return;
        }

        _syncingColor = true;
        _sharedColor = color;
        if (_colorEdit != null)
        {
            _colorEdit.Color = color;
        }
        _syncingColor = false;
    }

    private static string FormatHex(UiColor color, bool includeAlpha)
    {
        return includeAlpha
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void SetAsciiPageIndex(int index)
    {
        int maxIndex = _asciiPageItems.Count - 1;
        int clamped = maxIndex >= 0 ? Math.Clamp(index, 0, maxIndex) : 0;
        _asciiPageIndex = clamped;

        if (_asciiPageCombo != null && _asciiPageCombo.SelectedIndex != clamped)
        {
            _asciiPageCombo.SelectedIndex = clamped;
        }

        if (_renderer != null)
        {
            _renderer.CodePage = clamped == 1 ? TinyFontCodePage.Cp437 : TinyFontCodePage.Latin1;
        }

        UpdateAsciiTable();
    }

    private void UpdateAsciiTable()
    {
        if (_asciiTable == null)
        {
            return;
        }

        Encoding encoding = _asciiPageIndex == 1 ? Cp437Encoding : Latin1Encoding;
        _asciiRows.Clear();

        byte[] buffer = new byte[1];
        for (int i = 0; i < 256; i++)
        {
            buffer[0] = (byte)i;
            string text = encoding.GetString(buffer);
            char c = text.Length > 0 ? text[0] : ' ';
            string charCell = GetAsciiDisplay(c, i);
            _asciiRows.Add(new UiTableRow(i.ToString("000"), $"0x{i:X2}", charCell));
        }

        _asciiTable.Rows = _asciiRows;
    }

    private static string GetAsciiDisplay(char c, int code)
    {
        if (c == ' ')
        {
            return "SP";
        }

        if (c == '\u00A0')
        {
            return "NBSP";
        }

        if (char.IsControl(c))
        {
            if (code >= 0 && code < ControlNames.Length)
            {
                return ControlNames[code];
            }

            if (code == 0x7F)
            {
                return "DEL";
            }

            return "CTRL";
        }

        return c.ToString();
    }

    private void SaveUiState()
    {
        if (_root == null)
        {
            return;
        }

        string path = GetStatePath();
        UiStateSnapshot snapshot = UiStateSerializer.Capture(_root);
        UiStateSerializer.SaveToFile(path, snapshot);
        _menuStatus = "Menu: UI Saved";
    }

    private void LoadUiState()
    {
        if (_root == null)
        {
            return;
        }

        string path = GetStatePath();
        UiStateSnapshot snapshot = UiStateSerializer.LoadFromFile(path);
        UiStateSerializer.Apply(_root, snapshot);
        _standaloneInitialized = true;
        _clipPanelInitialized = true;
        _menuStatus = "Menu: UI Loaded";
        SyncExampleSelectionFromWindows();
    }

    private string GetStatePath()
    {
        if (!string.IsNullOrWhiteSpace(_statePath))
        {
            return _statePath;
        }

        string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenControls");
        Directory.CreateDirectory(basePath);
        _statePath = Path.Combine(basePath, "examples_ui_state.json");
        return _statePath;
    }

    private void SetActiveExample(ExamplePanel example)
    {
        _activeExample = example;
        ApplyExampleLayout(example);
        UpdateExampleMenuChecks();
    }

    private void ApplyExampleLayout(ExamplePanel example)
    {
        if (_dockWorkspace == null || _dockLeft == null)
        {
            return;
        }

        _dockWorkspace.ResetLayout();
        _dockLeft = _dockWorkspace.RootHost;
        _dockLeft.Id = "dock-left";
        _dockLeft.TabWidth = 160;
        _dockLeft.TabTextScale = FontScale;
        _dockRight = null;

        switch (example)
        {
            case ExamplePanel.All:
                UiDockHost allDockHost = CreateDockRightHost();
                DockBasics(_dockLeft);
                DockWidgets(_dockLeft);
                DockClipping(_dockLeft);
                DockSerialization(_dockLeft);
                DockDockingWindows(allDockHost, allDockHost, includeFloating: true);
                break;
            case ExamplePanel.Basics:
                DockBasics(_dockLeft);
                break;
            case ExamplePanel.Widgets:
                DockWidgets(_dockLeft);
                break;
            case ExamplePanel.Docking:
                UiDockHost dockRight = CreateDockRightHost();
                DockDockingWindows(_dockLeft, dockRight, includeFloating: true);
                break;
            case ExamplePanel.Clipping:
                DockClipping(_dockLeft);
                break;
            case ExamplePanel.Serialization:
                DockSerialization(_dockLeft);
                break;
            case ExamplePanel.Custom:
                break;
        }
    }

    private UiDockHost CreateDockRightHost()
    {
        if (_dockWorkspace == null || _dockLeft == null)
        {
            throw new InvalidOperationException("Dock workspace not ready.");
        }

        UiDockHost rightHost = _dockWorkspace.SplitHost(_dockLeft, UiDockWorkspace.DockTarget.Right);
        rightHost.Id = "dock-right";
        rightHost.TabWidth = _dockLeft.TabWidth;
        rightHost.TabTextScale = _dockLeft.TabTextScale;
        _dockRight = rightHost;
        return rightHost;
    }

    private void DockBasics(UiDockHost host)
    {
        if (_basicsWindow != null)
        {
            host.DockWindow(_basicsWindow);
        }
    }

    private void DockWidgets(UiDockHost host)
    {
        if (_widgetsWindow != null)
        {
            host.DockWindow(_widgetsWindow);
        }
    }

    private void DockClipping(UiDockHost host)
    {
        if (_clippingWindow != null)
        {
            host.DockWindow(_clippingWindow);
        }
    }

    private void DockSerialization(UiDockHost host)
    {
        if (_serializationWindow != null)
        {
            host.DockWindow(_serializationWindow);
        }
    }

    private void DockDockingWindows(UiDockHost leftHost, UiDockHost rightHost, bool includeFloating)
    {
        if (_assetsWindow != null)
        {
            leftHost.DockWindow(_assetsWindow);
        }

        if (_consoleWindow != null)
        {
            leftHost.DockWindow(_consoleWindow);
        }

        if (_inspectorWindow != null)
        {
            rightHost.DockWindow(_inspectorWindow);
        }

        if (includeFloating && _dockWorkspace != null && _standaloneWindow != null)
        {
            _dockWorkspace.AddFloatingWindow(_standaloneWindow);
        }
    }

    private void SyncExampleSelectionFromWindows()
    {
        bool basics = IsWindowInLayout(_basicsWindow);
        bool widgets = IsWindowInLayout(_widgetsWindow);
        bool docking = IsWindowInLayout(_assetsWindow) || IsWindowInLayout(_consoleWindow) || IsWindowInLayout(_inspectorWindow);
        bool clipping = IsWindowInLayout(_clippingWindow);
        bool serialization = IsWindowInLayout(_serializationWindow);

        if (basics && widgets && docking && clipping && serialization)
        {
            _activeExample = ExamplePanel.All;
        }
        else if (basics && !widgets && !docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Basics;
        }
        else if (!basics && widgets && !docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Widgets;
        }
        else if (!basics && !widgets && docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Docking;
        }
        else if (!basics && !widgets && !docking && clipping && !serialization)
        {
            _activeExample = ExamplePanel.Clipping;
        }
        else if (!basics && !widgets && !docking && !clipping && serialization)
        {
            _activeExample = ExamplePanel.Serialization;
        }
        else
        {
            _activeExample = ExamplePanel.Custom;
        }

        UpdateExampleMenuChecks();
    }

    private static bool IsWindowInLayout(UiWindow? window)
    {
        return window != null && window.Parent != null;
    }

    private void UpdateExampleMenuChecks()
    {
        if (_examplesMenuAllItem == null || _examplesMenuBasicsItem == null || _examplesMenuWidgetsItem == null || _examplesMenuDockingItem == null || _examplesMenuClippingItem == null || _examplesMenuSerializationItem == null)
        {
            return;
        }

        _examplesMenuAllItem.Checked = _activeExample == ExamplePanel.All;
        _examplesMenuBasicsItem.Checked = _activeExample == ExamplePanel.Basics;
        _examplesMenuWidgetsItem.Checked = _activeExample == ExamplePanel.Widgets;
        _examplesMenuDockingItem.Checked = _activeExample == ExamplePanel.Docking;
        _examplesMenuClippingItem.Checked = _activeExample == ExamplePanel.Clipping;
        _examplesMenuSerializationItem.Checked = _activeExample == ExamplePanel.Serialization;
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
