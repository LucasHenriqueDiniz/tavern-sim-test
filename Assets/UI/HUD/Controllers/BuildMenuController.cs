using System;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Building;
using BuildCategory = TavernSim.Building.BuildCategory;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para o menu de construção e decoração.
    /// Mostra opções de construção e decoração organizadas por categoria.
    /// </summary>
    public class BuildMenuController
    {
        private readonly VisualElement _root;
        private readonly BuildCatalog _catalog;
        private readonly GridPlacer _gridPlacer;

        // UI Elements
        private VisualElement _buildMenu;
        private Button _closeButton;
        private VisualElement _tabs;
        private ScrollView _optionsScroll;

        // Tab Buttons
        private Button _buildModeButton;
        private Button _decorModeButton;

        private BuildCategory _activeCategory = BuildCategory.Build;
        private bool _isVisible = false;

        public event Action<BuildCatalog.Entry> BuildOptionSelected;

        public BuildMenuController(VisualElement root, BuildCatalog catalog, GridPlacer gridPlacer)
        {
            _root = root;
            _catalog = catalog;
            _gridPlacer = gridPlacer;
        }

        public void Initialize()
        {
            SetupUI();
            UpdateOptions();
        }

        private void SetupUI()
        {
            _buildMenu = _root.Q("buildMenu");
            _closeButton = _root.Q<Button>("buildCloseBtn");
            _tabs = _root.Q("buildTabs");
            _optionsScroll = _root.Q<ScrollView>("buildOptionsScroll");

            // Tab buttons
            _buildModeButton = _root.Q<Button>("buildModeBtn");
            _decorModeButton = _root.Q<Button>("decorModeBtn");

            // Hook button events
            _closeButton?.RegisterCallback<ClickEvent>(_ => Hide());
            _buildModeButton?.RegisterCallback<ClickEvent>(_ => SetActiveCategory(BuildCategory.Build));
            _decorModeButton?.RegisterCallback<ClickEvent>(_ => SetActiveCategory(BuildCategory.Deco));
        }

        private void SetActiveCategory(BuildCategory category)
        {
            _activeCategory = category;

            // Update tab button states
            _buildModeButton?.RemoveFromClassList("tab-button--active");
            _decorModeButton?.RemoveFromClassList("tab-button--active");

            switch (category)
            {
                case BuildCategory.Build:
                    _buildModeButton?.AddToClassList("tab-button--active");
                    break;
                case BuildCategory.Deco:
                    _decorModeButton?.AddToClassList("tab-button--active");
                    break;
            }

            UpdateOptions();
        }

        private void UpdateOptions()
        {
            if (_optionsScroll == null || _catalog == null) return;

            _optionsScroll.Clear();

            // Buscar entradas do catálogo para a categoria ativa
            var entries = _catalog.GetEntries(_activeCategory);

            foreach (var entry in entries)
            {
                var optionButton = CreateBuildOption(entry);
                _optionsScroll.Add(optionButton);
            }
        }

        private Button CreateBuildOption(BuildCatalog.Entry entry)
        {
            var button = new Button(() => OnBuildOptionClicked(entry))
            {
                text = entry.Label
            };
            button.AddToClassList("build-option-button");

            // TODO: Adicionar ícone e descrição
            var icon = new VisualElement();
            icon.AddToClassList("build-option-icon");
            button.Add(icon);

            var description = new Label(entry.Description);
            description.AddToClassList("build-option-description");
            button.Add(description);

            return button;
        }

        private void OnBuildOptionClicked(BuildCatalog.Entry entry)
        {
            BuildOptionSelected?.Invoke(entry);
            // TODO: Ativar modo de construção no GridPlacer
        }

        // Public API
        public void Show()
        {
            if (_buildMenu != null)
            {
                _buildMenu.RemoveFromClassList("build-menu--hidden");
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_buildMenu != null)
            {
                _buildMenu.AddToClassList("build-menu--hidden");
                _isVisible = false;
            }
        }

    }

    // BuildCategory is defined in TavernSim.Building
}
