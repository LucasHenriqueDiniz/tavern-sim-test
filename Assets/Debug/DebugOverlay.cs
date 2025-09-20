using System;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TavernSim.Simulation.Systems;

namespace TavernSim.Debugging
{
    /// <summary>
    /// Lightweight overlay toggled via key to inspect simulation state.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        private bool _visible;
        private readonly StringBuilder _builder = new StringBuilder(256);
        private AgentSystem _agentSystem;
        private OrderSystem _orderSystem;

        public void Configure(AgentSystem agentSystem, OrderSystem orderSystem)
        {
            _agentSystem = agentSystem;
            _orderSystem = orderSystem;
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible || _agentSystem == null)
            {
                return;
            }

            _builder.Clear();
            _builder.AppendLine("Debug Overlay");
            _builder.AppendLine($"Customers: {_agentSystemCustomerCount()}");
            _builder.AppendLine($"Orders: {_orderSystemOrdersCount()}");

            GUI.Label(new Rect(10, 10, 300, 120), _builder.ToString());
        }

        private int _agentSystemCustomerCount()
        {
            return typeof(AgentSystem).GetField("_customers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_agentSystem) is System.Collections.ICollection collection ? collection.Count : 0;
        }

        private int _orderSystemOrdersCount()
        {
            return _orderSystem?.GetOrders()?.Count ?? 0;
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && TryGetKey(toggleKey, out var key))
            {
                var keyControl = Keyboard.current[key];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
            return Input.GetKeyDown(toggleKey);
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryGetKey(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.Alpha0:
                    key = Key.Digit0;
                    return true;
                case KeyCode.Alpha1:
                    key = Key.Digit1;
                    return true;
                case KeyCode.Alpha2:
                    key = Key.Digit2;
                    return true;
                case KeyCode.Alpha3:
                    key = Key.Digit3;
                    return true;
                case KeyCode.Alpha4:
                    key = Key.Digit4;
                    return true;
                case KeyCode.Alpha5:
                    key = Key.Digit5;
                    return true;
                case KeyCode.Alpha6:
                    key = Key.Digit6;
                    return true;
                case KeyCode.Alpha7:
                    key = Key.Digit7;
                    return true;
                case KeyCode.Alpha8:
                    key = Key.Digit8;
                    return true;
                case KeyCode.Alpha9:
                    key = Key.Digit9;
                    return true;
                case KeyCode.Keypad0:
                    key = Key.Numpad0;
                    return true;
                case KeyCode.Keypad1:
                    key = Key.Numpad1;
                    return true;
                case KeyCode.Keypad2:
                    key = Key.Numpad2;
                    return true;
                case KeyCode.Keypad3:
                    key = Key.Numpad3;
                    return true;
                case KeyCode.Keypad4:
                    key = Key.Numpad4;
                    return true;
                case KeyCode.Keypad5:
                    key = Key.Numpad5;
                    return true;
                case KeyCode.Keypad6:
                    key = Key.Numpad6;
                    return true;
                case KeyCode.Keypad7:
                    key = Key.Numpad7;
                    return true;
                case KeyCode.Keypad8:
                    key = Key.Numpad8;
                    return true;
                case KeyCode.Keypad9:
                    key = Key.Numpad9;
                    return true;
                case KeyCode.KeypadPeriod:
                    key = Key.NumpadPeriod;
                    return true;
                case KeyCode.KeypadDivide:
                    key = Key.NumpadDivide;
                    return true;
                case KeyCode.KeypadMultiply:
                    key = Key.NumpadMultiply;
                    return true;
                case KeyCode.KeypadMinus:
                    key = Key.NumpadMinus;
                    return true;
                case KeyCode.KeypadPlus:
                    key = Key.NumpadPlus;
                    return true;
                case KeyCode.KeypadEnter:
                    key = Key.NumpadEnter;
                    return true;
                case KeyCode.BackQuote:
                    key = Key.Backquote;
                    return true;
            }

            if (Enum.TryParse(keyCode.ToString(), out key))
            {
                return true;
            }

            key = default;
            return false;
        }
#endif
    }
}
