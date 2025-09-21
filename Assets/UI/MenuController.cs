using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Domain;

namespace TavernSim.UI
{
    public interface IMenuPolicy
    {
        bool IsAllowed(RecipeSO recipe);
    }

    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour, IMenuPolicy
    {
        [SerializeField] private Catalog catalog;
        private UIDocument _doc;
        private readonly HashSet<RecipeSO> _allowed = new HashSet<RecipeSO>();

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        public void Initialize(Catalog cat)
        {
            catalog = cat != null ? cat : catalog;
            BuildUI();
        }

        public bool IsAllowed(RecipeSO recipe)
        {
            return recipe != null && _allowed.Contains(recipe);
        }

        private void BuildUI()
        {
            if (_doc == null || catalog == null)
            {
                return;
            }

            var root = _doc.rootVisualElement;
            var fold = new Foldout { text = "Menu", value = false };
            fold.style.marginTop = 8;
            fold.style.marginLeft = 8;
            root.Add(fold);

            foreach (var kv in catalog.Recipes)
            {
                var recipe = kv.Value;
                var toggle = new Toggle(recipe.DisplayName ?? recipe.name) { value = true };
                _allowed.Add(recipe);
                toggle.RegisterValueChangedCallback(ev =>
                {
                    if (ev.newValue)
                    {
                        _allowed.Add(recipe);
                    }
                    else
                    {
                        _allowed.Remove(recipe);
                    }
                });
                fold.Add(toggle);
            }
        }
    }
}
