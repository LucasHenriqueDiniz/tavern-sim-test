using System.Collections.Generic;
using TavernSim.Core.Simulation;
using TavernSim.Simulation.Models;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Accumulates dirt on tables and provides a way for agents to clean them.
    /// </summary>
    public sealed class CleaningSystem : ISimSystem
    {
        private readonly List<Table> _tables = new List<Table>(8);
        private readonly float _dirtPerSecond;

        public CleaningSystem(float dirtPerSecond)
        {
            _dirtPerSecond = dirtPerSecond;
        }

        public void RegisterTable(Table table)
        {
            if (!_tables.Contains(table))
            {
                _tables.Add(table);
            }
        }

        public void Initialize(Simulation simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                _tables[i].AccumulateDirt(_dirtPerSecond * deltaTime);
            }
        }

        public void LateTick(float deltaTime)
        {
        }

        public void CleanTable(int tableId)
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].Id == tableId)
                {
                    _tables[i].Clean();
                    break;
                }
            }
        }

        public void Dispose()
        {
            _tables.Clear();
        }
    }
}
