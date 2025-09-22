using TavernSim.Domain;
using TavernSim.UI;

namespace TavernSim.Simulation.Systems
{
    public static class OrderRequestValidator
    {
        public static bool IsAllowed(IMenuPolicy menuPolicy, IInventoryService inventory, RecipeSO recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (menuPolicy != null && !menuPolicy.IsAllowed(recipe))
            {
                return false;
            }

            if (inventory != null && !inventory.CanCraft(recipe))
            {
                return false;
            }

            return true;
        }
    }
}
