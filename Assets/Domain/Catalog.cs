using System.Collections.Generic;
using UnityEngine;

namespace TavernSim.Domain
{
    /// <summary>
    /// Provides runtime lookup of ScriptableObjects by id.
    /// </summary>
    [CreateAssetMenu(menuName = "TavernSim/Catalog", fileName = "Catalog")]
    public sealed class Catalog : ScriptableObject
    {
        [SerializeField] private ItemSO[] items = System.Array.Empty<ItemSO>();
        [SerializeField] private RecipeSO[] recipes = System.Array.Empty<RecipeSO>();

        private readonly Dictionary<string, ItemSO> _itemLookup = new Dictionary<string, ItemSO>();
        private readonly Dictionary<string, RecipeSO> _recipeLookup = new Dictionary<string, RecipeSO>();
        private bool _initialized;

        public IReadOnlyDictionary<string, ItemSO> Items
        {
            get
            {
                EnsureInitialized();
                return _itemLookup;
            }
        }

        public IReadOnlyDictionary<string, RecipeSO> Recipes
        {
            get
            {
                EnsureInitialized();
                return _recipeLookup;
            }
        }

        public bool TryGetRecipe(string id, out RecipeSO recipe)
        {
            EnsureInitialized();
            return _recipeLookup.TryGetValue(id, out recipe);
        }

        public bool TryGetItem(string id, out ItemSO item)
        {
            EnsureInitialized();
            return _itemLookup.TryGetValue(id, out item);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _itemLookup.Clear();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                _itemLookup[item.Id] = item;
            }

            _recipeLookup.Clear();
            foreach (var recipe in recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.Id))
                {
                    continue;
                }

                _recipeLookup[recipe.Id] = recipe;
            }

            _initialized = true;
        }
    }
}
