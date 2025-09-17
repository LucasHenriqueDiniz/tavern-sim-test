using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Save;
using TavernSim.Simulation.Models;
using TavernSim.Simulation.Systems;

namespace TavernSim.UI
{
    /// <summary>
    /// Binds simulation systems to the UI Toolkit HUD.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HUDController : MonoBehaviour
    {
        private EconomySystem _economy;
        private OrderSystem _orders;
        private SaveService _saveService;

        private UIDocument _document;
        private Label _cashLabel;
        private Label _customerLabel;
        private ScrollView _ordersScroll;
        private Button _saveButton;
        private Button _loadButton;
        private readonly List<Label> _orderEntries = new List<Label>(16);

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

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            LoadVisualTree();
        }

        private void OnEnable()
        {
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

            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
        }

        public void SetCustomers(int count)
        {
            if (_customerLabel != null)
            {
                _customerLabel.text = $"Customers: {count}";
            }
        }

        private void LoadVisualTree()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/HUD");
            if (visualTree != null)
            {
                _document.visualTreeAsset = visualTree;
            }

            if (_document.visualTreeAsset == null)
            {
                var root = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                _document.rootVisualElement.Add(root);
            }

            var rootElement = _document.rootVisualElement;
            var styleSheet = Resources.Load<StyleSheet>("UI/HUD");
            if (styleSheet != null)
            {
                rootElement.styleSheets.Add(styleSheet);
            }

            _cashLabel = rootElement.Q<Label>("cashLabel") ?? CreateLabel(rootElement, "cashLabel", "Cash: 0");
            _customerLabel = rootElement.Q<Label>("customerLabel") ?? CreateLabel(rootElement, "customerLabel", "Customers: 0");
            _ordersScroll = rootElement.Q<ScrollView>("ordersScroll") ?? CreateScroll(rootElement);
            _saveButton = rootElement.Q<Button>("saveBtn") ?? CreateButton(rootElement, "saveBtn", "Save (F5)");
            _loadButton = rootElement.Q<Button>("loadBtn") ?? CreateButton(rootElement, "loadBtn", "Load (F9)");
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
                var label = new Label
                {
                    text = $"Table {orders[i].TableId} - {orders[i].Recipe?.DisplayName ?? "Unknown"} ({orders[i].Remaining:0.0}s)"
                };
                _ordersScroll.contentContainer.Add(label);
                _orderEntries.Add(label);
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
    }
}
