using System.Collections.Generic;
using TavernSim.Core.Simulation;
using TavernSim.Simulation.Models;

using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Keeps track of table occupancy and seat availability.
    /// </summary>
    public sealed class TableRegistry : ISimSystem
    {
        private readonly List<Table> _tables = new List<Table>(16);

        public IReadOnlyList<Table> Tables => _tables;

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void LateTick(float deltaTime)
        {
        }

        public void RegisterTable(Table table)
        {
            if (!_tables.Contains(table))
            {
                _tables.Add(table);
            }
        }

        public bool TryReserveSeat(out Table table, out Seat seat)
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].HasFreeSeat(out seat))
                {
                    seat.SetOccupied(true);
                    table = _tables[i];
                    return true;
                }
            }

            table = null;
            seat = null;
            return false;
        }

        public void ReleaseSeat(int tableId, int seatId)
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].Id == tableId)
                {
                    var seats = _tables[i].Seats;
                    for (int j = 0; j < seats.Count; j++)
                    {
                        if (seats[j].Id == seatId)
                        {
                            seats[j].SetOccupied(false);
                            return;
                        }
                    }
                }
            }
        }

        public Table GetTable(int tableId)
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].Id == tableId)
                {
                    return _tables[i];
                }
            }

            return null;
        }

        public bool HasAnySeat()
        {
            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].HasFreeSeat(out _))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _tables.Clear();
        }
    }
}
