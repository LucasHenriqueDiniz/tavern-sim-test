using UnityEngine;
using TavernSim.Core;
using TavernSim.Simulation.Models;

namespace TavernSim.Building
{
    /// <summary>
    /// Exposes table data through the selection system so the HUD can display details.
    /// </summary>
    public sealed class TablePresenter : MonoBehaviour, ISelectable
    {
        private Table _table;

        public string DisplayName => _table != null ? $"Mesa {_table.Id}" : gameObject.name;

        public Transform Transform => transform;

        public Table Table => _table;

        public int TableId => _table != null ? _table.Id : -1;

        public int SeatCount
        {
            get
            {
                if (_table == null)
                {
                    return 0;
                }

                var seats = _table.Seats;
                return seats != null ? seats.Count : 0;
            }
        }

        public int OccupiedSeats
        {
            get
            {
                if (_table == null)
                {
                    return 0;
                }

                var seats = _table.Seats;
                if (seats == null)
                {
                    return 0;
                }

                var occupied = 0;
                for (int i = 0; i < seats.Count; i++)
                {
                    if (seats[i].Occupied)
                    {
                        occupied++;
                    }
                }

                return occupied;
            }
        }

        public float Dirtiness => _table != null ? _table.Dirtiness : 0f;

        public void Initialize(Table table)
        {
            _table = table;
        }
    }
}
