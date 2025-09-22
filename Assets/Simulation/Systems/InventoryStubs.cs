using TavernSim.Domain;

namespace TavernSim.Simulation.Systems
{
    // Implementação infinita para MVP/DevBootstrap.
    public sealed class InfiniteInventory : IInventoryService
    {
        public bool CanCraft(RecipeSO recipe) => true;
        public bool TryConsume(RecipeSO recipe) => true;
    }
}
