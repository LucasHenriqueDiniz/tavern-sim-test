using System.Collections.Generic;
using UnityEngine;

namespace TavernSim.Simulation.Models
{
    /// <summary>
    /// Represents a table with multiple seats and cleanliness state.
    /// </summary>
    public sealed class Table
    {
        private readonly List<Seat> _seats = new List<Seat>(4);

        public int Id { get; }
        public Transform Transform { get; }
        public float Dirtiness { get; private set; }

        public IReadOnlyList<Seat> Seats => _seats;

        public Table(int id, Transform transform)
        {
            Id = id;
            Transform = transform;
        }

        public void AddSeat(Seat seat)
        {
            if (!_seats.Contains(seat))
            {
                _seats.Add(seat);
            }
        }

        public bool HasFreeSeat(out Seat seat)
        {
            for (int i = 0; i < _seats.Count; i++)
            {
                if (!_seats[i].Occupied)
                {
                    seat = _seats[i];
                    return true;
                }
            }

            seat = null;
            return false;
        }

        public void AccumulateDirt(float amount)
        {
            Dirtiness = Mathf.Max(0f, Dirtiness + amount);
        }

        public void Clean()
        {
            Dirtiness = 0f;
        }
    }
}
