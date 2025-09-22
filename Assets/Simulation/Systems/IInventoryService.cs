using TavernSim.Domain;

namespace TavernSim.Simulation.Systems
{
    // Interface extraída para uso compartilhado (AgentSystem, DevBootstrap, etc.)
    public interface IInventoryService
    {
        bool CanCraft(RecipeSO recipe);
        bool TryConsume(RecipeSO recipe);
    }
}
