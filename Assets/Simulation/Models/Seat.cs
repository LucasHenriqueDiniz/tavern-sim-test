using UnityEngine;

namespace TavernSim.Simulation.Models
{
    /// <summary>
    /// Represents a seat in the tavern that can be occupied by a customer.
    /// </summary>
    public sealed class Seat
    {
        public int Id { get; }
        public Transform Anchor { get; }
        public bool Occupied { get; private set; }

        public Seat(int id, Transform anchor)
        {
            Id = id;
            Anchor = anchor;
        }

        public void SetOccupied(bool occupied)
        {
            Occupied = occupied;
        }
    }
}
