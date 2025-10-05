using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller responsável pelos controles centrais de tempo do HUD.
    /// Gerencia botões de velocidade e sincroniza o relógio exibido.
    /// </summary>
    public class CentralHudController
    {
        private const string ActiveClass = "timeButton-active";

        private readonly VisualElement _root;
        private readonly GameClockSystem _clock;

        private Label _currentTimeLabel;
        private Button _pauseButton;
        private Button _step1Button;
        private Button _step2Button;
        private Button _step3Button;

        private readonly List<Button> _timeButtons = new List<Button>();

        public CentralHudController(VisualElement root, GameClockSystem clock)
        {
            _root = root;
            _clock = clock;
        }

        public void Initialize()
        {
            var central = _root?.Q("centralHUD") ?? _root;
            if (central == null)
            {
                return;
            }

            _currentTimeLabel = central.Q<Label>("currentTimeLabel");
            _pauseButton = central.Q<Button>("pauseBtn");
            _step1Button = central.Q<Button>("step1Btn");
            _step2Button = central.Q<Button>("step2Btn");
            _step3Button = central.Q<Button>("step3Btn");

            CacheButtons();

            _pauseButton?.RegisterCallback<ClickEvent>(_ => OnTimeButtonClicked(0f));
            _step1Button?.RegisterCallback<ClickEvent>(_ => OnTimeButtonClicked(1f));
            _step2Button?.RegisterCallback<ClickEvent>(_ => OnTimeButtonClicked(2f));
            _step3Button?.RegisterCallback<ClickEvent>(_ => OnTimeButtonClicked(3f));

            if (_clock != null)
            {
                _clock.TimeChanged -= OnTimeChanged;
                _clock.TimeChanged += OnTimeChanged;
                UpdateActiveButton(_clock.CurrentScale);
                UpdateTimeLabel(_clock.Snapshot.ToString());
            }
            else
            {
                UpdateActiveButton(1f);
                UpdateTimeLabel("Dia 1 – 08:00");
            }
        }

        private void CacheButtons()
        {
            _timeButtons.Clear();

            if (_pauseButton != null)
            {
                _timeButtons.Add(_pauseButton);
            }

            if (_step1Button != null)
            {
                _timeButtons.Add(_step1Button);
            }

            if (_step2Button != null)
            {
                _timeButtons.Add(_step2Button);
            }

            if (_step3Button != null)
            {
                _timeButtons.Add(_step3Button);
            }
        }

        private void OnTimeButtonClicked(float scale)
        {
            _clock?.SetScale(scale);
            UpdateActiveButton(scale);
        }

        private void OnTimeChanged(GameClockSystem.GameClockSnapshot snapshot)
        {
            UpdateTimeLabel(snapshot.ToString());
            if (_clock != null)
            {
                UpdateActiveButton(_clock.CurrentScale);
            }
        }

        private void UpdateTimeLabel(string text)
        {
            if (_currentTimeLabel == null)
            {
                return;
            }

            _currentTimeLabel.text = text;
        }

        private void UpdateActiveButton(float scale)
        {
            foreach (var button in _timeButtons)
            {
                button?.RemoveFromClassList(ActiveClass);
            }

            Button target = null;

            if (_pauseButton != null && (Mathf.Approximately(scale, 0f) || scale <= 0f))
            {
                target = _pauseButton;
            }
            else if (_step1Button != null && scale > 0f && scale < 1.5f)
            {
                target = _step1Button;
            }
            else if (_step2Button != null && scale >= 1.5f && scale < 2.5f)
            {
                target = _step2Button;
            }
            else if (_step3Button != null)
            {
                target = _step3Button;
            }

            target?.AddToClassList(ActiveClass);
        }
    }
}
