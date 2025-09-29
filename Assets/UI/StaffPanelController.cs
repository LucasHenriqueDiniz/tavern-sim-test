using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TavernSim.UI
{
    /// <summary>
    /// Controla o painel dedicado de gerenciamento de equipe.
    /// </summary>
    [ExecuteAlways]
    public sealed class StaffPanelController : MonoBehaviour
    {
        private const string PanelResourcePath = "UI/UXML/StaffPanel";

        private UIDocument _document;
        private VisualElement _panelRoot;
        private Button _closeButton;
        private readonly Dictionary<Button, StaffCategory> _tabLookup = new();
        private readonly Dictionary<Button, Action> _tabHandlers = new();
        private readonly Dictionary<StaffCategory, VisualElement> _tabContentLookup = new();
        private readonly Dictionary<StaffCategory, ScrollView> _staffLists = new();

        private Button _hireCookButton;
        private Button _hireWaiterButton;
        private Button _hireBartenderButton;
        private Button _hireCleanerButton;

        private StaffCategory _activeCategory = StaffCategory.Cooks;
        private bool _isOpen;

        public event Action HireWaiterRequested;
        public event Action HireCookRequested;
        public event Action HireBartenderRequested;
        public event Action HireCleanerRequested;
        public event Action<StaffCategory, ScrollView> StaffListRefreshRequested;
        public event Action<StaffCategory> ActiveCategoryChanged;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            EnsurePanel();
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                EnsurePanel();
            }
        }

        public void Initialize(UIDocument document)
        {
            _document = document;
            EnsurePanel();
            HookEvents();
        }

        public ScrollView GetStaffList(StaffCategory category)
        {
            EnsurePanel();
            return _staffLists.TryGetValue(category, out var list) ? list : null;
        }

        public void TogglePanel()
        {
            SetOpen(!_isOpen);
        }

        public void OpenPanel()
        {
            SetOpen(true);
        }

        public void ClosePanel()
        {
            SetOpen(false);
        }

        private void EnsurePanel()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document?.rootVisualElement == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            _panelRoot = root.Q<VisualElement>("staffPanel");

            if (_panelRoot == null)
            {
                var asset = Resources.Load<VisualTreeAsset>(PanelResourcePath);
                if (asset == null)
                {
                    Debug.LogWarning($"StaffPanelController: não foi possível carregar {PanelResourcePath}.");
                    return;
                }

                var instance = asset.Instantiate();
                root.Add(instance);
                _panelRoot = instance.Q<VisualElement>("staffPanel");
            }

            if (_panelRoot == null)
            {
                Debug.LogWarning("StaffPanelController: staffPanel não encontrado no visual tree.");
                return;
            }

            _panelRoot.RemoveFromClassList("open");

            _closeButton = _panelRoot.Q<Button>("staffCloseBtn");

            _tabLookup.Clear();
            _tabHandlers.Clear();
            _tabContentLookup.Clear();
            _staffLists.Clear();

            RegisterTab("cooksTab", StaffCategory.Cooks, "cooksContent", "cooksList");
            RegisterTab("waitersTab", StaffCategory.Waiters, "waitersContent", "waitersList");
            RegisterTab("bartendersTab", StaffCategory.Bartenders, "bartendersContent", "bartendersList");
            RegisterTab("cleanersTab", StaffCategory.Cleaners, "cleanersContent", "cleanersList");

            _hireCookButton = _panelRoot.Q<Button>("hireCookBtn");
            _hireWaiterButton = _panelRoot.Q<Button>("hireWaiterBtn");
            _hireBartenderButton = _panelRoot.Q<Button>("hireBartenderBtn");
            _hireCleanerButton = _panelRoot.Q<Button>("hireCleanerBtn");

            SetActiveCategory(_activeCategory);
        }

        private void RegisterTab(string tabName, StaffCategory category, string contentName, string listName)
        {
            var tabButton = _panelRoot?.Q<Button>(tabName);
            var content = _panelRoot?.Q<VisualElement>(contentName);
            var list = _panelRoot?.Q<ScrollView>(listName);

            if (tabButton != null)
            {
                _tabLookup[tabButton] = category;
            }

            if (content != null)
            {
                _tabContentLookup[category] = content;
            }

            if (list != null)
            {
                _staffLists[category] = list;
            }
        }

        private void HookEvents()
        {
            if (_panelRoot == null)
            {
                return;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= ClosePanel;
                _closeButton.clicked += ClosePanel;
            }

            foreach (var pair in _tabLookup)
            {
                var button = pair.Key;
                var category = pair.Value;

                if (!_tabHandlers.TryGetValue(button, out var handler))
                {
                    handler = () => OnTabClicked(category);
                    _tabHandlers[button] = handler;
                }

                button.clicked -= handler;
                button.clicked += handler;
            }

            if (_hireCookButton != null)
            {
                _hireCookButton.clicked -= OnHireCookClicked;
                _hireCookButton.clicked += OnHireCookClicked;
            }

            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.clicked -= OnHireWaiterClicked;
                _hireWaiterButton.clicked += OnHireWaiterClicked;
            }

            if (_hireBartenderButton != null)
            {
                _hireBartenderButton.clicked -= OnHireBartenderClicked;
                _hireBartenderButton.clicked += OnHireBartenderClicked;
            }

            if (_hireCleanerButton != null)
            {
                _hireCleanerButton.clicked -= OnHireCleanerClicked;
                _hireCleanerButton.clicked += OnHireCleanerClicked;
            }
        }

        private void UnhookEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= ClosePanel;
            }

            foreach (var pair in _tabLookup)
            {
                var button = pair.Key;
                if (_tabHandlers.TryGetValue(button, out var handler))
                {
                    button.clicked -= handler;
                }
            }

            if (_hireCookButton != null)
            {
                _hireCookButton.clicked -= OnHireCookClicked;
            }

            if (_hireWaiterButton != null)
            {
                _hireWaiterButton.clicked -= OnHireWaiterClicked;
            }

            if (_hireBartenderButton != null)
            {
                _hireBartenderButton.clicked -= OnHireBartenderClicked;
            }

            if (_hireCleanerButton != null)
            {
                _hireCleanerButton.clicked -= OnHireCleanerClicked;
            }
        }

        private void OnTabClicked(StaffCategory category)
        {
            SetActiveCategory(category);
        }

        private void SetActiveCategory(StaffCategory category)
        {
            _activeCategory = category;

            foreach (var pair in _tabLookup)
            {
                var button = pair.Key;
                var isActive = pair.Value == category;
                button.EnableInClassList("staff-tab--active", isActive);
            }

            foreach (var contentPair in _tabContentLookup)
            {
                var content = contentPair.Value;
                var isActive = contentPair.Key == category;
                content.EnableInClassList("staff-content--active", isActive);
            }

            ActiveCategoryChanged?.Invoke(_activeCategory);
            RefreshStaffList(_activeCategory);
        }

        private void SetOpen(bool open)
        {
            EnsurePanel();

            _isOpen = open;

            if (_panelRoot != null)
            {
                _panelRoot.EnableInClassList("open", open);
            }

            if (open)
            {
                RefreshAllStaffLists();
            }
        }

        private void OnHireCookClicked()
        {
            HireCookRequested?.Invoke();
        }

        private void OnHireWaiterClicked()
        {
            HireWaiterRequested?.Invoke();
        }

        private void OnHireBartenderClicked()
        {
            HireBartenderRequested?.Invoke();
        }

        private void OnHireCleanerClicked()
        {
            HireCleanerRequested?.Invoke();
        }

        public void RefreshAllStaffLists()
        {
            EnsurePanel();

            foreach (var pair in _staffLists)
            {
                RefreshStaffList(pair.Key);
            }
        }

        public void RefreshStaffList(StaffCategory category)
        {
            EnsurePanel();

            if (!_staffLists.TryGetValue(category, out var list) || list == null)
            {
                return;
            }

            if (StaffListRefreshRequested != null)
            {
                list.contentContainer.Clear();
                StaffListRefreshRequested.Invoke(category, list);
            }
            else
            {
                list.MarkDirtyRepaint();
            }
        }

        public enum StaffCategory
        {
            Cooks,
            Waiters,
            Bartenders,
            Cleaners
        }
    }
}
