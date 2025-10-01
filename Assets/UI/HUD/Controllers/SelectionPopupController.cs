using System;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.UI.Events;
using TavernSim.UI.SystemStubs;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller especializado para o popup de seleção.
    /// Mostra informações do objeto selecionado e ações contextuais.
    /// </summary>
    public class SelectionPopupController
    {
        private readonly VisualElement _root;

        // UI Elements
        private VisualElement _selectionPopup;
        private Label _selectionName;
        private Label _selectionType;
        private Label _selectionStatus;
        private Label _selectionSpeed;
        private VisualElement _selectionActions;

        // Action Buttons
        private Button _fireButton;
        private Button _moveButton;
        private Button _sellButton;

        private TavernSim.Core.ISelectable _currentSelection;
        private bool _isVisible = false;

        public event Action<TavernSim.Core.ISelectable> FireRequested;
        public event Action<TavernSim.Core.ISelectable> MoveRequested;
        public event Action<TavernSim.Core.ISelectable> SellRequested;

        public SelectionPopupController(VisualElement root)
        {
            _root = root;
        }

        public void Initialize()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            _selectionPopup = _root.Q("selectionPopup");
            _selectionName = _root.Q<Label>("selectionName");
            _selectionType = _root.Q<Label>("selectionType");
            _selectionStatus = _root.Q<Label>("selectionStatus");
            _selectionSpeed = _root.Q<Label>("selectionSpeed");
            _selectionActions = _root.Q("selectionActions");

            // Action buttons
            _fireButton = _root.Q<Button>("selectionFireBtn");
            _moveButton = _root.Q<Button>("selectionMoveBtn");
            _sellButton = _root.Q<Button>("selectionSellBtn");

            // Hook button events
            _fireButton?.RegisterCallback<ClickEvent>(_ => OnFireClicked());
            _moveButton?.RegisterCallback<ClickEvent>(_ => OnMoveClicked());
            _sellButton?.RegisterCallback<ClickEvent>(_ => OnSellClicked());
        }

        private void OnFireClicked()
        {
            if (_currentSelection != null)
            {
                FireRequested?.Invoke(_currentSelection);
            }
        }

        private void OnMoveClicked()
        {
            if (_currentSelection != null)
            {
                MoveRequested?.Invoke(_currentSelection);
            }
        }

        private void OnSellClicked()
        {
            if (_currentSelection != null)
            {
                SellRequested?.Invoke(_currentSelection);
            }
        }

        // Public API
        public void Show(TavernSim.Core.ISelectable selectable)
        {
            if (_selectionPopup == null) return;

            _currentSelection = selectable;
            UpdateSelectionInfo();
            
            _selectionPopup.RemoveFromClassList("selection-popup--hidden");
            _isVisible = true;
        }

        public void Hide()
        {
            if (_selectionPopup == null) return;

            _selectionPopup.AddToClassList("selection-popup--hidden");
            _currentSelection = null;
            _isVisible = false;
        }

        public void UpdateSelection(ISelectable selectable)
        {
            if (_isVisible && selectable != null)
            {
                _currentSelection = selectable;
                UpdateSelectionInfo();
            }
        }

        private void UpdateSelectionInfo()
        {
            if (_currentSelection == null) return;

            _selectionName.text = _currentSelection.DisplayName;
            _selectionType.text = _currentSelection.GetType().Name;
            _selectionStatus.text = "Active";
            _selectionSpeed.text = "0.0";

            // Mostrar/ocultar botões baseado no tipo de objeto
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            if (_currentSelection == null) return;

            // TODO: Implementar lógica para mostrar/ocultar botões baseado no tipo
            // Por exemplo, só mostrar "Demitir" para funcionários
            bool canFire = false;
            bool canMove = false;
            bool canSell = false;

            _fireButton?.SetEnabled(canFire);
            _moveButton?.SetEnabled(canMove);
            _sellButton?.SetEnabled(canSell);
        }
    }
}
