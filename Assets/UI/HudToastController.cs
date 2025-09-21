using UnityEngine;
using UnityEngine.UIElements;

namespace TavernSim.UI
{
    /// <summary>Renderiza toasts simples no canto inferior esquerdo (stub).</summary>
    public sealed class HudToastController : MonoBehaviour
    {
        private VisualElement _container;

        // Chamado pelo HUD para conectar o Toast ao UI Document
        public void AttachTo(UIDocument doc)
        {
            if (doc == null) return;
            AttachTo(doc.rootVisualElement);
        }

        // Sobrecarga conveniente
        public void AttachTo(VisualElement root)
        {
            if (root == null) return;
            _container = new VisualElement
            {
                name = "__ToastContainer"
            };
            _container.style.position = Position.Absolute;
            _container.style.left = 8;
            _container.style.bottom = 8;
            root.Add(_container);
        }

        public void Show(string message, float seconds = 2f)
        {
            if (_container == null || string.IsNullOrEmpty(message)) return;
            var label = new Label(message);
            _container.Add(label);
            // Remoção simples após alguns segundos (stub): sem corrotina aqui; em produção use um scheduler.
        }
    }
}
