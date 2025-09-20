using System.Collections.Generic;

namespace TavernSim.Core.Simulation
{
    public sealed class Simulation
    {
        private readonly List<ISimSystem> _systems = new(16);

        public void Register(ISimSystem system) => _systems.Add(system);

        public void Initialize()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Initialize(this);
            }
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Tick(dt);
            }
        }

        public void LateTick(float dt)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].LateTick(dt);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Dispose();
            }

            _systems.Clear();
        }
    }
}
