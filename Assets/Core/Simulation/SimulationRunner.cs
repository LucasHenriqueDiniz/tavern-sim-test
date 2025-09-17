using System.Collections.Generic;
using UnityEngine;
using TavernSim.Simulation.Systems;
using TavernSim.Domain;

namespace TavernSim.Core.Simulation
{
    /// <summary>
    /// MonoBehaviour bootstrap for the deterministic simulation. Runs the Simulation in FixedUpdate at 10 Hz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private float timeStep = 0.1f;

        private Simulation _simulation;
        private readonly List<ISimSystem> _registeredSystems = new List<ISimSystem>(16);

        public Simulation Simulation => _simulation;

        private void Awake()
        {
            Time.fixedDeltaTime = timeStep;
            _simulation = new Simulation(timeStep);
        }

        public void RegisterSystem(ISimSystem system, bool lateTick = false)
        {
            if (_registeredSystems.Contains(system))
            {
                return;
            }

            _registeredSystems.Add(system);
            _simulation.AddSystem(system, lateTick);
        }

        private void FixedUpdate()
        {
            if (_simulation == null)
            {
                return;
            }

            _simulation.Tick();
        }

        private void OnDestroy()
        {
            if (_simulation == null)
            {
                return;
            }

            _simulation.Dispose();
            _simulation = null;
            _registeredSystems.Clear();
        }
    }
}
