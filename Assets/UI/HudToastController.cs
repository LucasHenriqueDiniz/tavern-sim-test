using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Core.Events;

namespace TavernSim.UI
{
    /// <summary>Renderiza toasts com visual tematizado e expiração automática.</summary>
    public sealed class HudToastController : MonoBehaviour
    {
        private const int MaxVisibleToasts = 3;
        private const int MaxPendingToasts = 12;
        private VisualElement _container;
        private readonly Queue<ToastRequest> _pending = new Queue<ToastRequest>();
        private int _activeToasts;

        public void AttachTo(UIDocument doc)
        {
            if (doc == null)
            {
                return;
            }

            AttachTo(doc.rootVisualElement);
        }

        public void AttachTo(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            _container = root.Q<VisualElement>("toastLayer");
            if (_container == null)
            {
                _container = new VisualElement
                {
                    name = "toastLayer"
                };
                _container.AddToClassList("toast-layer");
                root.Add(_container);
            }
        }

        public void Show(string message, float seconds = 2.5f)
        {
            Show(GameEventSeverity.Info, message, seconds);
        }

        public void Show(GameEventSeverity severity, string message, float seconds = 2.5f)
        {
            if (_container == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var request = new ToastRequest(message, severity, Mathf.Max(0.5f, seconds));

            if (_activeToasts >= MaxVisibleToasts)
            {
                while (_pending.Count >= MaxPendingToasts)
                {
                    _pending.Dequeue();
                }
                _pending.Enqueue(request);
                return;
            }

            DisplayToast(request);
        }

        private void DisplayToast(ToastRequest request)
        {
            var toast = new VisualElement();
            toast.AddToClassList("toast");
            toast.AddToClassList(GetSeverityClass(request.Severity));
            toast.style.opacity = 0f;

            var label = new Label(request.Message);
            label.AddToClassList("toast__text");
            toast.Add(label);

            _container.Add(toast);
            toast.schedule.Execute(() => toast.style.opacity = 1f);
            _activeToasts++;

            StartCoroutine(RemoveToastAfterDelay(toast, request.Duration));
        }

        private IEnumerator RemoveToastAfterDelay(VisualElement toast, float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (toast == null)
            {
                yield break;
            }

            toast.style.opacity = 0f;
            yield return new WaitForSeconds(0.35f);

            toast.RemoveFromHierarchy();
            _activeToasts = Mathf.Max(0, _activeToasts - 1);

            if (_pending.Count > 0)
            {
                var next = _pending.Dequeue();
                DisplayToast(next);
            }
        }

        private static string GetSeverityClass(GameEventSeverity severity)
        {
            return severity switch
            {
                GameEventSeverity.Warning => "toast--warning",
                GameEventSeverity.Error => "toast--error",
                GameEventSeverity.Success => "toast--success",
                _ => "toast--info"
            };
        }

        private readonly struct ToastRequest
        {
            public readonly string Message;
            public readonly GameEventSeverity Severity;
            public readonly float Duration;

            public ToastRequest(string message, GameEventSeverity severity, float duration)
            {
                Message = message;
                Severity = severity;
                Duration = duration;
            }
        }
    }
}
