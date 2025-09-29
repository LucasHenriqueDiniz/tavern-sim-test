using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller para o painel lateral (informações da taverna, log de eventos).
    /// </summary>
    public class SidePanelController : MonoBehaviour
    {
        private UIDocument _document;
        private IEventBus _eventBus;
        private OrderSystem _orders;
        private ReputationSystem _reputationSystem;

        private VisualElement _sidePanel;
        private Button _panelToggleButton;
        private Button _panelPinButton;
        private ScrollView _sidePanelScroll;

        private Label _reputationScoreLabel;
        private Label _reputationTrendLabel;
        private Label _ordersQueueLabel;
        private Label _ordersAverageLabel;
        private Label _tipsLastLabel;
        private Label _tipsAverageLabel;
        private ScrollView _ordersScroll;
        private ScrollView _logScroll;
        private VisualElement _logFilters;

        private bool _panelPinned;
        private bool _sidePanelOpen;
        private readonly List<LogEntry> _logEntries = new List<LogEntry>(64);
        private readonly HashSet<EventCategory> _activeLogFilters = new HashSet<EventCategory>();
        private readonly Queue<float> _recentTips = new Queue<float>();
        private const int MaxTrackedTips = 8;
        private float _tipSum;
        private int _lastReputationValue;

        public event Action PanelToggled;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            SetupUI();
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private void SetupUI()
        {
            if (_document?.rootVisualElement == null) return;
            var root = _document.rootVisualElement;

            _sidePanel          = root.Q<VisualElement>("sidePanel");
            _panelToggleButton  = root.Q<Button>("panelToggleBtn");
            _panelPinButton     = _sidePanel?.Q<Button>("panelPinBtn");

            var content         = _sidePanel?.Q<ScrollView>(className: "panel-content");
            _sidePanelScroll    = content;

            _reputationScoreLabel = _sidePanel?.Q<Label>("reputationScore");
            _reputationTrendLabel = _sidePanel?.Q<Label>("reputationTrend");
            _ordersQueueLabel     = _sidePanel?.Q<Label>("ordersQueue");
            _ordersAverageLabel   = _sidePanel?.Q<Label>("ordersAverage");
            _tipsLastLabel        = _sidePanel?.Q<Label>("tipsLast");
            _tipsAverageLabel     = _sidePanel?.Q<Label>("tipsAverage");
            _ordersScroll         = _sidePanel?.Q<ScrollView>("ordersScroll");
            _logScroll            = _sidePanel?.Q<ScrollView>("logScroll");
            _logFilters           = _sidePanel?.Q<VisualElement>("logFilters");

            InitializeLogFilters();
            SetSidePanelOpen(false);
        }

        public void RebuildUI()
        {
            UnhookEvents();
            SetupUI();
            HookEvents();
        }

        public void BindEventBus(IEventBus eventBus)
        {
            if (_eventBus != null)
            {
                _eventBus.OnEvent -= OnGameEvent;
            }

            _eventBus = eventBus;

            if (_eventBus != null)
            {
                _eventBus.OnEvent += OnGameEvent;
            }
        }

        public void BindOrders(OrderSystem orderSystem)
        {
            if (_orders != null)
            {
                _orders.OrdersChanged -= OnOrdersChanged;
            }

            _orders = orderSystem;

            if (_orders != null)
            {
                _orders.OrdersChanged += OnOrdersChanged;
                OnOrdersChanged(_orders.GetOrders());
            }
        }

        public void BindReputation(ReputationSystem reputationSystem)
        {
            if (_reputationSystem != null)
            {
                _reputationSystem.ReputationChanged -= OnReputationChanged;
            }

            _reputationSystem = reputationSystem;

            if (_reputationSystem != null)
            {
                _reputationSystem.ReputationChanged += OnReputationChanged;
                UpdateReputation(_reputationSystem.Reputation);
            }
        }

        private void HookEvents()
        {
            if (_panelToggleButton != null)
            {
                _panelToggleButton.clicked += ToggleSidePanel;
            }

            if (_panelPinButton != null)
            {
                _panelPinButton.clicked += TogglePanelPin;
            }
        }

        private void UnhookEvents()
        {
            if (_panelToggleButton != null)
            {
                _panelToggleButton.clicked -= ToggleSidePanel;
            }

            if (_panelPinButton != null)
            {
                _panelPinButton.clicked -= TogglePanelPin;
            }

            if (_eventBus != null)
            {
                _eventBus.OnEvent -= OnGameEvent;
            }

            if (_orders != null)
            {
                _orders.OrdersChanged -= OnOrdersChanged;
            }

            if (_reputationSystem != null)
            {
                _reputationSystem.ReputationChanged -= OnReputationChanged;
            }
        }

        public void ToggleSidePanel()
        {
            Debug.Log($"ToggleSidePanel called! Current state: {_sidePanelOpen}");
            SetSidePanelOpen(!_sidePanelOpen);
            PanelToggled?.Invoke();
        }

        private void TogglePanelPin()
        {
            _panelPinned = !_panelPinned;
            _panelPinButton?.EnableInClassList("panel-pin--active", _panelPinned);
            if (_panelPinned)
            {
                SetSidePanelOpen(true);
            }
        }

        private void SetSidePanelOpen(bool open)
        {
            Debug.Log($"SetSidePanelOpen called with open={open}, pinned={_panelPinned}");
            
            if (!open && _panelPinned)
            {
                Debug.Log("Panel is pinned, not closing");
                return;
            }

            _sidePanelOpen = open;
            
            if (_sidePanel != null)
            {
                _sidePanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                _sidePanel.EnableInClassList("open", open);
                
                // Apply CSS classes instead of forcing styles
                if (open)
                {
                    _sidePanel.AddToClassList("side-panel");
                    _sidePanel.style.display = DisplayStyle.Flex;
                }
            }
            
            _panelToggleButton?.EnableInClassList("panel-toggle--active", open);
            _panelPinButton?.EnableInClassList("panel-pin--active", _panelPinned);
            
            Debug.Log($"Panel state set to: {_sidePanelOpen}, _sidePanel is null: {_sidePanel == null}");
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

            AddLogEntry(gameEvent, text);

            if (gameEvent.Source == "TipReceived" && gameEvent.Data is Dictionary<string, object> payload && 
                payload.TryGetValue("tip", out var value) && value != null)
            {
                if (float.TryParse(value.ToString(), System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out var tipValue))
                {
                    RegisterTip(tipValue);
                }
            }
        }

        private void OnOrdersChanged(IReadOnlyList<Order> orders)
        {
            if (_ordersScroll == null)
            {
                return;
            }

            _ordersScroll.contentContainer.Clear();

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
                var empty = new Label("Nenhum pedido ativo.");
                empty.AddToClassList("group-body");
                _ordersScroll.contentContainer.Add(empty);
            }

            if (_ordersQueueLabel != null)
            {
                _ordersQueueLabel.text = queueCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
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

        private void OnReputationChanged(int value)
        {
            UpdateReputation(value);
        }

        private void UpdateReputation(int value)
        {
            if (_reputationScoreLabel != null)
            {
                _reputationScoreLabel.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
                    _reputationTrendLabel.text = delta.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
                _tipsLastLabel.text = tip.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
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
                _tipsAverageLabel.text = average.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void InitializeLogFilters()
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

            RefreshLogFilters();
        }

        private void RefreshLogFilters()
        {
            if (_logFilters == null)
            {
                return;
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

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var entry = new LogEntry(displayText, gameEvent.Severity, ResolveCategory(gameEvent), timestamp);
            _logEntries.Add(entry);
            if (_logEntries.Count > 50)
            {
                _logEntries.RemoveAt(0);
            }

            RefreshEventLog();
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
