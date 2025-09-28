using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Domain;

namespace TavernSim.UI
{
    // Interface usada por AgentSystem/InventoryStubs
    public interface IMenuPolicy { bool IsAllowed(RecipeSO recipe); }

    /// <summary>Controla o cardápio no HUD (stub mínimo para compilar).</summary>
    public sealed class MenuController : MonoBehaviour, IMenuPolicy
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private Catalog catalog;

        private readonly HashSet<RecipeSO> _allowed = new();
        private bool _hasUserInteracted;

        // === métodos esperados pelos chamadores ===
        public void SetDocument(UIDocument doc)
        {
            document = doc;
        }

        public void Initialize(Catalog cat)
        {
            catalog = cat != null ? cat : catalog;
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            _allowed.Clear();
            _hasUserInteracted = false;
            RebuildMenu(); // reconstrói UI e estado
        }

        private void OnEnable()
        {
            RebuildMenu();
        }

        public void RebuildMenu()
        {
            if (catalog == null)
            {
                return;
            }

            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            var root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            EnsureAllowedSeeded();

            // limpa seção anterior do menu, se existir
            var anchor = root.Q<VisualElement>("menuAnchor");
            if (anchor == null)
            {
                anchor = root.Q<VisualElement>("sidePanel")?.Q<VisualElement>(className: "panel-content");
            }

            if (anchor == null)
            {
                return;
            }

            var old = anchor.Q<VisualElement>("__MenuContainer");
            old?.RemoveFromHierarchy();

            var container = new VisualElement { name = "__MenuContainer" };
            container.AddToClassList("group-body");
            anchor.Add(container);

            if (catalog.Recipes == null)
            {
                return;
            }

            foreach (var kv in catalog.Recipes)
            {
                var recipe = kv.Value;
                if (recipe == null)
                {
                    continue;
                }

                var toggle = new Toggle(recipe.DisplayName ?? recipe.name)
                {
                    value = _allowed.Contains(recipe)
                };

                toggle.RegisterValueChangedCallback(ev =>
                {
                    _hasUserInteracted = true;
                    if (ev.newValue)
                    {
                        _allowed.Add(recipe);
                    }
                    else
                    {
                        _allowed.Remove(recipe);
                    }
                });

                container.Add(toggle);
            }
        }

        // IMenuPolicy
        public bool IsAllowed(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            EnsureAllowedSeeded();

            if (_allowed.Count == 0)
            {
                // Enquanto nenhum toggle foi construído (ou o usuário ainda não interagiu),
                // tratamos o cardápio como totalmente liberado para não travar o fluxo.
                return !_hasUserInteracted;
            }

            return _allowed.Contains(recipe);
        }

        private void EnsureAllowedSeeded()
        {
            if (_hasUserInteracted)
            {
                return;
            }

            if (catalog == null || catalog.Recipes == null)
            {
                return;
            }

            foreach (var kv in catalog.Recipes)
            {
                var recipe = kv.Value;
                if (recipe != null)
                {
                    _allowed.Add(recipe);
                }
            }
        }
    }
}
