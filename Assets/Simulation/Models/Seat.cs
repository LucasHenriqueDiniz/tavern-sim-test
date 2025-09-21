using UnityEngine;

namespace TavernSim.Simulation.Models
{
    /// <summary>
    /// Represents a seat in the tavern that can be occupied by a customer.
    /// </summary>
    public sealed class Seat
    {
        public enum SeatKind
        {
            Single = 0,
            Bench = 1
        }

        public int Id { get; }
        public Transform Anchor { get; }
        public SeatKind Kind { get; }
        public bool Occupied { get; private set; }

        public Seat(int id, Transform anchor, SeatKind kind)
        {
            Id = id;
            Anchor = anchor;
            Kind = kind;
        }

        public void SetOccupied(bool occupied)
        {
            Occupied = occupied;
        }
    }
}
