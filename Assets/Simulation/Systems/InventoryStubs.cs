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

    /// <summary>Validador simples de pedido (menu + estoque).</summary>
    public static class OrderRequestValidator
    {
        public static bool IsAllowed(TavernSim.UI.IMenuPolicy menu, IInventoryService inv, RecipeSO recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (menu != null && !menu.IsAllowed(recipe))
            {
                return false;
            }

            if (inv != null && !inv.CanCraft(recipe))
            {
                return false;
            }

            return true;
        }
    }
}
