using System;

namespace TavernSim.Core.Simulation
{
    /// <summary>
    /// Defines the contract for a deterministic simulation system. Implementations must avoid allocations during Tick
    /// and should produce deterministic results for the same input state.
    /// </summary>
    public interface ISimSystem : IDisposable
    {
        void Initialize(Simulation simulation);

        void Tick(float deltaTime);

        void LateTick(float deltaTime);
    }
}
