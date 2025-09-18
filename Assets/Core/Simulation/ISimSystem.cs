namespace TavernSim.Core.Simulation
{
    public interface ISimSystem
    {
        void Initialize(Simulation simulation);
        void Tick(float deltaTime);
        void LateTick(float deltaTime);
        void Dispose();
    }
}
