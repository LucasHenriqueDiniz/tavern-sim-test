using TavernSim.Domain;

namespace TavernSim.UI
{
    /// <summary>
    /// Defines the contract used by gameplay systems to validate whether a recipe
    /// is currently available on the menu. Runtime callers should query this
    /// before enqueueing orders or spawning crafting jobs.
    /// </summary>
    public interface IMenuPolicy
    {
        bool IsAllowed(RecipeSO recipe);
    }
}
