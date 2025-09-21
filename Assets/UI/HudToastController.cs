using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Core.Events;

namespace TavernSim.UI
{
    /// <summary>Mostra toasts simples com base nos GameEvents publicados no bus compartilhado.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudToastController : MonoBehaviour
    {
        [SerializeField] private float toastSeconds = 3f;
        private readonly Queue<string> _queue = new Queue<string>();
        private UIDocument _doc;
        private Label _label;
        private Coroutine _runner;
        private IEventBus _bus;

        public void Initialize(IEventBus bus)
        {
            _bus = bus;
            _bus?.Subscribe(OnGameEvent);
        }

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.bottom = 8; container.style.left = 8;
            container.style.paddingLeft = 8; container.style.paddingRight = 8;
            container.style.paddingTop = 4; container.style.paddingBottom = 4;
            container.style.backgroundColor = new Color(0,0,0,0.6f);
            container.style.borderTopLeftRadius = 4; container.style.borderTopRightRadius = 4;
            container.style.borderBottomLeftRadius = 4; container.style.borderBottomRightRadius = 4;

            _label = new Label(string.Empty);
            root.Add(container);
            container.Add(_label);
        }

        private void OnDestroy()
        {
            _bus?.Unsubscribe(OnGameEvent);
        }

        private void OnGameEvent(GameEvent e)
        {
            var text = string.IsNullOrEmpty(e.Message) ? e.Type : $"{e.Type}: {e.Message}";
            _queue.Enqueue(text);
            if (_runner == null) _runner = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (_queue.Count > 0)
            {
                _label.text = _queue.Dequeue();
                yield return new WaitForSeconds(toastSeconds);
                _label.text = string.Empty;
            }
            _runner = null;
        }
    }
}
