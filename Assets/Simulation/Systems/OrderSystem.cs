using System;
using System.Collections.Generic;
using UnityEngine;
using TavernSim.Core.Events;
using TavernSim.Core.Simulation;
using TavernSim.Domain;

using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    public enum PrepArea
    {
        Kitchen = 0,
        Bar = 1
    }

    public enum OrderState
    {
        Queued = 0,
        InPreparation = 1
    }

    public sealed class OrderSystem : ISimSystem
    {
        private const string DefaultKitchenKeyword = "cozinha";
        private static readonly string[] BarKeywords =
        {
            "bar",
            "drink",
            "bebida",
            "cerveja",
            "beer",
            "wine",
            "ale",
            "coquetel",
            "cocktail"
        };

        private readonly Queue<Order> _kitchenQueue = new Queue<Order>(32);
        private readonly Queue<Order> _barQueue = new Queue<Order>(16);
        private readonly List<Order> _kitchenInPrep = new List<Order>(32);
        private readonly List<Order> _barInPrep = new List<Order>(16);
        private readonly List<Order> _ordersView = new List<Order>(48);

        private int _kitchenStations = 1;
        private int _barStations = 1;
        private bool _viewDirty;

        private IEventBus _eventBus;

        public event Action<IReadOnlyList<Order>> OrdersChanged;

        public void SetEventBus(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void SetKitchenStations(int count)
        {
            _kitchenStations = Mathf.Max(1, count);
        }

        public void SetBarStations(int count)
        {
            _barStations = Mathf.Max(1, count);
        }

        public PrepArea EnqueueOrder(int tableId, RecipeSO recipe)
        {
            if (recipe == null)
            {
                return PrepArea.Kitchen;
            }

            var area = ResolveArea(recipe);
            var order = new Order(tableId, recipe, recipe.PrepTime, area);
            GetQueue(area).Enqueue(order);
            _viewDirty = true;
            RaiseOrdersChanged();
            return area;
        }

        public bool TryConsumeReadyOrder(int tableId, out RecipeSO recipe, out PrepArea area)
        {
            if (TryConsumeFromList(_kitchenInPrep, tableId, out recipe, out area))
            {
                _viewDirty = true;
                RaiseOrdersChanged();
                return true;
            }

            if (TryConsumeFromList(_barInPrep, tableId, out recipe, out area))
            {
                _viewDirty = true;
                RaiseOrdersChanged();
                return true;
            }

            recipe = null;
            area = PrepArea.Kitchen;
            return false;
        }

        public IReadOnlyList<Order> GetOrders()
        {
            if (_viewDirty)
            {
                RebuildOrdersView();
            }

            return _ordersView;
        }

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float dt)
        {
            bool changed = false;

            changed |= FillStations(_kitchenQueue, _kitchenInPrep, _kitchenStations);
            changed |= FillStations(_barQueue, _barInPrep, _barStations);

            changed |= ProgressOrders(_kitchenInPrep, dt);
            changed |= ProgressOrders(_barInPrep, dt);

            if (changed)
            {
                RaiseOrdersChanged();
            }
        }

        public void LateTick(float dt)
        {
        }

        public void Dispose()
        {
            _kitchenQueue.Clear();
            _barQueue.Clear();
            _kitchenInPrep.Clear();
            _barInPrep.Clear();
            _ordersView.Clear();
            _viewDirty = true;
            OrdersChanged = null;
        }

        private bool TryConsumeFromList(List<Order> list, int tableId, out RecipeSO recipe, out PrepArea area)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var order = list[i];
                if (order.TableId != tableId || !order.IsReady)
                {
                    continue;
                }

                recipe = order.Recipe;
                area = order.Area;
                list.RemoveAt(i);
                return true;
            }

            recipe = null;
            area = PrepArea.Kitchen;
            return false;
        }

        private bool FillStations(Queue<Order> queue, List<Order> active, int stationLimit)
        {
            bool changed = false;
            while (active.Count < stationLimit && queue.Count > 0)
            {
                var order = queue.Dequeue();
                order.MarkInPreparation();
                active.Add(order);
                changed = true;
            }

            if (changed)
            {
                _viewDirty = true;
            }

            return changed;
        }

        private bool ProgressOrders(List<Order> orders, float deltaTime)
        {
            bool changed = false;
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.IsReady)
                {
                    continue;
                }

                float previous = order.Remaining;
                order.Remaining = Mathf.Max(0f, previous - deltaTime);
                if (!Mathf.Approximately(previous, order.Remaining))
                {
                    changed = true;
                }

                if (!order.IsReady && order.Remaining <= 0f)
                {
                    order.MarkReady();
                    PublishOrderReady(order);
                    changed = true;
                }
            }

            return changed;
        }

        private void PublishOrderReady(Order order)
        {
            if (_eventBus == null)
            {
                return;
            }

            var data = new Dictionary<string, object>
            {
                ["tableId"] = order.TableId,
                ["recipeId"] = order.Recipe != null ? order.Recipe.Id : string.Empty,
                ["area"] = order.Area.ToString()
            };
            var areaLabel = order.Area.GetDisplayName();
            var recipeName = order.Recipe != null ? order.Recipe.DisplayName : "Desconhecido";
            var message = $"Pedido pronto ({areaLabel}) - Mesa {order.TableId}: {recipeName}";
            _eventBus.Publish(new GameEvent("OrderReady", message, GameEventSeverity.Info, data));
        }

        private void RaiseOrdersChanged()
        {
            if (_viewDirty)
            {
                RebuildOrdersView();
            }

            OrdersChanged?.Invoke(_ordersView);
        }

        private void RebuildOrdersView()
        {
            _ordersView.Clear();
            _ordersView.AddRange(_kitchenInPrep);
            _ordersView.AddRange(_barInPrep);
            foreach (var order in _kitchenQueue)
            {
                _ordersView.Add(order);
            }

            foreach (var order in _barQueue)
            {
                _ordersView.Add(order);
            }

            _viewDirty = false;
        }

        private Queue<Order> GetQueue(PrepArea area)
        {
            return area == PrepArea.Bar ? _barQueue : _kitchenQueue;
        }

        private static PrepArea ResolveArea(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return PrepArea.Kitchen;
            }

            var source = (recipe.DisplayName + " " + recipe.Id).ToLowerInvariant();
            for (int i = 0; i < BarKeywords.Length; i++)
            {
                if (source.Contains(BarKeywords[i]))
                {
                    return PrepArea.Bar;
                }
            }

            if (source.Contains(DefaultKitchenKeyword))
            {
                return PrepArea.Kitchen;
            }

            return PrepArea.Kitchen;
        }
    }

    public sealed class Order
    {
        public int TableId { get; }
        public RecipeSO Recipe { get; }
        public PrepArea Area { get; }
        public OrderState State { get; private set; }
        public float Remaining { get; set; }
        public bool IsReady { get; private set; }

        public Order(int tableId, RecipeSO recipe, float prepTime, PrepArea area)
        {
            TableId = tableId;
            Recipe = recipe;
            Area = area;
            Remaining = Mathf.Max(0f, prepTime);
            State = OrderState.Queued;
            IsReady = false;
        }

        public void MarkInPreparation()
        {
            State = OrderState.InPreparation;
        }

        public void MarkReady()
        {
            IsReady = true;
        }
    }

    public static class PrepAreaExtensions
    {
        public static string GetDisplayName(this PrepArea area)
        {
            return area == PrepArea.Bar ? "Bar" : "Cozinha";
        }
    }
}
