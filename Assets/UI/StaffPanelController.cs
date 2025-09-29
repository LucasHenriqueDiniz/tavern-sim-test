using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Agents;
using TavernSim.Core;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller para o painel de equipe (Staff Panel).
    /// </summary>
    public class StaffPanelController : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _staffPanel;
        private Button _closeButton;
        private Button[] _tabButtons;
        private VisualElement[] _contentPanels;
        private ScrollView[] _staffLists;
        private bool _isInitialized = false;
        private bool _isOpen = false;

        public event Action HireWaiterRequested;
        public event Action HireCookRequested;
        public event Action HireBartenderRequested;
        public event Action HireCleanerRequested;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                HookEvents();
            }
        }

        private void OnDisable()
        {
            if (_isInitialized)
            {
                UnhookEvents();
            }
        }

        public void Initialize(UIDocument document)
        {
            _document = document;
            SetupUI();
        }

        private void SetupUI()
        {
            if (_document?.rootVisualElement == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            
            // Criar o painel de staff dinamicamente
            _staffPanel = CreateStaffPanel();
            root.Add(_staffPanel);

            if (_staffPanel == null)
            {
                Debug.LogError("StaffPanelController: Não foi possível criar o painel de equipe.");
                return;
            }

            _isInitialized = true;
            HookEvents();
        }

        private VisualElement CreateStaffPanel()
        {
            var panel = new VisualElement();
            panel.name = "staffPanel";
            panel.AddToClassList("staff-panel-root");

            // Header
            var header = new VisualElement();
            header.AddToClassList("panel-header");
            
            var title = new Label("Equipe");
            title.AddToClassList("panel-title");
            header.Add(title);

            _closeButton = new Button { text = "Fechar", name = "staffCloseBtn" };
            _closeButton.AddToClassList("panel-pin");
            header.Add(_closeButton);

            panel.Add(header);

            // Tabs
            var tabsContainer = new VisualElement();
            tabsContainer.AddToClassList("staff-tabs");

            var tabButtons = new VisualElement();
            tabButtons.AddToClassList("tab-buttons");

            _tabButtons = new Button[4];
            _tabButtons[0] = new Button { text = "Cozinheiros", name = "cooksTab" };
            _tabButtons[1] = new Button { text = "Garçons", name = "waitersTab" };
            _tabButtons[2] = new Button { text = "Bartenders", name = "bartendersTab" };
            _tabButtons[3] = new Button { text = "Faxineiros", name = "cleanersTab" };

            foreach (var tab in _tabButtons)
            {
                tab.AddToClassList("tab-button");
                tabButtons.Add(tab);
            }

            tabsContainer.Add(tabButtons);

            // Content panels
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("tab-content");

            _contentPanels = new VisualElement[4];
            _staffLists = new ScrollView[4];

            for (int i = 0; i < 4; i++)
            {
                _contentPanels[i] = new VisualElement();
                _contentPanels[i].AddToClassList("staff-list");
                
                _staffLists[i] = new ScrollView();
                _contentPanels[i].Add(_staffLists[i]);
                contentContainer.Add(_contentPanels[i]);
            }

            tabsContainer.Add(contentContainer);
            panel.Add(tabsContainer);

            // Hire controls
            var hireControls = new VisualElement();
            hireControls.AddToClassList("panel-content");
            hireControls.AddToClassList("hire-controls");

            var hireTitle = new Label("Contratação");
            hireTitle.AddToClassList("group-title");
            hireControls.Add(hireTitle);

            var hireButtons = new VisualElement();
            hireButtons.AddToClassList("group-body");
            hireButtons.AddToClassList("hire-buttons");

            var hireWaiterBtn = new Button { text = "Contratar Garçom", name = "hireWaiterBtn" };
            var hireCookBtn = new Button { text = "Contratar Cozinheiro", name = "hireCookBtn" };
            var hireBartenderBtn = new Button { text = "Contratar Bartender", name = "hireBartenderBtn" };
            var hireCleanerBtn = new Button { text = "Contratar Faxineiro", name = "hireCleanerBtn" };

            hireButtons.Add(hireWaiterBtn);
            hireButtons.Add(hireCookBtn);
            hireButtons.Add(hireBartenderBtn);
            hireButtons.Add(hireCleanerBtn);

            hireControls.Add(hireButtons);
            panel.Add(hireControls);

            // Initially hidden
            panel.style.display = DisplayStyle.None;

            return panel;
        }

        private void HookEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked += ClosePanel;
            }

            // Tab buttons
            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int tabIndex = i; // Capture for closure
                    _tabButtons[i]?.RegisterCallback<ClickEvent>(_ => SetActiveTab(tabIndex));
                }
            }

            // Hire buttons
            var hireWaiterBtn = _staffPanel?.Q<Button>("hireWaiterBtn");
            var hireCookBtn = _staffPanel?.Q<Button>("hireCookBtn");
            var hireBartenderBtn = _staffPanel?.Q<Button>("hireBartenderBtn");
            var hireCleanerBtn = _staffPanel?.Q<Button>("hireCleanerBtn");

            hireWaiterBtn?.RegisterCallback<ClickEvent>(_ => HireWaiterRequested?.Invoke());
            hireCookBtn?.RegisterCallback<ClickEvent>(_ => HireCookRequested?.Invoke());
            hireBartenderBtn?.RegisterCallback<ClickEvent>(_ => HireBartenderRequested?.Invoke());
            hireCleanerBtn?.RegisterCallback<ClickEvent>(_ => HireCleanerRequested?.Invoke());
        }

        private void UnhookEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= ClosePanel;
            }

            // Tab buttons
            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int tabIndex = i; // Capture for closure
                    _tabButtons[i]?.UnregisterCallback<ClickEvent>(_ => SetActiveTab(tabIndex));
                }
            }

            // Hire buttons
            var hireWaiterBtn = _staffPanel?.Q<Button>("hireWaiterBtn");
            var hireCookBtn = _staffPanel?.Q<Button>("hireCookBtn");
            var hireBartenderBtn = _staffPanel?.Q<Button>("hireBartenderBtn");
            var hireCleanerBtn = _staffPanel?.Q<Button>("hireCleanerBtn");

            hireWaiterBtn?.UnregisterCallback<ClickEvent>(_ => HireWaiterRequested?.Invoke());
            hireCookBtn?.UnregisterCallback<ClickEvent>(_ => HireCookRequested?.Invoke());
            hireBartenderBtn?.UnregisterCallback<ClickEvent>(_ => HireBartenderRequested?.Invoke());
            hireCleanerBtn?.UnregisterCallback<ClickEvent>(_ => HireCleanerRequested?.Invoke());
        }

        public void TogglePanel()
        {
            SetOpen(!_isOpen);
        }

        public void ClosePanel()
        {
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (_staffPanel != null)
            {
                _staffPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (open)
            {
                RefreshAllStaffLists();
            }
        }

        private void SetActiveTab(int tabIndex)
        {
            if (_tabButtons == null || _contentPanels == null) return;

            // Update tab buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].RemoveFromClassList("tab-button--active");
                    if (i == tabIndex)
                    {
                        _tabButtons[i].AddToClassList("tab-button--active");
                    }
                }
            }

            // Update content panels
            for (int i = 0; i < _contentPanels.Length; i++)
            {
                if (_contentPanels[i] != null)
                {
                    _contentPanels[i].RemoveFromClassList("active");
                    if (i == tabIndex)
                    {
                        _contentPanels[i].AddToClassList("active");
                    }
                }
            }
        }

        public void RefreshAllStaffLists()
        {
            if (_staffLists == null) return;

            // Refresh each staff list
            RefreshStaffList(0, FindObjectsOfType<Cook>(), "Cozinheiro");
            RefreshStaffList(1, FindObjectsOfType<Waiter>(), "Garçom");
            RefreshStaffList(2, FindObjectsOfType<Bartender>(), "Bartender");
            RefreshStaffList(3, FindObjectsOfType<Cleaner>(), "Faxineiro");
        }

        private void RefreshStaffList<T>(int index, T[] agents, string roleName) where T : MonoBehaviour, ISelectable
        {
            if (_staffLists == null || index >= _staffLists.Length || _staffLists[index] == null) return;

            var list = _staffLists[index];
            list.contentContainer.Clear();

            if (agents == null || agents.Length == 0)
            {
                var emptyLabel = new Label($"Nenhum {roleName} contratado.");
                emptyLabel.AddToClassList("group-body");
                list.contentContainer.Add(emptyLabel);
                return;
            }

            foreach (var agent in agents)
            {
                var entry = new VisualElement();
                entry.AddToClassList("staff-entry");

                var nameLabel = new Label(agent.DisplayName ?? agent.name);
                nameLabel.AddToClassList("staff-entry__name");

                // Get status from agent if it has the Status property
                string status = GetAgentStatus(agent);
                var statusLabel = new Label(status);
                statusLabel.AddToClassList("staff-entry__status");

                entry.Add(nameLabel);
                entry.Add(statusLabel);
                list.contentContainer.Add(entry);
            }
        }

        private string GetAgentStatus(ISelectable agent)
        {
            // Try to get status from different agent types
            switch (agent)
            {
                case Waiter waiter:
                    return waiter.Status;
                case Cook cook:
                    return cook.Status;
                case Bartender bartender:
                    return bartender.Status;
                case Cleaner cleaner:
                    return cleaner.Status;
                default:
                    return "Ativo";
            }
        }
    }
}