using TavernSim.Domain;

namespace TavernSim.Simulation.Models
{
    /// <summary>
    /// Represents an order that is being processed in the kitchen.
    /// </summary>
    public readonly struct Order
    {
        public readonly int TableId;
        public readonly RecipeSO Recipe;
        public readonly float Remaining;

        public Order(int tableId, RecipeSO recipe, float remaining)
        {
            TableId = tableId;
            Recipe = recipe;
            Remaining = remaining;
        }

        public Order WithRemaining(float remaining)
        {
            return new Order(TableId, Recipe, remaining);
        }
    }
}
