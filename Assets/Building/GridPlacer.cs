using System;
using UnityEngine;
using UnityEngine.Rendering;
using TavernSim.Core;
using TavernSim.Simulation.Models;
using TavernSim.Simulation.Systems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using Object = UnityEngine.Object;

namespace TavernSim.Building
{
    /// <summary>
    /// Grid based placement tool used for development of the MVP.
    /// Adds build-mode helpers such as grid visualization and placement previews.
    /// </summary>
    public sealed class GridPlacer : MonoBehaviour
    {
        public enum PlaceableKind
        {
            None = 0,
            SmallTable = 1,
            LargeTable = 2,
            Decoration = 3,
            KitchenStation = 4,
            BarCounter = 5,
            PickupPoint = 6
        }

        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float smallTableCost = 125f;
        [SerializeField] private float largeTableCost = 220f;
        [SerializeField] private float decorationCost = 30f;
        [SerializeField] private float kitchenStationCost = 420f;
        [SerializeField] private float barCounterCost = 360f;
        [SerializeField] private float pickupPointCost = 0f;
        [Header("Build Mode")]
        [SerializeField] private BuildGridVisualizer gridVisualizer;
        [SerializeField] private LayerMask placementMask = ~0;
        [SerializeField] private float previewHeight = 0.05f;
        [SerializeField] private Color previewValidColor = new Color(0.25f, 0.8f, 0.25f, 0.35f);
        [SerializeField] private Color previewInvalidColor = new Color(0.85f, 0.3f, 0.3f, 0.35f);

        private EconomySystem _economySystem;
        private SelectionService _selectionService;
        private TableRegistry _tableRegistry;
        private CleaningSystem _cleaningSystem;
        private PlaceableKind _activeKind = PlaceableKind.None;
        private bool _buildModeActive;
        private GameObject _previewObject;
        private Renderer[] _previewRenderers = Array.Empty<Renderer>();
        private Material _previewMaterial;
        private PlaceableKind _previewKind = PlaceableKind.None;
        private Vector3 _currentPreviewPosition;
        private bool _hasValidPreview;
        private bool _canAffordPreview = true;

        public event Action<PlaceableKind> PlacementModeChanged;

        public PlaceableKind ActiveKind => _activeKind;

        public bool HasActivePlacement => _activeKind != PlaceableKind.None;

        public bool BuildModeActive => _buildModeActive;

        public void Configure(
            EconomySystem economySystem,
            SelectionService selectionService,
            TableRegistry tableRegistry,
            CleaningSystem cleaningSystem)
        {
            _economySystem = economySystem;
            _selectionService = selectionService;
            _tableRegistry = tableRegistry;
            _cleaningSystem = cleaningSystem;

            ApplyGridSizeToVisualizer();
            UpdateGridVisibility();
        }

        private void Awake()
        {
            ApplyGridSizeToVisualizer();
            UpdateGridVisibility();
        }

        private void OnValidate()
        {
            gridSize = Mathf.Max(0.1f, gridSize);
            ApplyGridSizeToVisualizer();
        }

        private void OnDestroy()
        {
            DestroyPreview();

            if (_previewMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_previewMaterial);
                }
                else
                {
                    DestroyImmediate(_previewMaterial);
                }

                _previewMaterial = null;
            }
        }

        private void Update()
        {
            if (_selectionService == null)
            {
                return;
            }

            if (!TryGetPointerData(out var screenPoint, out var leftClick, out var rightClick, out var cancelPressed))
            {
                return;
            }

            if (HasActivePlacement)
            {
                if (rightClick || cancelPressed)
                {
                    ExitBuildMode();
                    return;
                }

                UpdatePlacementPreview(screenPoint);

                if (leftClick && _hasValidPreview)
                {
                    if (_canAffordPreview)
                    {
                        TryPlaceAt(_currentPreviewPosition);
                    }
                    else
                    {
                        UpdatePreviewColor(false);
                    }
                }
            }
            else
            {
                if (rightClick || cancelPressed)
                {
                    if (_buildModeActive)
                    {
                        SetBuildMode(false);
                    }

                    return;
                }

                if (leftClick)
                {
                    HandleSelection(screenPoint);
                }
            }
        }

        public void BeginPlacement(PlaceableKind kind)
        {
            if (kind == PlaceableKind.None)
            {
                CancelPlacement();
                return;
            }

            SetBuildMode(true);
            _selectionService?.ClearSelection();
            _activeKind = kind;
            DestroyPreview();
            PlacementModeChanged?.Invoke(_activeKind);
        }

        public void CancelPlacement()
        {
            CancelPlacementInternal();
            PlacementModeChanged?.Invoke(_activeKind);
            UpdateGridVisibility();
        }

        public void SetBuildMode(bool active)
        {
            if (_buildModeActive == active)
            {
                return;
            }

            _buildModeActive = active;

            if (!_buildModeActive)
            {
                CancelPlacementInternal();
                PlacementModeChanged?.Invoke(_activeKind);
            }

            UpdateGridVisibility();
        }

        public void ExitBuildMode()
        {
            SetBuildMode(false);
        }

        public float GetPlacementCost(PlaceableKind kind)
        {
            return kind switch
            {
                PlaceableKind.SmallTable => smallTableCost,
                PlaceableKind.LargeTable => largeTableCost,
                PlaceableKind.Decoration => decorationCost,
                PlaceableKind.KitchenStation => kitchenStationCost,
                PlaceableKind.BarCounter => barCounterCost,
                PlaceableKind.PickupPoint => pickupPointCost,
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
                UpdatePreviewColor(false);
                return;
            }

            var position = AlignToGrid(point);
            switch (_activeKind)
            {
                case PlaceableKind.SmallTable:
                    CreateAndRegisterTable(position, TableBuilderUtility.CreateSmallTable);
                    break;
                case PlaceableKind.LargeTable:
                    CreateAndRegisterTable(position, TableBuilderUtility.CreateLargeTable);
                    break;
                case PlaceableKind.Decoration:
                    CreateDecoration(position);
                    break;
                case PlaceableKind.KitchenStation:
                    CreateKitchenStation(position);
                    break;
                case PlaceableKind.BarCounter:
                    CreateBarCounter(position);
                    break;
                case PlaceableKind.PickupPoint:
                    CreatePickupMarker(position);
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

        private void HandleSelection(Vector3 screenPoint)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out var hit, 200f, placementMask))
            {
                SelectHit(hit.collider);
            }
            else
            {
                _selectionService.ClearSelection();
            }
        }

        private bool TryGetPointerData(out Vector3 screenPoint, out bool leftClick, out bool rightClick, out bool cancelPressed)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null)
            {
                screenPoint = default;
                leftClick = false;
                rightClick = false;
                cancelPressed = false;
                return false;
            }

            var pointerPosition = mouse.position.ReadValue();
            screenPoint = new Vector3(pointerPosition.x, pointerPosition.y, 0f);
            leftClick = mouse.leftButton.wasPressedThisFrame;
            rightClick = mouse.rightButton.wasPressedThisFrame;
            cancelPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            screenPoint = Input.mousePosition;
            leftClick = Input.GetMouseButtonDown(0);
            rightClick = Input.GetMouseButtonDown(1);
            cancelPressed = Input.GetKeyDown(KeyCode.Escape);
            return true;
#else
            screenPoint = default;
            leftClick = false;
            rightClick = false;
            cancelPressed = false;
            return false;
#endif
        }

        private void UpdatePlacementPreview(Vector3 screenPoint)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                HidePreview();
                return;
            }

            var ray = mainCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out var hit, 200f, placementMask))
            {
                _currentPreviewPosition = AlignToGrid(hit.point);
                EnsurePreviewObject();

                if (_previewObject != null)
                {
                    _previewObject.transform.position = _currentPreviewPosition + Vector3.up * previewHeight;
                }

                UpdatePreviewState(true);
            }
            else
            {
                UpdatePreviewState(false);
            }
        }

        private void UpdatePreviewState(bool hasValidPosition)
        {
            _hasValidPreview = hasValidPosition;

            if (_previewObject == null)
            {
                return;
            }

            if (!hasValidPosition)
            {
                _previewObject.SetActive(false);
                return;
            }

            _previewObject.SetActive(true);
            _canAffordPreview = IsPlacementAffordable(_activeKind);
            UpdatePreviewColor(_canAffordPreview);
        }

        private bool IsPlacementAffordable(PlaceableKind kind)
        {
            if (_economySystem == null)
            {
                return true;
            }

            var cost = GetPlacementCost(kind);
            return cost <= 0f || _economySystem.Cash >= cost - 0.001f;
        }

        private void UpdatePreviewColor(bool valid)
        {
            if (_previewMaterial != null)
            {
                _previewMaterial.color = valid ? previewValidColor : previewInvalidColor;
            }
        }

        private void EnsurePreviewObject()
        {
            if (_previewObject != null && _previewKind == _activeKind)
            {
                return;
            }

            DestroyPreview();

            if (_activeKind == PlaceableKind.None)
            {
                return;
            }

            _previewObject = CreatePreviewObject(_activeKind);
            _previewKind = _activeKind;
            _previewRenderers = _previewObject != null
                ? _previewObject.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();

            if (_previewMaterial == null)
            {
                CreatePreviewMaterial();
            }

            if (_previewRenderers != null)
            {
                for (int i = 0; i < _previewRenderers.Length; i++)
                {
                    var renderer = _previewRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.sharedMaterial = _previewMaterial;
                }
            }

            UpdatePreviewColor(true);
        }

        private void CreatePreviewMaterial()
        {
            if (_previewMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return;
            }

            _previewMaterial = new Material(shader)
            {
                color = previewValidColor,
                renderQueue = 3000,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private GameObject CreatePreviewObject(PlaceableKind kind)
        {
            var root = new GameObject($"{kind}Preview")
            {
                hideFlags = HideFlags.DontSave
            };

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "PreviewMesh";
            mesh.transform.SetParent(root.transform, false);
            var scale = GetPreviewScale(kind);
            mesh.transform.localScale = scale;
            mesh.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);

            if (mesh.TryGetComponent(out Collider collider))
            {
                Object.Destroy(collider);
            }

            return root;
        }

        private static Vector3 GetPreviewScale(PlaceableKind kind)
        {
            return kind switch
            {
                PlaceableKind.SmallTable => new Vector3(1.4f, 0.2f, 1.4f),
                PlaceableKind.LargeTable => new Vector3(2.6f, 0.2f, 1.4f),
                PlaceableKind.Decoration => new Vector3(0.6f, 0.2f, 0.6f),
                PlaceableKind.KitchenStation => new Vector3(2.4f, 0.25f, 1.2f),
                PlaceableKind.BarCounter => new Vector3(2.8f, 0.25f, 0.8f),
                PlaceableKind.PickupPoint => new Vector3(0.6f, 0.2f, 0.6f),
                _ => new Vector3(1f, 0.2f, 1f)
            };
        }

        private void HidePreview()
        {
            _hasValidPreview = false;
            if (_previewObject != null)
            {
                _previewObject.SetActive(false);
            }
        }

        private void DestroyPreview()
        {
            if (_previewObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_previewObject);
                }
                else
                {
                    DestroyImmediate(_previewObject);
                }
            }

            _previewObject = null;
            _previewRenderers = Array.Empty<Renderer>();
            _previewKind = PlaceableKind.None;
            _hasValidPreview = false;
            _canAffordPreview = true;
        }

        private void CancelPlacementInternal()
        {
            if (_activeKind == PlaceableKind.None && _previewObject == null)
            {
                return;
            }

            _activeKind = PlaceableKind.None;
            DestroyPreview();
        }

        private void ApplyGridSizeToVisualizer()
        {
            if (gridVisualizer != null)
            {
                gridVisualizer.SetGridSize(gridSize);
            }
        }

        private void UpdateGridVisibility()
        {
            if (gridVisualizer != null)
            {
                gridVisualizer.SetGridSize(gridSize);
                gridVisualizer.SetVisible(_buildModeActive);
            }
        }

        private void CreateAndRegisterTable(Vector3 position, Func<int, Vector3, Table> factory)
        {
            if (_tableRegistry == null)
            {
                Debug.LogWarning("Table placement requires a TableRegistry instance.");
                return;
            }

            var tableId = _tableRegistry.Tables.Count;
            var table = factory(tableId, position);
            _tableRegistry.RegisterTable(table);
            _cleaningSystem?.RegisterTable(table);
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

        private static void CreateKitchenStation(Vector3 position)
        {
            var root = new GameObject("KitchenStation");
            root.transform.position = position;

            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            counter.transform.localScale = new Vector3(2.4f, 1.2f, 1.2f);
            NavMeshSetup.MarkObstacle(counter);

            var cooktop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cooktop.name = "Cooktop";
            cooktop.transform.SetParent(root.transform, false);
            cooktop.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cooktop.transform.localScale = new Vector3(2.4f, 0.2f, 1.2f);
            NavMeshSetup.MarkObstacle(cooktop, false);

            var hood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hood.name = "VentHood";
            hood.transform.SetParent(root.transform, false);
            hood.transform.localPosition = new Vector3(0f, 2f, 0f);
            hood.transform.localScale = new Vector3(2f, 0.2f, 0.8f);
        }

        private static void CreateBarCounter(Vector3 position)
        {
            var root = new GameObject("BarCounter");
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BarBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale = new Vector3(2.8f, 1.2f, 0.8f);
            NavMeshSetup.MarkObstacle(body);

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "BarTop";
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            top.transform.localScale = new Vector3(2.8f, 0.2f, 1.1f);
            NavMeshSetup.MarkObstacle(top, false);

            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "BarShelf";
            shelf.transform.SetParent(root.transform, false);
            shelf.transform.localPosition = new Vector3(0f, 1.8f, -0.35f);
            shelf.transform.localScale = new Vector3(2.6f, 0.15f, 0.3f);
        }

        private static void CreatePickupMarker(Vector3 position)
        {
            var root = new GameObject("PickupPoint");
            root.transform.position = position;

            var baseMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseMarker.name = "MarkerBase";
            baseMarker.transform.SetParent(root.transform, false);
            baseMarker.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            baseMarker.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);

            var icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            icon.name = "MarkerIcon";
            icon.transform.SetParent(root.transform, false);
            icon.transform.localPosition = new Vector3(0f, 1f, 0f);
            icon.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            Object.Destroy(baseMarker.GetComponent<Collider>());
            Object.Destroy(icon.GetComponent<Collider>());
        }
    }

    internal static class TableBuilderUtility
    {
        private const float TableTopHeight = 0.55f;
        private const float TableLegHeight = 1.1f;
        private const float ChairSeatHeight = 0.25f;
        private const float BenchSeatHeight = 0.32f;
        private const float BenchLength = 1.2f;
        private const float BenchSeatSpacing = 0.45f;

        public static Table CreateSmallTable(int tableId, Vector3 position)
        {
            var root = CreateTableRoot($"Table_{tableId}", position);
            var table = new Table(tableId, root.transform);

            CreateTableTop(root.transform, new Vector3(1.4f, 0.1f, 1.4f));
            AddChair(table, root.transform, tableId, 0, new Vector3(0f, 0f, 0.9f));
            AddChair(table, root.transform, tableId, 1, new Vector3(0f, 0f, -0.9f));

            AttachPresenter(root, table);

            return table;
        }

        public static Table CreateLargeTable(int tableId, Vector3 position)
        {
            var root = CreateTableRoot($"LargeTable_{tableId}", position);
            var table = new Table(tableId, root.transform);

            CreateTableTop(root.transform, new Vector3(2.6f, 0.1f, 1.4f));
            AddBench(table, root.transform, tableId, 0, new Vector3(0f, 0f, 1.15f));
            AddBench(table, root.transform, tableId, 2, new Vector3(0f, 0f, -1.15f));

            AttachPresenter(root, table);

            return table;
        }

        private static GameObject CreateTableRoot(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            return root;
        }

        private static void CreateTableTop(Transform parent, Vector3 scale)
        {
            var tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(parent, false);
            tableTop.transform.localPosition = new Vector3(0f, TableTopHeight, 0f);
            tableTop.transform.localScale = scale;
            NavMeshSetup.MarkObstacle(tableTop);

            CreateTableLeg(tableTop.transform, new Vector3(scale.x * 0.5f - 0.15f, -TableTopHeight, scale.z * 0.5f - 0.15f));
            CreateTableLeg(tableTop.transform, new Vector3(-scale.x * 0.5f + 0.15f, -TableTopHeight, scale.z * 0.5f - 0.15f));
            CreateTableLeg(tableTop.transform, new Vector3(scale.x * 0.5f - 0.15f, -TableTopHeight, -scale.z * 0.5f + 0.15f));
            CreateTableLeg(tableTop.transform, new Vector3(-scale.x * 0.5f + 0.15f, -TableTopHeight, -scale.z * 0.5f + 0.15f));
        }

        private static void CreateTableLeg(Transform parent, Vector3 localPosition)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "TableLeg";
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = localPosition;
            leg.transform.localScale = new Vector3(0.15f, TableLegHeight, 0.15f);
        }

        private static void AddChair(Table table, Transform parent, int tableId, int seatIndex, Vector3 localPosition)
        {
            var anchor = new GameObject($"Seat_{seatIndex}");
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            var forward = localPosition.sqrMagnitude > 0.001f ? -localPosition.normalized : Vector3.forward;
            anchor.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);

            CreateChairGeometry(anchor.transform);

            var seat = new Seat(tableId * 10 + seatIndex, anchor.transform, Seat.SeatKind.Single);
            table.AddSeat(seat);
        }

        private static void AddBench(Table table, Transform parent, int tableId, int startSeatIndex, Vector3 localPosition)
        {
            var benchRoot = new GameObject($"Bench_{startSeatIndex}");
            benchRoot.transform.SetParent(parent, false);
            benchRoot.transform.localPosition = localPosition;
            var forward = localPosition.sqrMagnitude > 0.001f ? -localPosition.normalized : Vector3.forward;
            benchRoot.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);

            CreateBenchGeometry(benchRoot.transform);

            for (int i = 0; i < 2; i++)
            {
                var seatAnchor = new GameObject($"Seat_{startSeatIndex + i}");
                seatAnchor.transform.SetParent(benchRoot.transform, false);
                var offset = (i == 0 ? -BenchSeatSpacing : BenchSeatSpacing);
                seatAnchor.transform.localPosition = new Vector3(offset, 0f, 0f);
                seatAnchor.transform.localRotation = Quaternion.identity;

                var seat = new Seat(tableId * 10 + startSeatIndex + i, seatAnchor.transform, Seat.SeatKind.Bench);
                table.AddSeat(seat);
            }
        }

        private static void CreateChairGeometry(Transform parent)
        {
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Chair";
            seat.transform.SetParent(parent, false);
            seat.transform.localPosition = new Vector3(0f, ChairSeatHeight, 0f);
            seat.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            NavMeshSetup.MarkObstacle(seat);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "ChairBack";
            back.transform.SetParent(seat.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
            back.transform.localScale = new Vector3(0.6f, 1.2f, 0.2f);
        }

        private static void CreateBenchGeometry(Transform parent)
        {
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Bench";
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = new Vector3(0f, BenchSeatHeight, 0f);
            bench.transform.localScale = new Vector3(BenchLength, 0.4f, 0.6f);
            NavMeshSetup.MarkObstacle(bench);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "BenchBack";
            back.transform.SetParent(bench.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
            back.transform.localScale = new Vector3(BenchLength, 1.2f, 0.2f);
        }

        private static void AttachPresenter(GameObject root, Table table)
        {
            if (root == null || table == null)
            {
                return;
            }

            var presenter = root.GetComponent<TablePresenter>();
            if (presenter == null)
            {
                presenter = root.AddComponent<TablePresenter>();
            }

            presenter.Initialize(table);
        }
    }
}
