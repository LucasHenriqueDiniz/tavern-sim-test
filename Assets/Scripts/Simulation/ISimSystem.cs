// Scripts/Simulation/ISimSystem.cs
public interface ISimSystem { void Tick(float dt); }

// Scripts/Simulation/Simulation.cs
public sealed class Simulation {
    private readonly List<ISimSystem> _systems = new();
    public void Add(ISimSystem s) => _systems.Add(s);
    public void Tick(float dt) { foreach (var s in _systems) s.Tick(dt); }
}

// Scripts/Core/SimulationRunner.cs
using UnityEngine;
public sealed class SimulationRunner : MonoBehaviour {
    [SerializeField] float simHz = 10f;
    Simulation _sim; float _acc;
    void Awake() {
        _sim = new Simulation();
        _sim.Add(new EconomySystem());
        _sim.Add(new OrderSystem());
        _sim.Add(new AgentSystem());
        _sim.Add(new CleaningSystem());
    }
    void FixedUpdate() {
        _acc += Time.fixedDeltaTime;
        float step = 1f / simHz;
        while (_acc >= step) { _sim.Tick(step); _acc -= step; }
    }
}
