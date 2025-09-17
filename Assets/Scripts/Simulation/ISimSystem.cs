using System.Collections.Generic;
using UnityEngine;

// Scripts/Simulation/ISimSystem.cs
public interface ISimSystem
{
    void Tick(float dt);
}

// Scripts/Simulation/Simulation.cs
public sealed class Simulation
{
    private readonly List<ISimSystem> _systems = new();

    public void Add(ISimSystem system) => _systems.Add(system);

    public void Tick(float dt)
    {
        foreach (var system in _systems)
        {
            system.Tick(dt);
        }
    }
}

// Scripts/Core/SimulationRunner.cs
public sealed class SimulationRunner : MonoBehaviour
{
    [SerializeField]
    private float simHz = 10f;

    private Simulation _simulation;
    private float _accumulator;

    private void Awake()
    {
        _simulation = new Simulation();
        _simulation.Add(new EconomySystem());
        _simulation.Add(new OrderSystem());
        _simulation.Add(new AgentSystem());
        _simulation.Add(new CleaningSystem());
    }

    private void FixedUpdate()
    {
        _accumulator += Time.fixedDeltaTime;
        float step = 1f / simHz;
        while (_accumulator >= step)
        {
            _simulation.Tick(step);
            _accumulator -= step;
        }
    }
}
