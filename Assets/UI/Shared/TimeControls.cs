using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Simulation.Systems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TavernSim.UI
{
    /// <summary>
    /// Handles HUD time control buttons and keyboard shortcuts.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TimeControls : MonoBehaviour
    {
        private UIDocument _document;
        private Button _pause;
        private Button _play1;
        private Button _play2;
        private Button _play4;
        private float _baseFixedDelta = 0.1f;
        private float _currentScale = 1f;
        private GameClockSystem _clockSystem;

        private const string ActiveClass = "hud-button--active";

        public void Initialize(GameClockSystem clockSystem)
        {
            _document = GetComponent<UIDocument>();
            _clockSystem = clockSystem;
            Hook();
            ApplyScale(1f);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                ApplyScale(1f);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                ApplyScale(2f);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                ApplyScale(4f);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ApplyScale(1f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ApplyScale(2f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ApplyScale(4f);
            }
#else
            return;
#endif
        }

        private void Hook()
        {
            var root = _document.rootVisualElement;
            _pause = root.Q<Button>("pauseBtn");
            _play1 = root.Q<Button>("play1Btn");
            _play2 = root.Q<Button>("play2Btn");
            _play4 = root.Q<Button>("play4Btn");

            if (_pause != null) _pause.clicked += () => ApplyScale(0f);
            if (_play1 != null) _play1.clicked += () => ApplyScale(1f);
            if (_play2 != null) _play2.clicked += () => ApplyScale(2f);
            if (_play4 != null) _play4.clicked += () => ApplyScale(4f);

            UpdateButtonStates();
        }

        private void ApplyScale(float scale)
        {
            _currentScale = scale;
            Time.timeScale = scale;
            Time.fixedDeltaTime = Mathf.Max(0.001f, _baseFixedDelta * Mathf.Max(scale, 0.01f));
            _clockSystem?.SetScale(scale <= 0f ? 0f : scale);
            UpdateButtonStates();
        }

        private void TogglePause()
        {
            if (_currentScale == 0f)
            {
                ApplyScale(1f);
            }
            else
            {
                ApplyScale(0f);
            }
        }

        private void UpdateButtonStates()
        {
            var paused = Mathf.Approximately(_currentScale, 0f);
            _pause?.EnableInClassList(ActiveClass, paused);
            _play1?.EnableInClassList(ActiveClass, Mathf.Approximately(_currentScale, 1f));
            _play2?.EnableInClassList(ActiveClass, Mathf.Approximately(_currentScale, 2f));
            _play4?.EnableInClassList(ActiveClass, Mathf.Approximately(_currentScale, 4f));
        }
    }
}
