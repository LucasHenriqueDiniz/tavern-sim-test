// Scripts/Domain/ItemSO.cs
using UnityEngine;
[CreateAssetMenu(menuName="Tavern/Item")]
public sealed class ItemSO : ScriptableObject {
    public string id;
    public string displayName;
    public float unitCost;        // custo para comprar
    public float sellPrice;       // preço de venda
}

// Scripts/Domain/RecipeSO.cs
[CreateAssetMenu(menuName="Tavern/Recipe")]
public sealed class RecipeSO : ScriptableObject {
    public string id;
    public string displayName;
    public ItemSO[] ingredients;  // itens de estoque
    public float prepTime;        // segundos
}
