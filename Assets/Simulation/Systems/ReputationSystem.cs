using System;
using UnityEngine;
using TavernSim.Core.Simulation;
using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Tracks tavern reputation and exposes change notifications for the HUD.
    /// </summary>
    public sealed class ReputationSystem : ISimSystem
    {
        private int _reputation;

        public ReputationSystem(int initialReputation = 50)
        {
            _reputation = Mathf.Clamp(initialReputation, 0, 100);
        }

        public event Action<int> ReputationChanged;

        public int Reputation => _reputation;

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void LateTick(float deltaTime)
        {
        }

        public void Dispose()
        {
        }

        public void Set(int value)
        {
            value = Mathf.Clamp(value, 0, 100);
            if (value == _reputation)
            {
                return;
            }

            _reputation = value;
            ReputationChanged?.Invoke(_reputation);
        }

        public void Add(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            Set(_reputation + delta);
        }

        public void Remove(int delta)
        {
            if (delta <= 0)
            {
                return;
            }

            Set(_reputation - delta);
        }
    }
}
