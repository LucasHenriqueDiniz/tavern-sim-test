using TavernSim.Domain;

namespace TavernSim.Simulation.Systems
{
    public interface IInventoryService
    {
        bool CanCraft(RecipeSO recipe);
        bool TryConsume(RecipeSO recipe);
    }

    /// <summary>Inventário infinito para dev: sempre permite craft/consumo.</summary>
    public sealed class DevInfiniteInventory : IInventoryService
    {
        public bool CanCraft(RecipeSO recipe) => recipe != null;
        public bool TryConsume(RecipeSO recipe) => recipe != null;
    }

}
