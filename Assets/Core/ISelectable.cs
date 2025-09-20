using UnityEngine;

namespace TavernSim.Core
{
    /// <summary>
    /// Represents an entity that can be selected by the player in debug tooling.
    /// </summary>
    public interface ISelectable
    {
        string DisplayName { get; }

        Transform Transform { get; }
    }
}
