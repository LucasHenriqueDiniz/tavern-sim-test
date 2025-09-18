using System.Collections.Generic;
using UnityEngine;
using TavernSim.Core.Simulation;
using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    public sealed class OrderSystem : ISimSystem
    {
        private readonly Queue<Order> _pending = new(32);
        private readonly List<Order> _inPrep = new(32);
        private int _maxStations = 1;

        public event System.Action<IReadOnlyList<Order>> OrdersChanged;

        public void SetKitchenStations(int count) => _maxStations = Mathf.Max(1, count);

        public void EnqueueOrder(int tableId, TavernSim.Domain.RecipeSO recipe)
        {
            if (recipe == null)
            {
                return;
            }

            _pending.Enqueue(new Order(tableId, recipe, recipe.PrepTime));
            OrdersChanged?.Invoke(GetOrders());
        }

        public bool TryConsumeReadyOrder(int tableId, out TavernSim.Domain.RecipeSO recipe)
        {
            for (int i = 0; i < _inPrep.Count; i++)
            {
                if (_inPrep[i].TableId == tableId && _inPrep[i].Remaining <= 0f)
                {
                    recipe = _inPrep[i].Recipe;
                    _inPrep.RemoveAt(i);
                    OrdersChanged?.Invoke(GetOrders());
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        public IReadOnlyList<Order> GetOrders() => _inPrep;

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float dt)
        {
            while (_inPrep.Count < _maxStations && _pending.Count > 0)
            {
                _inPrep.Add(_pending.Dequeue());
            }

            bool changed = false;
            for (int i = 0; i < _inPrep.Count; i++)
            {
                float previous = _inPrep[i].Remaining;
                _inPrep[i].Remaining = Mathf.Max(0f, previous - dt);
                changed |= _inPrep[i].Remaining != previous;
            }

            if (changed)
            {
                OrdersChanged?.Invoke(GetOrders());
            }
        }

        public void LateTick(float dt)
        {
        }

        public void Dispose()
        {
            _pending.Clear();
            _inPrep.Clear();
        }
    }

    public sealed class Order
    {
        public int TableId { get; }
        public TavernSim.Domain.RecipeSO Recipe { get; }
        public float Remaining { get; set; }

        public Order(int tableId, TavernSim.Domain.RecipeSO recipe, float prepTime)
        {
            TableId = tableId;
            Recipe = recipe;
            Remaining = Mathf.Max(0f, prepTime);
        }
    }
}
