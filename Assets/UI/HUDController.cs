using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
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
        private Label _selectionLabel;
        private ScrollView _ordersScroll;
        private Button _saveButton;
        private Button _loadButton;
        private Button _buildToggleButton;
        private VisualElement _hireControls;
        private Button _hireWaiterButton;
        private Button _hireCookButton;
        private VisualElement _buildMenu;
        private readonly List<Label> _orderEntries = new List<Label>(16);
        private readonly List<Button> _buildOptionButtons = new List<Button>();
        private readonly Dictionary<Button, BuildOption> _buildOptionLookup = new Dictionary<Button, BuildOption>();
        private EventCallback<ClickEvent> _buildOptionHandler;
        private bool _buildMenuVisible;

        private SelectionService _selectionService;
        private GridPlacer _gridPlacer;
        private IEventBus _eventBus;

        public event Action HireWaiterRequested;
        public event Action HireCookRequested;

        private static readonly BuildOption[] BuildOptions =
        {
            new BuildOption("buildSmallTableBtn", "Mesa pequena", GridPlacer.PlaceableKind.SmallTable),
            new BuildOption("buildLargeTableBtn", "Mesa grande", GridPlacer.PlaceableKind.LargeTable),
            new BuildOption("buildDecorBtn", "Planta", GridPlacer.PlaceableKind.Decoration),
            new BuildOption("buildKitchenStationBtn", "Estação de cozinha", GridPlacer.PlaceableKind.KitchenStation),
            new BuildOption("buildBarCounterBtn", "Balcão do bar", GridPlacer.PlaceableKind.BarCounter),
            new BuildOption("buildPickupPointBtn", "Ponto de retirada", GridPlacer.PlaceableKind.PickupPoint)
        };

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
            UpdateSelectionLabel(_selectionService != null ? _selectionService.Current : null);
            SetBuildMenuVisible(false);

            if (isActiveAndEnabled)
            {
                HookEvents();
            }
        }

        public void BindEventBus(IEventBus eventBus)
        {
            _eventBus = eventBus;
            var toastController = GetComponent<HudToastController>();
            toastController?.Initialize(_eventBus);
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
        }

        private void Update()
        {
            if (_saveService == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null)
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
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
#else
            return;
#endif
        }

        public void SetCustomers(int count)
        {
            if (_customerLabel != null)
            {
                _customerLabel.text = $"Customers: {count}";
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

            rootElement.Clear();

            VisualElement layoutRoot;
            if (visualConfig != null && visualConfig.VisualTree != null)
            {
                layoutRoot = visualConfig.VisualTree.Instantiate();
                rootElement.Add(layoutRoot);
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

            _controlsLabel = rootElement.Q<Label>("controlsLabel") ?? CreateLabel(layoutRoot, "controlsLabel", string.Empty);
            _controlsLabel.text = GetControlsSummary();

            _cashLabel = rootElement.Q<Label>("cashLabel") ?? CreateLabel(layoutRoot, "cashLabel", "Cash: 0");
            _customerLabel = rootElement.Q<Label>("customerLabel") ?? CreateLabel(layoutRoot, "customerLabel", "Customers: 0");
            _ordersScroll = rootElement.Q<ScrollView>("ordersScroll") ?? CreateScroll(layoutRoot);
            _saveButton = rootElement.Q<Button>("saveBtn") ?? CreateButton(layoutRoot, "saveBtn", "Save (F5)");
            _loadButton = rootElement.Q<Button>("loadBtn") ?? CreateButton(layoutRoot, "loadBtn", "Load (F9)");
            _selectionLabel = rootElement.Q<Label>("selectionLabel") ?? CreateLabel(layoutRoot, "selectionLabel", "Selecionado: Nenhum");
            _hireControls = rootElement.Q<VisualElement>("hireControls") ?? CreateHireControls(layoutRoot);
            _hireWaiterButton = rootElement.Q<Button>("hireWaiterBtn") ?? CreateButton(_hireControls, "hireWaiterBtn", "Contratar garçom");
            _hireCookButton = rootElement.Q<Button>("hireCookBtn") ?? CreateButton(_hireControls, "hireCookBtn", "Contratar cozinheiro");
            _hireWaiterButton?.AddToClassList("hud-button");
            _hireCookButton?.AddToClassList("hud-button");
            _hireCookButton?.AddToClassList("stacked");
            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.style.marginTop = 0f;
            }
            _buildToggleButton = rootElement.Q<Button>("buildToggleBtn") ?? CreateButton(layoutRoot, "buildToggleBtn", "Construir");
            _buildMenu = rootElement.Q<VisualElement>("buildMenu") ?? CreateBuildMenu(layoutRoot);

            CreateBuildButtons();
            SetBuildMenuVisible(false);

            var menuController = GetComponent<MenuController>();
            menuController?.RebuildMenu(rootElement);

            var toastController = GetComponent<HudToastController>();
            toastController?.AttachTo(rootElement);
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
                _buildToggleButton.clicked -= ToggleBuildMenu;
                _buildToggleButton.clicked += ToggleBuildMenu;
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
            }

            AttachBuildOptionCallbacks();
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
                _buildToggleButton.clicked -= ToggleBuildMenu;
            }

            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.clicked -= OnHireWaiterClicked;
            }

            if (_hireCookButton != null)
            {
                _hireCookButton.clicked -= OnHireCookClicked;
            }

            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
            }

            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged -= OnPlacementModeChanged;
            }

            DetachBuildOptionCallbacks();
        }

        private void OnCashChanged(float value)
        {
            if (_cashLabel != null)
            {
                _cashLabel.text = $"Cash: {value:0}";
            }
        }

        private void OnOrdersChanged(IReadOnlyList<Order> orders)
        {
            if (_ordersScroll == null)
            {
                return;
            }

            _ordersScroll.contentContainer.Clear();
            _orderEntries.Clear();
            if (orders == null)
            {
                return;
            }

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

            var model = _saveService.CreateModel();
            var path = _saveService.GetDefaultPath();
            _saveService.Save(path, model);
            Debug.Log($"Game saved to {path}");
        }

        private void LoadGame()
        {
            if (_saveService == null)
            {
                return;
            }

            var path = _saveService.GetDefaultPath();
            _saveService.Load(path);
            Debug.Log($"Game loaded from {path}");
        }

        private void ToggleBuildMenu()
        {
            SetBuildMenuVisible(!_buildMenuVisible);
        }

        private void OnHireWaiterClicked()
        {
            HireWaiterRequested?.Invoke();
        }

        private void OnHireCookClicked()
        {
            HireCookRequested?.Invoke();
        }

        private void SetBuildMenuVisible(bool visible)
        {
            _buildMenuVisible = visible;
            if (_buildMenu != null)
            {
                _buildMenu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

            if (evt.currentTarget is Button optionButton && optionButton.userData is GridPlacer.PlaceableKind kind)
            {
                if (_gridPlacer.ActiveKind == kind)
                {
                    _gridPlacer.CancelPlacement();
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
            UpdateSelectionLabel(selectable);
        }

        private void UpdateSelectionLabel(ISelectable selectable)
        {
            if (_selectionLabel == null)
            {
                return;
            }

            _selectionLabel.text = selectable != null
                ? $"Selecionado: {selectable.DisplayName}"
                : "Selecionado: Nenhum";
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
        }

        private void RefreshBuildOptionLabels()
        {
            foreach (var pair in _buildOptionLookup)
            {
                var option = pair.Value;
                var cost = _gridPlacer != null ? _gridPlacer.GetPlacementCost(option.Kind) : 0f;
                pair.Key.text = cost > 0f ? $"{option.Label} ({cost:0})" : option.Label;
            }
        }

        private void CreateBuildButtons()
        {
            if (_buildMenu == null)
            {
                return;
            }

            _buildMenu.Clear();
            _buildOptionButtons.Clear();
            _buildOptionLookup.Clear();

            foreach (var option in BuildOptions)
            {
                var button = new Button
                {
                    name = option.Name,
                    text = option.Label,
                    userData = option.Kind
                };
                button.AddToClassList("build-option");
                if (_buildOptionButtons.Count > 0)
                {
                    button.AddToClassList("build-option--spaced");
                }
                _buildMenu.Add(button);
                _buildOptionButtons.Add(button);
                _buildOptionLookup[button] = option;
            }

            RefreshBuildOptionLabels();
            HighlightActiveOption(GridPlacer.PlaceableKind.None);
        }

        private static Label CreateLabel(VisualElement root, string name, string text)
        {
            var label = new Label(text) { name = name };
            root.Add(label);
            return label;
        }

        private static ScrollView CreateScroll(VisualElement root)
        {
            var scroll = new ScrollView { name = "ordersScroll" };
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
            root.Add(container);
            return container;
        }

        private static VisualElement CreateHireControls(VisualElement root)
        {
            var container = new VisualElement { name = "hireControls" };
            container.AddToClassList("hire-controls");
            container.AddToClassList("stacked");
            root.Add(container);
            return container;
        }

        private static string GetControlsSummary()
        {
            return "Camera: WASD/Arrows move • Shift sprint • Scroll zoom • Right Drag pan • Middle Drag orbit • Q/E rotate • Left Click select • Build button to place props";
        }

        private readonly struct BuildOption
        {
            public readonly string Name;
            public readonly string Label;
            public readonly GridPlacer.PlaceableKind Kind;

            public BuildOption(string name, string label, GridPlacer.PlaceableKind kind)
            {
                Name = name;
                Label = label;
                Kind = kind;
            }
        }
    }
}
