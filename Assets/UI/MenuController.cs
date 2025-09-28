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
        private readonly Dictionary<RecipeSO, List<Button>> _buttonsByRecipe = new();

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
            _buttonsByRecipe.Clear();

            var anchors = new List<VisualElement>(2);
            var panelAnchor = root.Q<VisualElement>("menuAnchor");
            if (panelAnchor != null)
            {
                anchors.Add(panelAnchor);
            }

            var quickAnchor = root.Q<VisualElement>("menuButtonAnchor");
            if (quickAnchor != null && quickAnchor != panelAnchor)
            {
                anchors.Add(quickAnchor);
            }

            if (anchors.Count == 0)
            {
                return;
            }

            foreach (var target in anchors)
            {
                target.Clear();
            }

            var recipes = catalog.Recipes;
            if (recipes == null || recipes.Count == 0)
            {
                foreach (var target in anchors)
                {
                    target.Add(new Label("Sem receitas disponíveis."));
                }

                return;
            }

            foreach (var kv in recipes)
            {
                var recipe = kv.Value;
                if (recipe == null)
                {
                    continue;
                }

                var buttons = new List<Button>(anchors.Count);
                foreach (var target in anchors)
                {
                    var button = CreateMenuButton(recipe);
                    target.Add(button);
                    buttons.Add(button);
                }

                _buttonsByRecipe[recipe] = buttons;
                UpdateRecipeButtons(recipe, _allowed.Contains(recipe));
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

        private Button CreateMenuButton(RecipeSO recipe)
        {
            var button = new Button
            {
                text = recipe.DisplayName ?? recipe.name
            };

            button.AddToClassList("menu-button");
            button.clicked += () => ToggleRecipe(recipe);
            return button;
        }

        private void ToggleRecipe(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return;
            }

            var nowAllowed = !_allowed.Contains(recipe);
            _hasUserInteracted = true;

            if (nowAllowed)
            {
                _allowed.Add(recipe);
            }
            else
            {
                _allowed.Remove(recipe);
            }

            UpdateRecipeButtons(recipe, nowAllowed);
        }

        private void UpdateRecipeButtons(RecipeSO recipe, bool active)
        {
            if (!_buttonsByRecipe.TryGetValue(recipe, out var buttons))
            {
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i]?.EnableInClassList("menu-button--active", active);
            }
        }
    }
}
