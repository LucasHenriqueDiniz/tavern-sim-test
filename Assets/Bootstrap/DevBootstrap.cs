using Unity.AI.Navigation; // Requires installing the AI Navigation package from the Package Manager.
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Agents;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.Core.Simulation;
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

        private Vector3 _entryPoint;
        private Vector3 _exitPoint;
        private Vector3 _kitchenPoint;

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

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            bar.transform.position = new Vector3(-2f, 0.75f, 1.5f);
            bar.transform.localScale = new Vector3(2f, 1.5f, 1f);
            NavMeshSetup.MarkObstacle(bar);

            var tableGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableGo.name = "Table";
            tableGo.transform.position = new Vector3(0f, 0.6f, 1f);
            tableGo.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            NavMeshSetup.MarkObstacle(tableGo);

            var seatA = new GameObject("Seat_A");
            seatA.transform.SetParent(tableGo.transform, false);
            seatA.transform.localPosition = new Vector3(0f, -0.5f, 0.8f);
            seatA.transform.LookAt(seatA.transform.position + Vector3.back);

            var seatB = new GameObject("Seat_B");
            seatB.transform.SetParent(tableGo.transform, false);
            seatB.transform.localPosition = new Vector3(0f, -0.5f, -0.8f);
            seatB.transform.LookAt(seatB.transform.position + Vector3.forward);

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

            var waiterGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            waiterGo.name = "Waiter";
            waiterGo.transform.position = new Vector3(-1f, 0f, 0f);
            var waiterAgent = waiterGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            waiterAgent.radius = 0.3f;
            waiterAgent.height = 1.8f;
            waiterGo.AddComponent<AgentIntentDisplay>();
            waiterGo.AddComponent<Waiter>();
        }

        private void SetupSimulation()
        {
            _runner = gameObject.AddComponent<SimulationRunner>();

            _economySystem = new EconomySystem(500f, 1f);
            _orderSystem = new OrderSystem();
            _cleaningSystem = new CleaningSystem(0.1f);
            _tableRegistry = new TableRegistry();
            _agentSystem = new AgentSystem(_tableRegistry, _orderSystem, _economySystem, _cleaningSystem, catalog);
            _agentSystem.Configure(_entryPoint, _exitPoint, _kitchenPoint);
            _agentSystem.ActiveCustomerCountChanged += count => _hudController?.SetCustomers(count);

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
            _gridPlacer?.Configure(_economySystem, _selectionService);

            var tableGo = GameObject.Find("Table");
            var table = new Table(0, tableGo.transform);
            table.AddSeat(new Seat(0, tableGo.transform.Find("Seat_A")));
            table.AddSeat(new Seat(1, tableGo.transform.Find("Seat_B")));
            _tableRegistry.RegisterTable(table);
            _cleaningSystem.RegisterTable(table);

            var waiter = FindObjectOfType<Waiter>();
            _agentSystem.RegisterWaiter(waiter);

            _debugOverlay.Configure(_agentSystem, _orderSystem);
        }

        private void SetupUI()
        {
            var uiGo = new GameObject("HUD");
            uiGo.transform.SetParent(transform, false);
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = GetOrCreatePanelSettings();
            _hudController = uiGo.AddComponent<HUDController>();
            _hudController.Initialize(_economySystem, _orderSystem);

            _timeControls = uiGo.AddComponent<TimeControls>();
            _timeControls.Initialize();

            _hudController.BindSaveService(_saveService);
            _hudController.BindSelection(_selectionService, _gridPlacer);
            _hudController.SetCustomers(0);
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

        private static PanelSettings GetOrCreatePanelSettings()
        {
            if (_panelSettings == null)
            {
                _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _panelSettings.name = "DevBootstrapPanelSettings";
                _panelSettings.hideFlags = HideFlags.HideAndDontSave;
                _panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _panelSettings.sortingOrder = 100;
                _panelSettings.targetTexture = null;
            }

            return _panelSettings;
        }
    }
}
