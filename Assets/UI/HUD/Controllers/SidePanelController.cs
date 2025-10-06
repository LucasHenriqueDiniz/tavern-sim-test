using System;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para o painel lateral (side panel).
    /// Mostra resumo da taverna, estatísticas e log de eventos.
    /// </summary>
    public class SidePanelController
    {
        private readonly VisualElement _root;
        private readonly EconomySystem _economy;
        private readonly OrderSystem _orders;
        private readonly ReputationSystem _reputation;

        // UI Elements
        private VisualElement _sidePanel;
        private Button _closeButton;
        private Button _toggleButton;

        // Stats Elements
        private Label _reputationScore;
        private Label _reputationTrend;
        private Label _ordersQueue;
        private Label _ordersAverage;
        private ScrollView _ordersScroll;
        private Label _tipsLast;
        private Label _tipsAverage;
        private ScrollView _logScroll;

        private bool _isVisible = false;

        public event Action PanelClosed;

        public SidePanelController(VisualElement root, EconomySystem economy, OrderSystem orders, ReputationSystem reputation)
        {
            _root = root;
            _economy = economy;
            _orders = orders;
            _reputation = reputation;
        }

        public void Initialize()
        {
            if (_root == null)
            {
                Debug.LogWarning("SidePanelController: raiz do painel lateral não encontrada. Inicialização ignorada.");
                return;
            }

            SetupUI();
            HookEvents();
            Hide(); // Começar oculto
        }

        private void SetupUI()
        {
            if (_root == null)
            {
                return;
            }

            _sidePanel = _root.Q("sidePanel");
            _closeButton = _root.Q<Button>("panelCloseBtn");
            _toggleButton = _root.Q<Button>("sidePanelToggleBtn");

            // Stats elements
            _reputationScore = _root.Q<Label>("reputationScore");
            _reputationTrend = _root.Q<Label>("reputationTrend");
            _ordersQueue = _root.Q<Label>("ordersQueue");
            _ordersAverage = _root.Q<Label>("ordersAverage");
            _ordersScroll = _root.Q<ScrollView>("ordersScroll");
            _tipsLast = _root.Q<Label>("tipsLast");
            _tipsAverage = _root.Q<Label>("tipsAverage");
            _logScroll = _root.Q<ScrollView>("logScroll");

            // Hook button events
            _closeButton?.RegisterCallback<ClickEvent>(_ => Hide());
            _toggleButton?.RegisterCallback<ClickEvent>(_ => Toggle());
        }

        private void HookEvents()
        {
            if (_economy != null)
            {
                _economy.CashChanged += (newAmount) => OnCashChanged(newAmount);
            }

            if (_orders != null)
            {
                // _orders.OrderPlaced += OnOrderPlaced; // TODO: Implementar quando OrderSystem tiver os eventos
                // _orders.OrderReady += OnOrderReady; // TODO: Implementar quando OrderSystem tiver os eventos
                // _orders.OrderCompleted += OnOrderCompleted; // TODO: Implementar quando OrderSystem tiver os eventos
            }

            if (_reputation != null)
            {
                _reputation.ReputationChanged += (newRep) => OnReputationChanged(newRep);
            }
        }

        private void OnCashChanged(float newAmount)
        {
            UpdateStats();
        }

        private void OnOrderPlaced(Order order)
        {
            UpdateOrders();
        }

        private void OnOrderReady(Order order)
        {
            UpdateOrders();
        }

        private void OnOrderCompleted(Order order)
        {
            UpdateOrders();
        }

        private void OnReputationChanged(int newReputation)
        {
            UpdateStats();
        }

        // Public API
        public void Show()
        {
            if (_sidePanel != null)
            {
                _sidePanel.RemoveFromClassList("side-panel--hidden");
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_sidePanel != null)
            {
                _sidePanel.AddToClassList("side-panel--hidden");
                _isVisible = false;
                PanelClosed?.Invoke();
            }
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public bool IsVisible => _isVisible;

        // Update Methods
        public void UpdateStats()
        {
            if (_reputation != null)
            {
                _reputationScore.text = "0.0";
                _reputationTrend.text = "+0.0"; // TODO: Calcular tendência
            }
        }

        public void UpdateOrders()
        {
            if (_orders != null)
            {
                var allOrders = _orders.GetOrders();
                _ordersQueue.text = "0";
                _ordersAverage.text = "0s"; // TODO: Calcular tempo médio
                
                // TODO: Atualizar lista de pedidos no scroll
            }
        }

        public void UpdateTips(float lastTip, float averageTip)
        {
            _tipsLast.text = lastTip.ToString("F2");
            _tipsAverage.text = averageTip.ToString("F2");
        }

        public void AddLogEntry(string message, LogType type = LogType.Info)
        {
            if (_logScroll == null) return;

            var logEntry = new Label(message);
            logEntry.AddToClassList($"log-entry--{type.ToString().ToLower()}");
            _logScroll.Add(logEntry);

            // Manter apenas as últimas 50 entradas
            while (_logScroll.childCount > 50)
            {
                _logScroll.RemoveAt(0);
            }

            // Scroll para o final
            _logScroll.scrollOffset = new Vector2(0, _logScroll.contentContainer.layout.height);
        }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error,
        Success
    }
}
