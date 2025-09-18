// Scripts/Simulation/OrderSystem.cs
using System.Collections.Generic;

public sealed class OrderSystem : ISimSystem {
    readonly Queue<Order> _pending = new();
    readonly List<Order> _inPrep = new();
    public void Enqueue(Order o) => _pending.Enqueue(o);
    public void Tick(float dt) {
        // Move pending -> inPrep se houver estação livre
        // Avança tempo de preparo dos que estão em _inPrep
    }
}

public sealed class Order {
    public RecipeSO recipe;
    public float remaining;
    public int tableId;
}
