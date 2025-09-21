using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Core;

namespace TavernSim.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudToastController : MonoBehaviour, IEventSink
    {
        [SerializeField] private float toastDuration = 4f;
        [SerializeField] private int maxToasts = 4;

        private UIDocument _document;
        private IEventBus _eventBus;
        private VisualElement _root;
        private VisualElement _container;
        private readonly List<ToastEntry> _entries = new List<ToastEntry>(8);

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_eventBus != null)
            {
                _eventBus.Subscribe(this);
            }
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe(this);
            }

            ClearToasts();
        }

        public void Initialize(IEventBus eventBus)
        {
            if (_eventBus == eventBus)
            {
                return;
            }

            if (isActiveAndEnabled && _eventBus != null)
            {
                _eventBus.Unsubscribe(this);
            }

            _eventBus = eventBus;

            if (isActiveAndEnabled && _eventBus != null)
            {
                _eventBus.Subscribe(this);
            }
        }

        public void AttachTo(VisualElement root)
        {
            _root = root;
            EnsureContainer();
        }

        public void Receive(GameEvent gameEvent)
        {
            EnsureContainer();
            if (_container == null)
            {
                return;
            }

            var element = CreateToastElement(gameEvent);
            var coroutine = StartCoroutine(AutoRemove(element, toastDuration));
            _entries.Add(new ToastEntry(element, coroutine));
            TrimToasts();
        }

        private void EnsureContainer()
        {
            if (_root == null)
            {
                _root = _document != null ? _document.rootVisualElement : null;
                if (_root == null)
                {
                    return;
                }
            }

            if (_container != null && _container.panel == _root.panel)
            {
                return;
            }

            _container = _root.Q<VisualElement>("hudToastContainer");
            if (_container == null)
            {
                _container = new VisualElement
                {
                    name = "hudToastContainer",
                    style =
                    {
                        position = Position.Absolute,
                        bottom = 16f,
                        left = 16f,
                        flexDirection = FlexDirection.Column,
                        maxWidth = 360f,
                        unityFontStyleAndWeight = FontStyle.Normal
                    }
                };
                _root.Add(_container);
            }

            _container.style.position = Position.Absolute;
            _container.style.bottom = 16f;
            _container.style.left = 16f;
            _container.style.flexDirection = FlexDirection.Column;
            _container.style.gap = 6f;
            _container.style.maxWidth = 360f;
        }

        private VisualElement CreateToastElement(GameEvent gameEvent)
        {
            var toast = new VisualElement
            {
                style =
                {
                    backgroundColor = new StyleColor(GetBackgroundColor(gameEvent.Severity)),
                    paddingLeft = 12f,
                    paddingRight = 12f,
                    paddingTop = 6f,
                    paddingBottom = 6f,
                    marginBottom = 4f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    borderRadius = 6f,
                    opacity = 0.95f
                }
            };

            var label = new Label(gameEvent.Message)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(Color.white)
                }
            };

            toast.Add(label);
            _container.Add(toast);
            return toast;
        }

        private void TrimToasts()
        {
            while (_entries.Count > maxToasts && _entries.Count > 0)
            {
                var entry = _entries[0];
                _entries.RemoveAt(0);
                if (entry.Coroutine != null)
                {
                    StopCoroutine(entry.Coroutine);
                }

                entry.Element.RemoveFromHierarchy();
            }
        }

        private IEnumerator AutoRemove(VisualElement element, float duration)
        {
            yield return new WaitForSeconds(duration);
            RemoveToast(element);
        }

        private void RemoveToast(VisualElement element)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Element == element)
                {
                    if (_entries[i].Coroutine != null)
                    {
                        StopCoroutine(_entries[i].Coroutine);
                    }

                    _entries.RemoveAt(i);
                    break;
                }
            }

            element.RemoveFromHierarchy();
        }

        private void ClearToasts()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Coroutine != null)
                {
                    StopCoroutine(entry.Coroutine);
                }

                entry.Element.RemoveFromHierarchy();
            }

            _entries.Clear();
        }

        private static Color GetBackgroundColor(GameEventSeverity severity)
        {
            switch (severity)
            {
                case GameEventSeverity.Warning:
                    return new Color(0.8f, 0.52f, 0.16f, 0.92f);
                case GameEventSeverity.Error:
                    return new Color(0.74f, 0.18f, 0.18f, 0.92f);
                default:
                    return new Color(0.16f, 0.52f, 0.8f, 0.92f);
            }
        }

        private readonly struct ToastEntry
        {
            public ToastEntry(VisualElement element, Coroutine coroutine)
            {
                Element = element;
                Coroutine = coroutine;
            }

            public VisualElement Element { get; }
            public Coroutine Coroutine { get; }
        }
    }
}
