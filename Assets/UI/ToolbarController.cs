using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Building;
using TavernSim.Simulation.Systems;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller para a barra de ferramentas inferior (construção, decoração, beleza).
    /// </summary>
    [ExecuteAlways]
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
        private Button _staffToggleButton;
        private VisualElement _buildMenu;
        private Button _buildModeButton;
        private Button _decorModeButton;
        private ScrollView _buildOptionsScroll;
        private VisualTreeAsset _buildOptionTemplate;
        private Button _topBuildButton;
        private Button _topDecorButton;
        private Button _buildStructuresButton;
        private Button _buildStairsButton;
        private Button _buildDoorsButton;
        private Button _buildDemolishButton;
        private Button _decorArtButton;
        private Button _decorBasicButton;
        private Button _decorThemeButton;
        private Button _decorNatureButton;
        private Button _managementStaffButton;

        private readonly System.Collections.Generic.List<Button> _buildOptionButtons = new System.Collections.Generic.List<Button>();
        private readonly System.Collections.Generic.Dictionary<Button, BuildCatalog.Entry> _buildOptionLookup = new System.Collections.Generic.Dictionary<Button, BuildCatalog.Entry>();
        private EventCallback<ClickEvent> _buildOptionHandler;

        private bool _buildMenuVisible;
        private bool _beautyOverlayEnabled;
        private BuildCategory _activeBuildCategory = BuildCategory.Build;

        public event System.Action BeautyToggleChanged;
        public event System.Action StaffToggleClicked;

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

            _buildToggleButton  = toolbarRoot.Q<Button>("buildToggleBtn");
            _decoToggleButton   = toolbarRoot.Q<Button>("decoToggleBtn");
            _staffToggleButton  = toolbarRoot.Q<Button>("staffToggleBtn");
            _beautyToggleButton = toolbarRoot.Q<Button>("beautyToggleBtn");
            _buildMenu          = toolbarRoot.Q<VisualElement>("buildMenu");
            _buildModeButton    = toolbarRoot.Q<Button>("buildModeBtn");
            _decorModeButton    = toolbarRoot.Q<Button>("decorModeBtn");
            _buildOptionsScroll = toolbarRoot.Q<ScrollView>("buildOptionsScroll");
            _topBuildButton     = root.Q<Button>("buildMenuBtn");
            _topDecorButton     = root.Q<Button>("decorMenuBtn");
            _buildStructuresButton = root.Q<Button>("buildStructuresBtn");
            _buildStairsButton     = root.Q<Button>("buildStairsBtn");
            _buildDoorsButton      = root.Q<Button>("buildDoorsBtn");
            _buildDemolishButton   = root.Q<Button>("buildDemolishBtn");
            _decorArtButton        = root.Q<Button>("decorArtBtn");
            _decorBasicButton      = root.Q<Button>("decorBasicBtn");
            _decorThemeButton      = root.Q<Button>("decorThemeBtn");
            _decorNatureButton     = root.Q<Button>("decorNatureBtn");
            _managementStaffButton = root.Q<Button>("managementStaffBtn");

            if (_buildMenu != null)
            {
                _buildMenu.RemoveFromClassList("open");
            }

            if (_buildOptionTemplate == null)
            {
                _buildOptionTemplate = Resources.Load<VisualTreeAsset>("UI/UXML/BuildOption");
            }
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
            if (_buildOptionsScroll != null)
            {
                RebuildBuildButtons();
            }
        }

        public void BindEconomy(EconomySystem economy)
        {
            _economy = economy;
            if (_buildOptionsScroll != null)
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

            if (_staffToggleButton != null)
            {
                _staffToggleButton.clicked += OnStaffToggleInternal;
            }

            if (_topBuildButton != null)
            {
                _topBuildButton.clicked += OnBuildButton;
            }

            if (_topDecorButton != null)
            {
                _topDecorButton.clicked += OnDecoToggleClicked;
            }

            if (_buildStructuresButton != null)
            {
                _buildStructuresButton.clicked += OnBuildCategoryShortcut;
            }

            if (_buildStairsButton != null)
            {
                _buildStairsButton.clicked += OnBuildCategoryShortcut;
            }

            if (_buildDoorsButton != null)
            {
                _buildDoorsButton.clicked += OnBuildCategoryShortcut;
            }

            if (_buildDemolishButton != null)
            {
                _buildDemolishButton.clicked += OnBuildCategoryShortcut;
            }

            if (_decorArtButton != null)
            {
                _decorArtButton.clicked += OnDecorCategoryShortcut;
            }

            if (_decorBasicButton != null)
            {
                _decorBasicButton.clicked += OnDecorCategoryShortcut;
            }

            if (_decorThemeButton != null)
            {
                _decorThemeButton.clicked += OnDecorCategoryShortcut;
            }

            if (_decorNatureButton != null)
            {
                _decorNatureButton.clicked += OnDecorCategoryShortcut;
            }

            if (_managementStaffButton != null)
            {
                _managementStaffButton.clicked += OnStaffToggleInternal;
            }

            if (_buildModeButton != null)
            {
                _buildModeButton.clicked += OnBuildModeClicked;
            }

            if (_decorModeButton != null)
            {
                _decorModeButton.clicked += OnDecorModeClicked;
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

            if (_staffToggleButton != null)
            {
                _staffToggleButton.clicked -= OnStaffToggleInternal;
            }

            if (_topBuildButton != null)
            {
                _topBuildButton.clicked -= OnBuildButton;
            }

            if (_topDecorButton != null)
            {
                _topDecorButton.clicked -= OnDecoToggleClicked;
            }

            if (_buildStructuresButton != null)
            {
                _buildStructuresButton.clicked -= OnBuildCategoryShortcut;
            }

            if (_buildStairsButton != null)
            {
                _buildStairsButton.clicked -= OnBuildCategoryShortcut;
            }

            if (_buildDoorsButton != null)
            {
                _buildDoorsButton.clicked -= OnBuildCategoryShortcut;
            }

            if (_buildDemolishButton != null)
            {
                _buildDemolishButton.clicked -= OnBuildCategoryShortcut;
            }

            if (_decorArtButton != null)
            {
                _decorArtButton.clicked -= OnDecorCategoryShortcut;
            }

            if (_decorBasicButton != null)
            {
                _decorBasicButton.clicked -= OnDecorCategoryShortcut;
            }

            if (_decorThemeButton != null)
            {
                _decorThemeButton.clicked -= OnDecorCategoryShortcut;
            }

            if (_decorNatureButton != null)
            {
                _decorNatureButton.clicked -= OnDecorCategoryShortcut;
            }

            if (_managementStaffButton != null)
            {
                _managementStaffButton.clicked -= OnStaffToggleInternal;
            }

            if (_buildModeButton != null)
            {
                _buildModeButton.clicked -= OnBuildModeClicked;
            }

            if (_decorModeButton != null)
            {
                _decorModeButton.clicked -= OnDecorModeClicked;
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
            if (_buildMenuVisible && _activeBuildCategory == BuildCategory.Build)
            {
                SetBuildMenuVisible(false);
            }
            else
            {
                ShowBuildCategory(BuildCategory.Build);
            }
        }

        public void OnDecoToggleClicked()
        {
            if (_buildMenuVisible && _activeBuildCategory == BuildCategory.Deco)
            {
                SetBuildMenuVisible(false);
            }
            else
            {
                ShowBuildCategory(BuildCategory.Deco);
            }
        }

        public void OnBeautyToggleClicked()
        {
            _beautyOverlayEnabled = !_beautyOverlayEnabled;
            UpdateCategoryButtons();
            BeautyToggleChanged?.Invoke();
        }

        private void OnStaffToggleInternal()
        {
            StaffToggleClicked?.Invoke();
        }

        private void OnBuildModeClicked()
        {
            ShowBuildCategory(BuildCategory.Build);
        }

        private void OnDecorModeClicked()
        {
            ShowBuildCategory(BuildCategory.Deco);
        }

        private void OnBuildCategoryShortcut()
        {
            ShowBuildCategory(BuildCategory.Build);
        }

        private void OnDecorCategoryShortcut()
        {
            ShowBuildCategory(BuildCategory.Deco);
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
                _buildMenu.EnableInClassList("open", visible);
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
            _buildModeButton?.EnableInClassList("toolbar-mode__button--active", _activeBuildCategory == BuildCategory.Build);
            _decorModeButton?.EnableInClassList("toolbar-mode__button--active", _activeBuildCategory == BuildCategory.Deco);
        }

        private void RebuildBuildButtons()
        {
            if (_buildOptionsScroll == null || _buildCatalog == null)
            {
                return;
            }

            DetachBuildOptionCallbacks();
            _buildOptionButtons.Clear();
            _buildOptionLookup.Clear();

            _buildOptionsScroll.contentContainer.Clear();

            foreach (var entry in _buildCatalog.GetEntries(_activeBuildCategory))
            {
                if (_buildOptionTemplate == null)
                {
                    continue;
                }

                var template = _buildOptionTemplate.Instantiate();
                var button = template.Q<Button>("buildOptionButton");
                if (button == null)
                {
                    Debug.LogWarning("ToolbarController: template buildOptionButton não encontrado.");
                    continue;
                }

                button.name = entry.Id + "Btn";
                button.userData = entry.Kind;
                button.AddToClassList("build-option");
                UpdateBuildButtonLabel(button, entry);
                button.tooltip = BuildTooltip(entry);

                _buildOptionsScroll.Add(template);

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
