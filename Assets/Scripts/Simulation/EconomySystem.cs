// Scripts/Simulation/EconomySystem.cs
public sealed class EconomySystem : ISimSystem {
    public float Cash { get; private set; } = 500f;
    public void AddRevenue(float v) => Cash += v;
    public bool TrySpend(float v) { if (Cash < v) return false; Cash -= v; return true; }
    public void Tick(float dt) { /* salários por minuto, etc. */ }
}
