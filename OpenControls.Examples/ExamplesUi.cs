using System.IO;
using System.Text;
using OpenControls;
using OpenControls.Controls;
using OpenControls.State;

namespace OpenControls.Examples;

public sealed class ExamplesUi
{
    private const int FontScale = 2;
    private const int Padding = 12;
    private const int SplitterSize = 6;

    private static readonly Encoding Latin1Encoding = Encoding.Latin1;
    private static readonly Encoding Cp437Encoding;
    private static readonly string[] ControlNames =
    {
        "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
        "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
        "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
        "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US"
    };
    private static readonly string[] CompletionHints =
    {
        "help",
        "hello",
        "helium",
        "heap",
        "load",
        "log",
        "layout",
        "license"
    };
    private static readonly string SampleEditorText =
@"namespace OpenControls;

public sealed class HeadlessUiRenderer : IUiRenderer
{
    private readonly TinyBitmapFont _font;

    public HeadlessUiRenderer(int width, int height, TinyBitmapFont? font = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        _font = font ?? new TinyBitmapFont();
    }
}";
    private static readonly string SampleEditorTextLarge = BuildSampleEditorTextLarge();
    private static readonly float[] WaveformSamples = BuildWaveformSamples();

    private static string BuildSampleEditorTextLarge()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(SampleEditorText);
        builder.AppendLine();
        builder.AppendLine("// Large file preview");
        for (int i = 0; i < 120; i++)
        {
            builder.Append("int value");
            builder.Append(i.ToString("D3"));
            builder.Append(" = ");
            builder.Append(i * 3);
            builder.AppendLine(";");
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }

    private static float[] BuildWaveformSamples()
    {
        const int sampleCount = 1024;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float phase = t * MathF.PI * 4f;
            float value = MathF.Sin(phase) * 0.75f + MathF.Sin(phase * 2f) * 0.25f;
            samples[i] = value;
        }

        return samples;
    }

    private readonly IUiRenderer _renderer;
    private readonly TinyBitmapFont _font;
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
    private UiTreeNode? _widgetsWaveformTree;
    private UiTreeNode? _widgetsColorTree;
    private UiTreeNode? _widgetsAsciiTree;
    private UiTreeNode? _widgetsSelectableTree;
    private UiTreeNode? _widgetsLayoutTree;
    private UiTreeNode? _widgetsTooltipTree;
    private UiTreeNode? _widgetsPopupTree;
    private UiTreeNode? _widgetsTreeHeaderTree;
    private UiTreeNode? _widgetsConfigTree;
    private UiTreeNode? _widgetsMenuTree;
    private UiTreeNode? _widgetsTextTree;
    private UiTreeNode? _widgetsTextFilterTree;
    private UiTreeNode? _widgetsTextInputTree;
    private UiTreeNode? _widgetsBulletsTree;
    private UiTreeNode? _widgetsImagesTree;
    private UiTreeNode? _widgetsMultiComponentTree;
    private UiTreeNode? _widgetsDisableTree;
    private UiTreeNode? _widgetsDragDropTree;
    private UiTreeNode? _widgetsStatusTree;
    private UiTreeNode? _widgetsInputFocusTree;
    private UiTreeNode? _widgetsToolsTree;
    private UiTreeNode? _widgetsExamplesTree;
    private UiCheckbox? _snapCheckbox;
    private UiCheckbox? _gizmoCheckbox;
    private UiSeparator? _widgetsSeparator;
    private UiRadioButton? _qualityLow;
    private UiRadioButton? _qualityMedium;
    private UiRadioButton? _qualityHigh;
    private UiLabel? _invisibleButtonLabel;
    private UiPanel? _invisibleButtonPanel;
    private UiInvisibleButton? _invisibleButton;
    private UiLabel? _invisibleButtonStatus;
    private int _invisibleButtonClicks;
    private UiLabel? _basicButtonsLabel;
    private UiButton? _basicPrimaryButton;
    private UiButton? _basicDangerButton;
    private UiButton? _basicRepeatButton;
    private UiLabel? _basicRepeatStatusLabel;
    private int _basicRepeatTicks;
    private float _basicRepeatAccumulator;
    private UiLabel? _basicInputLabel;
    private UiTextField? _basicInputText;
    private UiLabel? _basicInputIntLabel;
    private UiInputInt? _basicInputInt;
    private UiLabel? _basicInputFloatLabel;
    private UiInputFloat? _basicInputFloat;
    private UiSlider? _volumeSlider;
    private UiProgressBar? _volumeProgress;
    private UiLabel? _verticalProgressLabel;
    private UiProgressBar? _verticalProgress;
    private UiLabel? _vuMeterLabel;
    private UiProgressBar? _vuMeter;
    private UiLabel? _radialProgressLabel;
    private UiProgressBar? _radialProgress;
    private UiLabel? _angleSliderLabel;
    private UiSliderAngle? _angleSlider;
    private UiLabel? _angleSliderValueLabel;
    private UiLabel? _verticalSliderLabel;
    private UiVSlider? _verticalSlider;
    private UiLabel? _verticalSliderValueLabel;
    private UiLabel? _sliderFlagsLabel;
    private UiCheckbox? _sliderWholeNumbersCheckbox;
    private UiCheckbox? _sliderStepCheckbox;
    private UiLabel? _enumSliderLabel;
    private UiSlider? _enumSlider;
    private UiLabel? _enumSliderValueLabel;
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
    private UiLabel? _waveformLabel;
    private UiWaveform? _waveform;
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
    private UiLabel? _textEditorLabel;
    private UiTextEditor? _textEditor;
    private UiWindow? _textEditorWindow;
    private UiTextEditor? _textEditorDemo;
    private UiLabel? _gridLabel;
    private UiGrid? _grid;
    private UiButton? _gridPrimaryButton;
    private UiButton? _gridSecondaryButton;
    private UiLabel? _gridInfoLabel;
    private UiLabel? _gridStatusLabel;
    private UiLabel? _tabBarLabel;
    private UiTabBar? _tabBar;
    private UiTabItem? _tabOverview;
    private UiTabItem? _tabDetails;
    private UiTabItem? _tabSettings;
    private UiLabel? _tabOverviewLabel;
    private UiLabel? _tabDetailsLabel;
    private UiLabel? _tabSettingsLabel;
    private UiTabItemButton? _tabLeadingButton;
    private UiTabItemButton? _tabTrailingButton;
    private UiLabel? _tabButtonStatusLabel;
    private UiLabel? _canvasLabel;
    private UiCanvas? _canvas;
    private UiButton? _canvasNodeA;
    private UiButton? _canvasNodeB;
    private UiButton? _canvasNodeC;
    private UiLabel? _splitterLabel;
    private UiPanel? _splitterVerticalLeftPanel;
    private UiPanel? _splitterVerticalRightPanel;
    private UiSplitter? _splitterVertical;
    private UiPanel? _splitterHorizontalTopPanel;
    private UiPanel? _splitterHorizontalBottomPanel;
    private UiSplitter? _splitterHorizontal;
    private int _splitterVerticalLeftWidth = 140;
    private int _splitterHorizontalTopHeight = 50;
    private UiScrollPanel? _scrollPanel;
    private readonly List<UiLabel> _scrollPanelItems = new();
    private UiLabel? _layoutHorizontalLabel;
    private UiButton? _layoutButtonLeft;
    private UiButton? _layoutButtonCenter;
    private UiButton? _layoutButtonRight;
    private UiLabel? _layoutDummyLabel;
    private UiPanel? _layoutDummyPanel;
    private UiLabel? _layoutWrapLabel;
    private UiTextBlock? _layoutWrapBlock;
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
    private UiLabel? _configHelpLabel;
    private UiTextBlock? _configHelpText;
    private UiLabel? _configBackendLabel;
    private UiCheckbox? _configDockingCheckbox;
    private UiCheckbox? _configViewportCheckbox;
    private UiLabel? _configStyleLabel;
    private UiCheckbox? _configLargeFontCheckbox;
    private UiLabel? _configCaptureLabel;
    private UiCheckbox? _configCaptureKeyboardCheckbox;
    private UiCheckbox? _configCaptureMouseCheckbox;
    private UiLabel? _windowOptionsLabel;
    private UiCheckbox? _windowAllowResizeCheckbox;
    private UiCheckbox? _windowShowTitleCheckbox;
    private UiCheckbox? _windowAllowDragCheckbox;
    private UiCheckbox? _windowShowGripCheckbox;
    private UiMenuBar? _demoMenuBar;
    private UiLabel? _demoMenuStatusLabel;
    private UiLabel? _textHeaderLabel;
    private UiLabel? _textColoredLabel;
    private UiLabel? _textColoredSample;
    private UiLabel? _textFontSizeLabel;
    private UiLabel? _textFontSizeSmall;
    private UiLabel? _textFontSizeLarge;
    private UiTextBlock? _textWrappedBlock;
    private UiLabel? _textUtf8Label;
    private UiTextLink? _textLink;
    private UiLabel? _textFilterLabel;
    private UiTextField? _textFilterField;
    private UiListBox? _textFilterList;
    private UiLabel? _textFilterStatusLabel;
    private readonly List<string> _textFilterItems = new();
    private readonly List<string> _textFilterFilteredItems = new();
    private string _lastTextFilter = string.Empty;
    private UiLabel? _textInputLabel;
    private UiTextEditor? _multiLineInput;
    private UiLabel? _filteredInputLabel;
    private UiTextField? _filteredInputField;
    private UiLabel? _passwordInputLabel;
    private UiTextField? _passwordField;
    private UiLabel? _passwordMaskLabel;
    private UiLabel? _passwordHintLabel;
    private UiLabel? _completionInputLabel;
    private UiTextField? _completionField;
    private UiLabel? _completionHintLabel;
    private UiLabel? _completionHistoryLabel;
    private readonly List<string> _completionHistory = new();
    private UiLabel? _resizeInputLabel;
    private UiTextField? _resizeInputField;
    private UiLabel? _elidingInputLabel;
    private UiTextField? _elidingInputField;
    private UiLabel? _elidingResultLabel;
    private UiLabel? _miscInputLabel;
    private UiTextField? _miscInputField;
    private UiLabel? _bulletsLabel;
    private readonly List<UiBulletText> _bulletItems = new();
    private UiLabel? _imagesLabel;
    private UiImage? _imagePreview;
    private UiImageButton? _imageButton;
    private UiLabel? _imageButtonStatus;
    private int _imageButtonClicks;
    private UiLabel? _multiComponentLabel;
    private UiInputFloat2? _inputFloat2;
    private UiInputFloat3? _inputFloat3;
    private UiInputFloat4? _inputFloat4;
    private UiInputInt2? _inputInt2;
    private UiInputInt3? _inputInt3;
    private UiInputInt4? _inputInt4;
    private UiSliderFloat2? _sliderFloat2;
    private UiSliderFloat3? _sliderFloat3;
    private UiSliderFloat4? _sliderFloat4;
    private UiSliderInt2? _sliderInt2;
    private UiSliderInt3? _sliderInt3;
    private UiSliderInt4? _sliderInt4;
    private UiLabel? _multiComponentStatusLabel;
    private UiLabel? _disableLabel;
    private UiCheckbox? _disableToggle;
    private UiDisabledGroup? _disabledGroup;
    private UiButton? _disabledButton;
    private UiSlider? _disabledSlider;
    private UiTextField? _disabledField;
    private UiLabel? _dragDropLabel;
    private UiDragDropSource? _dragDropSource;
    private UiDragDropTarget? _dragDropTarget;
    private UiPanel? _dragDropSourcePanel;
    private UiPanel? _dragDropTargetPanel;
    private UiLabel? _dragDropSourceLabel;
    private UiLabel? _dragDropTargetLabel;
    private UiLabel? _dragDropStatusLabel;
    private UiTooltip? _dragDropTooltip;
    private UiTooltipRegion? _dragDropTooltipRegion;
    private readonly List<string> _dragDropItems = new();
    private readonly List<UiDragDropSource> _dragDropItemSources = new();
    private readonly List<UiDragDropTarget> _dragDropItemTargets = new();
    private readonly List<UiPanel> _dragDropItemPanels = new();
    private readonly List<UiLabel> _dragDropItemLabels = new();
    private UiLabel? _itemStatusLabel;
    private UiLabel? _windowStatusLabel;
    private UiLabel? _focusStatusLabel;
    private UiPoint _lastMousePosition;
    private UiLabel? _inputInfoLabel;
    private UiLabel? _shortcutLabel;
    private UiTextField? _focusInputField;
    private UiButton? _focusButton;
    private UiLabel? _focusResultLabel;
    private string _lastShortcutText = "none";
    private UiLabel? _toolsLabel;
    private UiTextLink? _toolsLink;
    private UiButton? _aboutButton;
    private UiButton? _themeDarkButton;
    private UiButton? _themeLightButton;
    private UiLabel? _examplesLabel;
    private UiLabel? _examplesHintLabel;

    private UiMenuBar.MenuItem? _examplesMenuAllItem;
    private UiMenuBar.MenuItem? _examplesMenuBasicsItem;
    private UiMenuBar.MenuItem? _examplesMenuWidgetsItem;
    private UiMenuBar.MenuItem? _examplesMenuTextEditorItem;
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
    private readonly List<string> _enumSliderItems = new()
    {
        "Default",
        "Fast",
        "Accurate",
        "Ultra"
    };

    private enum ExamplePanel
    {
        All,
        Basics,
        Widgets,
        TextEditor,
        Docking,
        Clipping,
        Serialization,
        Custom
    }

    private int _buttonClicks;

    static ExamplesUi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp437Encoding = Encoding.GetEncoding(437);
    }

    public ExamplesUi(IUiRenderer renderer, TinyBitmapFont font)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _font = font ?? throw new ArgumentNullException(nameof(font));
        BuildUi();
    }

    public event Action? ExitRequested;

    public void Update(UiInputState input, float deltaSeconds, int width, int height, bool saveRequested, bool loadRequested)
    {
        if (_context == null)
        {
            return;
        }

        _lastMousePosition = input.MousePosition;

        if (saveRequested)
        {
            SaveUiState();
        }

        if (loadRequested)
        {
            LoadUiState();
        }

        UpdateLayout(width, height);
        _context.Update(input, deltaSeconds);
        UpdateWindowContent();
        UpdateStatusLabel();
        UpdateWidgetPanel();
        UpdateInputPanels(input, deltaSeconds);
    }

    public void Render()
    {
        if (_context == null)
        {
            return;
        }

        _context.Render(_renderer);
    }

    public void SetTitleText(string text)
    {
        if (_titleLabel != null)
        {
            _titleLabel.Text = text ?? string.Empty;
        }
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
            Text = "OpenControls Examples",
            Color = UiColor.White,
            Scale = FontScale
        };

        _helpLabel = new UiLabel
        {
            Text = "Drag tabs to reorder; drag outside to float; use targets to dock/split; drag windows by title bar; Tab to move focus; Enter/Space to activate.",
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
            Placeholder = "Enter text",
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

        _invisibleButtonLabel = new UiLabel
        {
            Text = "Invisible Button",
            Color = UiColor.White,
            Scale = FontScale
        };

        _invisibleButtonPanel = new UiPanel
        {
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100),
            BorderThickness = 1
        };

        _invisibleButtonStatus = new UiLabel
        {
            Text = "Invisible: Idle",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _invisibleButton = new UiInvisibleButton();
        _invisibleButton.Clicked += () => _invisibleButtonClicks++;
        _invisibleButtonPanel.AddChild(_invisibleButton);

        _basicButtonsLabel = new UiLabel
        {
            Text = "Buttons",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _basicPrimaryButton = new UiButton
        {
            Text = "Primary",
            TextScale = FontScale
        };

        _basicDangerButton = new UiButton
        {
            Text = "Danger",
            TextScale = FontScale,
            Background = new UiColor(120, 60, 70),
            HoverBackground = new UiColor(150, 80, 90),
            PressedBackground = new UiColor(100, 50, 60),
            Border = new UiColor(170, 90, 100)
        };

        _basicRepeatButton = new UiButton
        {
            Text = "Repeat (hold)",
            TextScale = FontScale
        };
        _basicRepeatButton.Clicked += () => _basicRepeatTicks++;

        _basicRepeatStatusLabel = new UiLabel
        {
            Text = "Repeats: 0",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _basicInputLabel = new UiLabel
        {
            Text = "Input Text",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _basicInputText = new UiTextField
        {
            TextScale = FontScale,
            MaxLength = 48,
            Placeholder = "Type here",
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _basicInputIntLabel = new UiLabel
        {
            Text = "Input Int",
            Color = UiColor.White,
            Scale = FontScale
        };

        _basicInputInt = new UiInputInt
        {
            Value = 42,
            Min = 0,
            Max = 100,
            Clamp = true,
            TextScale = FontScale
        };

        _basicInputFloatLabel = new UiLabel
        {
            Text = "Input Float",
            Color = UiColor.White,
            Scale = FontScale
        };

        _basicInputFloat = new UiInputFloat
        {
            Value = 0.5f,
            Min = 0,
            Max = 1,
            Clamp = true,
            TextScale = FontScale
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

        _verticalProgressLabel = new UiLabel
        {
            Text = "Vertical Progress",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _verticalProgress = new UiProgressBar
        {
            Min = 0f,
            Max = 1f,
            Value = _volumeSlider.Value,
            FillDirection = UiProgressBarFillDirection.BottomToTop,
            ShowText = false
        };

        _vuMeterLabel = new UiLabel
        {
            Text = "Segmented VU Meter",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        UiColor[] vuColors =
        {
            new UiColor(64, 180, 96),
            new UiColor(64, 180, 96),
            new UiColor(64, 180, 96),
            new UiColor(64, 180, 96),
            new UiColor(64, 180, 96),
            new UiColor(64, 180, 96),
            new UiColor(200, 180, 80),
            new UiColor(200, 180, 80),
            new UiColor(210, 90, 80),
            new UiColor(210, 90, 80)
        };

        _vuMeter = new UiProgressBar
        {
            Min = 0f,
            Max = 1f,
            Value = _volumeSlider.Value,
            FillDirection = UiProgressBarFillDirection.BottomToTop,
            SegmentCount = vuColors.Length,
            SegmentGap = 2,
            SegmentFillColors = vuColors,
            ShowText = false
        };

        _radialProgressLabel = new UiLabel
        {
            Text = "Radial Progress",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _radialProgress = new UiProgressBar
        {
            Min = 0f,
            Max = 1f,
            Value = _volumeSlider.Value,
            Style = UiProgressBarStyle.Radial,
            RadialThickness = 6,
            ShowText = false
        };

        _angleSliderLabel = new UiLabel
        {
            Text = "Angle Slider",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _angleSlider = new UiSliderAngle
        {
            MinDegrees = -180f,
            MaxDegrees = 180f,
            Value = 0f,
            TextScale = FontScale,
            ValueFormat = "0 deg",
            ShowValue = true
        };

        _angleSliderValueLabel = new UiLabel
        {
            Text = "Angle: 0 deg (0.00 rad)",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _verticalSliderLabel = new UiLabel
        {
            Text = "Vertical Slider",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _verticalSlider = new UiVSlider
        {
            Min = 0f,
            Max = 1f,
            Value = 0.35f,
            TextScale = FontScale,
            ShowValue = false
        };

        _verticalSliderValueLabel = new UiLabel
        {
            Text = "Vertical: 35%",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _sliderFlagsLabel = new UiLabel
        {
            Text = "Slider Flags",
            Color = UiColor.White,
            Scale = FontScale
        };

        _sliderWholeNumbersCheckbox = new UiCheckbox
        {
            Text = "Whole Numbers",
            TextScale = FontScale
        };
        _sliderWholeNumbersCheckbox.CheckedChanged += _ => UpdateSliderFlags();

        _sliderStepCheckbox = new UiCheckbox
        {
            Text = "Snap to 0.1",
            TextScale = FontScale
        };
        _sliderStepCheckbox.CheckedChanged += _ => UpdateSliderFlags();
        UpdateSliderFlags();

        _enumSliderLabel = new UiLabel
        {
            Text = "Enum Slider",
            Color = UiColor.White,
            Scale = FontScale
        };

        _enumSlider = new UiSlider
        {
            Min = 0,
            Max = Math.Max(0, _enumSliderItems.Count - 1),
            Value = 1,
            WholeNumbers = true,
            TextScale = FontScale,
            ShowValue = true
        };

        _enumSliderValueLabel = new UiLabel
        {
            Text = "Enum: Fast",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
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

        _waveformLabel = new UiLabel
        {
            Text = "Waveform: Min/Max render",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _waveform = new UiWaveform
        {
            Samples = WaveformSamples,
            AutoScale = true,
            RenderMode = UiWaveformRenderMode.MinMax,
            ShowZeroLine = true,
            Padding = 4,
            LineThickness = 1,
            ZeroLineThickness = 1
        };

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

        _textEditorLabel = new UiLabel
        {
            Text = "Text Editor",
            Color = UiColor.White,
            Scale = FontScale
        };

        _textEditor = new UiTextEditor
        {
            TextScale = FontScale,
            ShowLineNumbers = true,
            VerticalScrollbar = UiScrollbarVisibility.Always,
            HorizontalScrollbar = UiScrollbarVisibility.Auto,
            Background = new UiColor(20, 22, 28),
            Border = new UiColor(60, 70, 90),
            LineNumberBackground = new UiColor(16, 18, 24)
        };
        _textEditor.SetText(SampleEditorText);

        _textEditorDemo = new UiTextEditor
        {
            Id = "text-editor-demo",
            TextScale = FontScale,
            ShowLineNumbers = true,
            VerticalScrollbar = UiScrollbarVisibility.Always,
            HorizontalScrollbar = UiScrollbarVisibility.Auto,
            Background = new UiColor(20, 22, 28),
            Border = new UiColor(60, 70, 90),
            LineNumberBackground = new UiColor(16, 18, 24)
        };
        _textEditorDemo.SetText(SampleEditorTextLarge);

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

        _tabBarLabel = new UiLabel
        {
            Text = "Tab Bar",
            Color = UiColor.White,
            Scale = FontScale
        };

        _tabBar = new UiTabBar
        {
            TabTextScale = FontScale,
            TabPadding = 8,
            TabSpacing = 4
        };

        _tabOverview = new UiTabItem { Text = "Overview" };
        _tabDetails = new UiTabItem { Text = "Details" };
        _tabSettings = new UiTabItem { Text = "Settings" };

        _tabOverviewLabel = new UiLabel
        {
            Text = "Overview: system status and summary.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tabDetailsLabel = new UiLabel
        {
            Text = "Details: metrics, logs, and diagnostics.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tabSettingsLabel = new UiLabel
        {
            Text = "Settings: tweak sliders and toggles.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tabLeadingButton = new UiTabItemButton
        {
            Text = "+",
            Placement = UiTabItemButtonPlacement.Leading,
            AutoSize = true
        };

        _tabTrailingButton = new UiTabItemButton
        {
            Text = "...",
            Placement = UiTabItemButtonPlacement.Trailing,
            AutoSize = true
        };

        _tabButtonStatusLabel = new UiLabel
        {
            Text = "Tab buttons: idle",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _tabLeadingButton.Clicked += _ => _tabButtonStatusLabel.Text = "Tab buttons: add";
        _tabTrailingButton.Clicked += _ => _tabButtonStatusLabel.Text = "Tab buttons: menu";

        _tabOverview.AddChild(_tabOverviewLabel);
        _tabDetails.AddChild(_tabDetailsLabel);
        _tabSettings.AddChild(_tabSettingsLabel);

        _tabBar.AddChild(_tabLeadingButton);
        _tabBar.AddChild(_tabTrailingButton);
        _tabBar.AddChild(_tabOverview);
        _tabBar.AddChild(_tabDetails);
        _tabBar.AddChild(_tabSettings);

        _splitterLabel = new UiLabel
        {
            Text = "Splitter",
            Color = UiColor.White,
            Scale = FontScale
        };

        UiColor splitterPanelA = new UiColor(26, 30, 42);
        UiColor splitterPanelB = new UiColor(20, 24, 34);
        UiColor splitterBorder = new UiColor(60, 70, 90);

        _splitterVerticalLeftPanel = new UiPanel
        {
            Background = splitterPanelA,
            Border = splitterBorder
        };

        _splitterVerticalRightPanel = new UiPanel
        {
            Background = splitterPanelB,
            Border = splitterBorder
        };

        _splitterVertical = new UiSplitter
        {
            Orientation = UiSplitterOrientation.Vertical,
            Color = new UiColor(30, 36, 48),
            HoverColor = new UiColor(44, 54, 72),
            ActiveColor = new UiColor(70, 86, 112)
        };
        _splitterVertical.Dragged += delta => _splitterVerticalLeftWidth += delta;

        _splitterHorizontalTopPanel = new UiPanel
        {
            Background = splitterPanelA,
            Border = splitterBorder
        };

        _splitterHorizontalBottomPanel = new UiPanel
        {
            Background = splitterPanelB,
            Border = splitterBorder
        };

        _splitterHorizontal = new UiSplitter
        {
            Orientation = UiSplitterOrientation.Horizontal,
            Color = new UiColor(30, 36, 48),
            HoverColor = new UiColor(44, 54, 72),
            ActiveColor = new UiColor(70, 86, 112)
        };
        _splitterHorizontal.Dragged += delta => _splitterHorizontalTopHeight += delta;

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

        _layoutHorizontalLabel = new UiLabel
        {
            Text = "Basic Horizontal Layout",
            Color = UiColor.White,
            Scale = FontScale
        };

        _layoutButtonLeft = new UiButton
        {
            Text = "Left",
            TextScale = FontScale
        };

        _layoutButtonCenter = new UiButton
        {
            Text = "Center",
            TextScale = FontScale
        };

        _layoutButtonRight = new UiButton
        {
            Text = "Right",
            TextScale = FontScale
        };

        _layoutDummyLabel = new UiLabel
        {
            Text = "Dummy Spacer",
            Color = UiColor.White,
            Scale = FontScale
        };

        _layoutDummyPanel = new UiPanel
        {
            Background = new UiColor(24, 28, 38),
            Border = new UiColor(70, 80, 100)
        };

        _layoutWrapLabel = new UiLabel
        {
            Text = "Manual Wrapping",
            Color = UiColor.White,
            Scale = FontScale
        };

        _layoutWrapBlock = new UiTextBlock
        {
            Text = "Wrap this line manually by constraining the width. The block should break across multiple lines.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale,
            Wrap = true,
            LineSpacing = 2,
            Padding = 2
        };

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

        _configHelpLabel = new UiLabel
        {
            Text = "Help",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _configHelpText = new UiTextBlock
        {
            Text = "Use Tab to move focus, Enter/Space to activate, and drag window title bars to move.",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale,
            Wrap = true,
            LineSpacing = 2,
            Padding = 2
        };

        _configBackendLabel = new UiLabel
        {
            Text = "Configuration / Backend Flags",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _configDockingCheckbox = new UiCheckbox
        {
            Text = "Enable Docking (simulated)",
            TextScale = FontScale,
            Checked = true
        };

        _configViewportCheckbox = new UiCheckbox
        {
            Text = "Enable Multi-Viewport (simulated)",
            TextScale = FontScale
        };

        _configStyleLabel = new UiLabel
        {
            Text = "Configuration / Style & Fonts",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _configLargeFontCheckbox = new UiCheckbox
        {
            Text = "Large help text",
            TextScale = FontScale
        };

        _configCaptureLabel = new UiLabel
        {
            Text = "Configuration / Capture & Logging",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _configCaptureKeyboardCheckbox = new UiCheckbox
        {
            Text = "Capture Keyboard",
            TextScale = FontScale,
            Checked = true
        };

        _configCaptureMouseCheckbox = new UiCheckbox
        {
            Text = "Capture Mouse",
            TextScale = FontScale,
            Checked = true
        };

        _windowOptionsLabel = new UiLabel
        {
            Text = "Window options",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _windowAllowResizeCheckbox = new UiCheckbox
        {
            Text = "Allow Resize",
            TextScale = FontScale,
            Checked = true
        };

        _windowShowTitleCheckbox = new UiCheckbox
        {
            Text = "Show Title Bar",
            TextScale = FontScale,
            Checked = true
        };

        _windowAllowDragCheckbox = new UiCheckbox
        {
            Text = "Allow Drag",
            TextScale = FontScale,
            Checked = true
        };

        _windowShowGripCheckbox = new UiCheckbox
        {
            Text = "Show Resize Grip",
            TextScale = FontScale,
            Checked = true
        };

        _demoMenuStatusLabel = new UiLabel
        {
            Text = "Menu: Ready",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _demoMenuBar = new UiMenuBar
        {
            TextScale = FontScale,
            BarHeight = 24,
            MeasureTextWidth = (text, scale) => _renderer?.MeasureTextWidth(text, scale) ?? text.Length * 6 * scale,
            MeasureTextHeight = scale => _renderer?.MeasureTextHeight(scale) ?? 7 * scale
        };

        void DemoMenuStatus(UiMenuBar.MenuItem item)
        {
            if (_demoMenuStatusLabel != null)
            {
                _demoMenuStatusLabel.Text = $"Menu: {item.Text}";
            }
        }

        UiMenuBar.MenuItem demoFileMenu = new() { Text = "File" };
        demoFileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "New", Clicked = DemoMenuStatus });
        demoFileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Open", Clicked = DemoMenuStatus });
        demoFileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Save", Clicked = DemoMenuStatus });
        demoFileMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        demoFileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Close", Clicked = DemoMenuStatus });

        UiMenuBar.MenuItem demoEditMenu = new() { Text = "Edit" };
        demoEditMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Undo", Shortcut = "Ctrl+Z", Clicked = DemoMenuStatus });
        demoEditMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Redo", Shortcut = "Ctrl+Y", Clicked = DemoMenuStatus });
        demoEditMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        demoEditMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Copy", Shortcut = "Ctrl+C", Clicked = DemoMenuStatus });
        demoEditMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Paste", Shortcut = "Ctrl+V", Clicked = DemoMenuStatus });

        UiMenuBar.MenuItem demoViewMenu = new() { Text = "View" };
        UiMenuBar.MenuItem demoGridToggle = new() { Text = "Show Grid", IsCheckable = true, Checked = true };
        demoGridToggle.Clicked = DemoMenuStatus;
        demoViewMenu.Items.Add(demoGridToggle);

        _demoMenuBar.Items.Add(demoFileMenu);
        _demoMenuBar.Items.Add(demoEditMenu);
        _demoMenuBar.Items.Add(demoViewMenu);

        _textHeaderLabel = new UiLabel
        {
            Text = "Text Samples",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _textColoredLabel = new UiLabel
        {
            Text = "Colored Text",
            Color = UiColor.White,
            Scale = FontScale
        };

        _textColoredSample = new UiLabel
        {
            Text = "Success: build completed",
            Color = new UiColor(120, 200, 120),
            Scale = FontScale
        };

        _textFontSizeLabel = new UiLabel
        {
            Text = "Font Size",
            Color = UiColor.White,
            Scale = FontScale
        };

        _textFontSizeSmall = new UiLabel
        {
            Text = "Scale 1",
            Color = new UiColor(180, 190, 210),
            Scale = 1
        };

        _textFontSizeLarge = new UiLabel
        {
            Text = "Scale 3",
            Color = new UiColor(220, 230, 240),
            Scale = 3
        };

        _textWrappedBlock = new UiTextBlock
        {
            Text = "Wrapping text demo: OpenControls keeps layout explicit, but long strings can still wrap inside text blocks with custom padding and line spacing.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale,
            Wrap = true,
            LineSpacing = 2,
            Padding = 2
        };

        _textUtf8Label = new UiLabel
        {
            Text = "UTF-8: caf\u00E9, na\u00EFve, \u6771\u4EAC",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _textLink = new UiTextLink
        {
            Text = "OpenControls manual",
            Url = "https://github.com/PippinwareLLC/OpenControls/blob/main/README.MD",
            OpenUrlOnClick = true,
            TextScale = FontScale
        };

        _textFilterLabel = new UiLabel
        {
            Text = "Text Filter",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _textFilterField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "Filter list",
            MaxLength = 48,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _textFilterList = new UiListBox
        {
            Items = _textFilterFilteredItems,
            TextScale = FontScale,
            AllowDeselect = true
        };

        _textFilterStatusLabel = new UiLabel
        {
            Text = "Filter: All",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _textFilterItems.AddRange(new[]
        {
            "Engine/Renderer",
            "Engine/Input",
            "Gameplay/Camera",
            "Gameplay/Inventory",
            "UI/HUD",
            "UI/Menu",
            "Audio/Mixer",
            "Tools/Console",
            "Tools/Profiler",
            "World/Lighting"
        });

        _textInputLabel = new UiLabel
        {
            Text = "Text Input",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _multiLineInput = new UiTextEditor
        {
            TextScale = FontScale,
            ShowLineNumbers = true,
            SyntaxMode = UiTextEditorSyntaxMode.CSharp
        };
        _multiLineInput.SetText("Line 1: multi-line input\nLine 2: supports scrolling\nLine 3: editable text");

        _filteredInputLabel = new UiLabel
        {
            Text = "Filtered Input (digits only)",
            Color = UiColor.White,
            Scale = FontScale
        };

        _filteredInputField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "1234",
            MaxLength = 8,
            CharacterFilter = char.IsDigit,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _passwordInputLabel = new UiLabel
        {
            Text = "Password Input (masked)",
            Color = UiColor.White,
            Scale = FontScale
        };

        _passwordField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "Enter password",
            MaxLength = 24,
            TextColor = UiColor.Transparent,
            CaretColor = UiColor.White,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _passwordMaskLabel = new UiLabel
        {
            Text = string.Empty,
            Color = new UiColor(220, 220, 230),
            Scale = FontScale
        };

        _passwordHintLabel = new UiLabel
        {
            Text = "Mask overlay is shown above the input field.",
            Color = new UiColor(160, 170, 190),
            Scale = FontScale
        };

        _completionInputLabel = new UiLabel
        {
            Text = "Completion + History",
            Color = UiColor.White,
            Scale = FontScale
        };

        _completionField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "Type 'he' or 'lo'",
            MaxLength = 32,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _completionHintLabel = new UiLabel
        {
            Text = "Suggestions: (none)",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _completionHistoryLabel = new UiLabel
        {
            Text = "History: (empty)",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _resizeInputLabel = new UiLabel
        {
            Text = "Resize Callback (auto width)",
            Color = UiColor.White,
            Scale = FontScale
        };

        _resizeInputField = new UiTextField
        {
            Text = "Auto width",
            TextScale = FontScale,
            MaxLength = 48,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _elidingInputLabel = new UiLabel
        {
            Text = "Eliding + Alignment",
            Color = UiColor.White,
            Scale = FontScale
        };

        _elidingInputField = new UiTextField
        {
            Text = "This text is intentionally too long for its field.",
            TextScale = FontScale,
            MaxLength = 80,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _elidingResultLabel = new UiLabel
        {
            Text = string.Empty,
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _miscInputLabel = new UiLabel
        {
            Text = "Miscellaneous",
            Color = UiColor.White,
            Scale = FontScale
        };

        _miscInputField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "Hinted input field",
            MaxLength = 40,
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _bulletsLabel = new UiLabel
        {
            Text = "Bullets",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        string[] bulletTexts =
        {
            "Bullet list entry one",
            "Bullet list entry two",
            "Bullet list entry three"
        };
        foreach (string bulletText in bulletTexts)
        {
            UiBulletText bullet = new UiBulletText
            {
                Text = bulletText,
                TextColor = new UiColor(200, 210, 230),
                BulletColor = new UiColor(120, 180, 220),
                TextScale = FontScale
            };
            _bulletItems.Add(bullet);
        }

        _imagesLabel = new UiLabel
        {
            Text = "Images",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _imagePreview = new UiImage
        {
            Background = new UiColor(18, 22, 32),
            Border = new UiColor(70, 80, 100),
            ShowCheckerboard = true,
            CheckerSize = 6,
            CornerRadius = 4
        };

        _imageButton = new UiImageButton
        {
            ShowCheckerboard = true,
            CornerRadius = 4
        };
        _imageButton.Clicked += () => _imageButtonClicks++;

        _imageButtonStatus = new UiLabel
        {
            Text = "Image button: 0 clicks",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _multiComponentLabel = new UiLabel
        {
            Text = "Multi-component Widgets",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _inputFloat2 = new UiInputFloat2 { FieldTextScale = FontScale };
        _inputFloat2.ValueX = 0.1f;
        _inputFloat2.ValueY = 0.8f;

        _inputFloat3 = new UiInputFloat3 { FieldTextScale = FontScale };
        _inputFloat3.ValueX = 0.2f;
        _inputFloat3.ValueY = 0.3f;
        _inputFloat3.ValueZ = 0.4f;

        _inputFloat4 = new UiInputFloat4 { FieldTextScale = FontScale };
        _inputFloat4.ValueX = 0.1f;
        _inputFloat4.ValueY = 0.2f;
        _inputFloat4.ValueZ = 0.3f;
        _inputFloat4.ValueW = 0.4f;

        _inputInt2 = new UiInputInt2 { FieldTextScale = FontScale };
        _inputInt2.ValueX = 2;
        _inputInt2.ValueY = 5;

        _inputInt3 = new UiInputInt3 { FieldTextScale = FontScale };
        _inputInt3.ValueX = 1;
        _inputInt3.ValueY = 2;
        _inputInt3.ValueZ = 3;

        _inputInt4 = new UiInputInt4 { FieldTextScale = FontScale };
        _inputInt4.ValueX = 1;
        _inputInt4.ValueY = 2;
        _inputInt4.ValueZ = 3;
        _inputInt4.ValueW = 4;

        _sliderFloat2 = new UiSliderFloat2 { TextScale = FontScale, Min = 0f, Max = 1f };
        _sliderFloat2.ValueX = 0.2f;
        _sliderFloat2.ValueY = 0.6f;

        _sliderFloat3 = new UiSliderFloat3 { TextScale = FontScale, Min = 0f, Max = 1f };
        _sliderFloat3.ValueX = 0.1f;
        _sliderFloat3.ValueY = 0.5f;
        _sliderFloat3.ValueZ = 0.8f;

        _sliderFloat4 = new UiSliderFloat4 { TextScale = FontScale, Min = 0f, Max = 1f };
        _sliderFloat4.ValueX = 0.1f;
        _sliderFloat4.ValueY = 0.3f;
        _sliderFloat4.ValueZ = 0.6f;
        _sliderFloat4.ValueW = 0.9f;

        _sliderInt2 = new UiSliderInt2 { TextScale = FontScale, Min = 0, Max = 10, WholeNumbers = true };
        _sliderInt2.ValueX = 3;
        _sliderInt2.ValueY = 7;

        _sliderInt3 = new UiSliderInt3 { TextScale = FontScale, Min = 0, Max = 10, WholeNumbers = true };
        _sliderInt3.ValueX = 2;
        _sliderInt3.ValueY = 4;
        _sliderInt3.ValueZ = 8;

        _sliderInt4 = new UiSliderInt4 { TextScale = FontScale, Min = 0, Max = 10, WholeNumbers = true };
        _sliderInt4.ValueX = 1;
        _sliderInt4.ValueY = 3;
        _sliderInt4.ValueZ = 5;
        _sliderInt4.ValueW = 7;

        _multiComponentStatusLabel = new UiLabel
        {
            Text = "Vectors: (0,0,0,0)",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _disableLabel = new UiLabel
        {
            Text = "Disable Blocks",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _disableToggle = new UiCheckbox
        {
            Text = "Enable group",
            TextScale = FontScale,
            Checked = true
        };

        _disabledGroup = new UiDisabledGroup();

        _disabledButton = new UiButton
        {
            Text = "Disabled Button",
            TextScale = FontScale
        };

        _disabledSlider = new UiSlider
        {
            Min = 0f,
            Max = 1f,
            Value = 0.7f,
            TextScale = FontScale,
            ShowValue = true
        };

        _disabledField = new UiTextField
        {
            TextScale = FontScale,
            Text = "Disabled input",
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _disabledGroup.AddChild(_disabledButton);
        _disabledGroup.AddChild(_disabledSlider);
        _disabledGroup.AddChild(_disabledField);

        _dragDropLabel = new UiLabel
        {
            Text = "Drag and Drop",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _dragDropSource = new UiDragDropSource
        {
            PayloadType = "color",
            PayloadData = new UiColor(120, 180, 220)
        };

        _dragDropSourcePanel = new UiPanel
        {
            Background = new UiColor(24, 28, 38),
            Border = new UiColor(70, 80, 100)
        };

        _dragDropSourceLabel = new UiLabel
        {
            Text = "Drag swatch",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _dragDropSourcePanel.AddChild(_dragDropSourceLabel);
        _dragDropSource.AddChild(_dragDropSourcePanel);

        _dragDropTarget = new UiDragDropTarget
        {
            PayloadType = "color"
        };

        _dragDropTargetPanel = new UiPanel
        {
            Background = new UiColor(28, 32, 44),
            Border = new UiColor(90, 100, 120)
        };

        _dragDropTargetLabel = new UiLabel
        {
            Text = "Drop swatch",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _dragDropTargetPanel.AddChild(_dragDropTargetLabel);
        _dragDropTarget.AddChild(_dragDropTargetPanel);

        _dragDropStatusLabel = new UiLabel
        {
            Text = "Drag status: idle",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _dragDropTooltip = new UiTooltip
        {
            TextScale = FontScale,
            Background = new UiColor(20, 24, 34),
            Border = new UiColor(70, 80, 100),
            TextColor = UiColor.White
        };

        _dragDropTooltipRegion = new UiTooltipRegion
        {
            Text = "Release to drop",
            Tooltip = _dragDropTooltip
        };
        _dragDropTarget.AddChild(_dragDropTooltipRegion);

        _dragDropItems.AddRange(new[]
        {
            "Item A",
            "Item B",
            "Item C",
            "Item D"
        });

        for (int i = 0; i < _dragDropItems.Count; i++)
        {
            int index = i;
            UiDragDropSource source = new UiDragDropSource
            {
                PayloadType = "reorder",
                PayloadData = index
            };

            UiDragDropTarget target = new UiDragDropTarget
            {
                PayloadType = "reorder"
            };

            UiPanel panel = new UiPanel
            {
                Background = new UiColor(20, 24, 34),
                Border = new UiColor(70, 80, 100)
            };

            UiLabel label = new UiLabel
            {
                Text = _dragDropItems[index],
                Color = new UiColor(200, 210, 230),
                Scale = FontScale
            };

            panel.AddChild(label);
            source.AddChild(panel);
            source.AddChild(target);

            target.PayloadDropped += payload =>
            {
                if (payload.Data is int fromIndex && fromIndex != index)
                {
                    string temp = _dragDropItems[index];
                    _dragDropItems[index] = _dragDropItems[fromIndex];
                    _dragDropItems[fromIndex] = temp;
                }
            };

            _dragDropItemSources.Add(source);
            _dragDropItemTargets.Add(target);
            _dragDropItemPanels.Add(panel);
            _dragDropItemLabels.Add(label);
        }

        _dragDropTarget.PayloadDropped += payload =>
        {
            if (payload.Data is UiColor color && _dragDropTargetPanel != null)
            {
                _dragDropTargetPanel.Background = color;
                _dragDropStatusLabel.Text = "Drag status: dropped color";
            }
        };

        _itemStatusLabel = new UiLabel
        {
            Text = "Item Status: idle",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _windowStatusLabel = new UiLabel
        {
            Text = "Window Status: idle",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _focusStatusLabel = new UiLabel
        {
            Text = "Focus: none",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _inputInfoLabel = new UiLabel
        {
            Text = "Input: Mouse (0,0)",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _shortcutLabel = new UiLabel
        {
            Text = "Shortcut: none",
            Color = new UiColor(170, 180, 200),
            Scale = FontScale
        };

        _focusInputField = new UiTextField
        {
            TextScale = FontScale,
            Placeholder = "Focusable field",
            CaretIndexFromPoint = GetCaretIndexFromPoint
        };

        _focusButton = new UiButton
        {
            Text = "Focus field",
            TextScale = FontScale
        };
        _focusButton.Clicked += () =>
        {
            if (_context != null && _focusInputField != null)
            {
                _context.Focus.RequestFocus(_focusInputField);
            }
        };

        _focusResultLabel = new UiLabel
        {
            Text = "Focused: none",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _toolsLabel = new UiLabel
        {
            Text = "Tools",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _toolsLink = new UiTextLink
        {
            Text = "OpenControls docs",
            Url = "https://github.com/PippinwareLLC/OpenControls/blob/main/API.MD",
            OpenUrlOnClick = true,
            TextScale = FontScale
        };

        _aboutButton = new UiButton
        {
            Text = "About",
            TextScale = FontScale
        };
        _aboutButton.Clicked += () =>
        {
            if (_modalLabel != null)
            {
                _modalLabel.Text = "OpenControls: lightweight UI demos and widgets.";
            }

            _modal?.Open();
        };

        _themeDarkButton = new UiButton
        {
            Text = "Theme: Dark",
            TextScale = FontScale
        };
        _themeDarkButton.Clicked += () =>
        {
            if (_rootPanel != null)
            {
                _rootPanel.Background = new UiColor(12, 14, 20);
            }

            _menuStatus = "Menu: Theme Dark";
        };

        _themeLightButton = new UiButton
        {
            Text = "Theme: Light",
            TextScale = FontScale
        };
        _themeLightButton.Clicked += () =>
        {
            if (_rootPanel != null)
            {
                _rootPanel.Background = new UiColor(24, 26, 32);
            }

            _menuStatus = "Menu: Theme Light";
        };

        _examplesLabel = new UiLabel
        {
            Text = "Examples",
            Color = UiColor.White,
            Scale = FontScale,
            Bold = true
        };

        _examplesHintLabel = new UiLabel
        {
            Text = "See Console, Assets, Inspector, and other example windows.",
            Color = new UiColor(200, 210, 230),
            Scale = FontScale
        };

        _widgetsTree = new UiTreeNode
        {
            Text = "Widgets",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsConfigTree = new UiTreeNode
        {
            Text = "Help & Configuration",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsMenuTree = new UiTreeNode
        {
            Text = "Menu",
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

        _widgetsBulletsTree = new UiTreeNode
        {
            Text = "Bullets",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTextTree = new UiTreeNode
        {
            Text = "Text",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTextFilterTree = new UiTreeNode
        {
            Text = "Text Filter",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsTextInputTree = new UiTreeNode
        {
            Text = "Text Input",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsImagesTree = new UiTreeNode
        {
            Text = "Images",
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

        _widgetsDragDropTree = new UiTreeNode
        {
            Text = "Drag and Drop",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsDisableTree = new UiTreeNode
        {
            Text = "Disable Blocks",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsMultiComponentTree = new UiTreeNode
        {
            Text = "Multi-component Widgets",
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

        _widgetsWaveformTree = new UiTreeNode
        {
            Text = "Waveforms",
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

        _widgetsStatusTree = new UiTreeNode
        {
            Text = "Item & Window Status",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsInputFocusTree = new UiTreeNode
        {
            Text = "Inputs & Focus",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsToolsTree = new UiTreeNode
        {
            Text = "Tools",
            TextScale = FontScale,
            ArrowColor = UiColor.White,
            ContentPadding = 4,
            IsOpen = false
        };

        _widgetsExamplesTree = new UiTreeNode
        {
            Text = "Examples",
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

        _widgetsConfigTree.AddChild(_configHelpLabel);
        _widgetsConfigTree.AddChild(_configHelpText);
        _widgetsConfigTree.AddChild(_configLargeFontCheckbox);
        _widgetsConfigTree.AddChild(_configBackendLabel);
        _widgetsConfigTree.AddChild(_configDockingCheckbox);
        _widgetsConfigTree.AddChild(_configViewportCheckbox);
        _widgetsConfigTree.AddChild(_configStyleLabel);
        _widgetsConfigTree.AddChild(_configCaptureLabel);
        _widgetsConfigTree.AddChild(_configCaptureKeyboardCheckbox);
        _widgetsConfigTree.AddChild(_configCaptureMouseCheckbox);
        _widgetsConfigTree.AddChild(_windowOptionsLabel);
        _widgetsConfigTree.AddChild(_windowAllowResizeCheckbox);
        _widgetsConfigTree.AddChild(_windowShowTitleCheckbox);
        _widgetsConfigTree.AddChild(_windowAllowDragCheckbox);
        _widgetsConfigTree.AddChild(_windowShowGripCheckbox);

        _widgetsMenuTree.AddChild(_demoMenuBar);
        _widgetsMenuTree.AddChild(_demoMenuStatusLabel);

        _widgetsBasicTree.AddChild(_snapCheckbox);
        _widgetsBasicTree.AddChild(_gizmoCheckbox);
        _widgetsBasicTree.AddChild(_widgetsSeparator);
        _widgetsBasicTree.AddChild(_qualityLow);
        _widgetsBasicTree.AddChild(_qualityMedium);
        _widgetsBasicTree.AddChild(_qualityHigh);
        _widgetsBasicTree.AddChild(_invisibleButtonLabel);
        _widgetsBasicTree.AddChild(_invisibleButtonPanel);
        _widgetsBasicTree.AddChild(_invisibleButtonStatus);
        _widgetsBasicTree.AddChild(_basicButtonsLabel);
        _widgetsBasicTree.AddChild(_basicPrimaryButton);
        _widgetsBasicTree.AddChild(_basicDangerButton);
        _widgetsBasicTree.AddChild(_basicRepeatButton);
        _widgetsBasicTree.AddChild(_basicRepeatStatusLabel);
        _widgetsBasicTree.AddChild(_basicInputLabel);
        _widgetsBasicTree.AddChild(_basicInputText);
        _widgetsBasicTree.AddChild(_basicInputIntLabel);
        _widgetsBasicTree.AddChild(_basicInputInt);
        _widgetsBasicTree.AddChild(_basicInputFloatLabel);
        _widgetsBasicTree.AddChild(_basicInputFloat);

        _widgetsBulletsTree.AddChild(_bulletsLabel);
        foreach (UiBulletText bullet in _bulletItems)
        {
            _widgetsBulletsTree.AddChild(bullet);
        }

        _widgetsTextTree.AddChild(_textHeaderLabel);
        _widgetsTextTree.AddChild(_textColoredLabel);
        _widgetsTextTree.AddChild(_textColoredSample);
        _widgetsTextTree.AddChild(_textFontSizeLabel);
        _widgetsTextTree.AddChild(_textFontSizeSmall);
        _widgetsTextTree.AddChild(_textFontSizeLarge);
        _widgetsTextTree.AddChild(_textWrappedBlock);
        _widgetsTextTree.AddChild(_textUtf8Label);
        _widgetsTextTree.AddChild(_textLink);

        _widgetsTextFilterTree.AddChild(_textFilterLabel);
        _widgetsTextFilterTree.AddChild(_textFilterField);
        _widgetsTextFilterTree.AddChild(_textFilterList);
        _widgetsTextFilterTree.AddChild(_textFilterStatusLabel);

        _widgetsTextInputTree.AddChild(_textInputLabel);
        _widgetsTextInputTree.AddChild(_multiLineInput);
        _widgetsTextInputTree.AddChild(_filteredInputLabel);
        _widgetsTextInputTree.AddChild(_filteredInputField);
        _widgetsTextInputTree.AddChild(_passwordInputLabel);
        _widgetsTextInputTree.AddChild(_passwordField);
        _widgetsTextInputTree.AddChild(_passwordMaskLabel);
        _widgetsTextInputTree.AddChild(_passwordHintLabel);
        _widgetsTextInputTree.AddChild(_completionInputLabel);
        _widgetsTextInputTree.AddChild(_completionField);
        _widgetsTextInputTree.AddChild(_completionHintLabel);
        _widgetsTextInputTree.AddChild(_completionHistoryLabel);
        _widgetsTextInputTree.AddChild(_resizeInputLabel);
        _widgetsTextInputTree.AddChild(_resizeInputField);
        _widgetsTextInputTree.AddChild(_elidingInputLabel);
        _widgetsTextInputTree.AddChild(_elidingInputField);
        _widgetsTextInputTree.AddChild(_elidingResultLabel);
        _widgetsTextInputTree.AddChild(_miscInputLabel);
        _widgetsTextInputTree.AddChild(_miscInputField);

        _widgetsImagesTree.AddChild(_imagesLabel);
        _widgetsImagesTree.AddChild(_imagePreview);
        _widgetsImagesTree.AddChild(_imageButton);
        _widgetsImagesTree.AddChild(_imageButtonStatus);

        _widgetsSliderTree.AddChild(_volumeSlider);
        _widgetsSliderTree.AddChild(_volumeProgress);
        _widgetsSliderTree.AddChild(_verticalProgressLabel);
        _widgetsSliderTree.AddChild(_verticalProgress);
        _widgetsSliderTree.AddChild(_vuMeterLabel);
        _widgetsSliderTree.AddChild(_vuMeter);
        _widgetsSliderTree.AddChild(_radialProgressLabel);
        _widgetsSliderTree.AddChild(_radialProgress);
        _widgetsSliderTree.AddChild(_angleSliderLabel);
        _widgetsSliderTree.AddChild(_angleSlider);
        _widgetsSliderTree.AddChild(_angleSliderValueLabel);
        _widgetsSliderTree.AddChild(_verticalSliderLabel);
        _widgetsSliderTree.AddChild(_verticalSlider);
        _widgetsSliderTree.AddChild(_verticalSliderValueLabel);
        _widgetsSliderTree.AddChild(_sliderFlagsLabel);
        _widgetsSliderTree.AddChild(_sliderWholeNumbersCheckbox);
        _widgetsSliderTree.AddChild(_sliderStepCheckbox);
        _widgetsSliderTree.AddChild(_enumSliderLabel);
        _widgetsSliderTree.AddChild(_enumSlider);
        _widgetsSliderTree.AddChild(_enumSliderValueLabel);

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

        _widgetsDragDropTree.AddChild(_dragDropLabel);
        _widgetsDragDropTree.AddChild(_dragDropSource);
        _widgetsDragDropTree.AddChild(_dragDropTarget);
        _widgetsDragDropTree.AddChild(_dragDropStatusLabel);
        foreach (UiDragDropSource source in _dragDropItemSources)
        {
            _widgetsDragDropTree.AddChild(source);
        }

        _widgetsDisableTree.AddChild(_disableLabel);
        _widgetsDisableTree.AddChild(_disableToggle);
        _widgetsDisableTree.AddChild(_disabledGroup);

        _widgetsMultiComponentTree.AddChild(_multiComponentLabel);
        _widgetsMultiComponentTree.AddChild(_inputFloat2);
        _widgetsMultiComponentTree.AddChild(_inputFloat3);
        _widgetsMultiComponentTree.AddChild(_inputFloat4);
        _widgetsMultiComponentTree.AddChild(_inputInt2);
        _widgetsMultiComponentTree.AddChild(_inputInt3);
        _widgetsMultiComponentTree.AddChild(_inputInt4);
        _widgetsMultiComponentTree.AddChild(_sliderFloat2);
        _widgetsMultiComponentTree.AddChild(_sliderFloat3);
        _widgetsMultiComponentTree.AddChild(_sliderFloat4);
        _widgetsMultiComponentTree.AddChild(_sliderInt2);
        _widgetsMultiComponentTree.AddChild(_sliderInt3);
        _widgetsMultiComponentTree.AddChild(_sliderInt4);
        _widgetsMultiComponentTree.AddChild(_multiComponentStatusLabel);

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

        _widgetsWaveformTree.AddChild(_waveformLabel);
        _widgetsWaveformTree.AddChild(_waveform);

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
        _widgetsLayoutTree.AddChild(_layoutHorizontalLabel);
        _widgetsLayoutTree.AddChild(_layoutButtonLeft);
        _widgetsLayoutTree.AddChild(_layoutButtonCenter);
        _widgetsLayoutTree.AddChild(_layoutButtonRight);
        _widgetsLayoutTree.AddChild(_layoutDummyLabel);
        _widgetsLayoutTree.AddChild(_layoutDummyPanel);
        _widgetsLayoutTree.AddChild(_layoutWrapLabel);
        _widgetsLayoutTree.AddChild(_layoutWrapBlock);
        _widgetsLayoutTree.AddChild(_tabBarLabel);
        _widgetsLayoutTree.AddChild(_tabBar);
        _widgetsLayoutTree.AddChild(_tabButtonStatusLabel);
        _widgetsLayoutTree.AddChild(_splitterLabel);
        _widgetsLayoutTree.AddChild(_splitterVerticalLeftPanel);
        _widgetsLayoutTree.AddChild(_splitterVertical);
        _widgetsLayoutTree.AddChild(_splitterVerticalRightPanel);
        _widgetsLayoutTree.AddChild(_splitterHorizontalTopPanel);
        _widgetsLayoutTree.AddChild(_splitterHorizontal);
        _widgetsLayoutTree.AddChild(_splitterHorizontalBottomPanel);
        _widgetsLayoutTree.AddChild(_textEditorLabel);
        _widgetsLayoutTree.AddChild(_textEditor);
        _widgetsLayoutTree.AddChild(_scrollPanelLabel);
        foreach (UiLabel item in _scrollPanelItems)
        {
            _widgetsLayoutTree.AddChild(item);
        }

        _widgetsStatusTree.AddChild(_itemStatusLabel);
        _widgetsStatusTree.AddChild(_windowStatusLabel);
        _widgetsStatusTree.AddChild(_focusStatusLabel);

        _widgetsInputFocusTree.AddChild(_inputInfoLabel);
        _widgetsInputFocusTree.AddChild(_shortcutLabel);
        _widgetsInputFocusTree.AddChild(_focusInputField);
        _widgetsInputFocusTree.AddChild(_focusButton);
        _widgetsInputFocusTree.AddChild(_focusResultLabel);

        _widgetsToolsTree.AddChild(_toolsLabel);
        _widgetsToolsTree.AddChild(_toolsLink);
        _widgetsToolsTree.AddChild(_aboutButton);
        _widgetsToolsTree.AddChild(_themeDarkButton);
        _widgetsToolsTree.AddChild(_themeLightButton);

        _widgetsExamplesTree.AddChild(_examplesLabel);
        _widgetsExamplesTree.AddChild(_examplesHintLabel);

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

        _widgetsTree.AddChild(_widgetsConfigTree);
        _widgetsTree.AddChild(_widgetsMenuTree);
        _widgetsTree.AddChild(_widgetsBasicTree);
        _widgetsTree.AddChild(_widgetsBulletsTree);
        _widgetsTree.AddChild(_widgetsTextTree);
        _widgetsTree.AddChild(_widgetsTextFilterTree);
        _widgetsTree.AddChild(_widgetsTextInputTree);
        _widgetsTree.AddChild(_widgetsImagesTree);
        _widgetsTree.AddChild(_widgetsSliderTree);
        _widgetsTree.AddChild(_widgetsStyleTree);
        _widgetsTree.AddChild(_widgetsDragTree);
        _widgetsTree.AddChild(_widgetsDragDropTree);
        _widgetsTree.AddChild(_widgetsDisableTree);
        _widgetsTree.AddChild(_widgetsMultiComponentTree);
        _widgetsTree.AddChild(_widgetsListTree);
        _widgetsTree.AddChild(_widgetsMultiSelectTree);
        _widgetsTree.AddChild(_widgetsComboTree);
        _widgetsTree.AddChild(_widgetsTableTree);
        _widgetsTree.AddChild(_widgetsPlotTree);
        _widgetsTree.AddChild(_widgetsWaveformTree);
        _widgetsTree.AddChild(_widgetsColorTree);
        _widgetsTree.AddChild(_widgetsAsciiTree);
        _widgetsTree.AddChild(_widgetsSelectableTree);
        _widgetsTree.AddChild(_widgetsLayoutTree);
        _widgetsTree.AddChild(_widgetsStatusTree);
        _widgetsTree.AddChild(_widgetsInputFocusTree);
        _widgetsTree.AddChild(_widgetsToolsTree);
        _widgetsTree.AddChild(_widgetsExamplesTree);
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
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Exit", Clicked = _ => ExitRequested?.Invoke() });

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
        _examplesMenuTextEditorItem = new UiMenuBar.MenuItem { Text = "Text Editor", IsCheckable = true };
        _examplesMenuDockingItem = new UiMenuBar.MenuItem { Text = "Docking", IsCheckable = true };
        _examplesMenuClippingItem = new UiMenuBar.MenuItem { Text = "Clipping", IsCheckable = true };
        _examplesMenuSerializationItem = new UiMenuBar.MenuItem { Text = "Serialization", IsCheckable = true };

        _examplesMenuAllItem.Clicked = _ => SetActiveExample(ExamplePanel.All);
        _examplesMenuBasicsItem.Clicked = _ => SetActiveExample(ExamplePanel.Basics);
        _examplesMenuWidgetsItem.Clicked = _ => SetActiveExample(ExamplePanel.Widgets);
        _examplesMenuTextEditorItem.Clicked = _ => SetActiveExample(ExamplePanel.TextEditor);
        _examplesMenuDockingItem.Clicked = _ => SetActiveExample(ExamplePanel.Docking);
        _examplesMenuClippingItem.Clicked = _ => SetActiveExample(ExamplePanel.Clipping);
        _examplesMenuSerializationItem.Clicked = _ => SetActiveExample(ExamplePanel.Serialization);

        examplesMenu.Items.Add(_examplesMenuAllItem);
        examplesMenu.Items.Add(UiMenuBar.MenuItem.Separator());
        examplesMenu.Items.Add(_examplesMenuBasicsItem);
        examplesMenu.Items.Add(_examplesMenuWidgetsItem);
        examplesMenu.Items.Add(_examplesMenuTextEditorItem);
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

        _textEditorWindow = new UiWindow
        {
            Id = "window-text-editor",
            Title = "Text Editor",
            TitleTextScale = FontScale,
            AllowResize = true,
            ShowResizeGrip = true
        };
        if (_textEditorDemo != null)
        {
            _textEditorWindow.AddChild(_textEditorDemo);
        }

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

        if (_dragDropTooltip != null)
        {
            _root.AddChild(_dragDropTooltip);
        }

        if (_modal != null)
        {
            _root.AddChild(_modal);
        }

        _context = new UiContext(_root);
        SetActiveExample(ExamplePanel.Custom);
    }

    private void UpdateLayout(int width, int height)
    {
        if (_root == null || _renderer == null || _dockWorkspace == null)
        {
            return;
        }

        int fontHeight = _renderer.MeasureTextHeight(FontScale);

        UiRect rootBounds = new UiRect(0, 0, width, height);
        _root.Bounds = rootBounds;
        if (_rootPanel != null)
        {
            _rootPanel.Bounds = rootBounds;
        }

        int contentTop = Padding;
        if (_menuBar != null)
        {
            _menuBar.Bounds = new UiRect(0, 0, width, _menuBar.BarHeight);
            contentTop = _menuBar.Bounds.Bottom + Padding;
        }

        int statusY = height - Padding - fontHeight;
        if (_statusLabel != null)
        {
            _statusLabel.Bounds = new UiRect(Padding, statusY, Math.Max(0, width - Padding * 2), fontHeight);
        }

        int dockHeight = Math.Max(0, statusY - contentTop - Padding);
        int dockWidth = Math.Max(0, width - Padding * 2);
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

        if (_widgetsWindow != null && _widgetsTitleLabel != null && _widgetsTree != null && _widgetsConfigTree != null && _widgetsMenuTree != null && _widgetsBasicTree != null
            && _widgetsBulletsTree != null && _widgetsTextTree != null && _widgetsTextFilterTree != null && _widgetsTextInputTree != null && _widgetsImagesTree != null
            && _widgetsSliderTree != null && _widgetsStyleTree != null && _widgetsDragTree != null && _widgetsDragDropTree != null && _widgetsDisableTree != null && _widgetsMultiComponentTree != null
            && _widgetsListTree != null && _widgetsMultiSelectTree != null && _widgetsComboTree != null && _widgetsTableTree != null
            && _widgetsPlotTree != null && _widgetsWaveformTree != null && _widgetsColorTree != null && _widgetsAsciiTree != null && _widgetsSelectableTree != null && _widgetsLayoutTree != null
            && _widgetsStatusTree != null && _widgetsInputFocusTree != null && _widgetsToolsTree != null && _widgetsExamplesTree != null && _widgetsTooltipTree != null
            && _widgetsPopupTree != null && _widgetsTreeHeaderTree != null && _snapCheckbox != null && _gizmoCheckbox != null
            && _widgetsSeparator != null
            && _qualityLow != null && _qualityMedium != null && _qualityHigh != null
            && _invisibleButtonLabel != null && _invisibleButtonPanel != null && _invisibleButton != null && _invisibleButtonStatus != null
            && _basicButtonsLabel != null && _basicPrimaryButton != null && _basicDangerButton != null && _basicRepeatButton != null && _basicRepeatStatusLabel != null
            && _basicInputLabel != null && _basicInputText != null && _basicInputIntLabel != null && _basicInputInt != null && _basicInputFloatLabel != null && _basicInputFloat != null
            && _configHelpLabel != null && _configHelpText != null && _configBackendLabel != null && _configDockingCheckbox != null && _configViewportCheckbox != null
            && _configStyleLabel != null && _configLargeFontCheckbox != null && _configCaptureLabel != null && _configCaptureKeyboardCheckbox != null && _configCaptureMouseCheckbox != null
            && _windowOptionsLabel != null && _windowAllowResizeCheckbox != null && _windowShowTitleCheckbox != null && _windowAllowDragCheckbox != null && _windowShowGripCheckbox != null
            && _demoMenuBar != null && _demoMenuStatusLabel != null
            && _bulletsLabel != null
            && _textHeaderLabel != null && _textColoredLabel != null && _textColoredSample != null && _textFontSizeLabel != null && _textFontSizeSmall != null && _textFontSizeLarge != null
            && _textWrappedBlock != null && _textUtf8Label != null && _textLink != null
            && _textFilterLabel != null && _textFilterField != null && _textFilterList != null && _textFilterStatusLabel != null
            && _textInputLabel != null && _multiLineInput != null && _filteredInputLabel != null && _filteredInputField != null && _passwordInputLabel != null && _passwordField != null && _passwordMaskLabel != null && _passwordHintLabel != null
            && _completionInputLabel != null && _completionField != null && _completionHintLabel != null && _completionHistoryLabel != null && _resizeInputLabel != null && _resizeInputField != null
            && _elidingInputLabel != null && _elidingInputField != null && _elidingResultLabel != null && _miscInputLabel != null && _miscInputField != null
            && _imagesLabel != null && _imagePreview != null && _imageButton != null && _imageButtonStatus != null
            && _volumeSlider != null && _volumeProgress != null
            && _verticalProgressLabel != null && _verticalProgress != null && _vuMeterLabel != null && _vuMeter != null
            && _radialProgressLabel != null && _radialProgress != null
            && _angleSliderLabel != null && _angleSlider != null && _angleSliderValueLabel != null
            && _verticalSliderLabel != null && _verticalSlider != null && _verticalSliderValueLabel != null
            && _sliderFlagsLabel != null && _sliderWholeNumbersCheckbox != null && _sliderStepCheckbox != null && _enumSliderLabel != null && _enumSlider != null && _enumSliderValueLabel != null
            && _roundingLabel != null && _roundingSlider != null && _roundingPreviewLabel != null && _roundingButton != null
            && _roundingField != null && _roundingPanel != null
            && _dragFloatLabel != null && _dragFloat != null && _dragIntLabel != null && _dragInt != null && _dragRangeLabel != null
            && _dragFloatRange != null && _dragIntRange != null && _dragVectorLabel != null && _dragFloat2 != null
            && _dragFloat3 != null && _dragFloat4 != null && _dragVectorIntLabel != null && _dragInt2 != null
            && _dragInt3 != null && _dragInt4 != null && _dragFlagsLabel != null && _dragClampCheckbox != null
            && _dragNoSlowFastCheckbox != null && _dragLogCheckbox != null && _dragHintLabel != null
            && _dragDropLabel != null && _dragDropSource != null && _dragDropTarget != null && _dragDropStatusLabel != null && _dragDropSourcePanel != null && _dragDropTargetPanel != null && _dragDropSourceLabel != null && _dragDropTargetLabel != null && _dragDropTooltipRegion != null
            && _disableLabel != null && _disableToggle != null && _disabledGroup != null && _disabledButton != null && _disabledSlider != null && _disabledField != null
            && _multiComponentLabel != null && _inputFloat2 != null && _inputFloat3 != null && _inputFloat4 != null && _inputInt2 != null && _inputInt3 != null && _inputInt4 != null
            && _sliderFloat2 != null && _sliderFloat3 != null && _sliderFloat4 != null && _sliderInt2 != null && _sliderInt3 != null && _sliderInt4 != null && _multiComponentStatusLabel != null
            && _sceneLabel != null && _sceneList != null && _sceneSelectionLabel != null && _multiSelectLabel != null
            && _multiSelectHintLabel != null && _multiSelectList != null && _multiSelectSelectionLabel != null && _comboLabel != null
            && _sceneComboBox != null && _comboSelectionLabel != null && _tableLabel != null && _sceneTable != null
            && _tableSelectionLabel != null && _plotLabel != null && _plotPanel != null && _waveformLabel != null && _waveform != null && _colorEditLabel != null && _colorEdit != null && _colorPickerLabel != null
            && _colorPicker != null && _colorSelectionLabel != null && _colorButtonLabel != null && _asciiLabel != null && _asciiPageLabel != null && _asciiPageCombo != null
            && _asciiTable != null && _selectablesLabel != null && _selectablesHintLabel != null
            && _selectableLighting != null && _selectableNavigation != null && _selectableAudio != null && _scrollPanelLabel != null
            && _layoutHorizontalLabel != null && _layoutButtonLeft != null && _layoutButtonCenter != null && _layoutButtonRight != null && _layoutDummyLabel != null && _layoutDummyPanel != null && _layoutWrapLabel != null && _layoutWrapBlock != null
            && _textEditorLabel != null && _textEditor != null
            && _gridLabel != null && _grid != null && _gridPrimaryButton != null && _gridSecondaryButton != null && _gridInfoLabel != null && _gridStatusLabel != null
            && _tabBarLabel != null && _tabBar != null && _tabOverview != null && _tabDetails != null && _tabSettings != null
            && _tabOverviewLabel != null && _tabDetailsLabel != null && _tabSettingsLabel != null && _tabButtonStatusLabel != null
            && _canvasLabel != null && _canvas != null && _canvasNodeA != null && _canvasNodeB != null && _canvasNodeC != null
            && _splitterLabel != null && _splitterVerticalLeftPanel != null && _splitterVertical != null && _splitterVerticalRightPanel != null
            && _splitterHorizontalTopPanel != null && _splitterHorizontal != null && _splitterHorizontalBottomPanel != null
            && _scrollPanel != null && _menuPopupLabel != null && _menuPopupButton != null && _menuPopupStatus != null
            && _menuPopup != null && _menuPopupTable != null && _menuPopupContentItem != null
            && _itemStatusLabel != null && _windowStatusLabel != null && _focusStatusLabel != null
            && _inputInfoLabel != null && _shortcutLabel != null && _focusInputField != null && _focusButton != null && _focusResultLabel != null
            && _toolsLabel != null && _toolsLink != null && _aboutButton != null && _themeDarkButton != null && _themeLightButton != null
            && _examplesLabel != null && _examplesHintLabel != null
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

            int configContentWidth = Math.Max(0, rootContentWidth - _widgetsConfigTree.Indent);
            int configPadding = Math.Max(0, _widgetsConfigTree.ContentPadding);
            int configY = 0;
            _configHelpLabel.Bounds = new UiRect(0, configY, configContentWidth, labelHeight);
            configY += labelHeight + 4;
            int helpTextHeight = labelHeight * 3;
            _configHelpText.Bounds = new UiRect(0, configY, configContentWidth, helpTextHeight);
            configY += helpTextHeight + 6;
            _configLargeFontCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 8;
            _configBackendLabel.Bounds = new UiRect(0, configY, configContentWidth, labelHeight);
            configY += labelHeight + 4;
            _configDockingCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            _configViewportCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 8;
            _configStyleLabel.Bounds = new UiRect(0, configY, configContentWidth, labelHeight);
            configY += labelHeight + 4;
            _configCaptureLabel.Bounds = new UiRect(0, configY, configContentWidth, labelHeight);
            configY += labelHeight + 4;
            _configCaptureKeyboardCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            _configCaptureMouseCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 8;
            _windowOptionsLabel.Bounds = new UiRect(0, configY, configContentWidth, labelHeight);
            configY += labelHeight + 4;
            _windowAllowResizeCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            _windowShowTitleCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            _windowAllowDragCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            _windowShowGripCheckbox.Bounds = new UiRect(0, configY, configContentWidth, rowHeight);
            configY += rowHeight + 4;
            int configHeight = treeHeaderHeight + (_widgetsConfigTree.IsOpen ? configPadding + configY : 0);
            _widgetsConfigTree.HeaderHeight = treeHeaderHeight;
            _widgetsConfigTree.Bounds = new UiRect(0, categoryY, rootContentWidth, configHeight);
            categoryY += configHeight + categorySpacing;

            int menuContentWidth = Math.Max(0, rootContentWidth - _widgetsMenuTree.Indent);
            int menuPadding = Math.Max(0, _widgetsMenuTree.ContentPadding);
            int menuY = 0;
            _demoMenuBar.Bounds = new UiRect(0, menuY, menuContentWidth, _demoMenuBar.BarHeight);
            menuY += _demoMenuBar.BarHeight + 6;
            _demoMenuStatusLabel.Bounds = new UiRect(0, menuY, menuContentWidth, labelHeight);
            menuY += labelHeight + 4;
            int menuHeight = treeHeaderHeight + (_widgetsMenuTree.IsOpen ? menuPadding + menuY : 0);
            _widgetsMenuTree.HeaderHeight = treeHeaderHeight;
            _widgetsMenuTree.Bounds = new UiRect(0, categoryY, rootContentWidth, menuHeight);
            categoryY += menuHeight + categorySpacing;

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
            _invisibleButtonLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 6;
            int invisibleWidth = Math.Min(220, basicContentWidth);
            int invisibleHeight = Math.Max(32, rowHeight + 6);
            _invisibleButtonPanel.Bounds = new UiRect(0, basicY, Math.Max(0, invisibleWidth), invisibleHeight);
            _invisibleButton.Bounds = _invisibleButtonPanel.Bounds;
            basicY += _invisibleButtonPanel.Bounds.Height + 4;
            _invisibleButtonStatus.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 4;

            _basicButtonsLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 6;
            int basicButtonWidth = Math.Min(140, Math.Max(0, (basicContentWidth - 8) / 2));
            _basicPrimaryButton.Bounds = new UiRect(0, basicY, basicButtonWidth, 24);
            _basicDangerButton.Bounds = new UiRect(basicButtonWidth + 8, basicY, basicButtonWidth, 24);
            basicY += 30;
            int repeatWidth = Math.Min(180, basicContentWidth);
            _basicRepeatButton.Bounds = new UiRect(0, basicY, Math.Max(0, repeatWidth), 24);
            basicY += 30;
            _basicRepeatStatusLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 6;
            _basicInputLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 4;
            int basicInputWidth = Math.Min(240, basicContentWidth);
            _basicInputText.Bounds = new UiRect(0, basicY, Math.Max(0, basicInputWidth), 22);
            basicY += 30;
            _basicInputIntLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 4;
            _basicInputInt.Bounds = new UiRect(0, basicY, Math.Max(0, basicInputWidth), 22);
            basicY += 30;
            _basicInputFloatLabel.Bounds = new UiRect(0, basicY, basicContentWidth, labelHeight);
            basicY += labelHeight + 4;
            _basicInputFloat.Bounds = new UiRect(0, basicY, Math.Max(0, basicInputWidth), 22);
            basicY += 30;

            int basicHeight = treeHeaderHeight + (_widgetsBasicTree.IsOpen ? basicPadding + basicY : 0);
            _widgetsBasicTree.HeaderHeight = treeHeaderHeight;
            _widgetsBasicTree.Bounds = new UiRect(0, categoryY, rootContentWidth, basicHeight);
            categoryY += basicHeight + categorySpacing;

            int bulletsContentWidth = Math.Max(0, rootContentWidth - _widgetsBulletsTree.Indent);
            int bulletsPadding = Math.Max(0, _widgetsBulletsTree.ContentPadding);
            int bulletsY = 0;
            _bulletsLabel.Bounds = new UiRect(0, bulletsY, bulletsContentWidth, labelHeight);
            bulletsY += labelHeight + 6;
            foreach (UiBulletText bullet in _bulletItems)
            {
                bullet.Bounds = new UiRect(0, bulletsY, bulletsContentWidth, labelHeight);
                bulletsY += labelHeight + 4;
            }
            int bulletsHeight = treeHeaderHeight + (_widgetsBulletsTree.IsOpen ? bulletsPadding + bulletsY : 0);
            _widgetsBulletsTree.HeaderHeight = treeHeaderHeight;
            _widgetsBulletsTree.Bounds = new UiRect(0, categoryY, rootContentWidth, bulletsHeight);
            categoryY += bulletsHeight + categorySpacing;

            int textContentWidth = Math.Max(0, rootContentWidth - _widgetsTextTree.Indent);
            int textPadding = Math.Max(0, _widgetsTextTree.ContentPadding);
            int textY = 0;
            _textHeaderLabel.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 4;
            _textColoredLabel.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 4;
            _textColoredSample.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 6;
            _textFontSizeLabel.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 4;
            int smallTextHeight = _renderer.MeasureTextHeight(_textFontSizeSmall.Scale);
            _textFontSizeSmall.Bounds = new UiRect(0, textY, textContentWidth, smallTextHeight);
            textY += smallTextHeight + 4;
            int largeTextHeight = _renderer.MeasureTextHeight(_textFontSizeLarge.Scale);
            _textFontSizeLarge.Bounds = new UiRect(0, textY, textContentWidth, largeTextHeight);
            textY += largeTextHeight + 6;
            int wrapHeight = labelHeight * 3;
            _textWrappedBlock.Bounds = new UiRect(0, textY, textContentWidth, wrapHeight);
            textY += wrapHeight + 6;
            _textUtf8Label.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 6;
            _textLink.Bounds = new UiRect(0, textY, textContentWidth, labelHeight);
            textY += labelHeight + 4;
            int textHeight = treeHeaderHeight + (_widgetsTextTree.IsOpen ? textPadding + textY : 0);
            _widgetsTextTree.HeaderHeight = treeHeaderHeight;
            _widgetsTextTree.Bounds = new UiRect(0, categoryY, rootContentWidth, textHeight);
            categoryY += textHeight + categorySpacing;

            int filterContentWidth = Math.Max(0, rootContentWidth - _widgetsTextFilterTree.Indent);
            int filterPadding = Math.Max(0, _widgetsTextFilterTree.ContentPadding);
            int filterY = 0;
            _textFilterLabel.Bounds = new UiRect(0, filterY, filterContentWidth, labelHeight);
            filterY += labelHeight + 6;
            _textFilterField.Bounds = new UiRect(0, filterY, filterContentWidth, 22);
            filterY += 30;
            _textFilterList.Bounds = new UiRect(0, filterY, filterContentWidth, 120);
            _textFilterList.ItemHeight = labelHeight + 6;
            filterY += _textFilterList.Bounds.Height + 6;
            _textFilterStatusLabel.Bounds = new UiRect(0, filterY, filterContentWidth, labelHeight);
            filterY += labelHeight + 4;
            int filterHeight = treeHeaderHeight + (_widgetsTextFilterTree.IsOpen ? filterPadding + filterY : 0);
            _widgetsTextFilterTree.HeaderHeight = treeHeaderHeight;
            _widgetsTextFilterTree.Bounds = new UiRect(0, categoryY, rootContentWidth, filterHeight);
            categoryY += filterHeight + categorySpacing;

            int textInputContentWidth = Math.Max(0, rootContentWidth - _widgetsTextInputTree.Indent);
            int textInputPadding = Math.Max(0, _widgetsTextInputTree.ContentPadding);
            int textInputY = 0;
            _textInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 6;
            int editorHeight = Math.Max(120, labelHeight * 6);
            _multiLineInput.Bounds = new UiRect(0, textInputY, textInputContentWidth, editorHeight);
            textInputY += editorHeight + 8;
            _filteredInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            _filteredInputField.Bounds = new UiRect(0, textInputY, textInputContentWidth, 22);
            textInputY += 30;
            _passwordInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            _passwordField.Bounds = new UiRect(0, textInputY, textInputContentWidth, 22);
            int passwordPadding = Math.Max(0, _passwordField.Padding);
            _passwordMaskLabel.Bounds = new UiRect(_passwordField.Bounds.X + passwordPadding, _passwordField.Bounds.Y + passwordPadding, Math.Max(0, _passwordField.Bounds.Width - passwordPadding * 2), labelHeight);
            textInputY += 30;
            _passwordHintLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 6;
            _completionInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            _completionField.Bounds = new UiRect(0, textInputY, textInputContentWidth, 22);
            textInputY += 30;
            _completionHintLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            _completionHistoryLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 6;
            _resizeInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            int resizeWidth = Math.Min(textInputContentWidth, Math.Max(140, _renderer.MeasureTextWidth(_resizeInputField.Text, FontScale) + _resizeInputField.Padding * 2 + 20));
            _resizeInputField.Bounds = new UiRect(0, textInputY, resizeWidth, 22);
            textInputY += 30;
            _elidingInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            int elideWidth = Math.Min(textInputContentWidth, 260);
            _elidingInputField.Bounds = new UiRect(0, textInputY, elideWidth, 22);
            textInputY += 30;
            _elidingResultLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 6;
            _miscInputLabel.Bounds = new UiRect(0, textInputY, textInputContentWidth, labelHeight);
            textInputY += labelHeight + 4;
            _miscInputField.Bounds = new UiRect(0, textInputY, textInputContentWidth, 22);
            textInputY += 30;
            int textInputHeight = treeHeaderHeight + (_widgetsTextInputTree.IsOpen ? textInputPadding + textInputY : 0);
            _widgetsTextInputTree.HeaderHeight = treeHeaderHeight;
            _widgetsTextInputTree.Bounds = new UiRect(0, categoryY, rootContentWidth, textInputHeight);
            categoryY += textInputHeight + categorySpacing;

            int imagesContentWidth = Math.Max(0, rootContentWidth - _widgetsImagesTree.Indent);
            int imagesPadding = Math.Max(0, _widgetsImagesTree.ContentPadding);
            int imagesY = 0;
            _imagesLabel.Bounds = new UiRect(0, imagesY, imagesContentWidth, labelHeight);
            imagesY += labelHeight + 6;
            int previewSize = Math.Max(80, Math.Min(160, imagesContentWidth));
            _imagePreview.Bounds = new UiRect(0, imagesY, previewSize, previewSize);
            imagesY += previewSize + 6;
            int imageButtonSize = Math.Max(40, Math.Min(80, imagesContentWidth));
            _imageButton.Bounds = new UiRect(0, imagesY, imageButtonSize, imageButtonSize);
            imagesY += imageButtonSize + 4;
            _imageButtonStatus.Bounds = new UiRect(0, imagesY, imagesContentWidth, labelHeight);
            imagesY += labelHeight + 4;
            int imagesHeight = treeHeaderHeight + (_widgetsImagesTree.IsOpen ? imagesPadding + imagesY : 0);
            _widgetsImagesTree.HeaderHeight = treeHeaderHeight;
            _widgetsImagesTree.Bounds = new UiRect(0, categoryY, rootContentWidth, imagesHeight);
            categoryY += imagesHeight + categorySpacing;

            int sliderContentWidth = Math.Max(0, rootContentWidth - _widgetsSliderTree.Indent);
            int sliderPadding = Math.Max(0, _widgetsSliderTree.ContentPadding);
            int sliderY = 0;
            _volumeSlider.Bounds = new UiRect(0, sliderY, sliderContentWidth, 24);
            sliderY += 32;
            _volumeProgress.Bounds = new UiRect(0, sliderY, sliderContentWidth, 20);
            sliderY += 30;
            _verticalProgressLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;
            int meterWidth = Math.Max(0, Math.Min(24, sliderContentWidth));
            int meterHeight = Math.Max(72, labelHeight * 6);
            _verticalProgress.Bounds = new UiRect(0, sliderY, meterWidth, meterHeight);
            sliderY += meterHeight + 8;
            _vuMeterLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;
            _vuMeter.Bounds = new UiRect(0, sliderY, meterWidth, meterHeight);
            sliderY += meterHeight + 4;
            _radialProgressLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;
            int radialSize = Math.Max(72, labelHeight * 6);
            radialSize = Math.Max(0, Math.Min(sliderContentWidth, radialSize));
            _radialProgress.Bounds = new UiRect(0, sliderY, radialSize, radialSize);
            _radialProgress.CornerRadius = radialSize / 2;
            _radialProgress.RadialThickness = Math.Max(4, radialSize / 5);
            sliderY += radialSize + 4;
            _angleSliderLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;
            _angleSlider.Bounds = new UiRect(0, sliderY, sliderContentWidth, 24);
            sliderY += 30;
            _angleSliderValueLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;

            _verticalSliderLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;
            int verticalSliderHeight = Math.Max(90, labelHeight * 6);
            _verticalSlider.Bounds = new UiRect(0, sliderY, Math.Max(24, Math.Min(40, sliderContentWidth)), verticalSliderHeight);
            sliderY += verticalSliderHeight + 4;
            _verticalSliderValueLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 6;

            _sliderFlagsLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 4;
            _sliderWholeNumbersCheckbox.Bounds = new UiRect(0, sliderY, sliderContentWidth, rowHeight);
            sliderY += rowHeight + 4;
            _sliderStepCheckbox.Bounds = new UiRect(0, sliderY, sliderContentWidth, rowHeight);
            sliderY += rowHeight + 6;

            _enumSliderLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 4;
            _enumSlider.Bounds = new UiRect(0, sliderY, sliderContentWidth, 24);
            sliderY += 30;
            _enumSliderValueLabel.Bounds = new UiRect(0, sliderY, sliderContentWidth, labelHeight);
            sliderY += labelHeight + 4;
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

            int dragDropContentWidth = Math.Max(0, rootContentWidth - _widgetsDragDropTree.Indent);
            int dragDropPadding = Math.Max(0, _widgetsDragDropTree.ContentPadding);
            int dragDropY = 0;
            _dragDropLabel.Bounds = new UiRect(0, dragDropY, dragDropContentWidth, labelHeight);
            dragDropY += labelHeight + 6;
            int dragPanelSize = Math.Max(80, Math.Min(140, (dragDropContentWidth - 12) / 2));
            _dragDropSource.Bounds = new UiRect(0, dragDropY, dragPanelSize, dragPanelSize);
            _dragDropSourcePanel.Bounds = _dragDropSource.Bounds;
            _dragDropSourceLabel.Bounds = new UiRect(_dragDropSource.Bounds.X + 6, _dragDropSource.Bounds.Y + 6, Math.Max(0, dragPanelSize - 12), labelHeight);
            _dragDropTarget.Bounds = new UiRect(dragPanelSize + 12, dragDropY, dragPanelSize, dragPanelSize);
            _dragDropTargetPanel.Bounds = _dragDropTarget.Bounds;
            _dragDropTargetLabel.Bounds = new UiRect(_dragDropTarget.Bounds.X + 6, _dragDropTarget.Bounds.Y + 6, Math.Max(0, dragPanelSize - 12), labelHeight);
            _dragDropTooltipRegion.Bounds = _dragDropTarget.Bounds;
            dragDropY += dragPanelSize + 6;
            _dragDropStatusLabel.Bounds = new UiRect(0, dragDropY, dragDropContentWidth, labelHeight);
            dragDropY += labelHeight + 6;

            int reorderWidth = Math.Min(240, dragDropContentWidth);
            int reorderRowHeight = labelHeight + 6;
            for (int i = 0; i < _dragDropItemSources.Count; i++)
            {
                int rowY = dragDropY + i * (reorderRowHeight + 4);
                UiDragDropSource source = _dragDropItemSources[i];
                UiDragDropTarget target = _dragDropItemTargets[i];
                UiPanel panel = _dragDropItemPanels[i];
                UiLabel label = _dragDropItemLabels[i];
                source.Bounds = new UiRect(0, rowY, reorderWidth, reorderRowHeight);
                target.Bounds = source.Bounds;
                panel.Bounds = source.Bounds;
                int labelY = rowY + (reorderRowHeight - labelHeight) / 2;
                label.Bounds = new UiRect(source.Bounds.X + 6, labelY, Math.Max(0, reorderWidth - 12), labelHeight);
            }

            dragDropY += _dragDropItemSources.Count * (reorderRowHeight + 4);
            int dragDropHeight = treeHeaderHeight + (_widgetsDragDropTree.IsOpen ? dragDropPadding + dragDropY : 0);
            _widgetsDragDropTree.HeaderHeight = treeHeaderHeight;
            _widgetsDragDropTree.Bounds = new UiRect(0, categoryY, rootContentWidth, dragDropHeight);
            categoryY += dragDropHeight + categorySpacing;

            int disableContentWidth = Math.Max(0, rootContentWidth - _widgetsDisableTree.Indent);
            int disablePadding = Math.Max(0, _widgetsDisableTree.ContentPadding);
            int disableY = 0;
            _disableLabel.Bounds = new UiRect(0, disableY, disableContentWidth, labelHeight);
            disableY += labelHeight + 6;
            _disableToggle.Bounds = new UiRect(0, disableY, disableContentWidth, rowHeight);
            disableY += rowHeight + 6;
            int groupWidth = Math.Min(260, disableContentWidth);
            int groupX = 0;
            int groupPadding = 8;
            int groupY = disableY;
            int groupInnerY = groupY + groupPadding;
            _disabledButton.Bounds = new UiRect(groupX + groupPadding, groupInnerY, Math.Max(0, groupWidth - groupPadding * 2), 24);
            groupInnerY += 30;
            _disabledSlider.Bounds = new UiRect(groupX + groupPadding, groupInnerY, Math.Max(0, groupWidth - groupPadding * 2), 24);
            groupInnerY += 30;
            _disabledField.Bounds = new UiRect(groupX + groupPadding, groupInnerY, Math.Max(0, groupWidth - groupPadding * 2), 22);
            groupInnerY += 28;
            int groupHeight = Math.Max(0, groupInnerY - groupY + groupPadding);
            _disabledGroup.Bounds = new UiRect(groupX, groupY, groupWidth, groupHeight);
            disableY += groupHeight + 4;
            int disableHeight = treeHeaderHeight + (_widgetsDisableTree.IsOpen ? disablePadding + disableY : 0);
            _widgetsDisableTree.HeaderHeight = treeHeaderHeight;
            _widgetsDisableTree.Bounds = new UiRect(0, categoryY, rootContentWidth, disableHeight);
            categoryY += disableHeight + categorySpacing;

            int multiComponentContentWidth = Math.Max(0, rootContentWidth - _widgetsMultiComponentTree.Indent);
            int multiComponentPadding = Math.Max(0, _widgetsMultiComponentTree.ContentPadding);
            int multiComponentY = 0;
            _multiComponentLabel.Bounds = new UiRect(0, multiComponentY, multiComponentContentWidth, labelHeight);
            multiComponentY += labelHeight + 6;
            int vectorWidth = Math.Min(360, multiComponentContentWidth);
            _inputFloat2.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _inputFloat3.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _inputFloat4.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _inputInt2.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _inputInt3.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _inputInt4.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderFloat2.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderFloat3.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderFloat4.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderInt2.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderInt3.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _sliderInt4.Bounds = new UiRect(0, multiComponentY, vectorWidth, 24);
            multiComponentY += 30;
            _multiComponentStatusLabel.Bounds = new UiRect(0, multiComponentY, multiComponentContentWidth, labelHeight);
            multiComponentY += labelHeight + 4;
            int multiComponentHeight = treeHeaderHeight + (_widgetsMultiComponentTree.IsOpen ? multiComponentPadding + multiComponentY : 0);
            _widgetsMultiComponentTree.HeaderHeight = treeHeaderHeight;
            _widgetsMultiComponentTree.Bounds = new UiRect(0, categoryY, rootContentWidth, multiComponentHeight);
            categoryY += multiComponentHeight + categorySpacing;

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

            int waveformContentWidth = Math.Max(0, rootContentWidth - _widgetsWaveformTree.Indent);
            int waveformPadding = Math.Max(0, _widgetsWaveformTree.ContentPadding);
            int waveformY = 0;
            _waveformLabel.Bounds = new UiRect(0, waveformY, waveformContentWidth, labelHeight);
            waveformY += labelHeight + 6;
            int waveformHeight = Math.Max(100, Math.Min(200, waveformContentWidth / 2));
            _waveform.Bounds = new UiRect(0, waveformY, waveformContentWidth, waveformHeight);
            waveformY += waveformHeight + 4;
            int waveformTreeHeight = treeHeaderHeight + (_widgetsWaveformTree.IsOpen ? waveformPadding + waveformY : 0);
            _widgetsWaveformTree.HeaderHeight = treeHeaderHeight;
            _widgetsWaveformTree.Bounds = new UiRect(0, categoryY, rootContentWidth, waveformTreeHeight);
            categoryY += waveformTreeHeight + categorySpacing;

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

            _layoutHorizontalLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;
            int horizontalButtonWidth = Math.Min(100, Math.Max(0, (layoutContentWidth - 16) / 3));
            _layoutButtonLeft.Bounds = new UiRect(0, layoutY, horizontalButtonWidth, 24);
            _layoutButtonCenter.Bounds = new UiRect(horizontalButtonWidth + 8, layoutY, horizontalButtonWidth, 24);
            _layoutButtonRight.Bounds = new UiRect((horizontalButtonWidth + 8) * 2, layoutY, horizontalButtonWidth, 24);
            layoutY += 30;

            _layoutDummyLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;
            int dummyWidth = Math.Min(100, layoutContentWidth);
            _layoutDummyPanel.Bounds = new UiRect(0, layoutY, dummyWidth, 20);
            layoutY += _layoutDummyPanel.Bounds.Height + 8;

            _layoutWrapLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;
            int wrapBlockHeight = labelHeight * 3;
            _layoutWrapBlock.Bounds = new UiRect(0, layoutY, layoutContentWidth, wrapBlockHeight);
            layoutY += wrapBlockHeight + 8;

            _tabBarLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int tabBarHeight = Math.Max(22, labelHeight + 6);
            int tabContentHeight = Math.Max(64, labelHeight * 3);
            _tabBar.TabBarHeight = tabBarHeight;
            _tabBar.Bounds = new UiRect(0, layoutY, layoutContentWidth, tabBarHeight + tabContentHeight);

            UiRect tabContent = _tabBar.ContentBounds;
            int tabContentPadding = 8;
            int tabTextWidth = Math.Max(0, tabContent.Width - tabContentPadding * 2);
            _tabOverviewLabel.Bounds = new UiRect(tabContent.X + tabContentPadding, tabContent.Y + tabContentPadding, tabTextWidth, labelHeight);
            _tabDetailsLabel.Bounds = new UiRect(tabContent.X + tabContentPadding, tabContent.Y + tabContentPadding, tabTextWidth, labelHeight);
            _tabSettingsLabel.Bounds = new UiRect(tabContent.X + tabContentPadding, tabContent.Y + tabContentPadding, tabTextWidth, labelHeight);

            layoutY += _tabBar.Bounds.Height + 6;

            _tabButtonStatusLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 8;

            _splitterLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int splitterWidth = Math.Max(0, layoutContentWidth);
            int splitterSampleHeight = Math.Max(70, Math.Min(120, layoutContentWidth / 3));
            int splitterMinPane = 60;
            int maxLeft = Math.Max(0, splitterWidth - SplitterSize - splitterMinPane);
            int minLeft = Math.Min(splitterMinPane, maxLeft);
            _splitterVerticalLeftWidth = Math.Clamp(_splitterVerticalLeftWidth, minLeft, maxLeft);
            int rightWidth = Math.Max(0, splitterWidth - SplitterSize - _splitterVerticalLeftWidth);

            _splitterVerticalLeftPanel.Bounds = new UiRect(0, layoutY, _splitterVerticalLeftWidth, splitterSampleHeight);
            _splitterVertical.Bounds = new UiRect(_splitterVerticalLeftPanel.Bounds.Right, layoutY, SplitterSize, splitterSampleHeight);
            _splitterVerticalRightPanel.Bounds = new UiRect(_splitterVertical.Bounds.Right, layoutY, rightWidth, splitterSampleHeight);
            layoutY += splitterSampleHeight + 6;

            int maxTop = Math.Max(0, splitterSampleHeight - SplitterSize - splitterMinPane);
            int minTop = Math.Min(splitterMinPane, maxTop);
            _splitterHorizontalTopHeight = Math.Clamp(_splitterHorizontalTopHeight, minTop, maxTop);
            int bottomHeight = Math.Max(0, splitterSampleHeight - SplitterSize - _splitterHorizontalTopHeight);

            _splitterHorizontalTopPanel.Bounds = new UiRect(0, layoutY, splitterWidth, _splitterHorizontalTopHeight);
            _splitterHorizontal.Bounds = new UiRect(0, _splitterHorizontalTopPanel.Bounds.Bottom, splitterWidth, SplitterSize);
            _splitterHorizontalBottomPanel.Bounds = new UiRect(0, _splitterHorizontal.Bounds.Bottom, splitterWidth, bottomHeight);
            layoutY += splitterSampleHeight + 8;

            _textEditorLabel.Bounds = new UiRect(0, layoutY, layoutContentWidth, labelHeight);
            layoutY += labelHeight + 6;

            int textEditorHeight = Math.Max(160, Math.Min(260, layoutContentWidth / 2));
            _textEditor.Bounds = new UiRect(0, layoutY, layoutContentWidth, textEditorHeight);
            layoutY += textEditorHeight + 8;

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

            int statusContentWidth = Math.Max(0, rootContentWidth - _widgetsStatusTree.Indent);
            int statusPadding = Math.Max(0, _widgetsStatusTree.ContentPadding);
            int statusY = 0;
            _itemStatusLabel.Bounds = new UiRect(0, statusY, statusContentWidth, labelHeight);
            statusY += labelHeight + 4;
            _windowStatusLabel.Bounds = new UiRect(0, statusY, statusContentWidth, labelHeight);
            statusY += labelHeight + 4;
            _focusStatusLabel.Bounds = new UiRect(0, statusY, statusContentWidth, labelHeight);
            statusY += labelHeight + 4;
            int statusHeight = treeHeaderHeight + (_widgetsStatusTree.IsOpen ? statusPadding + statusY : 0);
            _widgetsStatusTree.HeaderHeight = treeHeaderHeight;
            _widgetsStatusTree.Bounds = new UiRect(0, categoryY, rootContentWidth, statusHeight);
            categoryY += statusHeight + categorySpacing;

            int inputContentWidth = Math.Max(0, rootContentWidth - _widgetsInputFocusTree.Indent);
            int inputPadding = Math.Max(0, _widgetsInputFocusTree.ContentPadding);
            int inputY = 0;
            _inputInfoLabel.Bounds = new UiRect(0, inputY, inputContentWidth, labelHeight);
            inputY += labelHeight + 4;
            _shortcutLabel.Bounds = new UiRect(0, inputY, inputContentWidth, labelHeight);
            inputY += labelHeight + 6;
            _focusInputField.Bounds = new UiRect(0, inputY, Math.Min(240, inputContentWidth), 22);
            inputY += 30;
            _focusButton.Bounds = new UiRect(0, inputY, Math.Min(160, inputContentWidth), 24);
            inputY += 30;
            _focusResultLabel.Bounds = new UiRect(0, inputY, inputContentWidth, labelHeight);
            inputY += labelHeight + 4;
            int inputHeight = treeHeaderHeight + (_widgetsInputFocusTree.IsOpen ? inputPadding + inputY : 0);
            _widgetsInputFocusTree.HeaderHeight = treeHeaderHeight;
            _widgetsInputFocusTree.Bounds = new UiRect(0, categoryY, rootContentWidth, inputHeight);
            categoryY += inputHeight + categorySpacing;

            int toolsContentWidth = Math.Max(0, rootContentWidth - _widgetsToolsTree.Indent);
            int toolsPadding = Math.Max(0, _widgetsToolsTree.ContentPadding);
            int toolsY = 0;
            _toolsLabel.Bounds = new UiRect(0, toolsY, toolsContentWidth, labelHeight);
            toolsY += labelHeight + 6;
            _toolsLink.Bounds = new UiRect(0, toolsY, toolsContentWidth, labelHeight);
            toolsY += labelHeight + 6;
            _aboutButton.Bounds = new UiRect(0, toolsY, Math.Min(140, toolsContentWidth), 24);
            toolsY += 30;
            _themeDarkButton.Bounds = new UiRect(0, toolsY, Math.Min(160, toolsContentWidth), 24);
            toolsY += 30;
            _themeLightButton.Bounds = new UiRect(0, toolsY, Math.Min(160, toolsContentWidth), 24);
            toolsY += 30;
            int toolsHeight = treeHeaderHeight + (_widgetsToolsTree.IsOpen ? toolsPadding + toolsY : 0);
            _widgetsToolsTree.HeaderHeight = treeHeaderHeight;
            _widgetsToolsTree.Bounds = new UiRect(0, categoryY, rootContentWidth, toolsHeight);
            categoryY += toolsHeight + categorySpacing;

            int examplesContentWidth = Math.Max(0, rootContentWidth - _widgetsExamplesTree.Indent);
            int examplesPadding = Math.Max(0, _widgetsExamplesTree.ContentPadding);
            int examplesY = 0;
            _examplesLabel.Bounds = new UiRect(0, examplesY, examplesContentWidth, labelHeight);
            examplesY += labelHeight + 4;
            _examplesHintLabel.Bounds = new UiRect(0, examplesY, examplesContentWidth, labelHeight);
            examplesY += labelHeight + 4;
            int examplesHeight = treeHeaderHeight + (_widgetsExamplesTree.IsOpen ? examplesPadding + examplesY : 0);
            _widgetsExamplesTree.HeaderHeight = treeHeaderHeight;
            _widgetsExamplesTree.Bounds = new UiRect(0, categoryY, rootContentWidth, examplesHeight);
            categoryY += examplesHeight + categorySpacing;

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

        if (_textEditorWindow != null && _textEditorDemo != null)
        {
            UiRect content = _textEditorWindow.ContentBounds;
            int padding = Padding;
            _textEditorDemo.Bounds = new UiRect(
                content.X + padding,
                content.Y + padding,
                Math.Max(0, content.Width - padding * 2),
                Math.Max(0, content.Height - padding * 2));
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
            float value = _volumeSlider.Value;
            _volumeProgress.Value = value;
            _volumeProgress.Text = $"Volume {(int)Math.Round(value * 100f)}%";
            if (_verticalProgress != null)
            {
                _verticalProgress.Value = value;
            }

            if (_vuMeter != null)
            {
                _vuMeter.Value = value;
            }

            if (_radialProgress != null)
            {
                _radialProgress.Value = value;
            }
        }

        if (_angleSlider != null && _angleSliderValueLabel != null)
        {
            float degrees = _angleSlider.ValueDegrees;
            float radians = _angleSlider.Value;
            _angleSliderValueLabel.Text = $"Angle: {degrees:0} deg ({radians:0.00} rad)";
        }

        if (_invisibleButton != null && _invisibleButtonStatus != null)
        {
            string state = _invisibleButton.IsPressed ? "Pressed" : (_invisibleButton.IsHovered ? "Hover" : "Idle");
            _invisibleButtonStatus.Text = $"Invisible: {state} (Clicks: {_invisibleButtonClicks})";
            if (_invisibleButtonPanel != null)
            {
                _invisibleButtonPanel.Border = _invisibleButton.IsPressed
                    ? new UiColor(120, 140, 200)
                    : _invisibleButton.IsHovered ? new UiColor(90, 100, 120) : new UiColor(70, 80, 100);
            }
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

        if (_verticalSlider != null && _verticalSliderValueLabel != null)
        {
            _verticalSliderValueLabel.Text = $"Vertical: {(int)Math.Round(_verticalSlider.Value * 100f)}%";
        }

        if (_enumSlider != null && _enumSliderValueLabel != null)
        {
            int index = (int)MathF.Round(_enumSlider.Value);
            index = Math.Clamp(index, 0, Math.Max(0, _enumSliderItems.Count - 1));
            _enumSlider.Value = index;
            string name = _enumSliderItems.Count > 0 ? _enumSliderItems[index] : "None";
            _enumSliderValueLabel.Text = $"Enum: {name}";
        }

        if (_basicRepeatStatusLabel != null)
        {
            _basicRepeatStatusLabel.Text = $"Repeats: {_basicRepeatTicks}";
        }

        if (_imageButtonStatus != null)
        {
            _imageButtonStatus.Text = $"Image button: {_imageButtonClicks} clicks";
        }

        if (_passwordField != null && _passwordMaskLabel != null)
        {
            int length = _passwordField.Text.Length;
            _passwordMaskLabel.Text = length > 0 ? new string('*', length) : string.Empty;
        }

        if (_completionField != null && _completionHintLabel != null)
        {
            string hint = BuildCompletionHint(_completionField.Text);
            _completionHintLabel.Text = $"Suggestions: {hint}";
        }

        if (_completionHistoryLabel != null)
        {
            _completionHistoryLabel.Text = _completionHistory.Count == 0
                ? "History: (empty)"
                : $"History: {string.Join(", ", _completionHistory)}";
        }

        if (_elidingInputField != null && _elidingResultLabel != null && _renderer != null)
        {
            int maxWidth = Math.Max(0, _elidingInputField.Bounds.Width - 12);
            _elidingResultLabel.Text = $"Elided: {BuildElidedText(_elidingInputField.Text, maxWidth, FontScale)}";
        }

        if (_textFilterField != null)
        {
            UpdateTextFilter();
        }

        if (_textFilterList != null && _textFilterStatusLabel != null)
        {
            string selection;
            if (_textFilterFilteredItems.Count == 0)
            {
                selection = "None";
            }
            else if (_textFilterList.SelectedIndex >= 0 && _textFilterList.SelectedIndex < _textFilterFilteredItems.Count)
            {
                selection = _textFilterFilteredItems[_textFilterList.SelectedIndex];
            }
            else
            {
                selection = "All";
            }
            _textFilterStatusLabel.Text = $"Filter: {selection}";
        }

        if (_multiComponentStatusLabel != null && _inputFloat2 != null && _inputFloat3 != null && _inputFloat4 != null && _inputInt2 != null && _inputInt3 != null && _inputInt4 != null)
        {
            _multiComponentStatusLabel.Text = $"Vectors: F2({_inputFloat2.ValueX:0.00},{_inputFloat2.ValueY:0.00}) F3({_inputFloat3.ValueX:0.00},{_inputFloat3.ValueY:0.00},{_inputFloat3.ValueZ:0.00}) I2({_inputInt2.ValueX},{_inputInt2.ValueY})";
        }

        if (_disableToggle != null && _disabledGroup != null)
        {
            _disabledGroup.Enabled = _disableToggle.Checked;
        }

        if (_dragDropItemLabels.Count > 0)
        {
            SyncDragDropItems();
        }

        if (_dragDropStatusLabel != null && _context != null)
        {
            if (_context.DragDrop.IsDragging)
            {
                _dragDropStatusLabel.Text = "Drag status: dragging";
            }
            else if (_dragDropStatusLabel.Text.Contains("dragging", StringComparison.OrdinalIgnoreCase))
            {
                _dragDropStatusLabel.Text = "Drag status: idle";
            }

            if (_dragDropTooltipRegion != null)
            {
                _dragDropTooltipRegion.Text = _context.DragDrop.IsDragging ? "Release to drop" : string.Empty;
            }
        }

        if (_configLargeFontCheckbox != null && _helpLabel != null)
        {
            _helpLabel.Scale = _configLargeFontCheckbox.Checked ? FontScale + 1 : FontScale;
        }

        if (_windowAllowResizeCheckbox != null && _windowShowTitleCheckbox != null && _windowAllowDragCheckbox != null && _windowShowGripCheckbox != null && _standaloneWindow != null)
        {
            _standaloneWindow.AllowResize = _windowAllowResizeCheckbox.Checked;
            _standaloneWindow.ShowTitleBar = _windowShowTitleCheckbox.Checked;
            _standaloneWindow.AllowDrag = _windowAllowDragCheckbox.Checked;
            _standaloneWindow.ShowResizeGrip = _windowShowGripCheckbox.Checked;
        }
    }

    private void UpdateInputPanels(UiInputState input, float deltaSeconds)
    {
        if (_basicRepeatButton != null)
        {
            bool held = input.LeftDown && _basicRepeatButton.Bounds.Contains(input.MousePosition);
            if (held)
            {
                _basicRepeatAccumulator += Math.Max(0f, deltaSeconds);
                const float repeatInterval = 0.2f;
                while (_basicRepeatAccumulator >= repeatInterval)
                {
                    _basicRepeatAccumulator -= repeatInterval;
                    _basicRepeatTicks++;
                }
            }
            else
            {
                _basicRepeatAccumulator = 0f;
            }
        }

        if (_inputInfoLabel != null)
        {
            string modifiers = $"{(input.CtrlDown ? "Ctrl" : "Ctrl-")} {(input.ShiftDown ? "Shift" : "Shift-")}";
            string scrollText = $"Scroll {input.ScrollDelta}";
            string textInput = input.TextInput.Count > 0 ? $"Text '{input.TextInput[input.TextInput.Count - 1]}'" : "Text -";
            _inputInfoLabel.Text = $"Input: Mouse ({input.MousePosition.X},{input.MousePosition.Y}) {scrollText} {modifiers} {textInput}";
        }

        if (_shortcutLabel != null)
        {
            if (input.CtrlDown && input.TextInput.Count > 0)
            {
                char last = input.TextInput[input.TextInput.Count - 1];
                if (!char.IsControl(last))
                {
                    string key = char.ToUpperInvariant(last).ToString();
                    _lastShortcutText = input.ShiftDown ? $"Ctrl+Shift+{key}" : $"Ctrl+{key}";
                }
            }
            else if (input.Navigation.Enter || input.Navigation.KeypadEnter)
            {
                _lastShortcutText = "Enter";
            }
            else if (input.Navigation.Tab)
            {
                _lastShortcutText = "Tab";
            }

            _shortcutLabel.Text = $"Shortcut: {_lastShortcutText}";
        }

        if (_context == null)
        {
            return;
        }

        UiElement? hovered = _root?.HitTest(_lastMousePosition);
        if (hovered == _root || hovered == _rootPanel)
        {
            hovered = null;
        }

        if (_itemStatusLabel != null)
        {
            _itemStatusLabel.Text = $"Item: {DescribeElement(hovered)}";
        }

        UiElement? focused = _context.Focus.Focused;
        if (_focusStatusLabel != null)
        {
            _focusStatusLabel.Text = $"Focus: {DescribeElement(focused)}";
        }

        if (_focusResultLabel != null)
        {
            _focusResultLabel.Text = $"Focused: {DescribeElement(focused)}";
        }

        if (_windowStatusLabel != null)
        {
            UiWindow? window = FindAncestorWindow(focused ?? hovered);
            if (window == null)
            {
                _windowStatusLabel.Text = "Window: none";
            }
            else
            {
                string title = string.IsNullOrWhiteSpace(window.Title) ? window.GetType().Name : window.Title;
                _windowStatusLabel.Text = $"Window: {TrimText(title, 26)}";
            }
        }

        if (_completionField != null && focused == _completionField && (input.Navigation.Enter || input.Navigation.KeypadEnter))
        {
            string text = _completionField.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                _completionHistory.Remove(text);
                _completionHistory.Add(text);
                const int maxHistory = 5;
                while (_completionHistory.Count > maxHistory)
                {
                    _completionHistory.RemoveAt(0);
                }
            }
        }
    }

    private void UpdateSliderFlags()
    {
        bool wholeNumbers = _sliderWholeNumbersCheckbox?.Checked ?? false;
        float step = (_sliderStepCheckbox?.Checked ?? false) ? 0.1f : 0f;

        ApplySliderFlags(_volumeSlider, wholeNumbers, step);
        ApplySliderFlags(_verticalSlider, wholeNumbers, step);
    }

    private void UpdateTextFilter()
    {
        if (_textFilterField == null || _textFilterList == null)
        {
            return;
        }

        string filter = _textFilterField.Text ?? string.Empty;
        if (string.Equals(filter, _lastTextFilter, StringComparison.Ordinal) && _textFilterFilteredItems.Count > 0)
        {
            return;
        }

        string? previousSelection = null;
        if (_textFilterList.SelectedIndex >= 0 && _textFilterList.SelectedIndex < _textFilterFilteredItems.Count)
        {
            previousSelection = _textFilterFilteredItems[_textFilterList.SelectedIndex];
        }

        _textFilterFilteredItems.Clear();
        if (string.IsNullOrWhiteSpace(filter))
        {
            _textFilterFilteredItems.AddRange(_textFilterItems);
        }
        else
        {
            foreach (string item in _textFilterItems)
            {
                if (item.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    _textFilterFilteredItems.Add(item);
                }
            }
        }

        _lastTextFilter = filter;

        if (previousSelection != null)
        {
            _textFilterList.SelectedIndex = _textFilterFilteredItems.IndexOf(previousSelection);
        }
        else if (_textFilterList.SelectedIndex >= _textFilterFilteredItems.Count)
        {
            _textFilterList.SelectedIndex = -1;
        }
    }

    private void SyncDragDropItems()
    {
        int count = Math.Min(_dragDropItems.Count, _dragDropItemLabels.Count);
        for (int i = 0; i < count; i++)
        {
            _dragDropItemLabels[i].Text = _dragDropItems[i];
            _dragDropItemSources[i].PayloadData = i;
        }
    }

    private string BuildCompletionHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(none)";
        }

        string trimmed = text.Trim();
        List<string> matches = new();
        foreach (string option in CompletionHints)
        {
            if (option.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(option);
                if (matches.Count >= 4)
                {
                    break;
                }
            }
        }

        return matches.Count == 0 ? "(none)" : string.Join(", ", matches);
    }

    private string BuildElidedText(string text, int maxWidth, int scale)
    {
        if (_renderer == null || string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return string.Empty;
        }

        string line = text.Split('\n')[0];
        if (_renderer.MeasureTextWidth(line, scale) <= maxWidth)
        {
            return line;
        }

        const string ellipsis = "...";
        int ellipsisWidth = _renderer.MeasureTextWidth(ellipsis, scale);
        if (ellipsisWidth >= maxWidth)
        {
            return ellipsisWidth <= maxWidth ? ellipsis : string.Empty;
        }

        int low = 0;
        int high = line.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            string slice = line.Substring(0, mid);
            int width = _renderer.MeasureTextWidth(slice, scale);
            if (width + ellipsisWidth <= maxWidth)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (low <= 0)
        {
            return ellipsisWidth <= maxWidth ? ellipsis : string.Empty;
        }

        return line.Substring(0, low) + ellipsis;
    }

    private static void ApplySliderFlags(UiSlider? slider, bool wholeNumbers, float step)
    {
        if (slider == null)
        {
            return;
        }

        slider.WholeNumbers = wholeNumbers;
        slider.Step = step;
        slider.Value = slider.Value;
    }

    private static void ApplySliderFlags(UiVSlider? slider, bool wholeNumbers, float step)
    {
        if (slider == null)
        {
            return;
        }

        slider.WholeNumbers = wholeNumbers;
        slider.Step = step;
        slider.Value = slider.Value;
    }

    private static string DescribeElement(UiElement? element)
    {
        if (element == null)
        {
            return "none";
        }

        return element switch
        {
            UiWindow window => DescribeText("Window", window.Title),
            UiButton button => DescribeText("Button", button.Text),
            UiCheckbox checkbox => DescribeText("Checkbox", checkbox.Text),
            UiLabel label => DescribeText("Label", label.Text),
            UiTextBlock => "Text Block",
            UiTextField field => DescribeText("Input", field.Text),
            UiTreeNode node => DescribeText("Tree", node.Text),
            UiTabItem tab => DescribeText("Tab", tab.Text),
            UiSelectable selectable => DescribeText("Selectable", selectable.Text),
            UiComboBox => "Combo Box",
            UiListBox => "List Box",
            UiMenuBar => "Menu Bar",
            _ when !string.IsNullOrWhiteSpace(element.Id) => $"{element.GetType().Name} {element.Id}",
            _ => element.GetType().Name
        };
    }

    private static UiWindow? FindAncestorWindow(UiElement? element)
    {
        UiElement? current = element;
        while (current != null)
        {
            if (current is UiWindow window)
            {
                return window;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string DescribeText(string label, string? text)
    {
        string trimmed = text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            return label;
        }

        return $"{label} \"{TrimText(trimmed, 26)}\"";
    }

    private static string TrimText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        if (maxLength <= 3)
        {
            return text.Substring(0, Math.Max(0, maxLength));
        }

        return text.Substring(0, maxLength - 3) + "...";
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
            _font.CodePage = clamped == 1 ? TinyFontCodePage.Cp437 : TinyFontCodePage.Latin1;
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
                DockTextEditor(_dockLeft);
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
            case ExamplePanel.TextEditor:
                DockTextEditor(_dockLeft);
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

    private void DockTextEditor(UiDockHost host)
    {
        if (_textEditorWindow != null)
        {
            host.DockWindow(_textEditorWindow);
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
        bool textEditor = IsWindowInLayout(_textEditorWindow);
        bool docking = IsWindowInLayout(_assetsWindow) || IsWindowInLayout(_consoleWindow) || IsWindowInLayout(_inspectorWindow);
        bool clipping = IsWindowInLayout(_clippingWindow);
        bool serialization = IsWindowInLayout(_serializationWindow);

        if (basics && widgets && textEditor && docking && clipping && serialization)
        {
            _activeExample = ExamplePanel.All;
        }
        else if (basics && !widgets && !textEditor && !docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Basics;
        }
        else if (!basics && widgets && !textEditor && !docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Widgets;
        }
        else if (!basics && !widgets && !textEditor && docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.Docking;
        }
        else if (!basics && !widgets && !textEditor && !docking && clipping && !serialization)
        {
            _activeExample = ExamplePanel.Clipping;
        }
        else if (!basics && !widgets && !textEditor && !docking && !clipping && serialization)
        {
            _activeExample = ExamplePanel.Serialization;
        }
        else if (!basics && !widgets && textEditor && !docking && !clipping && !serialization)
        {
            _activeExample = ExamplePanel.TextEditor;
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
        if (_examplesMenuAllItem == null || _examplesMenuBasicsItem == null || _examplesMenuWidgetsItem == null || _examplesMenuTextEditorItem == null || _examplesMenuDockingItem == null || _examplesMenuClippingItem == null || _examplesMenuSerializationItem == null)
        {
            return;
        }

        _examplesMenuAllItem.Checked = _activeExample == ExamplePanel.All;
        _examplesMenuBasicsItem.Checked = _activeExample == ExamplePanel.Basics;
        _examplesMenuWidgetsItem.Checked = _activeExample == ExamplePanel.Widgets;
        _examplesMenuTextEditorItem.Checked = _activeExample == ExamplePanel.TextEditor;
        _examplesMenuDockingItem.Checked = _activeExample == ExamplePanel.Docking;
        _examplesMenuClippingItem.Checked = _activeExample == ExamplePanel.Clipping;
        _examplesMenuSerializationItem.Checked = _activeExample == ExamplePanel.Serialization;
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

}
