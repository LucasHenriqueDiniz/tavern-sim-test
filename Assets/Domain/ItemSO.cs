using UnityEngine;

namespace TavernSim.Domain
{
    /// <summary>
    /// Represents a purchasable or consumable item.
    /// </summary>
    [CreateAssetMenu(menuName = "TavernSim/Item", fileName = "Item")]
    public sealed class ItemSO : ScriptableObject
    {
        [SerializeField] private string id = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private int unitCost = 1;
        [SerializeField] private int sellPrice = 2;

        public string Id => id;
        public string DisplayName => displayName;
        public int UnitCost => unitCost;
        public int SellPrice => sellPrice;
    }
}
