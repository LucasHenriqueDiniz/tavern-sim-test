using System;
using System.Collections.Generic;
using UnityEngine;
using TavernSim.Agents;
using TavernSim.Core.Simulation;
using TavernSim.Domain;
using TavernSim.Simulation.Models;

using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Orchestrates deterministic finite state machines for customers and waiters.
    /// </summary>
    public sealed class AgentSystem : ISimSystem
    {
        private const float DestinationThresholdSqr = 0.04f;

        private readonly TableRegistry _tableRegistry;
        private readonly OrderSystem _orderSystem;
        private readonly EconomySystem _economySystem;
        private readonly CleaningSystem _cleaningSystem;
        private readonly Catalog _catalog;

        private readonly List<CustomerData> _customers = new List<CustomerData>(16);
        private readonly List<WaiterData> _waiters = new List<WaiterData>(4);
        private readonly List<Customer> _customersNeedingOrder = new List<Customer>(16);
        private readonly List<int> _tablesNeedingClean = new List<int>(8);
        private readonly List<CustomerData> _despawnQueue = new List<CustomerData>(8);

        private Vector3 _entryPoint;
        private Vector3 _exitPoint;
        private Vector3 _kitchenPoint;
        private RecipeSO _defaultRecipe;

        public event Action<int> ActiveCustomerCountChanged;
        public event Action<Customer> CustomerReleased;

        public AgentSystem(TableRegistry tableRegistry, OrderSystem orderSystem, EconomySystem economySystem, CleaningSystem cleaningSystem, Catalog catalog)
        {
            _tableRegistry = tableRegistry;
            _orderSystem = orderSystem;
            _economySystem = economySystem;
            _cleaningSystem = cleaningSystem;
            _catalog = catalog;
        }

        public void Configure(Vector3 entryPoint, Vector3 exitPoint, Vector3 kitchenPoint)
        {
            _entryPoint = entryPoint;
            _exitPoint = exitPoint;
            _kitchenPoint = kitchenPoint;
        }

        public void Initialize(Sim simulation)
        {
            if (_catalog != null)
            {
                foreach (var pair in _catalog.Recipes)
                {
                    _defaultRecipe = pair.Value;
                    break;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            TickCustomers(deltaTime);
            ProcessDespawnQueue();
            TickWaiters(deltaTime);
        }

        public void LateTick(float deltaTime)
        {
        }

        public void Dispose()
        {
            _customers.Clear();
            _waiters.Clear();
            _customersNeedingOrder.Clear();
            _tablesNeedingClean.Clear();
            _despawnQueue.Clear();
            ActiveCustomerCountChanged = null;
            CustomerReleased = null;
        }

        public void RegisterWaiter(Waiter waiter)
        {
            if (waiter == null)
            {
                return;
            }

            var data = new WaiterData(waiter);
            _waiters.Add(data);
            UpdateWaiterIntent(data);
        }

        public void SpawnCustomer(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            var data = new CustomerData(customer)
            {
                State = CustomerState.Enter,
                StateTimer = 0f
            };
            customer.gameObject.SetActive(true);
            customer.transform.position = _entryPoint;
            customer.SetDestination(_entryPoint);
            _customers.Add(data);
            ActiveCustomerCountChanged?.Invoke(_customers.Count);
            UpdateCustomerIntent(data);
        }

        private void MarkForDespawn(CustomerData data)
        {
            if (!_despawnQueue.Contains(data))
            {
                _despawnQueue.Add(data);
            }
        }

        private void ProcessDespawnQueue()
        {
            if (_despawnQueue.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _despawnQueue.Count; i++)
            {
                var data = _despawnQueue[i];
                var index = _customers.IndexOf(data);
                if (index >= 0)
                {
                    _customers.RemoveAt(index);
                }

                _customersNeedingOrder.Remove(data.Agent);
                CustomerReleased?.Invoke(data.Agent);
                data.Agent.gameObject.SetActive(false);
            }

            _despawnQueue.Clear();
            ActiveCustomerCountChanged?.Invoke(_customers.Count);
        }

        private void TickCustomers(float deltaTime)
        {
            for (int i = 0; i < _customers.Count; i++)
            {
                var data = _customers[i];
                data.StateTimer += deltaTime;

                switch (data.State)
                {
                    case CustomerState.Enter:
                        HandleEnter(ref data);
                        break;
                    case CustomerState.FindTable:
                        HandleFindTable(ref data);
                        break;
                    case CustomerState.Sit:
                        HandleSit(ref data);
                        break;
                    case CustomerState.Order:
                        HandleOrder(ref data, deltaTime);
                        break;
                    case CustomerState.WaitDrink:
                        HandleWaitDrink(ref data, deltaTime);
                        break;
                    case CustomerState.Drink:
                        HandleDrink(ref data, deltaTime);
                        break;
                    case CustomerState.Pay:
                        HandlePay(ref data);
                        break;
                    case CustomerState.Leave:
                        HandleLeave(ref data);
                        break;
                }

                UpdateCustomerIntent(data);
                _customers[i] = data;
            }
        }

        private void TickWaiters(float deltaTime)
        {
            for (int i = 0; i < _waiters.Count; i++)
            {
                var data = _waiters[i];
                data.StateTimer += deltaTime;
                switch (data.State)
                {
                    case WaiterState.Idle:
                        HandleWaiterIdle(ref data);
                        break;
                    case WaiterState.TakeOrder:
                        HandleWaiterTakeOrder(ref data);
                        break;
                    case WaiterState.WaitPrep:
                        HandleWaiterWaitPrep(ref data);
                        break;
                    case WaiterState.Deliver:
                        HandleWaiterDeliver(ref data);
                        break;
                    case WaiterState.Clean:
                        HandleWaiterClean(ref data);
                        break;
                }

                UpdateWaiterIntent(data);
                _waiters[i] = data;
            }
        }

        private void HandleEnter(ref CustomerData data)
        {
            data.Agent.SetDestination(_entryPoint);
            if (data.Agent.HasReached(DestinationThresholdSqr) || data.StateTimer >= 1f)
            {
                data.State = CustomerState.FindTable;
                data.StateTimer = 0f;
            }
        }

        private void HandleFindTable(ref CustomerData data)
        {
            if (data.Table == null)
            {
                if (_tableRegistry.TryReserveSeat(out var table, out var seat))
                {
                    data.Table = table;
                    data.Seat = seat;
                    data.Agent.SetDestination(seat.Anchor.position);
                }
            }

            if (data.Seat != null && data.Agent.HasReached(DestinationThresholdSqr))
            {
                data.Agent.SitAt(data.Seat.Anchor);
                data.State = CustomerState.Sit;
                data.StateTimer = 0f;
            }
        }

        private void HandleSit(ref CustomerData data)
        {
            if (data.StateTimer >= 0.5f)
            {
                data.State = CustomerState.Order;
                data.StateTimer = 0f;
                data.WaitTimer = 0f;
                if (!_customersNeedingOrder.Contains(data.Agent))
                {
                    _customersNeedingOrder.Add(data.Agent);
                }
            }
        }

        private void HandleOrder(ref CustomerData data, float deltaTime)
        {
            data.WaitTimer += deltaTime;
        }

        private void HandleWaitDrink(ref CustomerData data, float deltaTime)
        {
            data.WaitTimer += deltaTime;
        }

        private void HandleDrink(ref CustomerData data, float deltaTime)
        {
            data.DrinkTimer += deltaTime;
            if (data.DrinkTimer >= 6f)
            {
                data.State = CustomerState.Pay;
                data.StateTimer = 0f;
            }
        }

        private void HandlePay(ref CustomerData data)
        {
            if (data.OrderedRecipe == null)
            {
                data.OrderedRecipe = _defaultRecipe;
            }

            if (data.OrderedRecipe != null)
            {
                var baseRevenue = data.OrderedRecipe.OutputItem != null ? data.OrderedRecipe.OutputItem.SellPrice : 6f;
                var cost = data.OrderedRecipe.OutputItem != null ? data.OrderedRecipe.OutputItem.UnitCost : 2f;
                var tip = CalculateTip(data.WaitTimer);
                _economySystem.TrySpend(cost);
                _economySystem.AddRevenue(baseRevenue + tip);
            }

            data.State = CustomerState.Leave;
            data.Agent.StandUp();
            data.Agent.SetDestination(_exitPoint);
            data.StateTimer = 0f;
            if (data.Table != null)
            {
                if (!_tablesNeedingClean.Contains(data.Table.Id))
                {
                    _tablesNeedingClean.Add(data.Table.Id);
                }
            }
        }

        private void HandleLeave(ref CustomerData data)
        {
            if (data.Agent.HasReached(DestinationThresholdSqr) || data.StateTimer > 10f)
            {
                if (data.Table != null && data.Seat != null)
                {
                    _tableRegistry.ReleaseSeat(data.Table.Id, data.Seat.Id);
                }

                data.Reset();
                MarkForDespawn(data);
            }
        }

        private void HandleWaiterIdle(ref WaiterData data)
        {
            if (_customersNeedingOrder.Count > 0)
            {
                var customer = _customersNeedingOrder[0];
                _customersNeedingOrder.RemoveAt(0);
                data.TargetCustomer = customer;
                if (customer != null)
                {
                    var customerData = FindCustomerData(customer);
                    if (customerData != null && customerData.Seat != null)
                    {
                        data.Agent.SetDestination(customerData.Seat.Anchor.position);
                        data.State = WaiterState.TakeOrder;
                    }
                }
                return;
            }

            if (_tablesNeedingClean.Count > 0)
            {
                data.TargetTableId = _tablesNeedingClean[0];
                _tablesNeedingClean.RemoveAt(0);
                var table = _tableRegistry.GetTable(data.TargetTableId);
                if (table != null)
                {
                    data.Agent.SetDestination(table.Transform.position);
                    data.State = WaiterState.Clean;
                }
                return;
            }

            data.Agent.SetDestination(_kitchenPoint);
        }

        private void HandleWaiterTakeOrder(ref WaiterData data)
        {
            if (data.TargetCustomer == null)
            {
                data.State = WaiterState.Idle;
                return;
            }

            if (!data.Agent.HasReached(DestinationThresholdSqr))
            {
                return;
            }

            var customerData = FindCustomerData(data.TargetCustomer);
            if (customerData == null)
            {
                data.State = WaiterState.Idle;
                return;
            }

            var recipe = customerData.OrderedRecipe ?? _defaultRecipe;
            if (recipe != null && customerData.Table != null)
            {
                _orderSystem.EnqueueOrder(customerData.Table.Id, recipe);
                customerData.State = CustomerState.WaitDrink;
                customerData.WaitTimer = 0f;
                customerData.OrderedRecipe = recipe;
                data.Agent.SetDestination(_kitchenPoint);
                data.State = WaiterState.WaitPrep;
            }
            else
            {
                data.State = WaiterState.Idle;
            }
        }

        private void HandleWaiterWaitPrep(ref WaiterData data)
        {
            if (!data.Agent.HasReached(DestinationThresholdSqr))
            {
                return;
            }

            if (data.TargetCustomer == null)
            {
                data.State = WaiterState.Idle;
                return;
            }

            var customerData = FindCustomerData(data.TargetCustomer);
            if (customerData == null || customerData.Table == null)
            {
                data.State = WaiterState.Idle;
                return;
            }

            if (_orderSystem.TryConsumeReadyOrder(customerData.Table.Id, out var recipe))
            {
                data.CarryingRecipe = recipe;
                data.Agent.SetDestination(customerData.Seat.Anchor.position);
                data.State = WaiterState.Deliver;
            }
        }

        private void HandleWaiterDeliver(ref WaiterData data)
        {
            if (data.TargetCustomer == null)
            {
                data.State = WaiterState.Idle;
                return;
            }

            if (!data.Agent.HasReached(DestinationThresholdSqr))
            {
                return;
            }

            var customerData = FindCustomerData(data.TargetCustomer);
            if (customerData != null && customerData.State == CustomerState.WaitDrink)
            {
                customerData.State = CustomerState.Drink;
                customerData.DrinkTimer = 0f;
            }

            data.CarryingRecipe = null;
            data.TargetCustomer = null;
            data.State = WaiterState.Idle;
        }

        private void HandleWaiterClean(ref WaiterData data)
        {
            if (!data.Agent.HasReached(DestinationThresholdSqr))
            {
                return;
            }

            if (data.TargetTableId >= 0)
            {
                _cleaningSystem.CleanTable(data.TargetTableId);
            }

            data.TargetTableId = -1;
            data.State = WaiterState.Idle;
        }

        private CustomerData FindCustomerData(Customer customer)
        {
            for (int i = 0; i < _customers.Count; i++)
            {
                if (_customers[i].Agent == customer)
                {
                    return _customers[i];
                }
            }

            return null;
        }

        private static float CalculateTip(float waitTime)
        {
            if (waitTime < 10f)
            {
                return 2f;
            }

            if (waitTime > 25f)
            {
                return 0f;
            }

            return Mathf.Lerp(2f, 0f, (waitTime - 10f) / 15f);
        }

        private static void UpdateCustomerIntent(CustomerData data)
        {
            data.Agent?.SetIntent(GetCustomerIntent(data.State));
        }

        private static void UpdateWaiterIntent(WaiterData data)
        {
            data.Agent?.SetIntent(GetWaiterIntent(data.State));
        }

        private static string GetCustomerIntent(CustomerState state)
        {
            switch (state)
            {
                case CustomerState.Enter:
                    return "Entrando";
                case CustomerState.FindTable:
                    return "Procurando mesa";
                case CustomerState.Sit:
                    return "Sentando";
                case CustomerState.Order:
                    return "Fazendo pedido";
                case CustomerState.WaitDrink:
                    return "Esperando bebida";
                case CustomerState.Drink:
                    return "Bebendo";
                case CustomerState.Pay:
                    return "Pagando";
                case CustomerState.Leave:
                    return "Saindo";
                default:
                    return string.Empty;
            }
        }

        private static string GetWaiterIntent(WaiterState state)
        {
            switch (state)
            {
                case WaiterState.Idle:
                    return "Disponível";
                case WaiterState.TakeOrder:
                    return "Anotando pedido";
                case WaiterState.WaitPrep:
                    return "Aguardando preparo";
                case WaiterState.Deliver:
                    return "Servindo";
                case WaiterState.Clean:
                    return "Limpando";
                default:
                    return string.Empty;
            }
        }

        private enum CustomerState
        {
            Enter,
            FindTable,
            Sit,
            Order,
            WaitDrink,
            Drink,
            Pay,
            Leave
        }

        private enum WaiterState
        {
            Idle,
            TakeOrder,
            WaitPrep,
            Deliver,
            Clean
        }

        private sealed class CustomerData
        {
            public Customer Agent { get; }
            public CustomerState State;
            public Table Table;
            public Seat Seat;
            public float StateTimer;
            public float WaitTimer;
            public float DrinkTimer;
            public RecipeSO OrderedRecipe;

            public CustomerData(Customer agent)
            {
                Agent = agent;
            }

            public void Reset()
            {
                State = CustomerState.Enter;
                Table = null;
                Seat = null;
                StateTimer = 0f;
                WaitTimer = 0f;
                DrinkTimer = 0f;
                OrderedRecipe = null;
            }
        }

        private sealed class WaiterData
        {
            public Waiter Agent { get; }
            public WaiterState State;
            public float StateTimer;
            public Customer TargetCustomer;
            public int TargetTableId = -1;
            public RecipeSO CarryingRecipe;

            public WaiterData(Waiter agent)
            {
                Agent = agent;
                State = WaiterState.Idle;
            }
        }
    }
}
