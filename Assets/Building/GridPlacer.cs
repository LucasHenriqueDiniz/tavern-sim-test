using UnityEngine;
using TavernSim.Core;
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
        public enum PlaceableKind
        {
            None = 0,
            Table = 1,
            Chair = 2,
            Decoration = 3
        }

        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float tableCost = 125f;
        [SerializeField] private float chairCost = 45f;
        [SerializeField] private float decorationCost = 30f;

        private EconomySystem _economySystem;
        private SelectionService _selectionService;
        private PlaceableKind _activeKind = PlaceableKind.None;

        public event System.Action<PlaceableKind> PlacementModeChanged;

        public PlaceableKind ActiveKind => _activeKind;

        public bool HasActivePlacement => _activeKind != PlaceableKind.None;

        public void Configure(EconomySystem economySystem, SelectionService selectionService)
        {
            _economySystem = economySystem;
            _selectionService = selectionService;
        }

        private void Update()
        {
            if (_selectionService == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null)
            {
                return;
            }

            if (HasActivePlacement && (mouse.rightButton.wasPressedThisFrame || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)))
            {
                CancelPlacement();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = mouse.position.ReadValue();
            var screenPoint = new Vector3(pointerPosition.x, pointerPosition.y, 0f);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (HasActivePlacement && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelPlacement();
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            var screenPoint = Input.mousePosition;
#else
            return;
#endif

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out var hit, 100f))
            {
                if (!HasActivePlacement)
                {
                    _selectionService.ClearSelection();
                }

                return;
            }

            if (HasActivePlacement)
            {
                TryPlaceAt(hit.point);
            }
            else
            {
                SelectHit(hit.collider);
            }
        }

        public void BeginPlacement(PlaceableKind kind)
        {
            if (kind == PlaceableKind.None)
            {
                CancelPlacement();
                return;
            }

            _selectionService?.ClearSelection();
            _activeKind = kind;
            PlacementModeChanged?.Invoke(_activeKind);
        }

        public void CancelPlacement()
        {
            if (_activeKind == PlaceableKind.None)
            {
                return;
            }

            _activeKind = PlaceableKind.None;
            PlacementModeChanged?.Invoke(_activeKind);
        }

        public float GetPlacementCost(PlaceableKind kind)
        {
            return kind switch
            {
                PlaceableKind.Table => tableCost,
                PlaceableKind.Chair => chairCost,
                PlaceableKind.Decoration => decorationCost,
                _ => 0f
            };
        }

        private void TryPlaceAt(Vector3 point)
        {
            if (_economySystem == null)
            {
                return;
            }

            var cost = GetPlacementCost(_activeKind);
            if (cost > 0f && !_economySystem.TrySpend(cost))
            {
                Debug.LogWarning($"Not enough cash to place {_activeKind} (cost {cost:0}).");
                return;
            }

            var position = AlignToGrid(point);
            switch (_activeKind)
            {
                case PlaceableKind.Table:
                    CreateTable(position);
                    break;
                case PlaceableKind.Chair:
                    CreateChair(position);
                    break;
                case PlaceableKind.Decoration:
                    CreateDecoration(position);
                    break;
            }
        }

        private void SelectHit(Collider collider)
        {
            if (collider == null)
            {
                _selectionService.ClearSelection();
                return;
            }

            var selectable = collider.GetComponentInParent<ISelectable>();
            if (selectable != null)
            {
                _selectionService.Select(selectable);
            }
            else
            {
                _selectionService.ClearSelection();
            }
        }

        private Vector3 AlignToGrid(Vector3 point)
        {
            var position = point;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            position.y = 0f;
            return position;
        }

        private static void CreateTable(Vector3 position)
        {
            var tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "Table";
            tableTop.transform.position = position + new Vector3(0f, 0.55f, 0f);
            tableTop.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
            NavMeshSetup.MarkObstacle(tableTop);

            CreateTableLeg(tableTop.transform, new Vector3(0.55f, -0.55f, 0.55f));
            CreateTableLeg(tableTop.transform, new Vector3(-0.55f, -0.55f, 0.55f));
            CreateTableLeg(tableTop.transform, new Vector3(0.55f, -0.55f, -0.55f));
            CreateTableLeg(tableTop.transform, new Vector3(-0.55f, -0.55f, -0.55f));
        }

        private static void CreateTableLeg(Transform parent, Vector3 localPosition)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "TableLeg";
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = localPosition;
            leg.transform.localScale = new Vector3(0.15f, 1.1f, 0.15f);
        }

        private static void CreateChair(Vector3 position)
        {
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Chair";
            seat.transform.position = position + new Vector3(0f, 0.25f, 0f);
            seat.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            NavMeshSetup.MarkObstacle(seat);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "ChairBack";
            back.transform.SetParent(seat.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
            back.transform.localScale = new Vector3(0.6f, 1.2f, 0.2f);
        }

        private static void CreateDecoration(Vector3 position)
        {
            var planter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            planter.name = "Plant";
            planter.transform.position = position + new Vector3(0f, 0.3f, 0f);
            planter.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            NavMeshSetup.MarkObstacle(planter, false);

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(planter.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            foliage.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        }
    }
}
