using System;
using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;
using TavernSim.UI.Events;
using TavernSim.UI.SystemStubs;
using UIService = TavernSim.UI.IWeatherService;
using UIWeatherSnapshot = TavernSim.UI.WeatherSnapshot;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para a barra inferior do HUD.
    /// Responsável por controles de tempo, indicadores rápidos e botões de gestão.
    /// </summary>
    public class BottomBarController
    {
        private readonly VisualElement _root;
        private readonly GameClockSystem _clock;
        private readonly EconomySystem _economy;
        private readonly UIService _weather;

        // UI Elements
        private Label _clockLabel;
        private Label _goldLabel;
        private Label _weatherLargeLabel;
        private VisualElement _weatherLargeIcon;
        private Label _quickReputationLabel;
        private Label _quickCleanlinessLabel;
        private Label _quickSatisfactionLabel;

        // Buttons
        private Button _pauseButton;
        private Button _play1Button;
        private Button _play2Button;
        private Button _play4Button;
        private Button _buildButton;
        private Button _inventoryButton;
        private Button _financesButton;
        private Button _eventsButton;
        private Button _questsButton;

        public event Action BuildButtonClicked;
        public event Action SidePanelButtonClicked;

        public BottomBarController(VisualElement root, EconomySystem economy, GameClockSystem clock, UIService weather)
        {
            _root = root;
            _economy = economy;
            _clock = clock;
            _weather = weather;
        }

        public void Initialize()
        {
            SetupUI();
            HookEvents();
            UpdateAll();
        }

        private void SetupUI()
        {
            // Buscar elementos da bottom bar
            var bottomBar = _root.Q("bottomBar");
            if (bottomBar == null) return;

            // Clock e Weather
            _clockLabel = bottomBar.Q<Label>("clockLabel");
            _goldLabel = bottomBar.Q<Label>("goldLabel");
            _weatherLargeLabel = bottomBar.Q<Label>("weatherLargeLabel");
            _weatherLargeIcon = bottomBar.Q("weatherLargeIcon");

            // Indicadores rápidos
            _quickReputationLabel = bottomBar.Q<Label>("quickReputationLabel");
            _quickCleanlinessLabel = bottomBar.Q<Label>("quickCleanlinessLabel");
            _quickSatisfactionLabel = bottomBar.Q<Label>("quickSatisfactionLabel");

            // Controles de tempo
            _pauseButton = bottomBar.Q<Button>("pauseBtn");
            _play1Button = bottomBar.Q<Button>("play1Btn");
            _play2Button = bottomBar.Q<Button>("play2Btn");
            _play4Button = bottomBar.Q<Button>("play4Btn");

            // Botões de gestão
            _buildButton = bottomBar.Q<Button>("buildBtn");
            _inventoryButton = bottomBar.Q<Button>("inventoryBtn");
            _financesButton = bottomBar.Q<Button>("financesBtn");
            _eventsButton = bottomBar.Q<Button>("eventsBtn");
            _questsButton = bottomBar.Q<Button>("questsBtn");

            // Hook button events
            _pauseButton?.RegisterCallback<ClickEvent>(_ => OnPauseClicked());
            _play1Button?.RegisterCallback<ClickEvent>(_ => OnPlay1Clicked());
            _play2Button?.RegisterCallback<ClickEvent>(_ => OnPlay2Clicked());
            _play4Button?.RegisterCallback<ClickEvent>(_ => OnPlay4Clicked());
            
            _buildButton?.RegisterCallback<ClickEvent>(_ => BuildButtonClicked?.Invoke());
            _inventoryButton?.RegisterCallback<ClickEvent>(_ => OnInventoryClicked());
            _financesButton?.RegisterCallback<ClickEvent>(_ => OnFinancesClicked());
            _eventsButton?.RegisterCallback<ClickEvent>(_ => OnEventsClicked());
            _questsButton?.RegisterCallback<ClickEvent>(_ => OnQuestsClicked());
        }

        private void HookEvents()
        {
            if (_clock != null)
            {
                _clock.TimeChanged += (snapshot) => OnTimeChanged(snapshot.ToString());
            }

            if (_economy != null)
            {
                _economy.CashChanged += OnCashChanged;
            }

            if (_weather != null)
            {
                // _weather.WeatherChanged += (snapshot) => OnWeatherChanged(snapshot); // TODO: Implementar quando IWeatherService tiver o evento
            }
        }

        private void OnTimeChanged(string timeString)
        {
            UpdateClock(timeString);
        }

        private void OnWeatherChanged(UIWeatherSnapshot snapshot)
        {
            UpdateWeather(snapshot);
        }

        private void OnCashChanged(float newCash)
        {
            UpdateGold(newCash);
        }

        // Time Control Handlers
        private void OnPauseClicked()
        {
            _clock?.Pause();
        }

        private void OnPlay1Clicked()
        {
            _clock?.SetSpeed(1f);
        }

        private void OnPlay2Clicked()
        {
            _clock?.SetSpeed(2f);
        }

        private void OnPlay4Clicked()
        {
            _clock?.SetSpeed(4f);
        }

        // Management Button Handlers
        private void OnInventoryClicked()
        {
            // TODO: Implementar painel de estoque
            Debug.Log("Inventory clicked");
        }

        private void OnFinancesClicked()
        {
            // TODO: Implementar painel de finanças
            Debug.Log("Finances clicked");
        }

        private void OnEventsClicked()
        {
            // TODO: Implementar painel de eventos
            Debug.Log("Events clicked");
        }

        private void OnQuestsClicked()
        {
            // TODO: Implementar painel de objetivos
            Debug.Log("Quests clicked");
        }

        // Update Methods
        public void UpdateClock(string timeString)
        {
            if (_clockLabel != null) _clockLabel.text = timeString;
        }

        public void UpdateWeather(UIWeatherSnapshot snapshot)
        {
            if (_weatherLargeLabel != null) _weatherLargeLabel.text = snapshot.GetDisplayText();
            // TODO: Atualizar ícone do clima
        }

        public void UpdateGold(float amount)
        {
            if (_goldLabel != null) _goldLabel.text = amount.ToString("F0");
        }

        public void UpdateQuickIndicators(float reputation, float cleanliness, float satisfaction)
        {
            if (_quickReputationLabel != null) _quickReputationLabel.text = reputation.ToString("F1");
            if (_quickCleanlinessLabel != null) _quickCleanlinessLabel.text = cleanliness.ToString("F1");
            if (_quickSatisfactionLabel != null) _quickSatisfactionLabel.text = satisfaction.ToString("F1");
        }

        private void UpdateAll()
        {
            if (_clock != null) UpdateClock(_clock.GetTimeString());
            if (_weather != null) UpdateWeather(_weather.GetSnapshot());
            if (_economy != null) UpdateGold(_economy.Cash);
            
            // TODO: Atualizar indicadores rápidos com dados reais
            UpdateQuickIndicators(0f, 0f, 0f);
        }
    }
}
