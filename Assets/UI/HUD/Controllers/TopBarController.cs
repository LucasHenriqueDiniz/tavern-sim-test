using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;
using TavernSim.UI.Events;
using UIService = TavernSim.UI.IWeatherService;
using UIWeatherSnapshot = TavernSim.UI.WeatherSnapshot;
using TavernSim.UI.SystemStubs;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para a barra superior do HUD.
    /// Gerencia ícones SVG e sub-menus de construção/decoração.
    /// </summary>
    public class TopBarController
    {
        private readonly VisualElement _root;
        private readonly EconomySystem _economy;
        private readonly ReputationSystem _reputation;
        private readonly GameClockSystem _clock;
        private readonly UIService _weather;

        // UI Elements
        private Label _cashLabel;
        private Label _reputationLabel;
        private Label _customerLabel;
        private Label _timeLabel;
        private Label _weatherLabel;
        private VisualElement _weatherIcon;
        private Button _staffButton;

        // Info buttons (canto superior esquerdo)
        private Button _messagesButton;
        private Button _statsButton;
        private Button _infoButton;

        // Build/Decoration buttons (canto superior direito)
        private Button _buildMainButton;
        private Button _decorMainButton;
        private VisualElement _buildSubMenu;
        private VisualElement _decorSubMenu;
        private Button _buildStructuresButton;
        private Button _buildStairsButton;
        private Button _buildDoorsButton;
        private Button _buildDemolishButton;
        private Button _decorArtButton;
        private Button _decorBasicButton;
        private Button _decorThemeButton;
        private Button _decorNatureButton;

        // State
        private bool _buildSubMenuVisible = false;
        private bool _decorSubMenuVisible = false;

        public event System.Action StaffButtonClicked;
        public event System.Action<string> BuildCategorySelected;
        public event System.Action<string> DecorCategorySelected;

        public TopBarController(VisualElement root, EconomySystem economy, ReputationSystem reputation, 
            GameClockSystem clock, UIService weather)
        {
            _root = root;
            _economy = economy;
            _reputation = reputation;
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
            // Buscar elementos da top bar: aceitar tanto root inteiro quanto um container 'topBar'
            var topBar = _root != null ? (_root.Q("topBar") ?? _root) : null;
            if (topBar == null) return;

            // Métricas centrais
            _cashLabel = topBar.Q<Label>("cashLabel");
            _reputationLabel = topBar.Q<Label>("reputationLabel");
            _customerLabel = topBar.Q<Label>("customerLabel");
            _timeLabel = topBar.Q<Label>("timeLabel");
            _weatherLabel = topBar.Q<Label>("weatherLabel");
            _weatherIcon = topBar.Q("weatherIcon");

            // Info buttons (esquerda)
            _messagesButton = topBar.Q<Button>("messagesBtn");
            _statsButton = topBar.Q<Button>("statsBtn");
            _infoButton = topBar.Q<Button>("infoBtn");

            // Build/Decoration buttons (direita)
            _buildMainButton = topBar.Q<Button>("buildMainBtn");
            _decorMainButton = topBar.Q<Button>("decorMainBtn");
            _buildSubMenu = topBar.Q("buildSubMenu");
            _decorSubMenu = topBar.Q("decorSubMenu");

            // Sub-menu buttons
            _buildStructuresButton = topBar.Q<Button>("buildStructuresBtn");
            _buildStairsButton = topBar.Q<Button>("buildStairsBtn");
            _buildDoorsButton = topBar.Q<Button>("buildDoorsBtn");
            _buildDemolishButton = topBar.Q<Button>("buildDemolishBtn");
            _decorArtButton = topBar.Q<Button>("decorArtBtn");
            _decorBasicButton = topBar.Q<Button>("decorBasicBtn");
            _decorThemeButton = topBar.Q<Button>("decorThemeBtn");
            _decorNatureButton = topBar.Q<Button>("decorNatureBtn");

            // Hook button events
            _messagesButton?.RegisterCallback<ClickEvent>(_ => OnMessagesClicked());
            _statsButton?.RegisterCallback<ClickEvent>(_ => OnStatsClicked());
            _infoButton?.RegisterCallback<ClickEvent>(_ => OnInfoClicked());

            _buildMainButton?.RegisterCallback<ClickEvent>(_ => ToggleBuildSubMenu());
            _decorMainButton?.RegisterCallback<ClickEvent>(_ => ToggleDecorSubMenu());

            // Build sub-menu events
            _buildStructuresButton?.RegisterCallback<ClickEvent>(_ => OnBuildCategorySelected("structures"));
            _buildStairsButton?.RegisterCallback<ClickEvent>(_ => OnBuildCategorySelected("stairs"));
            _buildDoorsButton?.RegisterCallback<ClickEvent>(_ => OnBuildCategorySelected("doors"));
            _buildDemolishButton?.RegisterCallback<ClickEvent>(_ => OnBuildCategorySelected("demolish"));

            // Decor sub-menu events
            _decorArtButton?.RegisterCallback<ClickEvent>(_ => OnDecorCategorySelected("art"));
            _decorBasicButton?.RegisterCallback<ClickEvent>(_ => OnDecorCategorySelected("basic"));
            _decorThemeButton?.RegisterCallback<ClickEvent>(_ => OnDecorCategorySelected("theme"));
            _decorNatureButton?.RegisterCallback<ClickEvent>(_ => OnDecorCategorySelected("nature"));
        }

        private void HookEvents()
        {
            if (_economy != null)
            {
                _economy.CashChanged += (newAmount) => OnCashChanged(newAmount);
            }

            if (_reputation != null)
            {
                _reputation.ReputationChanged += (newRep) => OnReputationChanged(newRep);
            }

            if (_clock != null)
            {
                _clock.TimeChanged += (snapshot) => OnTimeChanged(snapshot);
            }

            if (_weather != null)
            {
                // _weather.WeatherChanged += OnWeatherChanged; // TODO: Implementar quando IWeatherService tiver o evento
            }
        }

        private void OnCashChanged(float newAmount)
        {
            UpdateCash(newAmount);
        }

        private void OnReputationChanged(int newReputation)
        {
            UpdateReputation(newReputation);
        }

        private void OnTimeChanged(GameClockSystem.GameClockSnapshot snapshot)
        {
            UpdateTime(snapshot.ToString());
        }

        private void OnWeatherChanged(UIWeatherSnapshot snapshot)
        {
            UpdateWeather(snapshot);
        }

        // Button Handlers
        private void OnMessagesClicked()
        {
            Debug.Log("Messages clicked");
            // TODO: Implementar painel de mensagens
        }

        private void OnStatsClicked()
        {
            Debug.Log("Stats clicked");
            // TODO: Implementar painel de estatísticas
        }

        private void OnInfoClicked()
        {
            Debug.Log("Info clicked");
            // TODO: Implementar painel de informações
        }

        private void ToggleBuildSubMenu()
        {
            _buildSubMenuVisible = !_buildSubMenuVisible;
            _decorSubMenuVisible = false; // Fechar o outro sub-menu

            if (_buildSubMenu != null)
            {
                if (_buildSubMenuVisible)
                    _buildSubMenu.AddToClassList("sub-menu--visible");
                else
                    _buildSubMenu.RemoveFromClassList("sub-menu--visible");
            }

            if (_decorSubMenu != null)
            {
                _decorSubMenu.RemoveFromClassList("sub-menu--visible");
            }
        }

        private void ToggleDecorSubMenu()
        {
            _decorSubMenuVisible = !_decorSubMenuVisible;
            _buildSubMenuVisible = false; // Fechar o outro sub-menu

            if (_decorSubMenu != null)
            {
                if (_decorSubMenuVisible)
                    _decorSubMenu.AddToClassList("sub-menu--visible");
                else
                    _decorSubMenu.RemoveFromClassList("sub-menu--visible");
            }

            if (_buildSubMenu != null)
            {
                _buildSubMenu.RemoveFromClassList("sub-menu--visible");
            }
        }

        private void OnBuildCategorySelected(string category)
        {
            BuildCategorySelected?.Invoke(category);
            ToggleBuildSubMenu(); // Fechar sub-menu após seleção
        }

        private void OnDecorCategorySelected(string category)
        {
            DecorCategorySelected?.Invoke(category);
            ToggleDecorSubMenu(); // Fechar sub-menu após seleção
        }

        // Update Methods
        public void UpdateCash(float amount)
        {
            if (_cashLabel != null) _cashLabel.text = amount.ToString("F0");
        }

        public void UpdateReputation(float reputation)
        {
            if (_reputationLabel != null) _reputationLabel.text = reputation.ToString("F1");
        }

        public void UpdateCustomerCount()
        {
            if (_customerLabel != null) _customerLabel.text = "0";
        }

        public void UpdateTime(string timeString)
        {
            if (_timeLabel != null) _timeLabel.text = timeString;
        }

        public void UpdateWeather(UIWeatherSnapshot snapshot)
        {
            if (_weatherLabel != null) _weatherLabel.text = snapshot.GetDisplayText();
        }

        private void UpdateAll()
        {
            if (_economy != null) UpdateCash(_economy.Cash);
            if (_reputation != null) UpdateReputation(_reputation.Reputation);
            if (_clock != null) UpdateTime(_clock.GetTimeString());
            if (_weather != null) UpdateWeather(_weather.GetSnapshot());
            
            UpdateCustomerCount();
        }
    }
}
