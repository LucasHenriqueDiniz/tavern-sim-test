using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Building;
using TavernSim.Simulation.Systems;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller para a barra de ferramentas inferior (construção, decoração, beleza).
    /// </summary>
    public class ToolbarController : MonoBehaviour
    {
        private UIDocument _document;
        private GridPlacer _gridPlacer;
        private BuildCatalog _buildCatalog;
        private EconomySystem _economy;
        private HUDController _hudController;

        private Button _buildToggleButton;
        private Button _decoToggleButton;
        private Button _beautyToggleButton;
        private VisualElement _buildMenu;
        private VisualElement _toolbarGroup;

        private readonly System.Collections.Generic.List<Button> _buildOptionButtons = new System.Collections.Generic.List<Button>();
        private readonly System.Collections.Generic.Dictionary<Button, BuildCatalog.Entry> _buildOptionLookup = new System.Collections.Generic.Dictionary<Button, BuildCatalog.Entry>();
        private EventCallback<ClickEvent> _buildOptionHandler;

        private bool _buildMenuVisible;
        private bool _beautyOverlayEnabled;
        private BuildCategory _activeBuildCategory = BuildCategory.Build;

        public event System.Action BeautyToggleChanged;

        public void SetHUDController(HUDController hudController)
        {
            _hudController = hudController;
            if (isActiveAndEnabled)
            {
                SetupUI();
                HookEvents();
            }
        }

        public void Initialize(UIDocument document)
        {
            _document = document;
            if (isActiveAndEnabled)
            {
                SetupUI();
                HookEvents();
            }
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _hudController = GetComponent<HUDController>();
        }

        private void OnEnable()
        {
            if (_hudController != null)
            {
                SetupUI();
                HookEvents();
            }
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private void SetupUI()
        {
            if (_document?.rootVisualElement == null) return;

            var root = _document.rootVisualElement;
            var toolbarRoot = root.Q<VisualElement>("toolbarRoot");
            if (toolbarRoot == null)
            {
                // Ainda não aplicaram o HUD.uxml — sem log de erro aqui.
                return;
            }

            _toolbarGroup       = toolbarRoot.Q<VisualElement>("toolbarButtons");
            _buildToggleButton  = toolbarRoot.Q<Button>("buildToggleBtn");
            _decoToggleButton   = toolbarRoot.Q<Button>("decoToggleBtn");
            _beautyToggleButton = toolbarRoot.Q<Button>("beautyToggleBtn");
            _buildMenu          = toolbarRoot.Q<VisualElement>("buildMenu");
        }

        public void BindGridPlacer(GridPlacer gridPlacer)
        {
            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged -= OnPlacementModeChanged;
                _gridPlacer.PreviewStateChanged -= OnPreviewStateChanged;
                _gridPlacer.PlacementFailedInsufficientFunds -= OnPlacementFailedInsufficientFunds;
            }

            _gridPlacer = gridPlacer;

            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged += OnPlacementModeChanged;
                _gridPlacer.PreviewStateChanged += OnPreviewStateChanged;
                _gridPlacer.PlacementFailedInsufficientFunds += OnPlacementFailedInsufficientFunds;
            }
        }

        public void BindBuildCatalog(BuildCatalog catalog)
        {
            _buildCatalog = catalog;
            if (_buildMenu != null)
            {
                RebuildBuildButtons();
            }
        }

        public void BindEconomy(EconomySystem economy)
        {
            _economy = economy;
            if (_buildMenu != null)
            {
                UpdateBuildButtonsByCash(_economy?.Cash ?? 0f);
            }
        }

        private void HookEvents()
        {
            if (_buildToggleButton != null)
            {
                _buildToggleButton.clicked += OnBuildButton;
            }

            if (_decoToggleButton != null)
            {
                _decoToggleButton.clicked += OnDecoToggleClicked;
            }

            if (_beautyToggleButton != null)
            {
                _beautyToggleButton.clicked += OnBeautyToggleClicked;
            }
        }

        private void UnhookEvents()
        {
            if (_buildToggleButton != null)
            {
                _buildToggleButton.clicked -= OnBuildButton;
            }

            if (_decoToggleButton != null)
            {
                _decoToggleButton.clicked -= OnDecoToggleClicked;
            }

            if (_beautyToggleButton != null)
            {
                _beautyToggleButton.clicked -= OnBeautyToggleClicked;
            }

            DetachBuildOptionCallbacks();

            if (_gridPlacer != null)
            {
                _gridPlacer.PlacementModeChanged -= OnPlacementModeChanged;
                _gridPlacer.PreviewStateChanged -= OnPreviewStateChanged;
                _gridPlacer.PlacementFailedInsufficientFunds -= OnPlacementFailedInsufficientFunds;
            }
        }

        public void OnBuildButton()
        {
            ShowBuildCategory(BuildCategory.Build);
        }

        public void OnDecoToggleClicked()
        {
            ShowBuildCategory(BuildCategory.Deco);
        }

        public void OnBeautyToggleClicked()
        {
            _beautyOverlayEnabled = !_beautyOverlayEnabled;
            UpdateCategoryButtons();
            BeautyToggleChanged?.Invoke();
        }

        private void ShowBuildCategory(BuildCategory category)
        {
            if (_activeBuildCategory != category)
            {
                _activeBuildCategory = category;
                RebuildBuildButtons();
            }

            SetBuildMenuVisible(true);
            UpdateCategoryButtons();
        }

        private void SetBuildMenuVisible(bool visible)
        {
            _buildMenuVisible = visible;
            if (_buildMenu != null)
            {
                _buildMenu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateCategoryButtons();

            if (visible && _gridPlacer != null)
            {
                _gridPlacer.SetBuildMode(true);
            }
            else if (!visible && _gridPlacer != null && !_gridPlacer.HasActivePlacement)
            {
                _gridPlacer.SetBuildMode(false);
            }
        }

        private void UpdateCategoryButtons()
        {
            var buildActive = _buildMenuVisible && _activeBuildCategory == BuildCategory.Build;
            var decoActive = _buildMenuVisible && _activeBuildCategory == BuildCategory.Deco;
            
            _buildToggleButton?.EnableInClassList("tool-button--active", buildActive);
            _decoToggleButton?.EnableInClassList("tool-button--active", decoActive);
            _beautyToggleButton?.EnableInClassList("tool-button--active", _beautyOverlayEnabled);
        }

        private void RebuildBuildButtons()
        {
            if (_buildMenu == null || _buildCatalog == null)
            {
                return;
            }

            DetachBuildOptionCallbacks();
            _buildMenu.Clear();
            _buildOptionButtons.Clear();
            _buildOptionLookup.Clear();

            foreach (var entry in _buildCatalog.GetEntries(_activeBuildCategory))
            {
                var button = new Button
                {
                    name = entry.Id + "Btn",
                    userData = entry.Kind
                };
                button.AddToClassList("build-option");
                UpdateBuildButtonLabel(button, entry);
                button.tooltip = BuildTooltip(entry);
                _buildMenu.Add(button);
                _buildOptionButtons.Add(button);
                _buildOptionLookup[button] = entry;
            }

            AttachBuildOptionCallbacks();
            UpdateBuildButtonsByCash(_economy?.Cash ?? 0f);
        }

        private void UpdateBuildButtonsByCash(float cash)
        {
            foreach (var pair in _buildOptionLookup)
            {
                var entry = pair.Value;
                var button = pair.Key;
                UpdateBuildButtonLabel(button, entry);
                button.SetEnabled(cash + 0.01f >= Mathf.Max(0f, entry.Cost));
            }
        }

        private static void UpdateBuildButtonLabel(Button button, BuildCatalog.Entry entry)
        {
            if (button == null)
            {
                return;
            }

            if (entry.Cost > 0f)
            {
                button.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                    "{0}\n{1:N0}", entry.Label, entry.Cost);
            }
            else
            {
                button.text = entry.Label;
            }
        }

        private static string BuildTooltip(BuildCatalog.Entry entry)
        {
            var builder = new System.Text.StringBuilder(entry.Label);
            if (entry.Cost > 0f)
            {
                builder.Append("\nCusto: ");
                builder.Append(entry.Cost.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                builder.Append('\n');
                builder.Append(entry.Description);
            }

            return builder.ToString();
        }

        private void AttachBuildOptionCallbacks()
        {
            if (_buildMenu == null || _buildOptionButtons.Count == 0)
            {
                return;
            }

            _buildOptionHandler ??= OnBuildOptionClicked;
            foreach (var button in _buildOptionButtons)
            {
                button.UnregisterCallback(_buildOptionHandler);
                button.RegisterCallback(_buildOptionHandler);
            }
        }

        private void DetachBuildOptionCallbacks()
        {
            if (_buildOptionHandler == null)
            {
                return;
            }

            foreach (var button in _buildOptionButtons)
            {
                button.UnregisterCallback(_buildOptionHandler);
            }
        }

        private void OnBuildOptionClicked(ClickEvent evt)
        {
            if (_gridPlacer == null)
            {
                return;
            }

            if (evt.currentTarget is Button optionButton && _buildOptionLookup.TryGetValue(optionButton, out var entry))
            {
                var kind = entry.Kind;
                if (_gridPlacer.ActiveKind == kind && _gridPlacer.BuildModeActive)
                {
                    _gridPlacer.ExitBuildMode();
                }
                else
                {
                    _gridPlacer.BeginPlacement(kind);
                }

                SetBuildMenuVisible(false);
            }
        }

        private void OnPlacementModeChanged(GridPlacer.PlaceableKind kind)
        {
            // Highlight active option if needed
        }

        private void OnPreviewStateChanged(bool hasValid, bool canAfford)
        {
            // Update preview state if needed
        }

        private void OnPlacementFailedInsufficientFunds(float cost)
        {
            // Show error message if needed
        }

    }
}
