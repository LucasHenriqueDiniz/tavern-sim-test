using System;
using System.Collections.Generic;

namespace TavernSim.Core.Simulation
{
    /// <summary>
    /// Holds the deterministic systems and provides an execution order for the simulation step.
    /// </summary>
    public sealed class Simulation : IDisposable
    {
        private readonly List<ISimSystem> _systems = new List<ISimSystem>(16);
        private readonly List<ISimSystem> _lateSystems = new List<ISimSystem>(16);

        public float TimeStep { get; }
        public float ElapsedTime { get; private set; }

        public Simulation(float timeStep)
        {
            TimeStep = timeStep;
        }

        public void AddSystem(ISimSystem system, bool runInLateTick = false)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (runInLateTick)
            {
                _lateSystems.Add(system);
            }
            else
            {
                _systems.Add(system);
            }
            system.Initialize(this);
        }

        public void Tick()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Tick(TimeStep);
            }

            for (int i = 0; i < _lateSystems.Count; i++)
            {
                _lateSystems[i].LateTick(TimeStep);
            }

            ElapsedTime += TimeStep;
        }

        public void Dispose()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Dispose();
            }

            for (int i = 0; i < _lateSystems.Count; i++)
            {
                if (!_systems.Contains(_lateSystems[i]))
                {
                    _lateSystems[i].Dispose();
                }
            }
            _systems.Clear();
            _lateSystems.Clear();
        }
    }
}
