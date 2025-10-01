using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;

namespace TavernSim.UI.Legacy
{
    /// <summary>
    /// Controller para a barra superior do HUD (recursos, clima, controles de tempo).
    /// </summary>
    [ExecuteAlways]
    public class TopBarController : MonoBehaviour
    {
        private UIDocument _document;
        private IWeatherService _weatherService;
        private EconomySystem _economy;
        private GameClockSystem _clockSystem;

        private Label _cashLabel;
        private Label _reputationLabel;
        private Label _customerLabel;
        private Label _weatherLabel;
        private Label _timeLabel;
        private VisualElement _weatherIcon;
        private Button _staffButton;
        private Label _clockLargeLabel;
        private Label _weatherLargeLabel;
        private VisualElement _weatherLargeIcon;
        private Label _quickReputationLabel;
        private Label _quickCleanlinessLabel;
        private Label _quickSatisfactionLabel;

        public event System.Action StaffButtonClicked;

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
            if (_document?.rootVisualElement == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            _cashLabel = root.Q<Label>("cashLabel");
            _reputationLabel = root.Q<Label>("reputationLabel");
            _customerLabel = root.Q<Label>("customerLabel");
            _weatherLabel = root.Q<Label>("weatherLabel");
            _weatherIcon = root.Q<VisualElement>("topWeatherIcon") ?? root.Q<VisualElement>("weatherIcon");
            _timeLabel = root.Q<Label>("timeLabel");
            _staffButton = root.Q<Button>("staffBtn");
            _clockLargeLabel = root.Q<Label>("clockLargeLabel");
            _weatherLargeLabel = root.Q<Label>("weatherLargeLabel");
            _weatherLargeIcon = root.Q<VisualElement>("weatherLargeIcon");
            _quickReputationLabel = root.Q<Label>("quickReputationLabel");
            _quickCleanlinessLabel = root.Q<Label>("quickCleanlinessLabel");
            _quickSatisfactionLabel = root.Q<Label>("quickSatisfactionLabel");

            if (_staffButton != null)
            {
                _staffButton.clicked -= OnStaffButtonClicked;
                _staffButton.clicked += OnStaffButtonClicked;
            }
        }

        public void BindEconomy(EconomySystem economy)
        {
            if (_economy != null)
            {
                _economy.CashChanged -= OnCashChanged;
            }

            _economy = economy;

            if (_economy != null)
            {
                _economy.CashChanged += OnCashChanged;
                OnCashChanged(_economy.Cash);
            }
        }

        public void BindWeather(IWeatherService weatherService)
        {
            _weatherService = weatherService;
            UpdateWeather();
        }

        public void BindClock(GameClockSystem clockSystem)
        {
            if (_clockSystem != null)
            {
                _clockSystem.TimeChanged -= OnTimeChanged;
            }

            _clockSystem = clockSystem;

            if (_clockSystem != null)
            {
                _clockSystem.TimeChanged += OnTimeChanged;
                UpdateTime(_clockSystem.Snapshot);
            }
        }

        public void SetCustomers(int count)
        {
            if (_customerLabel != null)
            {
                _customerLabel.text = count.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public void SetSatisfaction(float satisfaction)
        {
            if (_quickSatisfactionLabel != null)
            {
                _quickSatisfactionLabel.text = satisfaction.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public void SetReputation(int reputation)
        {
            if (_reputationLabel != null)
            {
                _reputationLabel.text = reputation.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (_quickReputationLabel != null)
            {
                _quickReputationLabel.text = reputation.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void HookEvents()
        {
            // Eventos já configurados no SetupUI
        }

        private void UnhookEvents()
        {
            if (_economy != null)
            {
                _economy.CashChanged -= OnCashChanged;
            }

            if (_clockSystem != null)
            {
                _clockSystem.TimeChanged -= OnTimeChanged;
            }

            if (_staffButton != null)
            {
                _staffButton.clicked -= OnStaffButtonClicked;
            }
        }

        private void OnCashChanged(float cash)
        {
            if (_cashLabel != null)
            {
                _cashLabel.text = HUDStrings.FormatCurrency(cash);
            }
        }

        private void OnTimeChanged(GameClockSystem.GameClockSnapshot snapshot)
        {
            UpdateTime(snapshot);
        }

        private void UpdateTime(GameClockSystem.GameClockSnapshot snapshot)
        {
            if (_timeLabel == null)
            {
                return;
            }

            var day = Mathf.Max(1, snapshot.Day);
            var hour = Mathf.Clamp(snapshot.Hour, 0, 23);
            var minute = Mathf.Clamp(snapshot.Minute, 0, 59);
            _timeLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Dia {0} – {1:00}:{2:00}", day, hour, minute);

            if (_clockLargeLabel != null)
            {
                _clockLargeLabel.text = _timeLabel.text;
            }
        }

        private void UpdateWeather()
        {
            if (_weatherLabel == null || _weatherService == null)
            {
                return;
            }

            var weather = _weatherService.GetSnapshot();
            _weatherLabel.text = weather.GetDisplayText();
            // Ícone de clima via PNG estático no UXML/USS; sem aplicação dinâmica

            if (_weatherLargeLabel != null)
            {
                _weatherLargeLabel.text = weather.GetDisplayText();
            }

            // Ícone grande de clima via PNG estático no UXML/USS; sem aplicação dinâmica
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // Atualizar clima periodicamente
            if (_weatherService != null && Time.time % 5f < Time.deltaTime)
            {
                UpdateWeather();
            }
        }

        private void OnStaffButtonClicked()
        {
            StaffButtonClicked?.Invoke();
        }
    }
}

