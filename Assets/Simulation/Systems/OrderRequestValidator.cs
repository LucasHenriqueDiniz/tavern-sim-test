using TavernSim.Domain;

namespace TavernSim.Simulation.Systems
{
    public enum OrderBlockReason
    {
        None = 0,
        MenuPolicy = 1,
        InventoryUnavailable = 2
    }

    public readonly struct OrderValidationResult
    {
        public bool IsAllowed { get; }
        public OrderBlockReason Reason { get; }

        public OrderValidationResult(bool isAllowed, OrderBlockReason reason)
        {
            IsAllowed = isAllowed;
            Reason = reason;
        }

        public static OrderValidationResult Allowed => new OrderValidationResult(true, OrderBlockReason.None);
        public static OrderValidationResult MenuBlocked => new OrderValidationResult(false, OrderBlockReason.MenuPolicy);
        public static OrderValidationResult InventoryBlocked => new OrderValidationResult(false, OrderBlockReason.InventoryUnavailable);
    }

    public interface IMenuPolicy
    {
        bool IsAllowed(RecipeSO recipe);
    }

    public interface IInventoryService
    {
        bool CanCraft(RecipeSO recipe);
        bool TryConsume(RecipeSO recipe);
    }

    public sealed class OrderRequestValidator
    {
        private readonly IMenuPolicy _menuPolicy;
        private readonly IInventoryService _inventory;

        public OrderRequestValidator(IMenuPolicy menuPolicy, IInventoryService inventory)
        {
            _menuPolicy = menuPolicy;
            _inventory = inventory;
        }

        public OrderValidationResult Validate(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return OrderValidationResult.Allowed;
            }

            if (_menuPolicy != null && !_menuPolicy.IsAllowed(recipe))
            {
                return OrderValidationResult.MenuBlocked;
            }

            if (_inventory == null)
            {
                return OrderValidationResult.Allowed;
            }

            if (!_inventory.CanCraft(recipe))
            {
                return OrderValidationResult.InventoryBlocked;
            }

            if (!_inventory.TryConsume(recipe))
            {
                return OrderValidationResult.InventoryBlocked;
            }

            return OrderValidationResult.Allowed;
        }
    }
}
