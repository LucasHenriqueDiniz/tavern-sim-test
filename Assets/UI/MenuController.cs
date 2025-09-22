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

        // === métodos esperados pelos chamadores ===
        public void Initialize(Catalog cat)
        {
            catalog = cat != null ? cat : catalog;
            if (document == null) document = GetComponent<UIDocument>();
            RebuildMenu(); // reconstrói UI e estado
        }

        public void RebuildMenu()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (document == null || catalog == null) return;
            var root = document.rootVisualElement;
            if (root == null) return;

            // limpa seção anterior do menu, se existir
            var old = root.Q<Foldout>("__MenuFoldout");
            old?.RemoveFromHierarchy();

            var fold = new Foldout { name = "__MenuFoldout", text = "Menu", value = false };
            root.Add(fold);

            _allowed.Clear();
            if (catalog.Recipes == null) return;

            foreach (var kv in catalog.Recipes)
            {
                var recipe = kv.Value;
                if (recipe == null) continue;
                var t = new Toggle(recipe.DisplayName ?? recipe.name) { value = true };
                _allowed.Add(recipe);
                t.RegisterValueChangedCallback(ev =>
                {
                    if (ev.newValue) _allowed.Add(recipe);
                    else _allowed.Remove(recipe);
                });
                fold.Add(t);
            }
        }

        // IMenuPolicy
        public bool IsAllowed(RecipeSO recipe) => recipe != null && _allowed.Contains(recipe);
    }
}
