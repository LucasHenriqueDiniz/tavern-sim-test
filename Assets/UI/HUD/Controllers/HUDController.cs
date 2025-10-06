using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;
using TavernSim.Building;
using TavernSim.Agents;
using TavernSim.Save;
using TavernSim.Core;
using TavernSim.UI.Events;
using TavernSim.UI.SystemStubs;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller principal do HUD - coordena todos os componentes.
    /// Segue o padrão UI-first: UXML define layout, C# apenas handlers.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HUDController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EconomySystem economySystem;
        [SerializeField] private OrderSystem orderSystem;
        [SerializeField] private ReputationSystem reputationSystem;
        [SerializeField] private GameClockSystem clockSystem;
        [SerializeField] private IWeatherService weatherService;
        [SerializeField] private BuildCatalog buildCatalog;
        [SerializeField] private GridPlacer gridPlacer;
        [SerializeField] private IEventBus eventBus;

        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset visualTree;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private string visualTreeResourcePath = "UI/HUD";

        // UI Document
        private UIDocument _document;
        private VisualElement _root;

        // Component Controllers
        private TopBarController _topBarController;
        private BottomBarController _bottomBarController;
        private SidePanelController _sidePanelController;
        private StaffPanelController _staffPanelController;
        private BuildMenuController _buildMenuController;
        private SelectionPopupController _selectionPopupController;
        private HudToastController _toastController;
        private CentralHudController _centralHudController;
        
        // Sistemas para compatibilidade
        private SaveService _saveService;
        private SelectionService _selectionService;
        private IEventBus _eventBus;
        private ReputationSystem _reputationSystem;
        private BuildCatalog _buildCatalog;
        private GameClockSystem _clockSystem;
        private IWeatherService _weatherService;

        // Events
        public event Action StaffPanelRequested;
        public event Action BuildMenuRequested;
        public event Action SidePanelRequested;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
            _centralHudController?.Dispose();
        }

        private void SetupControllers()
        {
            // Buscar elementos dos templates importados
            var topBar = _root.Q("topBar");
            var bottomBar = _root.Q("bottomBar");
            var centralHud = _root.Q("centralHUD");
            var sidePanel = _root.Q("sidePanel");
            var staffPanel = _root.Q("staffPanel");
            var buildMenu = _root.Q("buildMenu");
            var selectionPopup = _root.Q("selectionPopup");

            // Criar controllers especializados
            _topBarController = new TopBarController(topBar, economySystem, reputationSystem, clockSystem, weatherService);
            _bottomBarController = new BottomBarController(bottomBar, economySystem, clockSystem, weatherService);
            _centralHudController = new CentralHudController(centralHud, clockSystem ?? _clockSystem);
            _sidePanelController = new SidePanelController(sidePanel, economySystem, orderSystem, reputationSystem);
            _staffPanelController = new StaffPanelController(staffPanel);
            _buildMenuController = new BuildMenuController(buildMenu, buildCatalog, gridPlacer);
            _selectionPopupController = new SelectionPopupController(selectionPopup);
            _toastController = gameObject.AddComponent<HudToastController>();

            // Hook eventos dos controllers
            _topBarController.StaffButtonClicked += HandleStaffButtonClicked;
            _bottomBarController.BuildButtonClicked += HandleBuildButtonClicked;
            _bottomBarController.SidePanelButtonClicked += HandleSidePanelButtonClicked;
        }

        private void InitializeControllers()
        {
            _topBarController?.Initialize();
            _bottomBarController?.Initialize();
            _centralHudController?.Initialize();
            _sidePanelController?.Initialize();
            _staffPanelController?.Initialize();
            _buildMenuController?.Initialize();
            _selectionPopupController?.Initialize();
            _toastController?.Initialize(eventBus ?? _eventBus, _root);
        }

        private void HookEvents()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<AgentEvent.CustomerEntered>(OnCustomerEntered);
                eventBus.Subscribe<AgentEvent.CustomerLeft>(OnCustomerLeft);
                eventBus.Subscribe<EconomyEvent.CashChanged>(OnCashChanged);
                eventBus.Subscribe<OrderEvent.OrderPlaced>(OnOrderPlaced);
                eventBus.Subscribe<OrderEvent.OrderReady>(OnOrderReady);
            }
        }

        private void UnhookEvents()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<AgentEvent.CustomerEntered>(OnCustomerEntered);
                eventBus.Unsubscribe<AgentEvent.CustomerLeft>(OnCustomerLeft);
                eventBus.Unsubscribe<EconomyEvent.CashChanged>(OnCashChanged);
                eventBus.Unsubscribe<OrderEvent.OrderPlaced>(OnOrderPlaced);
                eventBus.Unsubscribe<OrderEvent.OrderReady>(OnOrderReady);
            }
        }

        // Event Handlers
        private void OnCustomerEntered(AgentEvent.CustomerEntered evt)
        {
            _topBarController?.UpdateCustomerCount();
        }

        private void OnCustomerLeft(AgentEvent.CustomerLeft evt)
        {
            _topBarController?.UpdateCustomerCount();
        }

        private void OnCashChanged(EconomyEvent.CashChanged evt)
        {
            _topBarController?.UpdateCash(evt.Data2); // Data2 é o newAmount
        }

        private void OnOrderPlaced(OrderEvent.OrderPlaced evt)
        {
            _sidePanelController?.UpdateOrders();
        }

        private void OnOrderReady(OrderEvent.OrderReady evt)
        {
            _sidePanelController?.UpdateOrders();
        }

        // Public API
        public void ShowStaffPanel() => _staffPanelController?.Show();
        public void HideStaffPanel() => _staffPanelController?.Hide();
        public void ShowBuildMenu() => _buildMenuController?.Show();
        public void HideBuildMenu() => _buildMenuController?.Hide();
        public void ShowSidePanel() => _sidePanelController?.Show();
        public void HideSidePanel() => _sidePanelController?.Hide();
        public void ShowSelectionPopup(TavernSim.Core.ISelectable selectable) => _selectionPopupController?.Show(selectable);
        public void HideSelectionPopup() => _selectionPopupController?.Hide();
        public void ShowToast(string message, ToastType type = ToastType.Info) => _toastController?.ShowToast(message, type);
        
        // Métodos para compatibilidade com DevBootstrap
        public void SetCustomers(int count) 
        {
            // Implementar lógica para atualizar contador de clientes
            Debug.Log($"Setting customer count to: {count}");
        }
        
        public void SetVisualAssets(VisualTreeAsset tree, StyleSheet sheet = null)
        {
            visualTree = tree;
            styleSheet = sheet;
        }
        
        public void Initialize() 
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            // Ensure a VisualTree is assigned either via serialized field or loaded from Resources
            if (_document.visualTreeAsset == null)
            {
                if (visualTree != null)
                {
                    _document.visualTreeAsset = visualTree;
                }
                else if (!string.IsNullOrEmpty(visualTreeResourcePath))
                {
                    var loaded = Resources.Load<VisualTreeAsset>(visualTreeResourcePath);
                    if (loaded != null)
                    {
                        _document.visualTreeAsset = loaded;
                    }
                }
            }

            _root = _document?.rootVisualElement;
            if (_root == null)
            {
                // Painel ainda não criado. Tentar novamente no próximo frame
                StartCoroutine(InitializeNextFrame());
                Debug.LogWarning("HUDController: rootVisualElement is null. Ensure PanelSettings and VisualTree are assigned.");
                return;
            }

            // Prefer element with id/name 'hudRoot' if present
            var hudRoot = _root.Q("hudRoot");
            if (hudRoot != null)
            {
                _root = hudRoot;
            }

            // Apply stylesheet if provided
            if (styleSheet != null && !_root.styleSheets.Contains(styleSheet))
            {
                _root.styleSheets.Add(styleSheet);
            }

            SetupControllers();
            InitializeControllers();
            Debug.Log("HUDController initialized");
        }

        private IEnumerator InitializeNextFrame()
        {
            // aguardar um frame para permitir que o UIDocument crie o painel
            yield return null;

            _root = _document != null ? _document.rootVisualElement : null;
            if (_root == null)
            {
                // tentar mais uma vez
                yield return null;
                _root = _document != null ? _document.rootVisualElement : null;
            }

            if (_root == null)
            {
                yield break;
            }

            // Prefer element with id/name 'hudRoot' if present
            var hudRoot = _root.Q("hudRoot");
            if (hudRoot != null)
            {
                _root = hudRoot;
            }

            if (styleSheet != null && !_root.styleSheets.Contains(styleSheet))
            {
                _root.styleSheets.Add(styleSheet);
            }

            SetupControllers();
            InitializeControllers();
            Debug.Log("HUDController initialized (delayed)");
        }
        
        public void BindSaveService(SaveService saveService) 
        {
            _saveService = saveService;
        }
        
        public void BindSelection(SelectionService selectionService) 
        {
            _selectionService = selectionService;
        }
        
        public void BindEventBus(IEventBus eventBus) 
        {
            _eventBus = eventBus;
        }
        
        public void BindReputation(ReputationSystem reputationSystem) 
        {
            _reputationSystem = reputationSystem;
        }
        
        public void BindBuildCatalog(BuildCatalog buildCatalog) 
        {
            _buildCatalog = buildCatalog;
        }
        
        public void BindClock(GameClockSystem clockSystem)
        {
            _clockSystem = clockSystem;
            _centralHudController?.BindClock(clockSystem);
        }
        
        public void BindWeather(IWeatherService weatherService) 
        {
            _weatherService = weatherService;
        }

        // Eventos para compatibilidade
        public event Action HireWaiterRequested;
        public event Action HireCookRequested;
        public event Action HireBartenderRequested;
        public event Action HireCleanerRequested;
        public event Action FireWaiterRequested;
        public event Action FireCookRequested;
        public event Action FireBartenderRequested;
        public event Action FireCleanerRequested;

        private void HandleStaffButtonClicked()
        {
            if (_staffPanelController != null)
            {
                if (_staffPanelController.IsVisible)
                {
                    _staffPanelController.Hide();
                }
                else
                {
                    HideBuildMenu();
                    HideSidePanel();
                    _staffPanelController.Show();
                }
            }

            StaffPanelRequested?.Invoke();
        }

        private void HandleBuildButtonClicked()
        {
            if (_buildMenuController != null)
            {
                if (_buildMenuController.IsVisible)
                {
                    _buildMenuController.Hide();
                }
                else
                {
                    HideStaffPanel();
                    HideSidePanel();
                    _buildMenuController.Show();
                }
            }

            BuildMenuRequested?.Invoke();
        }

        private void HandleSidePanelButtonClicked()
        {
            if (_sidePanelController != null)
            {
                if (_sidePanelController.IsVisible)
                {
                    _sidePanelController.Hide();
                }
                else
                {
                    HideStaffPanel();
                    HideBuildMenu();
                    _sidePanelController.Show();
                }
            }

            SidePanelRequested?.Invoke();
        }
    }
}
