using UnityEngine;
using TavernSim.Domain;

namespace TavernSim.UI
{
    public interface IMenuPolicy
    {
        bool IsAllowed(RecipeSO recipe);
    }

    /// <summary>
    /// MVP: libera todos os itens; apenas para satisfazer dependências do AgentSystem e InventoryStubs.
    /// Pode ser expandido depois para UI Toolkit.
    /// </summary>
    public sealed class MenuController : MonoBehaviour, IMenuPolicy
    {
        public bool IsAllowed(RecipeSO recipe) => true;
    }
}
