using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using TavernSim.Agents;
using TavernSim.Save;
using TavernSim.Simulation.Systems;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.Core.Events;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TavernSim.UI
{
    /// <summary>
    /// Binds simulation systems to the UI Toolkit HUD.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private HUDVisualConfig visualConfig;

        private EconomySystem _economy;
        private OrderSystem _orders;
        private SaveService _saveService;

        private UIDocument _document;
        private Label _controlsLabel;
        private Label _cashLabel;
        private Label _customerLabel;
        private Label _timeLabel;
        private Label _reputationTopLabel;
        private Label _reputationScoreLabel;
        private Label _reputationTrendLabel;
        private Label _ordersQueueLabel;
        private Label _ordersAverageLabel;
        private Label _tipsLastLabel;
        private Label _tipsAverageLabel;
        private VisualElement _selectionPopup;
        private Label _selectionNameLabel;
        private Label _selectionTypeLabel;
        private Label _selectionStatusLabel;
        private Label _selectionSpeedLabel;
        private VisualElement _contextActions;
        private ScrollView _ordersScroll;
        private ScrollView _logScroll;
        private VisualElement _logBlock;
        private VisualElement _logFilters;
        private Button _saveButton;
        private Button _loadButton;
        private Button _buildToggleButton;
        private Button _decoToggleButton;
        private Button _beautyToggleButton;
        private Button _staffToggleButton;
        private Button _panelToggleButton;
        private Button _panelPinButton;
        private Button _staffCloseButton;
        private Button _logToggleButton;
        private Button _logCloseButton;
        private Button _devLogButton;
        private VisualElement _sidePanel;
        private VisualElement _staffPanel;
        private Label _currentModeLabel;
        private VisualElement _hireControls;
        private Button _hireWaiterButton;
        private Button _hireCookButton;
        private Button _hireBartenderButton;
        private VisualElement _buildMenu;
        private ScrollView _staffList;
        private readonly List<Label> _orderEntries = new List<Label>(16);
        private readonly List<Button> _buildOptionButtons = new List<Button>();
        private readonly Dictionary<Button, BuildCatalog.Entry> _buildOptionLookup = new Dictionary<Button, BuildCatalog.Entry>();
        private EventCallback<ClickEvent> _buildOptionHandler;
        private bool _buildMenuVisible;
        private bool _logVisible;
        private bool _panelPinned;
        private bool _staffOpen;
        private bool _beautyOverlayEnabled;
        private BuildCategory _activeBuildCategory = BuildCategory.Build;

        private SelectionService _selectionService;
        private GridPlacer _gridPlacer;
        private IEventBus _eventBus;
        private HudToastController _toastController;
        private ReputationSystem _reputationSystem;
        private BuildCatalog _buildCatalog;
        private GameClockSystem _clockSystem;
        private bool _pointerGuardsRegistered;
        private bool _isPointerOverHud;
        private bool _sidePanelOpen;
        private readonly List<LogEntry> _logEntries = new List<LogEntry>(64);
        private readonly HashSet<EventCategory> _activeLogFilters = new HashSet<EventCategory>();
        private readonly Queue<float> _recentTips = new Queue<float>();
        private const int MaxTrackedTips = 8;
        private float _tipSum;
        private bool _lastPreviewValid = true;
        private bool _lastPreviewAffordable = true;
        private int _lastReputationValue;

        public event Action HireWaiterRequested;
        public event Action HireCookRequested;
        public event Action HireBartenderRequested;

        public void Initialize(EconomySystem economySystem, OrderSystem orderSystem)
        {
            UnhookEvents();
            _economy = economySystem;
            _orders = orderSystem;
            if (isActiveAndEnabled)
            {
                HookEvents();
            }
        }

        public void BindSaveService(SaveService saveService)
        {
            _saveService = saveService;
            UpdateSaveButtons();
        }

        public void BindSelection(SelectionService selectionService, GridPlacer gridPlacer)
        {
            var changed = _selectionService != selectionService || _gridPlacer != gridPlacer;
            if (!changed)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                UnhookEvents();
            }

            _selectionService = selectionService;
            _gridPlacer = gridPlacer;

            RefreshBuildOptionLabels();
            HighlightActiveOption(_gridPlacer != null ? _gridPlacer.ActiveKind : GridPlacer.PlaceableKind.None);
            UpdateSelectionDetails(_selectionService != null ? _selectionService.Current : null);
            SetBuildMenuVisible(false, true);
            UpdatePointerOverHud();

            if (isActiveAndEnabled)
            {
                HookEvents();
            }
        }

        public void BindEventBus(IEventBus eventBus)
        {
            if (_eventBus == eventBus)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                UnhookEvents();
            }

            _eventBus = eventBus;

            if (isActiveAndEnabled)
            {
                HookEvents();
            }
        }

        public void BindReputation(ReputationSystem reputationSystem)
        {
            if (_reputationSystem == reputationSystem)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                UnhookEvents();
            }

            _reputationSystem = reputationSystem;

            if (isActiveAndEnabled)
            {
                HookEvents();
            }

            UpdateReputation(_reputationSystem != null ? _reputationSystem.Reputation : 0);
        }

        public void BindBuildCatalog(BuildCatalog catalog)
        {
            _buildCatalog = catalog;
            if (isActiveAndEnabled)
            {
                RebuildBuildButtons();
                UpdateBuildButtonsByCash(_economy != null ? _economy.Cash : 0f);
            }
        }

        public void BindClock(GameClockSystem clockSystem)
        {
            if (_clockSystem == clockSystem)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                UnhookEvents();
            }

            _clockSystem = clockSystem;

            if (isActiveAndEnabled)
            {
                HookEvents();
            }

            if (_clockSystem != null)
            {
                UpdateTimeLabel(_clockSystem.Snapshot);
            }
            else
            {
                UpdateTimeLabel(default);
            }
        }

        public void PublishEvent(GameEvent gameEvent)
        {
            _eventBus?.Publish(gameEvent);
        }

        public void SetVisualConfig(HUDVisualConfig config)
        {
            if (visualConfig == config)
            {
                return;
            }

            visualConfig = config;
            if (isActiveAndEnabled)
            {
                ApplyVisualTree();
            }
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document != null)
            {
                // garante que o HUD fique acima de outros UIDocument legados
                _document.sortingOrder = 100;
            }
            _toastController = GetComponent<HudToastController>();
            if (visualConfig == null)
            {
                visualConfig = Resources.Load<HUDVisualConfig>("UI/HUDVisualConfig");
            }
        }

        private void OnEnable()
        {
            ApplyVisualTree();
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
            _isPointerOverHud = false;
            UpdatePointerOverHud();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (_sidePanelOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetSidePanelOpen(false);
            }

            if (_staffOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetStaffPanelOpen(false);
            }

            if (_saveService == null)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SaveGame();
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                LoadGame();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (_sidePanelOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                SetSidePanelOpen(false);
            }

            if (_staffOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                SetStaffPanelOpen(false);
            }

            if (_saveService == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
#else
            if (_saveService == null)
            {
                return;
            }
#endif
        }

        public void SetCustomers(int count)
        {
            if (_customerLabel != null)
            {
                _customerLabel.text = count.ToString("N0", CultureInfo.InvariantCulture);
            }
        }

        private void ApplyVisualTree()
        {
            if (_document == null)
            {
                Debug.LogWarning("HUDController requires a UIDocument to build the HUD visuals.");
                return;
            }

            var rootElement = _document.rootVisualElement;
            if (rootElement == null)
            {
                Debug.LogWarning("HUDController could not access the UIDocument root. UI will be applied once the panel is ready.");
                return;
            }

            // ocupa a tela toda de forma explícita (cura casos de tema/panel)
            rootElement.style.position = Position.Absolute;
            rootElement.style.top = 0;
            rootElement.style.left = 0;
            rootElement.style.right = 0;
            rootElement.style.bottom = 0;

            rootElement.Clear();

            VisualElement layoutRoot;
            if (visualConfig != null && visualConfig.VisualTree != null)
            {
                layoutRoot = visualConfig.VisualTree.Instantiate();
                rootElement.Add(layoutRoot);

                // Garante que o elemento principal ocupe toda a tela mesmo sem USS carregado
                var hudRoot = layoutRoot.Q<VisualElement>("hudRoot");
                if (hudRoot != null)
                {
                    hudRoot.style.position = Position.Absolute;
                    hudRoot.style.top = 0;
                    hudRoot.style.left = 0;
                    hudRoot.style.right = 0;
                    hudRoot.style.bottom = 0;
                    hudRoot.style.flexGrow = 1f;
                }
            }
            else
            {
                layoutRoot = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                rootElement.Add(layoutRoot);
            }

            if (visualConfig != null && visualConfig.StyleSheet != null && !rootElement.styleSheets.Contains(visualConfig.StyleSheet))
            {
                rootElement.styleSheets.Add(visualConfig.StyleSheet);
            }

            var toolbarElement = layoutRoot.Q<VisualElement>(className: "toolbar");
            if (toolbarElement != null)
            {
                toolbarElement.style.position = Position.Absolute;
                toolbarElement.style.left = 0f;
                toolbarElement.style.right = 0f;
                toolbarElement.style.bottom = 0f;
                toolbarElement.style.height = 110f;
                toolbarElement.style.display = DisplayStyle.Flex;
            }

            _controlsLabel = rootElement.Q<Label>("controlsLabel");
            if (_controlsLabel != null)
            {
                _controlsLabel.text = GetControlsSummary();
            }

            _cashLabel = rootElement.Q<Label>("cashLabel") ?? CreateLabel(layoutRoot, "cashLabel", "0");
            _customerLabel = rootElement.Q<Label>("customerLabel") ?? CreateLabel(layoutRoot, "customerLabel", "0");
            _timeLabel = rootElement.Q<Label>("timeLabel") ?? CreateLabel(layoutRoot, "timeLabel", "Dia 1 – 08:00");
            _reputationTopLabel = rootElement.Q<Label>("reputationLabel");
            _reputationScoreLabel = rootElement.Q<Label>("reputationScore") ?? CreateLabel(layoutRoot, "reputationScore", "0");
            _reputationTrendLabel = rootElement.Q<Label>("reputationTrend") ?? CreateLabel(layoutRoot, "reputationTrend", "0");
            _ordersQueueLabel = rootElement.Q<Label>("ordersQueue") ?? CreateLabel(layoutRoot, "ordersQueue", "0");
            _ordersAverageLabel = rootElement.Q<Label>("ordersAverage") ?? CreateLabel(layoutRoot, "ordersAverage", "0s");
            _tipsLastLabel = rootElement.Q<Label>("tipsLast") ?? CreateLabel(layoutRoot, "tipsLast", "0");
            _tipsAverageLabel = rootElement.Q<Label>("tipsAverage") ?? CreateLabel(layoutRoot, "tipsAverage", "0");

            _selectionPopup = rootElement.Q<VisualElement>("selectionPopup");
            _selectionNameLabel = rootElement.Q<Label>("selectionName") ?? CreateLabel(layoutRoot, "selectionName", HUDStrings.NoSelection);
            _selectionTypeLabel = rootElement.Q<Label>("selectionType") ?? CreateLabel(layoutRoot, "selectionType", "-");
            _selectionStatusLabel = rootElement.Q<Label>("selectionStatus") ?? CreateLabel(layoutRoot, "selectionStatus", "-");
            _selectionSpeedLabel = rootElement.Q<Label>("selectionSpeed") ?? CreateLabel(layoutRoot, "selectionSpeed", "-");

            _ordersScroll = rootElement.Q<ScrollView>("ordersScroll") ?? CreateScroll(layoutRoot, "ordersScroll");
            _logBlock = rootElement.Q<VisualElement>("logBlock");
            _logFilters = rootElement.Q<VisualElement>("logFilters") ?? new VisualElement();
            _logScroll = rootElement.Q<ScrollView>("logScroll") ?? CreateScroll(layoutRoot, "logScroll");
            _contextActions = rootElement.Q<VisualElement>("contextActions") ?? layoutRoot;

            _saveButton = rootElement.Q<Button>("saveBtn") ?? CreateButton(layoutRoot, "saveBtn", HUDStrings.SaveLabel);
            _loadButton = rootElement.Q<Button>("loadBtn") ?? CreateButton(layoutRoot, "loadBtn", HUDStrings.LoadLabel);
            _buildToggleButton = rootElement.Q<Button>("buildToggleBtn") ?? CreateButton(layoutRoot, "buildToggleBtn", HUDStrings.BuildToggle);
            _decoToggleButton = rootElement.Q<Button>("decoToggleBtn") ?? CreateButton(layoutRoot, "decoToggleBtn", HUDStrings.DecoToggle);
            _beautyToggleButton = rootElement.Q<Button>("beautyToggleBtn") ?? CreateButton(layoutRoot, "beautyToggleBtn", HUDStrings.BeautyToggle);
            _staffToggleButton = rootElement.Q<Button>("staffBtn") ?? CreateButton(layoutRoot, "staffBtn", HUDStrings.StaffButton);
            _panelToggleButton = rootElement.Q<Button>("panelToggleBtn") ?? CreateButton(layoutRoot, "panelToggleBtn", "Painel");
            _panelPinButton = rootElement.Q<Button>("panelPinBtn") ?? CreateButton(layoutRoot, "panelPinBtn", "Fixar");
            _staffCloseButton = rootElement.Q<Button>("staffCloseBtn");
            _logToggleButton = rootElement.Q<Button>("logToggleBtn");
            _logCloseButton = rootElement.Q<Button>("logCloseBtn");
            _devLogButton = rootElement.Q<Button>("devLogBtn");

            _sidePanel = rootElement.Q<VisualElement>("sidePanel");
            _staffPanel = rootElement.Q<VisualElement>("staffPanel");
            _currentModeLabel = rootElement.Q<Label>("currentModeLabel");
            _buildMenu = rootElement.Q<VisualElement>("buildMenu") ?? CreateBuildMenu(layoutRoot);
            _hireControls = rootElement.Q<VisualElement>("hireControls");
            _hireWaiterButton = rootElement.Q<Button>("hireWaiterBtn");
            if (_hireWaiterButton == null)
            {
                _hireWaiterButton = new Button { name = "hireWaiterBtn", text = HUDStrings.HireWaiter };
                (_hireControls ?? layoutRoot).Add(_hireWaiterButton);
            }

            _hireCookButton = rootElement.Q<Button>("hireCookBtn");
            if (_hireCookButton == null)
            {
                _hireCookButton = new Button { name = "hireCookBtn", text = HUDStrings.HireCook };
                (_hireControls ?? layoutRoot).Add(_hireCookButton);
            }

            _hireBartenderButton = rootElement.Q<Button>("hireBartenderBtn");
            if (_hireBartenderButton == null)
            {
                _hireBartenderButton = new Button { name = "hireBartenderBtn", text = HUDStrings.HireBartender };
                (_hireControls ?? layoutRoot).Add(_hireBartenderButton);
            }
            _staffList = rootElement.Q<ScrollView>("staffList");

            _hireWaiterButton?.AddToClassList("hud-button");
            _hireCookButton?.AddToClassList("hud-button");
            _hireBartenderButton?.AddToClassList("hud-button");
            _saveButton?.AddToClassList("hud-button");
            _loadButton?.AddToClassList("hud-button");
            _buildToggleButton?.AddToClassList("tool-button");
            _decoToggleButton?.AddToClassList("tool-button");
            _beautyToggleButton?.AddToClassList("tool-button");
            _staffToggleButton?.AddToClassList("tool-button");
            _panelToggleButton?.AddToClassList("panel-toggle");
            _panelPinButton?.AddToClassList("panel-pin");
            _logToggleButton?.AddToClassList("hud-button");

            if (_buildToggleButton != null)
            {
                _buildToggleButton.text = HUDStrings.BuildToggle;
            }

            if (_decoToggleButton != null)
            {
                _decoToggleButton.text = HUDStrings.DecoToggle;
            }

            if (_beautyToggleButton != null)
            {
                _beautyToggleButton.text = HUDStrings.BeautyToggle;
            }

            if (_staffToggleButton != null)
            {
                _staffToggleButton.text = HUDStrings.StaffButton;
            }

            if (_staffPanel != null)
            {
                var titleLabel = _staffPanel.Q<Label>("staffTitleLabel");
                if (titleLabel != null)
                {
                    titleLabel.text = HUDStrings.StaffTitle;
                }
            }

#if !UNITY_EDITOR
            if (_devLogButton != null)
            {
                _devLogButton.RemoveFromHierarchy();
                _devLogButton = null;
            }
#endif

            RebuildBuildButtons();
            SetBuildMenuVisible(false, true);
            UpdateCategoryButtons();
            UpdateSaveButtons();

            var menuController = GetComponent<MenuController>();
            if (menuController != null)
            {
                // garante que ele use o mesmo UIDocument do HUD
                menuController.SetDocument(_document);
                menuController.RebuildMenu();
            }

            _toastController?.AttachTo(rootElement);

            RegisterHudPointerGuards(rootElement);
            SetSidePanelOpen(false);
            SetLogVisible(false);
            SetStaffPanelOpen(false);
            if (_selectionPopup != null)
            {
                _selectionPopup.RemoveFromClassList("open");
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
            }
            UpdateCurrentModeLabel(_gridPlacer != null ? _gridPlacer.ActiveKind : GridPlacer.PlaceableKind.None);
            RefreshEventLog();
            RefreshLogFilters();
        }

        private void HookEvents()
        {
            if (_economy != null)
            {
                _economy.CashChanged -= OnCashChanged;
                _economy.CashChanged += OnCashChanged;
                OnCashChanged(_economy.Cash);
            }

            if (_orders != null)
            {
                _orders.OrdersChanged -= OnOrdersChanged;
                _orders.OrdersChanged += OnOrdersChanged;
                OnOrdersChanged(_orders.GetOrders());
            }

            if (_saveButton != null)
            {
                _saveButton.clicked -= SaveGame;
                _saveButton.clicked += SaveGame;
            }

            if (_loadButton != null)
            {
                _loadButton.clicked -= LoadGame;
                _loadButton.clicked += LoadGame;
            }

            if (_buildToggleButton != null)
            {
                _buildToggleButton.clicked -= OnBuildButton;
                _buildToggleButton.clicked += OnBuildButton;
            }

            if (_panelToggleButton != null)
            {
                _panelToggleButton.clicked -= ToggleSidePanel;
                _panelToggleButton.clicked += ToggleSidePanel;
            }

            if (_panelPinButton != null)
            {
                _panelPinButton.clicked -= TogglePanelPin;
                _panelPinButton.clicked += TogglePanelPin;
            }

            if (_staffToggleButton != null)
            {
                _staffToggleButton.clicked -= ToggleStaffPanel;
                _staffToggleButton.clicked += ToggleStaffPanel;
            }

            if (_staffCloseButton != null)
            {
                _staffCloseButton.clicked -= CloseStaffPanel;
                _staffCloseButton.clicked += CloseStaffPanel;
            }

            if (_logToggleButton != null)
            {
                _logToggleButton.clicked -= ToggleLog;
                _logToggleButton.clicked += ToggleLog;
            }

            if (_devLogButton != null)
            {
                _devLogButton.clicked -= ToggleLog;
                _devLogButton.clicked += ToggleLog;
            }

            if (_logCloseButton != null)
            {
                _logCloseButton.clicked -= CloseLogOverlay;
                _logCloseButton.clicked += CloseLogOverlay;
            }

            if (_decoToggleButton != null)
            {
                _decoToggleButton.clicked -= OnDecoToggleClicked;
                _decoToggleButton.clicked += OnDecoToggleClicked;
            }

            if (_beautyToggleButton != null)
            {
                _beautyToggleButton.clicked -= OnBeautyToggleClicked;
                _beautyToggleButton.clicked += OnBeautyToggleClicked;
            }

            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.clicked -= OnHireWaiterClicked;
                _hireWaiterButton.clicked += OnHireWaiterClicked;
            }

            if (_hireCookButton != null)
            {
                _hireCookButton.clicked -= OnHireCookClicked;
                _hireCookButton.clicked += OnHireCookClicked;
            }

            if (_hireBartenderButton != null)
            {
                _hireBartenderButton.clicked -= OnHireBartenderClicked;
                _hireBartenderButton.clicked += OnHireBartenderClicked;
            }

            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
                _selectionService.SelectionChanged += OnSelectionChanged;
                OnSelectionChanged(_selectionService.Current);
            }

            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged -= OnPlacementModeChanged;
                _gridPlacer.PlacementModeChanged += OnPlacementModeChanged;
                OnPlacementModeChanged(_gridPlacer.ActiveKind);
                _gridPlacer.PreviewStateChanged -= OnPreviewStateChanged;
                _gridPlacer.PreviewStateChanged += OnPreviewStateChanged;
                _gridPlacer.PlacementFailedInsufficientFunds -= OnPlacementFailedInsufficientFunds;
                _gridPlacer.PlacementFailedInsufficientFunds += OnPlacementFailedInsufficientFunds;
            }

            AttachBuildOptionCallbacks();

            if (_eventBus != null)
            {
                _eventBus.OnEvent -= OnGameEvent;
                _eventBus.OnEvent += OnGameEvent;
            }

            if (_reputationSystem != null)
            {
                _reputationSystem.ReputationChanged -= OnReputationChanged;
                _reputationSystem.ReputationChanged += OnReputationChanged;
                UpdateReputation(_reputationSystem.Reputation);
            }

            if (_clockSystem != null)
            {
                _clockSystem.TimeChanged -= OnClockChanged;
                _clockSystem.TimeChanged += OnClockChanged;
                UpdateTimeLabel(_clockSystem.Snapshot);
            }
        }

        private void UnhookEvents()
        {
            if (_economy != null)
            {
                _economy.CashChanged -= OnCashChanged;
            }

            if (_orders != null)
            {
                _orders.OrdersChanged -= OnOrdersChanged;
            }

            if (_saveButton != null)
            {
                _saveButton.clicked -= SaveGame;
            }

            if (_loadButton != null)
            {
                _loadButton.clicked -= LoadGame;
            }

            if (_buildToggleButton != null)
            {
                _buildToggleButton.clicked -= OnBuildButton;
            }

            if (_panelToggleButton != null)
            {
                _panelToggleButton.clicked -= ToggleSidePanel;
            }

            if (_panelPinButton != null)
            {
                _panelPinButton.clicked -= TogglePanelPin;
            }

            if (_staffToggleButton != null)
            {
                _staffToggleButton.clicked -= ToggleStaffPanel;
            }

            if (_staffCloseButton != null)
            {
                _staffCloseButton.clicked -= CloseStaffPanel;
            }

            if (_logToggleButton != null)
            {
                _logToggleButton.clicked -= ToggleLog;
            }

            if (_devLogButton != null)
            {
                _devLogButton.clicked -= ToggleLog;
            }

            if (_logCloseButton != null)
            {
                _logCloseButton.clicked -= CloseLogOverlay;
            }

            if (_decoToggleButton != null)
            {
                _decoToggleButton.clicked -= OnDecoToggleClicked;
            }

            if (_beautyToggleButton != null)
            {
                _beautyToggleButton.clicked -= OnBeautyToggleClicked;
            }

            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.clicked -= OnHireWaiterClicked;
            }

            if (_hireCookButton != null)
            {
                _hireCookButton.clicked -= OnHireCookClicked;
            }

            if (_hireBartenderButton != null)
            {
                _hireBartenderButton.clicked -= OnHireBartenderClicked;
            }

            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
            }

            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged -= OnPlacementModeChanged;
                _gridPlacer.PreviewStateChanged -= OnPreviewStateChanged;
                _gridPlacer.PlacementFailedInsufficientFunds -= OnPlacementFailedInsufficientFunds;
            }

            DetachBuildOptionCallbacks();

            if (_eventBus != null)
            {
                _eventBus.OnEvent -= OnGameEvent;
            }

            if (_reputationSystem != null)
            {
                _reputationSystem.ReputationChanged -= OnReputationChanged;
            }

            if (_clockSystem != null)
            {
                _clockSystem.TimeChanged -= OnClockChanged;
            }
        }

        private void OnGameEvent(GameEvent gameEvent)
        {
            var text = string.IsNullOrEmpty(gameEvent.Source)
                ? gameEvent.Message
                : $"{gameEvent.Source}: {gameEvent.Message}";

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _toastController?.Show(gameEvent.Severity, text);
            AddLogEntry(gameEvent, text);

            if (gameEvent.Source == "TipReceived" && gameEvent.Data is Dictionary<string, object> payload && payload.TryGetValue("tip", out var value) && value != null)
            {
                if (float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var tipValue))
                {
                    RegisterTip(tipValue);
                }
            }
        }

        private void OnCashChanged(float value)
        {
            if (_cashLabel != null)
            {
                _cashLabel.text = HUDStrings.FormatCurrency(value);
                _cashLabel.RemoveFromClassList("cash--warning");
            }

            UpdateBuildButtonsByCash(value);
        }

        private void OnOrdersChanged(IReadOnlyList<Order> orders)
        {
            if (_ordersScroll == null)
            {
                return;
            }

            _ordersScroll.contentContainer.Clear();
            _orderEntries.Clear();

            int queueCount = 0;
            float totalRemaining = 0f;
            int timedOrders = 0;

            if (orders != null && orders.Count > 0)
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    var recipeName = order.Recipe != null ? order.Recipe.DisplayName : "Desconhecido";
                    var areaLabel = order.Area.GetDisplayName();
                    var status = order.IsReady ? "Pronto" : order.State == OrderState.Queued ? "Fila" : $"{order.Remaining:0.0}s";
                    var label = new Label
                    {
                        text = $"Mesa {order.TableId} - {recipeName} ({areaLabel}) [{status}]"
                    };
                    ApplyOrderStyle(label, order);
                    _ordersScroll.contentContainer.Add(label);
                    _orderEntries.Add(label);

                    if (!order.IsReady)
                    {
                        queueCount++;
                        if (order.Remaining > 0f)
                        {
                            totalRemaining += order.Remaining;
                            timedOrders++;
                        }
                    }
                }
            }
            else
            {
                var empty = new Label(HUDStrings.EmptyOrders);
                empty.AddToClassList("group-body");
                _ordersScroll.contentContainer.Add(empty);
            }

            if (_ordersQueueLabel != null)
            {
                _ordersQueueLabel.text = queueCount.ToString("N0", CultureInfo.InvariantCulture);
            }

            if (_ordersAverageLabel != null)
            {
                var average = timedOrders > 0 ? totalRemaining / timedOrders : 0f;
                _ordersAverageLabel.text = average > 0f ? $"{average:0.0}s" : "0s";
            }
        }

        private static void ApplyOrderStyle(Label label, Order order)
        {
            if (label == null || order == null)
            {
                return;
            }

            if (order.IsReady)
            {
                label.style.color = new StyleColor(new Color(0.56f, 0.87f, 0.3f));
            }
            else if (order.State == OrderState.Queued)
            {
                label.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            }
            else
            {
                label.style.color = new StyleColor(Color.white);
            }
        }

        private void SaveGame()
        {
            if (_saveService == null)
            {
                return;
            }

            try
            {
                var model = _saveService.CreateModel();
                var path = _saveService.GetDefaultPath();
                _saveService.Save(path, model);
                _toastController?.Show(GameEventSeverity.Success, HUDStrings.SaveSuccess);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Falha ao salvar o jogo: {ex}");
                _toastController?.Show(GameEventSeverity.Error, HUDStrings.SaveFailed);
            }

            UpdateSaveButtons();
        }

        private void LoadGame()
        {
            if (_saveService == null)
            {
                return;
            }

            var path = _saveService.GetDefaultPath();
            if (!_saveService.HasSave(path))
            {
                _toastController?.Show(GameEventSeverity.Warning, HUDStrings.LoadUnavailable);
                UpdateSaveButtons();
                return;
            }

            try
            {
                _saveService.Load(path);
                _toastController?.Show(GameEventSeverity.Success, HUDStrings.LoadSuccess);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Falha ao carregar o jogo: {ex}");
                _toastController?.Show(GameEventSeverity.Error, HUDStrings.LoadFailed);
            }

            UpdateSaveButtons();
        }

        private void OnHireWaiterClicked()
        {
            HireWaiterRequested?.Invoke();
        }

        private void OnHireCookClicked()
        {
            HireCookRequested?.Invoke();
        }

        private void OnHireBartenderClicked()
        {
            HireBartenderRequested?.Invoke();
        }

        private void SetBuildMenuVisible(bool visible, bool triggeredByToggle = false)
        {
            _buildMenuVisible = visible;
            if (_buildMenu != null)
            {
                _buildMenu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateCategoryButtons();

            if (_gridPlacer != null)
            {
                if (visible)
                {
                    _gridPlacer.SetBuildMode(true);
                }
                else if (triggeredByToggle)
                {
                    _gridPlacer.ExitBuildMode();
                }
                else if (!_gridPlacer.HasActivePlacement)
                {
                    _gridPlacer.SetBuildMode(false);
                }
            }
        }

        private void UpdateCategoryButtons()
        {
            var buildActive = _buildMenuVisible && _activeBuildCategory == BuildCategory.Build;
            var decoActive = _buildMenuVisible && _activeBuildCategory == BuildCategory.Deco;
            _buildToggleButton?.EnableInClassList("tool-button--active", buildActive);
            _decoToggleButton?.EnableInClassList("tool-button--active", decoActive);
            _beautyToggleButton?.EnableInClassList("tool-button--active", _beautyOverlayEnabled);
        }

        private void RebuildBuildButtons()
        {
            if (_buildMenu == null)
            {
                return;
            }

            DetachBuildOptionCallbacks();
            _buildMenu.Clear();
            _buildOptionButtons.Clear();
            _buildOptionLookup.Clear();

            if (_buildCatalog == null)
            {
                return;
            }

            foreach (var entry in _buildCatalog.GetEntries(_activeBuildCategory))
            {
                var button = new Button
                {
                    name = entry.Id + "Btn",
                    userData = entry.Kind
                };
                button.AddToClassList("build-option");
                UpdateBuildButtonLabel(button, entry);
                button.tooltip = BuildTooltip(entry);
                _buildMenu.Add(button);
                _buildOptionButtons.Add(button);
                _buildOptionLookup[button] = entry;
            }

            AttachBuildOptionCallbacks();
            UpdateBuildButtonsByCash(_economy != null ? _economy.Cash : 0f);
            HighlightActiveOption(_gridPlacer != null ? _gridPlacer.ActiveKind : GridPlacer.PlaceableKind.None);
        }

        private void UpdateBuildButtonsByCash(float cash)
        {
            foreach (var pair in _buildOptionLookup)
            {
                var entry = pair.Value;
                var button = pair.Key;
                UpdateBuildButtonLabel(button, entry);
                button.SetEnabled(cash + 0.01f >= Mathf.Max(0f, entry.Cost));
            }
        }

        private static void UpdateBuildButtonLabel(Button button, BuildCatalog.Entry entry)
        {
            if (button == null)
            {
                return;
            }

            if (entry.Cost > 0f)
            {
                button.text = string.Format(CultureInfo.InvariantCulture, "{0}\n{1:N0}", entry.Label, entry.Cost);
            }
            else
            {
                button.text = entry.Label;
            }
        }

        private static string BuildTooltip(BuildCatalog.Entry entry)
        {
            var builder = new StringBuilder(entry.Label);
            if (entry.Cost > 0f)
            {
                builder.Append("\nCusto: ");
                builder.Append(entry.Cost.ToString("N0", CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                builder.Append('\n');
                builder.Append(entry.Description);
            }

            return builder.ToString();
        }

        private void UpdateReputation(int value)
        {
            if (_reputationTopLabel != null)
            {
                _reputationTopLabel.text = value.ToString(CultureInfo.InvariantCulture);
            }

            if (_reputationScoreLabel != null)
            {
                _reputationScoreLabel.text = value.ToString(CultureInfo.InvariantCulture);
            }

            if (_reputationTrendLabel != null)
            {
                var delta = _lastReputationValue == 0 ? 0 : value - _lastReputationValue;
                if (delta > 0)
                {
                    _reputationTrendLabel.text = $"+{delta}";
                }
                else if (delta < 0)
                {
                    _reputationTrendLabel.text = delta.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    _reputationTrendLabel.text = "0";
                }
            }

            _lastReputationValue = value;
        }

        private void RegisterTip(float tip)
        {
            if (tip < 0f)
            {
                tip = 0f;
            }

            if (_tipsLastLabel != null)
            {
                _tipsLastLabel.text = tip.ToString("N2", CultureInfo.InvariantCulture);
            }

            _recentTips.Enqueue(tip);
            _tipSum += tip;
            while (_recentTips.Count > MaxTrackedTips)
            {
                _tipSum -= _recentTips.Dequeue();
            }

            if (_tipsAverageLabel != null)
            {
                var average = _recentTips.Count > 0 ? _tipSum / _recentTips.Count : 0f;
                _tipsAverageLabel.text = average.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        private void HighlightCashWarning()
        {
            if (_cashLabel == null)
            {
                return;
            }

            _cashLabel.AddToClassList("cash--warning");
            _cashLabel.schedule.Execute(() => _cashLabel.RemoveFromClassList("cash--warning")).StartingIn(1200);
        }

        private void OnPreviewStateChanged(bool hasValid, bool canAfford)
        {
            _lastPreviewValid = hasValid;
            _lastPreviewAffordable = canAfford;
            UpdateCurrentModeLabel(_gridPlacer != null ? _gridPlacer.ActiveKind : GridPlacer.PlaceableKind.None);
        }

        private void OnPlacementFailedInsufficientFunds(float cost)
        {
            var label = GetBuildOptionLabel(_gridPlacer != null ? _gridPlacer.ActiveKind : GridPlacer.PlaceableKind.None);
            _toastController?.Show(GameEventSeverity.Warning, $"Saldo insuficiente para {label} ({cost:0})");
            HighlightCashWarning();
        }

        private void ShowBuildCategory(BuildCategory category)
        {
            if (_activeBuildCategory != category)
            {
                _activeBuildCategory = category;
                RebuildBuildButtons();
            }

            SetStaffPanelOpen(false);
            SetBuildMenuVisible(true, true);
            UpdateCategoryButtons();
        }

        private void OnBuildButton()
        {
            ShowBuildCategory(BuildCategory.Build);
        }

        private void OnDecoToggleClicked()
        {
            ShowBuildCategory(BuildCategory.Deco);
        }

        private void OnBeautyToggleClicked()
        {
            _beautyOverlayEnabled = !_beautyOverlayEnabled;
            UpdateCategoryButtons();
            OnBeautyToggle(_beautyOverlayEnabled);
        }

        private void OnBeautyToggle(bool enabled)
        {
            _toastController?.Show(GameEventSeverity.Info, enabled ? "Overlay de beleza ativado" : "Overlay de beleza desativado");
        }

        private void ToggleStaffPanel()
        {
            SetStaffPanelOpen(!_staffOpen);
        }

        private void CloseStaffPanel()
        {
            SetStaffPanelOpen(false);
        }

        private void SetStaffPanelOpen(bool open)
        {
            if (_staffPanel == null && open)
            {
                _staffOpen = false;
                _staffToggleButton?.EnableInClassList("tool-button--active", false);
                return;
            }

            _staffOpen = open;
            if (_staffPanel != null)
            {
                _staffPanel.EnableInClassList("open", open);
            }

            _staffToggleButton?.EnableInClassList("tool-button--active", open);

            if (open)
            {
                SetBuildMenuVisible(false, true);
                var wasPinned = _panelPinned;
                if (_panelPinned)
                {
                    _panelPinned = false;
                }

                SetSidePanelOpen(false);

                if (wasPinned)
                {
                    _panelPinned = true;
                    _panelPinButton?.EnableInClassList("panel-pin--active", true);
                }

                RefreshStaffList();
            }
        }

        private void RefreshStaffList()
        {
            if (_staffList == null)
            {
                return;
            }

            var container = _staffList.contentContainer;
            container.Clear();

            void AddRow(string role, string name, string status)
            {
                var row = new VisualElement();
                row.AddToClassList("stat-line");

                var left = new Label($"{role}: {name}");
                left.AddToClassList("stat-label");

                var right = new Label(status);
                right.AddToClassList("stat-value");

                row.Add(left);
                row.Add(right);
                container.Add(row);
            }

            foreach (var waiter in FindObjectsOfType<Waiter>())
            {
                AddRow("Garçom", waiter.DisplayName ?? waiter.name, waiter.Status);
            }

            foreach (var cook in FindObjectsOfType<Cook>())
            {
                AddRow("Cozinheiro", cook.DisplayName ?? cook.name, cook.Status);
            }

            foreach (var bartender in FindObjectsOfType<Bartender>())
            {
                AddRow("Bartender", bartender.DisplayName ?? bartender.name, bartender.Status);
            }

            if (container.childCount == 0)
            {
                container.Add(new Label("Sem funcionários."));
            }
        }

        private void ToggleLog()
        {
            var visible = !_logVisible;
            SetLogVisible(visible);
        }

        private void SetLogVisible(bool visible)
        {
            _logVisible = visible;
            if (_logBlock != null)
            {
                _logBlock.EnableInClassList("open", visible);
                _logBlock.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _logToggleButton?.EnableInClassList("hud-button--active", visible);
            _devLogButton?.EnableInClassList("hud-button--active", visible);
        }

        private void CloseLogOverlay()
        {
            SetLogVisible(false);
        }

        private void TogglePanelPin()
        {
            _panelPinned = !_panelPinned;
            _panelPinButton?.EnableInClassList("panel-pin--active", _panelPinned);
            if (_panelPinned)
            {
                SetSidePanelOpen(true);
                _toastController?.Show(GameEventSeverity.Info, HUDStrings.PinHint);
            }
        }

        private void RefreshLogFilters()
        {
            if (_logFilters == null)
            {
                return;
            }

            if (_activeLogFilters.Count == 0)
            {
                foreach (EventCategory category in Enum.GetValues(typeof(EventCategory)))
                {
                    _activeLogFilters.Add(category);
                }
            }

            _logFilters.Clear();

            foreach (EventCategory category in Enum.GetValues(typeof(EventCategory)))
            {
                var button = new Button { text = GetCategoryLabel(category) };
                button.AddToClassList("log-filter");
                var isActive = _activeLogFilters.Contains(category);
                button.EnableInClassList("log-filter--active", isActive);
                button.clicked += () => ToggleLogFilter(category);
                _logFilters.Add(button);
            }
        }

        private void ToggleLogFilter(EventCategory category)
        {
            if (_activeLogFilters.Contains(category) && _activeLogFilters.Count > 1)
            {
                _activeLogFilters.Remove(category);
            }
            else
            {
                _activeLogFilters.Add(category);
            }

            RefreshLogFilters();
            RefreshEventLog();
        }

        private void RefreshEventLog()
        {
            if (_logScroll == null)
            {
                return;
            }

            _logScroll.contentContainer.Clear();
            bool any = false;
            for (int i = 0; i < _logEntries.Count; i++)
            {
                var entry = _logEntries[i];
                if (!_activeLogFilters.Contains(entry.Category))
                {
                    continue;
                }

                any = true;
                var container = new VisualElement();
                container.AddToClassList("log-entry");

                var meta = new Label(entry.Timestamp);
                meta.AddToClassList("log-entry__meta");
                container.Add(meta);

                var text = new Label(entry.Message);
                container.Add(text);

                _logScroll.contentContainer.Add(container);
            }

            if (!any)
            {
                var empty = new Label("Sem eventos registrados.");
                empty.AddToClassList("group-body");
                _logScroll.contentContainer.Add(empty);
            }
        }

        private static string GetCategoryLabel(EventCategory category)
        {
            return category switch
            {
                EventCategory.Orders => "Pedidos",
                EventCategory.Economy => "Economia",
                EventCategory.Reputation => "Reputação",
                _ => "Sistema"
            };
        }

        private EventCategory ResolveCategory(GameEvent gameEvent)
        {
            var source = gameEvent.Source ?? string.Empty;
            if (source.IndexOf("Order", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EventCategory.Orders;
            }

            if (source.IndexOf("Tip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf("Economy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf("Cash", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EventCategory.Economy;
            }

            if (source.IndexOf("Reputation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return EventCategory.Reputation;
            }

            return EventCategory.System;
        }

        private void AddLogEntry(GameEvent gameEvent, string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return;
            }

            var snapshot = _clockSystem != null ? _clockSystem.Snapshot : new GameClockSystem.GameClockSnapshot(1, 0, 0);
            var timestamp = string.Format(CultureInfo.InvariantCulture, "Dia {0} – {1:00}:{2:00}", Mathf.Max(1, snapshot.Day), Mathf.Clamp(snapshot.Hour, 0, 23), Mathf.Clamp(snapshot.Minute, 0, 59));
            var entry = new LogEntry(displayText, gameEvent.Severity, ResolveCategory(gameEvent), timestamp);
            _logEntries.Add(entry);
            if (_logEntries.Count > 50)
            {
                _logEntries.RemoveAt(0);
            }

            RefreshEventLog();
        }

        private void PopulateContextActions(ISelectable selectable)
        {
            if (_contextActions == null)
            {
                return;
            }

            _contextActions.Clear();

            if (selectable == null)
            {
                var hint = new Label(HUDStrings.NoSelection);
                hint.AddToClassList("group-body");
                _contextActions.Add(hint);
                return;
            }

            void AddAction(string label, Action handler)
            {
                var button = new Button(handler) { text = label };
                button.AddToClassList("hud-button");
                _contextActions.Add(button);
            }

            switch (selectable)
            {
                case Waiter waiter:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(waiter.DisplayName ?? waiter.name + " será removido em breve."));
                    break;
                case Cook cook:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(cook.DisplayName ?? cook.name + " será removido em breve."));
                    break;
                case Bartender bartender:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(bartender.DisplayName ?? bartender.name + " será removido em breve."));
                    break;
                case TablePresenter:
                    AddAction(HUDStrings.MoveAction, () => ShowActionToast("Movimentar mesa ainda não implementado."));
                    AddAction(HUDStrings.SellAction, () => ShowActionToast("Venda rápida disponível futuramente."));
                    break;
                default:
                    var placeholder = new Label("Sem ações disponíveis");
                    placeholder.AddToClassList("group-body");
                    _contextActions.Add(placeholder);
                    break;
            }
        }

        private void ShowActionToast(string message)
        {
            _toastController?.Show(GameEventSeverity.Info, message);
        }

        private void UpdateTimeLabel(GameClockSystem.GameClockSnapshot snapshot)
        {
            if (_timeLabel == null)
            {
                return;
            }

            var day = Mathf.Max(1, snapshot.Day);
            var hour = Mathf.Clamp(snapshot.Hour, 0, 23);
            var minute = Mathf.Clamp(snapshot.Minute, 0, 59);
            _timeLabel.text = string.Format(CultureInfo.InvariantCulture, "Dia {0} – {1:00}:{2:00}", day, hour, minute);
        }

        private void OnClockChanged(GameClockSystem.GameClockSnapshot snapshot)
        {
            UpdateTimeLabel(snapshot);
        }

        private string GetBuildOptionLabel(GridPlacer.PlaceableKind kind)
        {
            if (_buildCatalog != null && _buildCatalog.TryGetEntry(kind, out var entry))
            {
                return entry.Label;
            }

            return kind.ToString();
        }

        private static string GetPlacementHint(GridPlacer.PlaceableKind kind)
        {
            return kind switch
            {
                GridPlacer.PlaceableKind.SmallTable => "Rotacione com Q/E para alinhar.",
                GridPlacer.PlaceableKind.LargeTable => "Rotacione com Q/E e garanta espaço.",
                GridPlacer.PlaceableKind.Decoration => "Arraste para cobrir várias células.",
                GridPlacer.PlaceableKind.KitchenStation => "Reserve área livre para circulação.",
                GridPlacer.PlaceableKind.BarCounter => "Conecte ao balcão existente.",
                GridPlacer.PlaceableKind.PickupPoint => "Escolha um local acessível para a equipe.",
                _ => "Clique para posicionar."
            };
        }

        private void AttachBuildOptionCallbacks()
        {
            if (_buildMenu == null || _buildOptionButtons.Count == 0)
            {
                return;
            }

            _buildOptionHandler ??= OnBuildOptionClicked;
            foreach (var button in _buildOptionButtons)
            {
                button.UnregisterCallback(_buildOptionHandler);
                button.RegisterCallback(_buildOptionHandler);
            }
        }

        private void DetachBuildOptionCallbacks()
        {
            if (_buildOptionHandler == null)
            {
                return;
            }

            foreach (var button in _buildOptionButtons)
            {
                button.UnregisterCallback(_buildOptionHandler);
            }
        }

        private void OnBuildOptionClicked(ClickEvent evt)
        {
            if (_gridPlacer == null)
            {
                return;
            }

            if (evt.currentTarget is Button optionButton && _buildOptionLookup.TryGetValue(optionButton, out var entry))
            {
                var kind = entry.Kind;
                if (_gridPlacer.ActiveKind == kind && _gridPlacer.BuildModeActive)
                {
                    _gridPlacer.ExitBuildMode();
                }
                else
                {
                    _gridPlacer.BeginPlacement(kind);
                }

                HighlightActiveOption(_gridPlacer.ActiveKind);
                SetBuildMenuVisible(false);
            }
        }

        private void OnSelectionChanged(ISelectable selectable)
        {
            UpdateSelectionDetails(selectable);

            if (_selectionPopup == null)
            {
                return;
            }

            var hasSelection = selectable != null;
            _selectionPopup.EnableInClassList("open", hasSelection);

            if (!hasSelection)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            var targetTransform = selectable.Transform;
            if (targetTransform == null)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            ScheduleSelectionPopupPosition(targetTransform);
        }

        private void ScheduleSelectionPopupPosition(Transform target)
        {
            if (_selectionPopup == null || target == null)
            {
                return;
            }

            void Perform()
            {
                if (target != null)
                {
                    PositionSelectionPopup(target);
                }
            }

            PositionSelectionPopup(target);

            var scheduler = _selectionPopup.schedule;
            if (scheduler == null)
            {
                return;
            }

            scheduler.Execute(Perform).ExecuteLater(0);
        }

        private void PositionSelectionPopup(Transform target)
        {
            if (_selectionPopup == null || target == null || _document == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var screenPoint = camera.WorldToScreenPoint(target.position);
            if (screenPoint.z <= 0f)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            var panel = _document.rootVisualElement?.panel;
            if (panel == null)
            {
                return;
            }

            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPoint.x, screenPoint.y));
            float desiredLeft = panelPosition.x + 12f;
            float popupHeight = _selectionPopup.resolvedStyle.height;
            if (float.IsNaN(popupHeight) || float.IsInfinity(popupHeight) || popupHeight <= 0f)
            {
                popupHeight = 120f;
            }

            float desiredTop = panelPosition.y - popupHeight - 12f;
            var rootStyle = _document.rootVisualElement.resolvedStyle;
            float popupWidth = _selectionPopup.resolvedStyle.width;
            if (float.IsNaN(popupWidth) || float.IsInfinity(popupWidth) || popupWidth <= 0f)
            {
                popupWidth = 300f;
            }

            float maxLeft = rootStyle.width - popupWidth - 24f;
            if (float.IsNaN(maxLeft) || float.IsInfinity(maxLeft))
            {
                maxLeft = desiredLeft;
            }

            maxLeft = Mathf.Max(24f, maxLeft);
            desiredLeft = Mathf.Clamp(desiredLeft, 24f, maxLeft);

            float maxTop = rootStyle.height - 140f;
            if (float.IsNaN(maxTop) || float.IsInfinity(maxTop))
            {
                maxTop = desiredTop;
            }

            maxTop = Mathf.Max(72f, maxTop);
            desiredTop = Mathf.Clamp(desiredTop, 72f, maxTop);

            _selectionPopup.style.right = StyleKeyword.Null;
            _selectionPopup.style.left = desiredLeft;
            _selectionPopup.style.top = desiredTop;
        }

        private void UpdateSelectionDetails(ISelectable selectable)
        {
            if (_selectionNameLabel == null || _selectionTypeLabel == null || _selectionStatusLabel == null || _selectionSpeedLabel == null)
            {
                return;
            }

            if (selectable == null)
            {
                _selectionNameLabel.text = HUDStrings.NoSelection;
                _selectionTypeLabel.text = "-";
                _selectionStatusLabel.text = "-";
                _selectionSpeedLabel.text = "-";
                PopulateContextActions(null);
                return;
            }

            _selectionNameLabel.text = selectable.DisplayName ?? selectable.Transform?.name ?? "Selecionado";
            _selectionTypeLabel.text = selectable.GetType().Name;
            string status = string.Empty;
            string speedText = "-";
            string extra = string.Empty;
            switch (selectable)
            {
                case Customer customer:
                    status = customer.Status;
                    var customerSpeed = customer.Agent != null ? customer.Agent.speed : 0f;
                    speedText = customerSpeed > 0.01f ? customerSpeed.ToString("0.##", CultureInfo.InvariantCulture) : "-";
                    extra = customer.Gold >= 0 ? $"Ouro: {customer.Gold}" : string.Empty;
                    break;
                case Waiter waiter:
                    status = waiter.Status;
                    speedText = waiter.MovementSpeed > 0.01f ? waiter.MovementSpeed.ToString("0.##", CultureInfo.InvariantCulture) : "-";
                    extra = waiter.Salary > 0f ? $"Salário: {waiter.Salary:0.##}" : string.Empty;
                    break;
                case Bartender bartender:
                    status = bartender.Status;
                    speedText = bartender.MovementSpeed > 0.01f ? bartender.MovementSpeed.ToString("0.##", CultureInfo.InvariantCulture) : "-";
                    extra = bartender.Salary > 0f ? $"Salário: {bartender.Salary:0.##}" : string.Empty;
                    break;
                case Cook cook:
                    status = cook.Status;
                    speedText = cook.MovementSpeed > 0.01f ? cook.MovementSpeed.ToString("0.##", CultureInfo.InvariantCulture) : "-";
                    extra = cook.Salary > 0f ? $"Salário: {cook.Salary:0.##}" : string.Empty;
                    break;
                case TablePresenter tablePresenter:
                    status = tablePresenter.OccupiedSeats > 0 ? $"Ocupada ({tablePresenter.OccupiedSeats}/{tablePresenter.SeatCount})" : "Livre";
                    extra = tablePresenter.Dirtiness > 0.01f ? "Precisa limpeza" : string.Empty;
                    break;
                default:
                    var go = selectable.Transform != null ? selectable.Transform.gameObject : null;
                    var agentSpeed = GetNavAgentSpeed(go);
                    speedText = agentSpeed > 0.01f ? agentSpeed.ToString("0.##", CultureInfo.InvariantCulture) : "-";
                    status = GetIntent(go);
                    break;
            }

            _selectionStatusLabel.text = string.IsNullOrWhiteSpace(status) ? "-" : status;
            _selectionSpeedLabel.text = speedText;
            if (!string.IsNullOrWhiteSpace(extra))
            {
                if (string.IsNullOrWhiteSpace(status) || status == "-")
                {
                    _selectionStatusLabel.text = extra;
                }
                else
                {
                    _selectionStatusLabel.text += $" • {extra}";
                }
            }

            PopulateContextActions(selectable);
        }

        private void OnPlacementModeChanged(GridPlacer.PlaceableKind kind)
        {
            HighlightActiveOption(kind);
        }

        private void HighlightActiveOption(GridPlacer.PlaceableKind kind)
        {
            foreach (var button in _buildOptionButtons)
            {
                if (button.userData is GridPlacer.PlaceableKind buttonKind && buttonKind == kind)
                {
                    button.AddToClassList("build-option--active");
                }
                else
                {
                    button.RemoveFromClassList("build-option--active");
                }
            }

            UpdateCurrentModeLabel(kind);
        }

        private void ToggleSidePanel()
        {
            SetSidePanelOpen(!_sidePanelOpen);
        }

        private void SetSidePanelOpen(bool open)
        {
            if (!open && _panelPinned)
            {
                return;
            }

            if (open)
            {
                SetStaffPanelOpen(false);
            }

            _sidePanelOpen = open;
            _sidePanel?.EnableInClassList("open", open);
            _panelToggleButton?.EnableInClassList("panel-toggle--active", open);
            _panelPinButton?.EnableInClassList("panel-pin--active", _panelPinned);
        }

        private static float GetNavAgentSpeed(GameObject go)
        {
            if (go != null && go.TryGetComponent(out NavMeshAgent agent))
            {
                return agent.speed;
            }

            return 0f;
        }

        private static string GetIntent(GameObject go)
        {
            if (go != null && go.TryGetComponent(out AgentIntentDisplay intentDisplay))
            {
                return intentDisplay.CurrentIntent;
            }

            return string.Empty;
        }

        private void RefreshBuildOptionLabels()
        {
            foreach (var pair in _buildOptionLookup)
            {
                UpdateBuildButtonLabel(pair.Key, pair.Value);
                pair.Key.tooltip = BuildTooltip(pair.Value);
            }
        }

        private void UpdateSaveButtons()
        {
            if (_saveButton != null)
            {
                _saveButton.SetEnabled(_saveService != null);
            }

            if (_loadButton != null)
            {
                var hasSave = _saveService != null && _saveService.HasSave();
                _loadButton.SetEnabled(hasSave);
            }
        }


        private int _hudPointerDepth;

        private void RegisterHudPointerGuards(VisualElement rootElement)
        {
            if (_pointerGuardsRegistered || rootElement == null)
            {
                return;
            }

            rootElement.RegisterCallback<PointerEnterEvent>(OnHudPointerEnter, TrickleDown.TrickleDown);
            rootElement.RegisterCallback<PointerLeaveEvent>(OnHudPointerLeave, TrickleDown.TrickleDown);
            _pointerGuardsRegistered = true;
        }

        private void OnHudPointerEnter(PointerEnterEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            _hudPointerDepth++;

            if (_hudPointerDepth == 1)
            {
                _isPointerOverHud = true;
                UpdatePointerOverHud();
            }
        }

        private void OnHudPointerLeave(PointerLeaveEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (_hudPointerDepth > 0)
            {
                _hudPointerDepth--;
            }

            if (_hudPointerDepth == 0)
            {
                _isPointerOverHud = false;
                UpdatePointerOverHud();
            }
        }

        private void UpdatePointerOverHud()
        {
            _gridPlacer?.SetPointerOverUI(_isPointerOverHud);
        }

        private void OnReputationChanged(int value)
        {
            UpdateReputation(value);
        }

        private static Label CreateLabel(VisualElement root, string name, string text)
        {
            var label = new Label(text) { name = name };
            root.Add(label);
            return label;
        }

        private static ScrollView CreateScroll(VisualElement root, string name)
        {
            var scroll = new ScrollView { name = name };
            root.Add(scroll);
            return scroll;
        }

        private static Button CreateButton(VisualElement root, string name, string text)
        {
            var button = new Button { name = name, text = text };
            root.Add(button);
            return button;
        }

        private static VisualElement CreateBuildMenu(VisualElement root)
        {
            var container = new VisualElement { name = "buildMenu" };
            container.AddToClassList("toolbar-options");
            root.Add(container);
            return container;
        }

        private static string GetControlsSummary()
        {
            return "Camera: WASD/Arrows move • Shift sprint • Scroll zoom • Right Drag pan • Middle Drag orbit • Q/E rotate • Left Click select • Build button to place props";
        }

        private void UpdateCurrentModeLabel(GridPlacer.PlaceableKind kind)
        {
            if (_currentModeLabel == null)
            {
                return;
            }

            if (kind == GridPlacer.PlaceableKind.None)
            {
                _currentModeLabel.text = HUDStrings.Ready;
                return;
            }

            var label = GetBuildOptionLabel(kind);
            var hint = GetPlacementHint(kind);
            var issues = !_lastPreviewValid ? " — posição inválida" : (!_lastPreviewAffordable ? " — saldo insuficiente" : string.Empty);
            _currentModeLabel.text = string.Format(CultureInfo.InvariantCulture, "⚒️ {0}{1}\n{2}", label, issues, hint);
        }

        private enum EventCategory
        {
            Orders,
            Economy,
            Reputation,
            System
        }

        private readonly struct LogEntry
        {
            public readonly string Message;
            public readonly GameEventSeverity Severity;
            public readonly EventCategory Category;
            public readonly string Timestamp;

            public LogEntry(string message, GameEventSeverity severity, EventCategory category, string timestamp)
            {
                Message = message;
                Severity = severity;
                Category = category;
                Timestamp = timestamp;
            }
        }
    }
}
