using UnityEngine;
using TavernSim.Simulation.Systems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

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

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = mouse.position.ReadValue();
            var screenPoint = new Vector3(pointerPosition.x, pointerPosition.y, 0f);
#else
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            var screenPoint = Input.mousePosition;
#endif

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(screenPoint);
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
