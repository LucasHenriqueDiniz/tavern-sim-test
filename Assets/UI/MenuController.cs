using UnityEngine;
using TavernSim.Domain;

namespace TavernSim.UI
{
    // Política mínima para o AgentSystem consultar.
    public interface IMenuPolicy
    {
        bool IsAllowed(RecipeSO recipe);
    }

    // Stub simples: por padrão permite tudo (HUD real pode vir depois).
    public sealed class MenuController : MonoBehaviour, IMenuPolicy
    {
        [SerializeField] private Catalog catalog;
        public bool IsAllowed(RecipeSO recipe) => recipe != null;
    }

}
