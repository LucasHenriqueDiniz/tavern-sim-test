using UnityEngine;
using TavernSim.Simulation.Systems;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.Core.Events;
using TavernSim.Agents;
using TavernSim.Save;

namespace TavernSim.UI
{
    /// <summary>
    /// Exemplo de como integrar o HUD refatorado com os sistemas do jogo.
    /// </summary>
    public class HUDIntegrationExample : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private EconomySystem economySystem;
        [SerializeField] private OrderSystem orderSystem;
        [SerializeField] private ReputationSystem reputationSystem;
        [SerializeField] private GameClockSystem clockSystem;
        [SerializeField] private BuildCatalog buildCatalog;
        [SerializeField] private SelectionService selectionService;
        [SerializeField] private GridPlacer gridPlacer;
        [SerializeField] private SaveService saveService;
        [SerializeField] private GameEventBus eventBus;

        [Header("UI")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private StaffPanelController staffPanelController;
        [SerializeField] private WeatherServiceStub weatherService;

        private void Start()
        {
            SetupHUD();
            SetupStaffPanel();
            SetupWeather();
            SetupCursorManagement();
        }

        private void SetupHUD()
        {
            if (hudController == null)
            {
                hudController = FindObjectOfType<HUDController>();
            }

            if (hudController == null)
            {
                Debug.LogError("HUDController not found!");
                return;
            }

            // Initialize core systems
            hudController.Initialize(economySystem, orderSystem);
            hudController.BindSaveService(saveService);
            hudController.BindSelection(selectionService, gridPlacer);
            hudController.BindEventBus(eventBus);
            hudController.BindReputation(reputationSystem);
            hudController.BindBuildCatalog(buildCatalog);
            hudController.BindClock(clockSystem);
            hudController.BindWeather(weatherService);

            // Hook events
            hudController.HireWaiterRequested += OnHireWaiter;
            hudController.HireCookRequested += OnHireCook;
            hudController.HireBartenderRequested += OnHireBartender;
            hudController.HireCleanerRequested += OnHireCleaner;
            hudController.FireWaiterRequested += OnFireWaiter;
            hudController.FireCookRequested += OnFireCook;
            hudController.FireBartenderRequested += OnFireBartender;
            hudController.FireCleanerRequested += OnFireCleaner;
        }

        private void SetupStaffPanel()
        {
            if (staffPanelController == null)
            {
                staffPanelController = FindObjectOfType<StaffPanelController>();
            }

            if (staffPanelController != null)
            {
                // Staff panel events are already hooked in HUDController
                // Just ensure it's properly initialized
                staffPanelController.RefreshAllStaffLists();
            }
        }

        private void SetupWeather()
        {
            if (weatherService == null)
            {
                weatherService = FindObjectOfType<WeatherServiceStub>();
            }

            if (weatherService == null)
            {
                // Create a weather service if none exists
                var weatherGO = new GameObject("WeatherService");
                weatherService = weatherGO.AddComponent<WeatherServiceStub>();
            }
        }

        private void SetupCursorManagement()
        {
            var cursorManager = FindObjectOfType<CursorManager>();
            if (cursorManager == null)
            {
                var cursorGO = new GameObject("CursorManager");
                cursorManager = cursorGO.AddComponent<CursorManager>();
            }

            // Cursor management is handled automatically by HUDController
            // when GridPlacer is bound
        }

        // Staff management methods
        private void OnHireWaiter()
        {
            Debug.Log("Hiring waiter...");
            // Implement waiter hiring logic
            // Example: Spawn waiter prefab, deduct cost, etc.
        }

        private void OnHireCook()
        {
            Debug.Log("Hiring cook...");
            // Implement cook hiring logic
        }

        private void OnHireBartender()
        {
            Debug.Log("Hiring bartender...");
            // Implement bartender hiring logic
        }

        private void OnHireCleaner()
        {
            Debug.Log("Hiring cleaner...");
            // Implement cleaner hiring logic
        }

        private void OnFireWaiter(Waiter waiter)
        {
            Debug.Log($"Firing waiter: {waiter.name}");
            // Implement waiter firing logic
            // Example: Remove from scene, refund partial cost, etc.
        }

        private void OnFireCook(Cook cook)
        {
            Debug.Log($"Firing cook: {cook.name}");
            // Implement cook firing logic
        }

        private void OnFireBartender(Bartender bartender)
        {
            Debug.Log($"Firing bartender: {bartender.name}");
            // Implement bartender firing logic
        }

        private void OnFireCleaner(Cleaner cleaner)
        {
            Debug.Log($"Firing cleaner: {cleaner.name}");
            // Implement cleaner firing logic
        }

        // Example of updating customer count
        private void Update()
        {
            // Example: Update customer count based on active customers
            var customerCount = FindObjectsOfType<Customer>().Length;
            hudController?.SetCustomers(customerCount);
        }
    }
}
