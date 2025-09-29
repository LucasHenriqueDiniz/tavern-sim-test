using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Building;
using TavernSim.Core;
using TavernSim.Agents;

namespace TavernSim.UI
{
    /// <summary>
    /// Controller para o popup de seleção que aparece quando algo está selecionado.
    /// </summary>
    public class SelectionPopupController : MonoBehaviour
    {
        private UIDocument _document;
        private SelectionService _selectionService;

        private VisualElement _selectionPopup;
        private Label _selectionNameLabel;
        private Label _selectionTypeLabel;
        private Label _selectionStatusLabel;
        private Label _selectionSpeedLabel;
        private VisualElement _contextActionsGroup;
        private VisualElement _contextActions;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            SetupUI();
            HookEvents();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private void SetupUI()
        {
            if (_document?.rootVisualElement == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            _selectionPopup = root.Q<VisualElement>("selectionPopup");
            _selectionNameLabel = root.Q<Label>("selectionName");
            _selectionTypeLabel = root.Q<Label>("selectionType");
            _selectionStatusLabel = root.Q<Label>("selectionStatus");
            _selectionSpeedLabel = root.Q<Label>("selectionSpeed");
            _contextActionsGroup = root.Q<VisualElement>("contextActions");
            _contextActions = root.Q<VisualElement>("contextActionsBody") ?? _contextActionsGroup ?? root;

            if (_selectionPopup != null)
            {
                _selectionPopup.RemoveFromClassList("open");
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
            }
        }

        public void BindSelection(SelectionService selectionService)
        {
            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
            }

            _selectionService = selectionService;

            if (_selectionService != null)
            {
                _selectionService.SelectionChanged += OnSelectionChanged;
                OnSelectionChanged(_selectionService.Current);
            }
        }

        private void HookEvents()
        {
            // Events are hooked in BindSelection
        }

        private void UnhookEvents()
        {
            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(ISelectable selectable)
        {
            UpdateSelectionDetails(selectable);

            if (_selectionPopup == null)
            {
                return;
            }

            var hasSelection = selectable != null;
            _selectionPopup.EnableInClassList("open", hasSelection);

            if (!hasSelection)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            var targetTransform = selectable.Transform;
            if (targetTransform == null)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            ScheduleSelectionPopupPosition(targetTransform);
        }

        private void ScheduleSelectionPopupPosition(Transform target)
        {
            if (_selectionPopup == null || target == null)
            {
                return;
            }

            void Perform()
            {
                if (target != null)
                {
                    PositionSelectionPopup(target);
                }
            }

            PositionSelectionPopup(target);

            var scheduler = _selectionPopup.schedule;
            if (scheduler == null)
            {
                return;
            }

            scheduler.Execute(Perform).ExecuteLater(0);
        }

        private void PositionSelectionPopup(Transform target)
        {
            if (_selectionPopup == null || target == null || _document == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var screenPoint = camera.WorldToScreenPoint(target.position);
            if (screenPoint.z <= 0f)
            {
                _selectionPopup.style.left = StyleKeyword.Null;
                _selectionPopup.style.right = 24f;
                _selectionPopup.style.top = 96f;
                return;
            }

            var panel = _document.rootVisualElement?.panel;
            if (panel == null)
            {
                return;
            }

            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPoint.x, screenPoint.y));
            float desiredLeft = panelPosition.x + 12f;
            float popupHeight = _selectionPopup.resolvedStyle.height;
            if (float.IsNaN(popupHeight) || float.IsInfinity(popupHeight) || popupHeight <= 0f)
            {
                popupHeight = 120f;
            }

            float desiredTop = panelPosition.y - popupHeight - 12f;
            var rootStyle = _document.rootVisualElement.resolvedStyle;
            float popupWidth = _selectionPopup.resolvedStyle.width;
            if (float.IsNaN(popupWidth) || float.IsInfinity(popupWidth) || popupWidth <= 0f)
            {
                popupWidth = 300f;
            }

            float maxLeft = rootStyle.width - popupWidth - 24f;
            if (float.IsNaN(maxLeft) || float.IsInfinity(maxLeft))
            {
                maxLeft = desiredLeft;
            }

            maxLeft = Mathf.Max(24f, maxLeft);
            desiredLeft = Mathf.Clamp(desiredLeft, 24f, maxLeft);

            float maxTop = rootStyle.height - 140f;
            if (float.IsNaN(maxTop) || float.IsInfinity(maxTop))
            {
                maxTop = desiredTop;
            }

            maxTop = Mathf.Max(72f, maxTop);
            desiredTop = Mathf.Clamp(desiredTop, 72f, maxTop);

            _selectionPopup.style.right = StyleKeyword.Null;
            _selectionPopup.style.left = desiredLeft;
            _selectionPopup.style.top = desiredTop;
        }

        private void UpdateSelectionDetails(ISelectable selectable)
        {
            if (_selectionNameLabel == null || _selectionTypeLabel == null || 
                _selectionStatusLabel == null || _selectionSpeedLabel == null)
            {
                return;
            }

            if (selectable == null)
            {
                _selectionNameLabel.text = HUDStrings.NoSelection;
                _selectionTypeLabel.text = "-";
                _selectionStatusLabel.text = "-";
                _selectionSpeedLabel.text = "-";
                PopulateContextActions(null);
                return;
            }

            _selectionNameLabel.text = selectable.DisplayName ?? selectable.Transform?.name ?? "Selecionado";
            _selectionTypeLabel.text = selectable.GetType().Name;
            string status = string.Empty;
            string speedText = "-";
            string extra = string.Empty;
            
            switch (selectable)
            {
                case Customer customer:
                    status = customer.Status;
                    var customerSpeed = customer.Agent != null ? customer.Agent.speed : 0f;
                    speedText = customerSpeed > 0.01f ? customerSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    extra = customer.Gold >= 0 ? $"Ouro: {customer.Gold}" : string.Empty;
                    break;
                case Waiter waiter:
                    status = waiter.Status;
                    speedText = waiter.MovementSpeed > 0.01f ? waiter.MovementSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    extra = waiter.Salary > 0f ? $"Salário: {waiter.Salary:0.##}" : string.Empty;
                    break;
                case Bartender bartender:
                    status = bartender.Status;
                    speedText = bartender.MovementSpeed > 0.01f ? bartender.MovementSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    extra = bartender.Salary > 0f ? $"Salário: {bartender.Salary:0.##}" : string.Empty;
                    break;
                case Cook cook:
                    status = cook.Status;
                    speedText = cook.MovementSpeed > 0.01f ? cook.MovementSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    extra = cook.Salary > 0f ? $"Salário: {cook.Salary:0.##}" : string.Empty;
                    break;
                case TablePresenter tablePresenter:
                    status = tablePresenter.OccupiedSeats > 0 ? $"Ocupada ({tablePresenter.OccupiedSeats}/{tablePresenter.SeatCount})" : "Livre";
                    extra = tablePresenter.Dirtiness > 0.01f ? "Precisa limpeza" : string.Empty;
                    break;
                default:
                    var go = selectable.Transform != null ? selectable.Transform.gameObject : null;
                    var agentSpeed = GetNavAgentSpeed(go);
                    speedText = agentSpeed > 0.01f ? agentSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
                    status = GetIntent(go);
                    break;
            }

            _selectionStatusLabel.text = string.IsNullOrWhiteSpace(status) ? "-" : status;
            _selectionSpeedLabel.text = speedText;
            if (!string.IsNullOrWhiteSpace(extra))
            {
                if (string.IsNullOrWhiteSpace(status) || status == "-")
                {
                    _selectionStatusLabel.text = extra;
                }
                else
                {
                    _selectionStatusLabel.text += $" • {extra}";
                }
            }

            PopulateContextActions(selectable);
        }

        private void PopulateContextActions(ISelectable selectable)
        {
            if (_contextActions == null)
            {
                return;
            }

            _contextActions.Clear();

            if (_contextActionsGroup != null)
            {
                _contextActionsGroup.style.display = selectable != null ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (selectable == null)
            {
                return;
            }

            void AddAction(string label, System.Action handler)
            {
                var button = new Button(handler) { text = label };
                button.AddToClassList("hud-button");
                _contextActions.Add(button);
            }

            switch (selectable)
            {
                case Waiter waiter:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(waiter.DisplayName ?? waiter.name + " será removido em breve."));
                    break;
                case Cook cook:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(cook.DisplayName ?? cook.name + " será removido em breve."));
                    break;
                case Bartender bartender:
                    AddAction(HUDStrings.FireAction, () => ShowActionToast(bartender.DisplayName ?? bartender.name + " será removido em breve."));
                    break;
                case TablePresenter tablePresenter:
                    AddAction(HUDStrings.MoveAction, () => ShowActionToast(tablePresenter.name + " será reposicionada."));
                    if (tablePresenter.SeatCount > 0)
                    {
                        AddAction(HUDStrings.SellAction, () => ShowActionToast(tablePresenter.name + " será vendida."));
                    }
                    break;
                default:
                    break;
            }

            if (_contextActions.childCount == 0)
            {
                var placeholder = new Label(HUDStrings.NoActions);
                placeholder.AddToClassList("context-actions__empty");
                _contextActions.Add(placeholder);
            }
        }

        private void ShowActionToast(string message)
        {
            // This would be handled by the main HUD controller
            Debug.Log($"Action: {message}");
        }

        private static float GetNavAgentSpeed(GameObject go)
        {
            if (go != null && go.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
            {
                return agent.speed;
            }

            return 0f;
        }

        private static string GetIntent(GameObject go)
        {
            if (go != null && go.TryGetComponent(out AgentIntentDisplay intentDisplay))
            {
                return intentDisplay.CurrentIntent;
            }

            return string.Empty;
        }
    }
}
