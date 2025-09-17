using System;
using System.Collections.Generic;
using UnityEngine;
using TavernSim.Core.Simulation;
using TavernSim.Domain;
using TavernSim.Simulation.Models;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Tracks orders placed by customers and advances their preparation timers.
    /// </summary>
    public sealed class OrderSystem : ISimSystem
    {
        private readonly List<Order> _activeOrders = new List<Order>(16);
        private readonly List<Order> _buffer = new List<Order>(16);

        public event Action<IReadOnlyList<Order>> OrdersChanged;

        public void Initialize(Simulation simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            if (_activeOrders.Count == 0)
            {
                return;
            }

            _buffer.Clear();
            for (int i = 0; i < _activeOrders.Count; i++)
            {
                var order = _activeOrders[i];
                var remaining = Mathf.Max(0f, order.Remaining - deltaTime);
                _buffer.Add(order.WithRemaining(remaining));
            }

            _activeOrders.Clear();
            _activeOrders.AddRange(_buffer);
            OrdersChanged?.Invoke(_activeOrders);
        }

        public void LateTick(float deltaTime)
        {
        }

        public void EnqueueOrder(int tableId, RecipeSO recipe)
        {
            var order = new Order(tableId, recipe, recipe.PrepTime);
            _activeOrders.Add(order);
            OrdersChanged?.Invoke(_activeOrders);
        }

        public bool TryConsumeReadyOrder(int tableId, out RecipeSO recipe)
        {
            for (int i = 0; i < _activeOrders.Count; i++)
            {
                if (_activeOrders[i].TableId == tableId && _activeOrders[i].Remaining <= 0f)
                {
                    recipe = _activeOrders[i].Recipe;
                    _activeOrders.RemoveAt(i);
                    OrdersChanged?.Invoke(_activeOrders);
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        public IReadOnlyList<Order> GetOrders() => _activeOrders;

        public void Dispose()
        {
            _activeOrders.Clear();
            _buffer.Clear();
            OrdersChanged = null;
        }
    }
}
