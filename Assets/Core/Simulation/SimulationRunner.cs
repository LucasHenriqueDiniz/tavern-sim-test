using UnityEngine;

namespace TavernSim.Core.Simulation
{
    [DefaultExecutionOrder(-100)]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private float simHz = 10f;

        private readonly Simulation _simulation = new();
        private float _accumulator;

        public void RegisterSystem(ISimSystem system) => _simulation.Register(system);

        private void Start()
        {
            _simulation.Initialize();
        }

        private void OnDestroy()
        {
            _simulation.Dispose();
        }

        private void FixedUpdate()
        {
            _accumulator += Time.fixedDeltaTime;
            float step = 1f / Mathf.Max(1f, simHz);

            while (_accumulator >= step)
            {
                _simulation.Tick(step);
                _accumulator -= step;
            }

            _simulation.LateTick(Time.fixedDeltaTime);
        }
    }
}
