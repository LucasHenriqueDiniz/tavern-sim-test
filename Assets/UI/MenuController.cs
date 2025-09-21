using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Domain;
using TavernSim.Simulation.Systems;

namespace TavernSim.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour, IMenuPolicy
    {
        private const string MenuFoldoutName = "menuFoldout";
        private const string PlayerPrefsPrefix = "TavernSim.Menu.";

        private Catalog _catalog;
        private UIDocument _document;
        private Foldout _foldout;
        private readonly Dictionary<string, bool> _allowLookup = new Dictionary<string, bool>();
        private readonly Dictionary<string, Toggle> _toggleLookup = new Dictionary<string, Toggle>();

        public void Initialize(Catalog catalog)
        {
            _catalog = catalog;
            BuildStateCache();
            RebuildMenu(GetRoot());
        }

        public void RebuildMenu(VisualElement root)
        {
            _document ??= GetComponent<UIDocument>();
            if (root == null)
            {
                root = GetRoot();
            }

            if (root == null)
            {
                return;
            }

            _foldout = root.Q<Foldout>(MenuFoldoutName);
            if (_foldout != null)
            {
                _foldout.Clear();
            }
            else
            {
                _foldout = new Foldout
                {
                    text = "Menu",
                    name = MenuFoldoutName,
                    value = true,
                    style =
                    {
                        marginTop = 8f,
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };
                root.Add(_foldout);
            }

            _toggleLookup.Clear();
            if (_catalog == null)
            {
                return;
            }

            foreach (var pair in _catalog.Recipes)
            {
                var recipe = pair.Value;
                if (recipe == null)
                {
                    continue;
                }

                var allowed = GetState(recipe.Id);
                var toggle = new Toggle(recipe.DisplayName)
                {
                    value = allowed,
                    tooltip = recipe.Id
                };
                toggle.RegisterValueChangedCallback(evt => OnToggleChanged(recipe, evt.newValue));
                _foldout.Add(toggle);
                _toggleLookup[recipe.Id] = toggle;
            }
        }

        public bool IsAllowed(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (_allowLookup.TryGetValue(recipe.Id, out var allowed))
            {
                return allowed;
            }

            return true;
        }

        private VisualElement GetRoot()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            return _document != null ? _document.rootVisualElement : null;
        }

        private void BuildStateCache()
        {
            _allowLookup.Clear();
            if (_catalog == null)
            {
                return;
            }

            foreach (var pair in _catalog.Recipes)
            {
                var recipe = pair.Value;
                if (recipe == null || string.IsNullOrEmpty(recipe.Id))
                {
                    continue;
                }

                _allowLookup[recipe.Id] = PlayerPrefs.GetInt(PlayerPrefsPrefix + recipe.Id, 1) != 0;
            }
        }

        private bool GetState(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return true;
            }

            if (_allowLookup.TryGetValue(recipeId, out var allowed))
            {
                return allowed;
            }

            _allowLookup[recipeId] = true;
            return true;
        }

        private void OnToggleChanged(RecipeSO recipe, bool allowed)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.Id))
            {
                return;
            }

            _allowLookup[recipe.Id] = allowed;
            PlayerPrefs.SetInt(PlayerPrefsPrefix + recipe.Id, allowed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
