using System;
using TavernSim.Core.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Handles all money related simulation logic such as recurring costs and revenue.
    /// </summary>
    public sealed class EconomySystem : ISimSystem
    {
        private readonly float _overheadPerMinute;
        private float _overheadTimer;

        public float Cash { get; private set; }
        public event Action<float> CashChanged;

        public EconomySystem(float startingCash, float overheadPerMinute)
        {
            Cash = startingCash;
            _overheadPerMinute = overheadPerMinute;
        }

        public void Initialize(Simulation simulation)
        {
            CashChanged?.Invoke(Cash);
        }

        public void Tick(float deltaTime)
        {
            _overheadTimer += deltaTime;
            if (_overheadTimer >= 60f)
            {
                _overheadTimer -= 60f;
                TrySpend(_overheadPerMinute);
            }
        }

        public void LateTick(float deltaTime)
        {
        }

        public bool TrySpend(float amount)
        {
            if (Cash < amount)
            {
                return false;
            }

            Cash -= amount;
            CashChanged?.Invoke(Cash);
            return true;
        }

        public void AddRevenue(float amount)
        {
            Cash += amount;
            CashChanged?.Invoke(Cash);
        }

        public void Dispose()
        {
            CashChanged = null;
        }
    }
}
