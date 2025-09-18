using UnityEngine;
using TavernSim.Core.Simulation;
using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    public sealed class EconomySystem : ISimSystem
    {
        public float Cash { get; private set; }
        private readonly float _overheadPerGameMinute;
        private float _minuteAccumulator;

        public event System.Action<float> CashChanged;

        public EconomySystem(float initialCash, float overheadPerMinute)
        {
            Cash = initialCash;
            _overheadPerGameMinute = Mathf.Max(0f, overheadPerMinute);
        }

        public void AddRevenue(float v)
        {
            if (v <= 0f)
            {
                return;
            }

            Cash += v;
            CashChanged?.Invoke(Cash);
        }

        public bool TrySpend(float v)
        {
            if (v < 0f)
            {
                return true;
            }

            if (Cash < v)
            {
                return false;
            }

            Cash -= v;
            CashChanged?.Invoke(Cash);
            return true;
        }

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float dt)
        {
            _minuteAccumulator += dt;
            if (_minuteAccumulator >= 60f)
            {
                _minuteAccumulator -= 60f;
                if (_overheadPerGameMinute > 0f)
                {
                    Cash = Mathf.Max(0f, Cash - _overheadPerGameMinute);
                    CashChanged?.Invoke(Cash);
                }
            }
        }

        public void LateTick(float dt)
        {
        }

        public void Dispose()
        {
        }
    }
}
