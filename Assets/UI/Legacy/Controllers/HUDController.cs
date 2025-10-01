using System;
using System.Collections.Generic;
using UnityEngine;
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

namespace TavernSim.UI.Legacy
{
    /// <summary>
    /// Coordenador principal do HUD que integra todos os controllers especializados.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [ExecuteAlways]
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private TopBarController topBarController;
        [SerializeField] private ToolbarController toolbarController;
        [SerializeField] private SidePanelController sidePanelController;
        [SerializeField] private SelectionPopupController selectionPopupController;
        [SerializeField] private StaffPanelController staffPanelController;
        [SerializeField] private CursorManager cursorManager;
        [SerializeField] private HudToastController toastController;

        [Header("Configuration")]
        [SerializeField] private HUDVisualConfig visualConfig;

        // Systems
        private EconomySystem _economy;
        private OrderSystem _orders;
        private SaveService _saveService;
        private IEventBus _eventBus;
        private ReputationSystem _reputationSystem;
        private BuildCatalog _buildCatalog;
        private GameClockSystem _clockSystem;
        private IWeatherService _weatherService;
        private SelectionService _selectionService;
        private GridPlacer _gridPlacer;

        // UI Elements
        private UIDocument _document;
        private Label _controlsLabel;
        private Button _devLogButton;
        private Button _panelToggleButton;
        private bool _logVisible;

        // Events
        public event Action HireWaiterRequested;
        public event Action HireCookRequested;
        public event Action HireBartenderRequested;
        public event Action HireCleanerRequested;
        public event Action<Waiter> FireWaiterRequested;
        public event Action<Cook> FireCookRequested;
        public event Action<Bartender> FireBartenderRequested;
        public event Action<Cleaner> FireCleanerRequested;
        public event Action<ISelectable> MoveSelectionRequested;
        public event Action<ISelectable> SellSelectionRequested;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document != null)
            {
                _document.sortingOrder = 100;
            }

            if (visualConfig == null)
            {
                visualConfig = Resources.Load<HUDVisualConfig>("UI/HUDVisualConfig");
            }

            SetupControllers();
        }

        private void OnEnable()
        {
            ApplyVisualTree();
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            HandleInput();
        }

        private void SetupControllers()
        {
            topBarController ??= GetComponent<TopBarController>();
            toolbarController ??= GetComponent<ToolbarController>();
            sidePanelController ??= GetComponent<SidePanelController>();
            selectionPopupController ??= GetComponent<SelectionPopupController>();
            staffPanelController ??= GetComponent<StaffPanelController>();
            cursorManager ??= GetComponent<CursorManager>();
            toastController ??= GetComponent<HudToastController>();

            // Setup toolbar controller to use this HUDController's UIDocument
            if (toolbarController != null)
            {
                toolbarController.Initialize(_document);
                toolbarController.SetHUDController(this);
            }

            // Setup staff panel controller
            if (staffPanelController != null)
            {
                staffPanelController.Initialize(_document);
            }

            // Hook controller events
            if (topBarController != null)
            {
                topBarController.StaffButtonClicked += OnStaffButtonClicked;
            }

            if (toolbarController != null)
            {
                toolbarController.BeautyToggleChanged += OnBeautyToggleChanged;
                toolbarController.StaffToggleClicked += OnStaffButtonClicked;
            }

            if (sidePanelController != null)
            {
                sidePanelController.PanelToggled += OnSidePanelToggled;
            }

            if (selectionPopupController != null)
            {
                selectionPopupController.FireRequested += OnSelectionFireRequested;
                selectionPopupController.MoveRequested += OnSelectionMoveRequested;
                selectionPopupController.SellRequested += OnSelectionSellRequested;
            }

            if (staffPanelController != null)
            {
                staffPanelController.HireWaiterRequested += () => HireWaiterRequested?.Invoke();
                staffPanelController.HireCookRequested += () => HireCookRequested?.Invoke();
                staffPanelController.HireBartenderRequested += () => HireBartenderRequested?.Invoke();
                staffPanelController.HireCleanerRequested += () => HireCleanerRequested?.Invoke();
            }
        }

        private void ApplyVisualTree()
        {
            if (_document == null)
            {
                return;
            }

            var rootElement = _document.rootVisualElement;
            if (rootElement == null)
            {
                return;
            }

            // Setup root element
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

            // Setup controls label
            _controlsLabel = rootElement.Q<Label>("controlsLabel");
            if (_controlsLabel != null)
            {
                _controlsLabel.text = GetControlsSummary();
            }

            // Setup dev log button
            _devLogButton = rootElement.Q<Button>("devLogBtn");
#if !UNITY_EDITOR
            if (_devLogButton != null)
            {
                _devLogButton.RemoveFromHierarchy();
                _devLogButton = null;
            }
#endif

            // Setup panel toggle button
            _panelToggleButton = rootElement.Q<Button>("panelToggleBtn");
            if (_panelToggleButton != null)
            {
                _panelToggleButton.clicked -= OnPanelToggleClicked;
                _panelToggleButton.clicked += OnPanelToggleClicked;
            }

            // Attach toast controller
            toastController?.AttachTo(rootElement);


            // Setup pointer guards for cursor management
            RegisterHudPointerGuards(rootElement);

            // Rebind depois que o UXML foi injetado
            toolbarController?.Initialize(_document);
            sidePanelController?.RebuildUI();
            staffPanelController?.Initialize(_document);
        }

        private void HookEvents()
        {
            // Events are hooked in SetupControllers
        }

        private void UnhookEvents()
        {
            if (topBarController != null)
            {
                topBarController.StaffButtonClicked -= OnStaffButtonClicked;
            }

            if (toolbarController != null)
            {
                toolbarController.BeautyToggleChanged -= OnBeautyToggleChanged;
                toolbarController.StaffToggleClicked -= OnStaffButtonClicked;
            }

            if (sidePanelController != null)
            {
                sidePanelController.PanelToggled -= OnSidePanelToggled;
            }

            if (selectionPopupController != null)
            {
                selectionPopupController.FireRequested -= OnSelectionFireRequested;
                selectionPopupController.MoveRequested -= OnSelectionMoveRequested;
                selectionPopupController.SellRequested -= OnSelectionSellRequested;
            }
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (staffPanelController != null && staffPanelController.IsOpen)
                {
                    staffPanelController.ClosePanel();
                }
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (staffPanelController != null && staffPanelController.IsOpen)
                {
                    staffPanelController.ClosePanel();
                }
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
#endif
        }

        // Public API methods
        public void Initialize(EconomySystem economySystem, OrderSystem orderSystem)
        {
            _economy = economySystem;
            _orders = orderSystem;

            // Bind to controllers
            topBarController?.BindEconomy(_economy);
            toolbarController?.BindEconomy(_economy);
            sidePanelController?.BindOrders(_orders);
        }

        public void BindSaveService(SaveService saveService)
        {
            _saveService = saveService;
        }

        public void BindSelection(SelectionService selectionService, GridPlacer gridPlacer)
        {
            _selectionService = selectionService;
            _gridPlacer = gridPlacer;

            selectionPopupController?.BindSelection(_selectionService);
            toolbarController?.BindGridPlacer(_gridPlacer);

            // Setup cursor management
            if (cursorManager != null)
            {
                if (_gridPlacer != null)
                {
                    _gridPlacer.PreviewStateChanged += (hasValid, canAfford) => 
                        cursorManager.SetPreviewState(hasValid, canAfford);
                    _gridPlacer.PlacementModeChanged += (kind) => 
                        cursorManager.SetBuildMode(kind != GridPlacer.PlaceableKind.None);
                }
            }
        }

        public void BindEventBus(IEventBus eventBus)
        {
            _eventBus = eventBus;
            sidePanelController?.BindEventBus(_eventBus);
        }

        public void BindReputation(ReputationSystem reputationSystem)
        {
            _reputationSystem = reputationSystem;
            topBarController?.SetReputation(_reputationSystem?.Reputation ?? 0);
            sidePanelController?.BindReputation(_reputationSystem);
        }

        public void BindBuildCatalog(BuildCatalog catalog)
        {
            _buildCatalog = catalog;
            toolbarController?.BindBuildCatalog(_buildCatalog);
        }

        public void BindClock(GameClockSystem clockSystem)
        {
            _clockSystem = clockSystem;
            topBarController?.BindClock(_clockSystem);
        }

        public void BindWeather(IWeatherService weatherService)
        {
            _weatherService = weatherService;
            topBarController?.BindWeather(_weatherService);
        }

        public void SetCustomers(int count)
        {
            topBarController?.SetCustomers(count);
        }

        public void SetSatisfaction(float satisfaction)
        {
            topBarController?.SetSatisfaction(satisfaction);
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

        // Event handlers
        private void OnStaffButtonClicked()
        {
            staffPanelController?.TogglePanel();
        }

        private void OnPanelToggleClicked()
        {
            sidePanelController?.ToggleSidePanel();
        }

        private void OnBeautyToggleChanged()
        {
            // Handle beauty toggle if needed
        }

        private void OnSidePanelToggled()
        {
            // Handle side panel toggle if needed
        }

        private void OnSelectionFireRequested(ISelectable selectable)
        {
            switch (selectable)
            {
                case Waiter waiter:
                    FireWaiterRequested?.Invoke(waiter);
                    break;
                case Cook cook:
                    FireCookRequested?.Invoke(cook);
                    break;
                case Bartender bartender:
                    FireBartenderRequested?.Invoke(bartender);
                    break;
                case Cleaner cleaner:
                    FireCleanerRequested?.Invoke(cleaner);
                    break;
            }
        }

        private void OnSelectionMoveRequested(ISelectable selectable)
        {
            MoveSelectionRequested?.Invoke(selectable);
        }

        private void OnSelectionSellRequested(ISelectable selectable)
        {
            SellSelectionRequested?.Invoke(selectable);
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
                toastController?.Show(GameEventSeverity.Success, HUDStrings.SaveSuccess);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Falha ao salvar o jogo: {ex}");
                toastController?.Show(GameEventSeverity.Error, HUDStrings.SaveFailed);
            }
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
                toastController?.Show(GameEventSeverity.Warning, HUDStrings.LoadUnavailable);
                return;
            }

            try
            {
                _saveService.Load(path);
                toastController?.Show(GameEventSeverity.Success, HUDStrings.LoadSuccess);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Falha ao carregar o jogo: {ex}");
                toastController?.Show(GameEventSeverity.Error, HUDStrings.LoadFailed);
            }
        }

        private void RegisterHudPointerGuards(VisualElement rootElement)
        {
            if (rootElement == null)
            {
                return;
            }

            rootElement.UnregisterCallback<PointerEnterEvent>(OnHudPointerEnter, TrickleDown.TrickleDown);
            rootElement.UnregisterCallback<PointerLeaveEvent>(OnHudPointerLeave, TrickleDown.TrickleDown);
            rootElement.RegisterCallback<PointerEnterEvent>(OnHudPointerEnter, TrickleDown.TrickleDown);
            rootElement.RegisterCallback<PointerLeaveEvent>(OnHudPointerLeave, TrickleDown.TrickleDown);
        }

        private void OnHudPointerEnter(PointerEnterEvent evt)
        {
            cursorManager?.SetPointerOverHud(true);
        }

        private void OnHudPointerLeave(PointerLeaveEvent evt)
        {
            cursorManager?.SetPointerOverHud(false);
        }

        private static string GetControlsSummary()
        {
            return "Camera: WASD/Arrows move • Shift sprint • Scroll zoom • Right Drag pan • Middle Drag orbit • Q/E rotate • Left Click select • Build button to place props";
        }

    }
}
