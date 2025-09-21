using Unity.AI.Navigation; // Requires installing the AI Navigation package from the Package Manager.
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif
using TavernSim.Agents;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.Core.Simulation;
using TavernSim.Core.Events;
using TavernSim.Domain;
using TavernSim.Debugging;
using TavernSim.Save;
using TavernSim.Simulation.Models;
using TavernSim.Simulation.Systems;
using TavernSim.UI;

namespace TavernSim.Bootstrap
{
    /// <summary>
    /// Builds a graybox scene and wires up the deterministic simulation for development.
    /// </summary>
    public sealed class DevBootstrap : MonoBehaviour
    {
        [SerializeField] private Catalog catalog;

        private static PanelSettings _panelSettings;
        private static ThemeStyleSheet _panelTheme;
        private static PanelTextSettings _panelTextSettings;
        private static bool _themeLookupAttempted;
        private static bool _panelTextSettingsLookupAttempted;

        private SimulationRunner _runner;
        private EconomySystem _economySystem;
        private OrderSystem _orderSystem;
        private AgentSystem _agentSystem;
        private CleaningSystem _cleaningSystem;
        private TableRegistry _tableRegistry;
        private CustomerSpawner _customerSpawner;
        private SaveService _saveService;
        private NavMeshSurface _navMeshSurface;
        private DebugOverlay _debugOverlay;
        private HUDController _hudController;
        private TimeControls _timeControls;
        private GridPlacer _gridPlacer;
        private SelectionService _selectionService;
        private GameEventBus _eventBus;
        private IInventoryService _inventoryService;
        private MenuController _menuController;
        private HudToastController _toastController;

        private Waiter _initialWaiter;
        private Cook _initialCook;
        private Bartender _initialBartender;
        private int _waiterCount;
        private int _cookCount;
        private int _bartenderCount;

        private Vector3 _entryPoint;
        private Vector3 _exitPoint;
        private Vector3 _kitchenPoint;
        private Vector3 _kitchenPickupPoint;
        private Vector3 _barPickupPoint;

        private static readonly Vector3 WaiterSpawnBase = new Vector3(-1f, 0f, 0f);
        private static readonly Vector3 CookSpawnBase = new Vector3(-1.5f, 0f, 2.6f);
        private static readonly Vector3 BartenderSpawnBase = new Vector3(-2.5f, 0f, 1.5f);
        private const float StaffSpawnSpacing = 0.75f;

        public EconomySystem Economy => _economySystem;
        public OrderSystem Orders => _orderSystem;
        public AgentSystem Agents => _agentSystem;
        public SaveService SaveService => _saveService;

        private void Awake()
        {
            if (catalog == null)
            {
                catalog = Resources.Load<Catalog>("TavernCatalog");
            }

            if (catalog == null)
            {
                catalog = Resources.Load<Catalog>("TavernCatalog");
            }

            if (catalog == null)
            {
                Debug.LogWarning("DevBootstrap could not locate a Catalog asset in Resources. Assign one in the inspector to enable recipes and menu items.");
                catalog = ScriptableObject.CreateInstance<Catalog>();
            }

            SetupScene();
            SetupSimulation();
            SetupUI();
        }

        private void SetupScene()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            _navMeshSurface = NavMeshSetup.EnsureSurface(ground);

            _entryPoint = new Vector3(0f, 0f, -4f);
            _exitPoint = new Vector3(0f, 0f, -5f);
            _kitchenPoint = new Vector3(-2f, 0f, 2f);
            _kitchenPickupPoint = new Vector3(-1.5f, 0f, 2.25f);
            _barPickupPoint = new Vector3(-2.5f, 0f, 1.25f);

            var kitchenPickup = new GameObject("Kitchen_Pickup");
            kitchenPickup.transform.position = _kitchenPickupPoint;
            kitchenPickup.transform.SetParent(transform, false);

            var barPickup = new GameObject("Bar_Pickup");
            barPickup.transform.position = _barPickupPoint;
            barPickup.transform.SetParent(transform, false);

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            bar.transform.position = new Vector3(-2f, 0.75f, 1.5f);
            bar.transform.localScale = new Vector3(2f, 1.5f, 1f);
            NavMeshSetup.MarkObstacle(bar);

            var cameraGo = new GameObject("DevCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 6f, -6f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            cameraGo.AddComponent<FullCameraController>();

            var placerGo = new GameObject("GridPlacer");
            _gridPlacer = placerGo.AddComponent<GridPlacer>();

            _selectionService = new SelectionService();

            _debugOverlay = gameObject.AddComponent<DebugOverlay>();

            _navMeshSurface?.BuildNavMesh();

            _waiterCount = 0;
            _cookCount = 0;
            _bartenderCount = 0;

            _initialWaiter = CreateWaiter(GetWaiterSpawnPosition(_waiterCount));
            _waiterCount++;

            _initialBartender = CreateBartender(GetBartenderSpawnPosition(_bartenderCount));
            _bartenderCount++;

            _initialCook = CreateCook(GetCookSpawnPosition(_cookCount));
            _cookCount++;
        }

        private void SetupSimulation()
        {
            _runner = gameObject.AddComponent<SimulationRunner>();

            _eventBus = new GameEventBus();
            _inventoryService ??= new DevInventoryService();

            _economySystem = new EconomySystem(500f, 1f);
            _orderSystem = new OrderSystem();
            _orderSystem.SetEventBus(_eventBus);
            _orderSystem.SetKitchenStations(2);
            _orderSystem.SetBarStations(1);
            _cleaningSystem = new CleaningSystem(0.1f);
            _tableRegistry = new TableRegistry();
            _agentSystem = new AgentSystem(_tableRegistry, _orderSystem, _economySystem, _cleaningSystem, catalog);
            _agentSystem.Configure(_entryPoint, _exitPoint, _kitchenPoint, _kitchenPickupPoint, _barPickupPoint);
            _agentSystem.SetInventory(_inventoryService);
            _agentSystem.SetEventBus(_eventBus);
            _agentSystem.ActiveCustomerCountChanged += count => _hudController?.SetCustomers(count);
            _agentSystem.CustomerLeftAngry += HandleCustomerLeftAngry;

            _runner.RegisterSystem(_economySystem);
            _runner.RegisterSystem(_orderSystem);
            _runner.RegisterSystem(_cleaningSystem);
            _runner.RegisterSystem(_tableRegistry);
            _runner.RegisterSystem(_agentSystem);

            _customerSpawner = new GameObject("CustomerSpawner").AddComponent<CustomerSpawner>();
            _customerSpawner.transform.SetParent(transform, false);
            var customerPrefab = CreateCustomerPrefab(_customerSpawner.transform);

            if (customerPrefab == null)
            {
                Debug.LogWarning(
                    "DevBootstrap could not create a runtime customer prefab with a NavMeshAgent. " +
                    "Customer spawning will be disabled until a valid NavMesh is baked.");
            }

            _customerSpawner.Configure(_agentSystem, customerPrefab);
            _runner.RegisterSystem(_customerSpawner);

            _saveService = new SaveService(_economySystem);
            _gridPlacer?.Configure(_economySystem, _selectionService, _tableRegistry, _cleaningSystem);

            var initialTable = TableBuilderUtility.CreateSmallTable(_tableRegistry.Tables.Count, new Vector3(0f, 0f, 1f));
            _tableRegistry.RegisterTable(initialTable);
            _cleaningSystem.RegisterTable(initialTable);

            if (_initialWaiter != null)
            {
                _agentSystem.RegisterWaiter(_initialWaiter);
            }

            _debugOverlay.Configure(_agentSystem, _orderSystem);
        }

        private void SetupUI()
        {
            var uiGo = new GameObject("HUD");
            uiGo.transform.SetParent(transform, false);
            uiGo.SetActive(false);

            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = GetOrCreatePanelSettings();

            _hudController = uiGo.AddComponent<HUDController>();
            _hudController.Initialize(_economySystem, _orderSystem);
            _hudController.BindSaveService(_saveService);
            _hudController.BindSelection(_selectionService, _gridPlacer);
            _hudController.BindEventBus(_eventBus);

            _toastController = uiGo.AddComponent<HudToastController>();
            _toastController.Initialize(_eventBus);

            _menuController = uiGo.AddComponent<MenuController>();
            _menuController.Initialize(catalog);
            _agentSystem.SetMenuPolicy(_menuController);

            _timeControls = uiGo.AddComponent<TimeControls>();

            EnsureEventSystem();

            if (_hudController != null)
            {
                _hudController.HireWaiterRequested += HandleHireWaiterRequested;
                _hudController.HireCookRequested += HandleHireCookRequested;
            }

            uiGo.SetActive(true);

            _timeControls.Initialize();
            _hudController.SetCustomers(_agentSystem != null ? _agentSystem.ActiveCustomerCount : 0);
        }

        private void HandleCustomerLeftAngry(Customer customer)
        {
            if (customer != null)
            {
                Debug.LogWarning($"Cliente {customer.name} saiu irritado por falta de mesas.");
            }
            else
            {
                Debug.LogWarning("Um cliente saiu irritado por falta de mesas.");
            }
        }

        private void OnDestroy()
        {
            if (_agentSystem != null)
            {
                _agentSystem.CustomerLeftAngry -= HandleCustomerLeftAngry;
            }

            if (_hudController != null)
            {
                _hudController.HireWaiterRequested -= HandleHireWaiterRequested;
                _hudController.HireCookRequested -= HandleHireCookRequested;
            }
        }

        private void HandleHireWaiterRequested()
        {
            if (_agentSystem == null)
            {
                return;
            }

            var spawn = GetWaiterSpawnPosition(_waiterCount);
            var waiter = CreateWaiter(spawn);
            _waiterCount++;
            _agentSystem.RegisterWaiter(waiter);
        }

        private void HandleHireCookRequested()
        {
            var spawn = GetCookSpawnPosition(_cookCount);
            CreateCook(spawn);
            _cookCount++;
        }

        private Waiter CreateWaiter(Vector3 position)
        {
            var index = _waiterCount + 1;
            var waiterGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            waiterGo.name = index == 1 ? "Waiter" : $"Waiter_{index}";
            waiterGo.transform.SetParent(transform, false);
            var agent = EnsureNavMeshAgent(waiterGo);
            if (!agent.Warp(position))
            {
                Debug.LogWarning($"Waiter NavMeshAgent.Warp falhou para {waiterGo.name}; verifique o NavMesh.");
            }

            waiterGo.AddComponent<AgentIntentDisplay>();
            return waiterGo.AddComponent<Waiter>();
        }

        private Bartender CreateBartender(Vector3 position)
        {
            var index = _bartenderCount + 1;
            var bartenderGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bartenderGo.name = index == 1 ? "Bartender" : $"Bartender_{index}";
            bartenderGo.transform.SetParent(transform, false);
            var agent = EnsureNavMeshAgent(bartenderGo);
            if (!agent.Warp(position))
            {
                Debug.LogWarning($"Bartender NavMeshAgent.Warp falhou para {bartenderGo.name}; verifique o NavMesh.");
            }

            return bartenderGo.AddComponent<Bartender>();
        }

        private Cook CreateCook(Vector3 position)
        {
            var index = _cookCount + 1;
            var cookGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cookGo.name = index == 1 ? "Cook" : $"Cook_{index}";
            cookGo.transform.SetParent(transform, false);
            var agent = EnsureNavMeshAgent(cookGo);
            if (!agent.Warp(position))
            {
                Debug.LogWarning($"Cook NavMeshAgent.Warp falhou para {cookGo.name}; verifique o NavMesh.");
            }

            return cookGo.AddComponent<Cook>();
        }

        private static NavMeshAgent EnsureNavMeshAgent(GameObject go)
        {
            if (!go.TryGetComponent(out NavMeshAgent agent))
            {
                agent = go.AddComponent<NavMeshAgent>();
            }

            agent.radius = 0.3f;
            agent.height = 1.8f;
            return agent;
        }

        private static Vector3 GetWaiterSpawnPosition(int index)
        {
            return WaiterSpawnBase + new Vector3(-StaffSpawnSpacing * index, 0f, 0f);
        }

        private static Vector3 GetCookSpawnPosition(int index)
        {
            return CookSpawnBase + new Vector3(StaffSpawnSpacing * index, 0f, 0f);
        }

        private static Vector3 GetBartenderSpawnPosition(int index)
        {
            return BartenderSpawnBase + new Vector3(-StaffSpawnSpacing * index, 0f, 0f);
        }

        private static Customer CreateCustomerPrefab(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "CustomerPrefab";
            go.transform.SetParent(parent, false);
            
            if (!go.TryGetComponent(out NavMeshAgent agent))
            {
                agent = go.AddComponent<NavMeshAgent>();
            }

            if (agent == null)
            {
                Debug.LogError(
                    "Customer prefab requires the AI Navigation package. " +
                    "Install the package or bake a NavMesh before running the dev bootstrap scene.");
                Destroy(go);
                return null;
            }

            agent.radius = 0.3f;
            agent.height = 1.8f;
            go.AddComponent<AgentIntentDisplay>();
            var customer = go.AddComponent<Customer>();
            go.hideFlags = HideFlags.HideInHierarchy;
            go.SetActive(false);
            return customer;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(transform, false);
            eventSystemGo.hideFlags = HideFlags.HideAndDontSave;
            var eventSystem = eventSystemGo.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (!eventSystemGo.TryGetComponent<InputSystemUIInputModule>(out _))
            {
                eventSystemGo.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (!eventSystemGo.TryGetComponent<StandaloneInputModule>(out _))
            {
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            if (_panelSettings != null)
            {
                return _panelSettings;
            }

            var asset = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (asset != null)
            {
                _panelSettings = Instantiate(asset);
                _panelSettings.name = asset.name + " (Runtime)";
                _panelSettings.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _panelSettings.name = "DevBootstrapPanelSettings";
                _panelSettings.hideFlags = HideFlags.HideAndDontSave;
                _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _panelSettings.sortingOrder = 100;
                _panelSettings.targetTexture = null;
            }

            var theme = GetOrLoadTheme();
            if (theme != null)
            {
                _panelSettings.themeStyleSheet = theme;
            }

            var panelTextSettings = GetOrLoadPanelTextSettings();
            if (panelTextSettings != null)
            {
                _panelSettings.textSettings = panelTextSettings;
            }

            return _panelSettings;
        }

        private static ThemeStyleSheet GetOrLoadTheme()
        {
            if (_panelTheme != null || _themeLookupAttempted)
            {
                return _panelTheme;
            }

            _themeLookupAttempted = true;

            _panelTheme = Resources.Load<ThemeStyleSheet>(ThemeResourcePath);

            if (_panelTheme == null)
            {
                Debug.LogWarning("DevBootstrap could not locate the runtime UI theme. HUD will use default styling.");
            }

            return _panelTheme;
        }

        private static PanelTextSettings GetOrLoadPanelTextSettings()
        {
            if (_panelTextSettings != null || _panelTextSettingsLookupAttempted)
            {
                return _panelTextSettings;
            }

            _panelTextSettingsLookupAttempted = true;

            var resourceAsset = Resources.Load<PanelTextSettings>(PanelTextSettingsResourcePath);
            if (resourceAsset != null)
            {
                _panelTextSettings = Instantiate(resourceAsset);
                _panelTextSettings.hideFlags = HideFlags.HideAndDontSave;
                return _panelTextSettings;
            }

            if (_panelTextSettings == null)
            {
                Debug.LogWarning("DevBootstrap could not locate PanelTextSettings. UI text may not use the intended font assets.");
            }

            return _panelTextSettings;
        }
        private const string PanelSettingsResourcePath = "UIToolkit/DevBootstrapPanelSettings";
        private const string ThemeResourcePath = "UIToolkit/UnityThemes/UnityDefaultRuntimeTheme";
        private const string PanelTextSettingsResourcePath = "UIToolkit/DevBootstrapPanelTextSettings";

        private sealed class DevInventoryService : IInventoryService
        {
            public bool CanCraft(RecipeSO recipe) => true;
            public bool TryConsume(RecipeSO recipe) => true;
        }

    }
}
