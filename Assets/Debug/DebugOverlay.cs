using System.Text;
using UnityEngine;
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
            if (Input.GetKeyDown(toggleKey))
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
    }
}
