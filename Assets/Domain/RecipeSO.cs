using UnityEngine;

namespace TavernSim.Domain
{
    /// <summary>
    /// Defines a recipe referencing ingredients and preparation time.
    /// </summary>
    [CreateAssetMenu(menuName = "TavernSim/Recipe", fileName = "Recipe")]
    public sealed class RecipeSO : ScriptableObject
    {
        [SerializeField] private string id = "recipe";
        [SerializeField] private string displayName = "Recipe";
        [SerializeField] private ItemSO[] ingredients = System.Array.Empty<ItemSO>();
        [SerializeField] private float prepTime = 3f;
        [SerializeField] private ItemSO outputItem;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemSO[] Ingredients => ingredients;
        public float PrepTime => prepTime;
        public ItemSO OutputItem => outputItem;
    }
}
