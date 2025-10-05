using System;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.UI.Events;
using TavernSim.UI.SystemStubs;
using System.Collections.Generic;
using TavernSim.Agents;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para o painel de gestão de funcionários.
    /// Mostra funcionários atuais e candidatos para contratação.
    /// </summary>
    public class StaffPanelController
    {
        private readonly VisualElement _root;

        // UI Elements
        private VisualElement _staffPanel;
        private Button _closeButton;
        private VisualElement _tabs;
        private ScrollView _content;

        // Tab Buttons
        private Button _waitersTab;
        private Button _cooksTab;
        private Button _bartendersTab;
        private Button _cleanersTab;

        private StaffType _activeTab = StaffType.Waiter;
        private bool _isVisible = false;

        public event Action<StaffType> HireRequested;
        public event Action<StaffType, int> FireRequested;

        public StaffPanelController(VisualElement root)
        {
            _root = root;
        }

        public void Initialize()
        {
            SetupUI();
            UpdateStaffList(StaffType.Waiter, new List<StaffMember>(), new List<StaffMember>());
        }

        private void SetupUI()
        {
            _staffPanel = _root.Q("staffPanel");
            _closeButton = _root.Q<Button>("staffCloseBtn");
            _tabs = _root.Q("staffTabs");
            _content = _root.Q<ScrollView>("staffContent");

            // Tab buttons
            _waitersTab = _root.Q<Button>("waitersTab");
            _cooksTab = _root.Q<Button>("cooksTab");
            _bartendersTab = _root.Q<Button>("bartendersTab");
            _cleanersTab = _root.Q<Button>("cleanersTab");

            // Hook button events
            _closeButton?.RegisterCallback<ClickEvent>(_ => Hide());
            _waitersTab?.RegisterCallback<ClickEvent>(_ => SetActiveTab(StaffType.Waiter));
            _cooksTab?.RegisterCallback<ClickEvent>(_ => SetActiveTab(StaffType.Cook));
            _bartendersTab?.RegisterCallback<ClickEvent>(_ => SetActiveTab(StaffType.Bartender));
            _cleanersTab?.RegisterCallback<ClickEvent>(_ => SetActiveTab(StaffType.Cleaner));
        }

        private void SetActiveTab(StaffType tabType)
        {
            _activeTab = tabType;

            // Update tab button states
            _waitersTab?.RemoveFromClassList("tab-button--active");
            _cooksTab?.RemoveFromClassList("tab-button--active");
            _bartendersTab?.RemoveFromClassList("tab-button--active");
            _cleanersTab?.RemoveFromClassList("tab-button--active");

            switch (tabType)
            {
                case StaffType.Waiter:
                    _waitersTab?.AddToClassList("tab-button--active");
                    break;
                case StaffType.Cook:
                    _cooksTab?.AddToClassList("tab-button--active");
                    break;
                case StaffType.Bartender:
                    _bartendersTab?.AddToClassList("tab-button--active");
                    break;
                case StaffType.Cleaner:
                    _cleanersTab?.AddToClassList("tab-button--active");
                    break;
            }

            // TODO: Atualizar conteúdo do painel
            UpdateContent();
        }

        private void UpdateContent()
        {
            if (_content == null) return;

            _content.Clear();

            // TODO: Implementar lista de funcionários e candidatos
            var title = new Label($"Funcionários - {_activeTab}");
            title.AddToClassList("staff-section-title");
            _content.Add(title);

            var hireButton = new Button(() => HireRequested?.Invoke(_activeTab))
            {
                text = $"Contratar {_activeTab}"
            };
            hireButton.AddToClassList("staff-hire-button");
            _content.Add(hireButton);
        }

        // Public API
        public void Show()
        {
            if (_staffPanel != null)
            {
                _staffPanel.RemoveFromClassList("staff-panel--hidden");
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_staffPanel != null)
            {
                _staffPanel.AddToClassList("staff-panel--hidden");
                _isVisible = false;
            }
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public bool IsVisible => _isVisible;

        public void UpdateStaffList(StaffType type, List<StaffMember> current, List<StaffMember> candidates)
        {
            if (type != _activeTab) return;

            // TODO: Implementar atualização da lista de funcionários
            Debug.Log($"Updating staff list for {type}: {current.Count} current, {candidates.Count} candidates");
        }
    }

    public enum StaffType
    {
        Waiter,
        Cook,
        Bartender,
        Cleaner
    }

    public class StaffMember
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StaffType Type { get; set; }
        public float Efficiency { get; set; }
        public float Salary { get; set; }
        public bool IsHired { get; set; }
    }
}
