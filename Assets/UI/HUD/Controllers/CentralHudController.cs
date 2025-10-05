using System;
using System.Collections.Generic;
using System.Reflection;
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
        private float _lastKnownScale = 1f;
        private PropertyInfo _currentScaleProperty;

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

            _pauseButton?.RegisterCallback<ClickEvent>(evt => OnTimeButtonClicked(0f, evt.currentTarget as VisualElement));
            _step1Button?.RegisterCallback<ClickEvent>(evt => OnTimeButtonClicked(1f, evt.currentTarget as VisualElement));
            _step2Button?.RegisterCallback<ClickEvent>(evt => OnTimeButtonClicked(2f, evt.currentTarget as VisualElement));
            _step3Button?.RegisterCallback<ClickEvent>(evt => OnTimeButtonClicked(3f, evt.currentTarget as VisualElement));

            if (_clock != null)
            {
                _clock.TimeChanged -= OnTimeChanged;
                _clock.TimeChanged += OnTimeChanged;
                UpdateActiveButton(GetClockScale());
                UpdateTimeLabel(FormatTime(_clock.Snapshot));
            }
            else
            {
                UpdateActiveButton(1f);
                UpdateTimeLabel("Dia 1 – 08:00");
            }
        }

        public void Dispose()
        {
            if (_clock != null)
            {
                _clock.TimeChanged -= OnTimeChanged;
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

        private void OnTimeButtonClicked(float scale, VisualElement source)
        {
            var buttonName = source?.name;
            if (!string.IsNullOrEmpty(buttonName))
            {
                Debug.Log($"[CentralHudController] Botão '{buttonName}' clicado com escala {scale}.");
            }
            else
            {
                Debug.Log($"[CentralHudController] Controle de tempo clicado com escala {scale}.");
            }

            _clock?.SetScale(scale);
            _lastKnownScale = scale;
            UpdateActiveButton(scale);
        }

        private void OnTimeChanged(GameClockSystem.GameClockSnapshot snapshot)
        {
            UpdateTimeLabel(FormatTime(snapshot));
            UpdateActiveButton(GetClockScale());
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
            _lastKnownScale = scale;

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

        private float GetClockScale()
        {
            if (_clock == null)
            {
                return _lastKnownScale;
            }

            if (_currentScaleProperty == null)
            {
                _currentScaleProperty = typeof(GameClockSystem).GetProperty("CurrentScale", BindingFlags.Public | BindingFlags.Instance);
            }

            if (_currentScaleProperty != null)
            {
                try
                {
                    var value = _currentScaleProperty.GetValue(_clock);
                    if (value is float scale)
                    {
                        _lastKnownScale = scale;
                        return scale;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Debug.LogException(ex);
                }
            }

            return _lastKnownScale;
        }

        private static string FormatTime(GameClockSystem.GameClockSnapshot snapshot)
        {
            return $"Dia {snapshot.Day} – {snapshot.Hour:00}:{snapshot.Minute:00}";
        }
    }
}
