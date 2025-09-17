using UnityEngine;
using TavernSim.Simulation.Systems;

namespace TavernSim.Building
{
    /// <summary>
    /// Minimal grid based placement tool used for development of the MVP.
    /// </summary>
    public sealed class GridPlacer : MonoBehaviour
    {
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float propCost = 25f;

        private EconomySystem _economySystem;

        public void Configure(EconomySystem economySystem)
        {
            _economySystem = economySystem;
        }

        private void Update()
        {
            if (_economySystem == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : default;
                if (Physics.Raycast(ray, out var hit, 100f))
                {
                    var position = hit.point;
                    position.x = Mathf.Round(position.x / gridSize) * gridSize;
                    position.z = Mathf.Round(position.z / gridSize) * gridSize;
                    position.y = 0f;

                    if (_economySystem.TrySpend(propCost))
                    {
                        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.position = position;
                        cube.transform.localScale = new Vector3(1f, 0.5f, 1f);
                    }
                }
            }
        }
    }
}
