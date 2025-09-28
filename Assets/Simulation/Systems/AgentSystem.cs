using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TavernSim.Agents;
using TavernSim.Core;
using TavernSim.Core.Events;
using TavernSim.Core.Simulation;
using TavernSim.Domain;
using TavernSim.Simulation.Models;
using TavernSim.UI; // para IMenuPolicy
using TavernSim.Simulation.Systems; // para IInventoryService

using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Orchestrates deterministic finite state machines for customers and waiters.
    /// </summary>
    public sealed class AgentSystem : ISimSystem
    {
        private const float DestinationThresholdSqr = 0.04f;
        private const float TableSearchTimeout = 12f;
        private const float TableSearchRepathInterval = 1.5f;
        private const float MealDuration = 8f;
        private const float WaiterServiceOffset = 0.55f;

        private readonly TableRegistry _tableRegistry;
        private readonly OrderSystem _orderSystem;
        private readonly EconomySystem _economySystem;
        private readonly CleaningSystem _cleaningSystem;
        private readonly Catalog _catalog;
        private ReputationSystem _reputationSystem;

        private readonly List<CustomerData> _customers = new List<CustomerData>(16);
        private readonly List<WaiterData> _waiters = new List<WaiterData>(4);
        private readonly List<Customer> _customersNeedingOrder = new List<Customer>(16);
        private readonly List<int> _tablesNeedingClean = new List<int>(8);
        private readonly List<CustomerData> _despawnQueue = new List<CustomerData>(8);

        private Vector3 _entryPoint;
        private Vector3 _exitPoint;
        private Vector3 _kitchenPickupPoint;
        private Vector3 _barPickupPoint;
        private RecipeSO _defaultRecipe;
        private IMenuPolicy _menuPolicy;
        private IInventoryService _inventory;
        private IEventBus _eventBus;

        public event Action<int> ActiveCustomerCountChanged;
        public int ActiveCustomerCount => _customers.Count;
        public event Action<Customer> CustomerReleased;
        public event Action<Customer, string> CustomerLeftAngry;

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
            Configure(entryPoint, exitPoint, kitchenPoint, kitchenPoint, kitchenPoint);
        }

        public void Configure(Vector3 entryPoint, Vector3 exitPoint, Vector3 kitchenPoint, Vector3 kitchenPickupPoint, Vector3 barPickupPoint)
        {
            _entryPoint = entryPoint;
            _exitPoint = exitPoint;
            _kitchenPickupPoint = kitchenPickupPoint;
            _barPickupPoint = barPickupPoint;
        }

        public void SetMenuPolicy(IMenuPolicy menuPolicy)
        {
            _menuPolicy = menuPolicy;
        }

        public void SetInventory(IInventoryService inventory)
        {
            _inventory = inventory;
        }

        public void SetEventBus(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void SetReputationSystem(ReputationSystem reputationSystem)
        {
            _reputationSystem = reputationSystem;
        }

        public bool HasAvailableSeating()
        {
            return _tableRegistry != null && _tableRegistry.HasAnySeat();
        }

        public bool HasActiveWaiter()
        {
            return _waiters.Count > 0;
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
            CustomerLeftAngry = null;
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
            var navAgent = customer.Agent;
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.Warp(_entryPoint);
            }
            else
            {
                customer.transform.position = _entryPoint;
            }
            customer.SetDestination(_entryPoint);
            data.Favorite = _defaultRecipe;
            data.Gold = UnityEngine.Random.Range(8, 24);
            data.Patience = UnityEngine.Random.Range(10f, 25f);
            data.Name = $"Cliente {UnityEngine.Random.Range(1000, 9999)}";
            data.OrderBlocked = false;
            data.BlockReason = string.Empty;
            data.WaiterAssigned = false;
            customer.ApplyProfile(data.Name, data.Gold, data.Patience);
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
                        HandleFindTable(ref data, deltaTime);
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
                    case CustomerState.Eat:
                        HandleEat(ref data, deltaTime);
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
            data.LeftAngry = false;
            data.SearchTimer = 0f;
            if (data.Agent.HasReached(DestinationThresholdSqr) || data.StateTimer >= 1f)
            {
                data.State = CustomerState.FindTable;
                data.StateTimer = 0f;
            }
        }

        private void HandleFindTable(ref CustomerData data, float deltaTime)
        {
            if (data.Table == null)
            {
                if (_tableRegistry.TryReserveSeat(out var table, out var seat))
                {
                    data.Table = table;
                    data.Seat = seat;
                    data.SearchTimer = 0f;
                    data.LeftAngry = false;
                    data.Agent.SetDestination(seat.Anchor.position);
                }
                else
                {
                    data.SearchTimer += deltaTime;
                    if (data.SearchTimer >= TableSearchRepathInterval)
                    {
                        data.SearchTimer = 0f;
                        var offset = UnityEngine.Random.insideUnitCircle * 1.5f;
                        var wanderTarget = _entryPoint + new Vector3(offset.x, 0f, Mathf.Abs(offset.y) + 0.5f);
                        data.Agent.SetDestination(wanderTarget);
                    }

                    if (data.StateTimer >= TableSearchTimeout)
                    {
                        data.LeftAngry = true;
                        data.Agent.SetDestination(_exitPoint);
                        data.State = CustomerState.Leave;
                        data.StateTimer = 0f;
                        data.SearchTimer = 0f;
                        data.BlockReason = "Sem mesas disponíveis";
                        PublishCustomerAngry(data, data.BlockReason, -1);
                        CustomerLeftAngry?.Invoke(data.Agent, data.BlockReason);
                        _reputationSystem?.Add(-2);
                        return;
                    }
                }
            }

            if (data.Seat != null && data.Agent.HasReached(DestinationThresholdSqr))
            {
                data.Agent.SitAt(data.Seat.Anchor);
                data.State = CustomerState.Sit;
                data.StateTimer = 0f;
                data.SearchTimer = 0f;
            }
        }

        private void HandleSit(ref CustomerData data)
        {
            if (data.StateTimer >= 0.5f)
            {
                data.State = CustomerState.Order;
                data.StateTimer = 0f;
                data.WaitTimer = 0f;
                data.WaiterAssigned = false;
                data.OrderBlocked = false;
                data.BlockReason = string.Empty;
                if (!_customersNeedingOrder.Contains(data.Agent))
                {
                    _customersNeedingOrder.Add(data.Agent);
                }
            }
        }

        private void HandleOrder(ref CustomerData data, float deltaTime)
        {
            data.WaitTimer += deltaTime;
            data.TotalWaitTime += deltaTime;
            if (data.PendingRecipe == null)
            {
                data.PendingRecipe = data.Favorite ?? _defaultRecipe;
            }

            if (data.OrderBlocked)
            {
                if (CanAttemptOrder(data.PendingRecipe))
                {
                    data.OrderBlocked = false;
                    data.BlockReason = string.Empty;
                    if (!data.WaiterAssigned && !_customersNeedingOrder.Contains(data.Agent))
                    {
                        _customersNeedingOrder.Add(data.Agent);
                    }
                }
            }
            else if (!data.WaiterAssigned && !_customersNeedingOrder.Contains(data.Agent))
            {
                _customersNeedingOrder.Add(data.Agent);
            }

            CheckPatience(ref data);
        }

        private void HandleWaitDrink(ref CustomerData data, float deltaTime)
        {
            data.WaitTimer += deltaTime;
            data.TotalWaitTime += deltaTime;
            CheckPatience(ref data);
        }

        private void HandleEat(ref CustomerData data, float deltaTime)
        {
            data.ConsumeTimer += deltaTime;
            if (data.ConsumeTimer < MealDuration)
            {
                return;
            }

            FinalizeCourse(ref data);

            if (data.CompletedCourses < data.DesiredCourses)
            {
                data.State = CustomerState.Order;
                data.StateTimer = 0f;
                data.WaitTimer = 0f;
                if (!_customersNeedingOrder.Contains(data.Agent))
                {
                    _customersNeedingOrder.Add(data.Agent);
                }
            }
            else
            {
                data.State = CustomerState.Pay;
                data.StateTimer = 0f;
            }
        }

        private void FinalizeCourse(ref CustomerData data)
        {
            data.CompletedCourses++;
            data.ConsumeTimer = 0f;

            var recipe = data.CurrentRecipe ?? _defaultRecipe;
            if (recipe != null)
            {
                var baseRevenue = recipe.OutputItem != null ? recipe.OutputItem.SellPrice : 6f;
                var cost = recipe.OutputItem != null ? recipe.OutputItem.UnitCost : 2f;
                data.BillRevenue += baseRevenue;
                data.BillCost += cost;
            }
            else
            {
                data.BillRevenue += 6f;
                data.BillCost += 2f;
            }

            data.CurrentRecipe = null;
            data.PendingRecipe = null;
            data.OrderBlocked = false;
            data.WaiterAssigned = false;
            data.BlockReason = string.Empty;
        }

        private void HandlePay(ref CustomerData data)
        {
            if (data.BillRevenue <= 0f && _defaultRecipe != null)
            {
                var fallbackRevenue = _defaultRecipe.OutputItem != null ? _defaultRecipe.OutputItem.SellPrice : 6f;
                var fallbackCost = _defaultRecipe.OutputItem != null ? _defaultRecipe.OutputItem.UnitCost : 2f;
                data.BillRevenue += fallbackRevenue;
                data.BillCost += fallbackCost;
            }

            if (data.BillRevenue > 0f)
            {
                if (data.BillCost > 0f)
                {
                    _economySystem.TrySpend(data.BillCost);
                }

                var tip = CalculateTip(data.TotalWaitTime);
                _economySystem.AddRevenue(data.BillRevenue + tip);

                if (_eventBus != null)
                {
                    var tableId = data.Table != null ? data.Table.Id : -1;
                    var eventData = new Dictionary<string, object>
                    {
                        ["tableId"] = tableId,
                        ["tip"] = tip,
                        ["revenue"] = data.BillRevenue
                    };

                    var severity = tip > 0.01f ? GameEventSeverity.Success : GameEventSeverity.Info;
                    var tipText = tip > 0.01f ? $" + gorjeta {tip:0.##}" : string.Empty;
                    var message = tableId >= 0
                        ? $"Mesa {tableId} pagou {data.BillRevenue:0.##}{tipText}"
                        : $"Conta paga{tipText}";
                    _eventBus.Publish(new GameEvent(message, severity, "TipReceived", eventData));
                }
            }

            data.State = CustomerState.Leave;
            data.Agent.StandUp();
            data.Agent.SetDestination(_exitPoint);
            data.StateTimer = 0f;
            data.LeftAngry = false;
            data.SearchTimer = 0f;
            data.BillRevenue = 0f;
            data.BillCost = 0f;
            data.TotalWaitTime = 0f;
            data.PendingRecipe = null;
            data.CurrentRecipe = null;
            data.OrderBlocked = false;
            data.WaiterAssigned = false;
            data.BlockReason = string.Empty;
            if (data.Table != null)
            {
                if (!_tablesNeedingClean.Contains(data.Table.Id))
                {
                    _tablesNeedingClean.Add(data.Table.Id);
                }
            }

            if (data.Table != null && data.Seat != null)
            {
                _tableRegistry.ReleaseSeat(data.Table.Id, data.Seat.Id);
            }

            data.Table = null;
            data.Seat = null;
        }

        private void HandleLeave(ref CustomerData data)
        {
            if (data.Agent.HasReached(DestinationThresholdSqr) || data.StateTimer > 10f)
            {
                data.Reset();
                MarkForDespawn(data);
            }
        }

        private void HandleWaiterIdle(ref WaiterData data)
        {
            if (TryAssignNextCustomer(ref data))
            {
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

            data.Agent.SetDestination(GetPickupPoint(PrepArea.Kitchen));
        }

        private void HandleWaiterTakeOrder(ref WaiterData data)
        {
            if (data.TargetCustomer == null)
            {
                data.State = WaiterState.Idle;
                data.ServicePosition = Vector3.zero;
                data.StateTimer = 0f;
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
                data.ServicePosition = Vector3.zero;
                data.StateTimer = 0f;
                return;
            }

            var recipe = customerData.PendingRecipe ?? _defaultRecipe;
            if (recipe != null && customerData.Table != null)
            {
                customerData.PendingRecipe = recipe;
                var isAllowed = OrderRequestValidator.IsAllowed(_menuPolicy, _inventory, recipe);
                if (!isAllowed)
                {
                    customerData.OrderBlocked = true;
                    customerData.WaiterAssigned = false;

                    if (_menuPolicy != null && !_menuPolicy.IsAllowed(recipe))
                    {
                        customerData.BlockReason = "Bloqueado pelo cardápio";
                        PublishOrderBlockedByMenu(customerData, recipe);
                    }
                    else if (_inventory != null && !_inventory.CanCraft(recipe))
                    {
                        customerData.BlockReason = "Sem ingredientes";
                        PublishNoIngredients(customerData, recipe);
                    }
                    else
                    {
                        customerData.BlockReason = "Pedido indisponível";
                    }

                    data.TargetCustomer = null;
                    data.State = WaiterState.Idle;
                    data.ServicePosition = Vector3.zero;
                    data.Agent.SetDestination(GetPickupPoint(PrepArea.Kitchen));
                    data.StateTimer = 0f;
                    return;
                }

                customerData.State = CustomerState.WaitDrink;
                customerData.StateTimer = 0f;
                customerData.WaitTimer = 0f;
                customerData.PendingRecipe = recipe;
                customerData.OrderBlocked = false;
                customerData.WaiterAssigned = false;
                customerData.BlockReason = string.Empty;
                UpdateCustomerIntent(customerData);

                var area = _orderSystem.EnqueueOrder(customerData.Table.Id, recipe);
                data.TargetArea = area;
                data.Agent.SetDestination(GetPickupPoint(area));
                data.State = WaiterState.WaitPrep;
                data.StateTimer = 0f;
                return;
            }

            data.State = WaiterState.Idle;
            data.TargetCustomer = null;
            data.ServicePosition = Vector3.zero;
            data.Agent.SetDestination(GetPickupPoint(PrepArea.Kitchen));
            data.StateTimer = 0f;
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
                data.ServicePosition = Vector3.zero;
                data.StateTimer = 0f;
                return;
            }

            var customerData = FindCustomerData(data.TargetCustomer);
            if (customerData == null || customerData.Table == null)
            {
                data.State = WaiterState.Idle;
                data.ServicePosition = Vector3.zero;
                data.StateTimer = 0f;
                return;
            }

            if (_orderSystem.TryConsumeReadyOrder(customerData.Table.Id, out var recipe, out var area))
            {
                data.CarryingRecipe = recipe;
                data.CarryingArea = area;
                data.ServicePosition = GetSeatServicePosition(customerData.Seat);
                data.Agent.SetDestination(data.ServicePosition);
                data.State = WaiterState.Deliver;
                data.StateTimer = 0f;
            }
        }

        private void HandleWaiterDeliver(ref WaiterData data)
        {
            if (data.TargetCustomer == null)
            {
                data.State = WaiterState.Idle;
                data.ServicePosition = Vector3.zero;
                data.StateTimer = 0f;
                return;
            }

            if (!data.Agent.HasReached(DestinationThresholdSqr))
            {
                return;
            }

            var customerData = FindCustomerData(data.TargetCustomer);
            if (customerData != null && customerData.State == CustomerState.WaitDrink)
            {
                customerData.State = CustomerState.Eat;
                customerData.StateTimer = 0f;
                customerData.ConsumeTimer = 0f;
                customerData.CurrentRecipe = data.CarryingRecipe ?? customerData.PendingRecipe;
                customerData.PendingRecipe = null;
                customerData.WaitTimer = 0f;
                customerData.OrderBlocked = false;
                customerData.WaiterAssigned = false;
                customerData.BlockReason = string.Empty;
                PublishOrderDelivered(customerData, data.CarryingRecipe ?? customerData.CurrentRecipe, data.CarryingArea);
                _reputationSystem?.Add(1);
                UpdateCustomerIntent(customerData);
            }

            data.CarryingRecipe = null;
            data.CarryingArea = PrepArea.Kitchen;
            data.TargetCustomer = null;
            data.TargetArea = PrepArea.Kitchen;
            data.State = WaiterState.Idle;
            data.StateTimer = 0f;
            data.ServicePosition = Vector3.zero;
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

        private bool TryAssignNextCustomer(ref WaiterData data)
        {
            for (int i = 0; i < _customersNeedingOrder.Count; i++)
            {
                var customer = _customersNeedingOrder[i];
                if (customer == null)
                {
                    _customersNeedingOrder.RemoveAt(i);
                    i--;
                    continue;
                }

                var customerData = FindCustomerData(customer);
                if (customerData == null || customerData.Seat == null)
                {
                    _customersNeedingOrder.RemoveAt(i);
                    i--;
                    continue;
                }

                if (customerData.WaiterAssigned)
                {
                    _customersNeedingOrder.RemoveAt(i);
                    i--;
                    continue;
                }

                _customersNeedingOrder.RemoveAt(i);
                data.TargetCustomer = customer;
                customerData.WaiterAssigned = true;
                data.ServicePosition = GetSeatServicePosition(customerData.Seat);
                data.Agent.SetDestination(data.ServicePosition);
                data.State = WaiterState.TakeOrder;
                data.StateTimer = 0f;
                return true;
            }

            return false;
        }

        private Vector3 GetPickupPoint(PrepArea area)
        {
            if (area == PrepArea.Bar)
            {
                return _barPickupPoint != Vector3.zero ? _barPickupPoint : _kitchenPickupPoint;
            }

            return _kitchenPickupPoint != Vector3.zero ? _kitchenPickupPoint : _entryPoint;
        }

        private Vector3 GetSeatServicePosition(Seat seat)
        {
            if (seat == null || seat.Anchor == null)
            {
                return GetPickupPoint(PrepArea.Kitchen);
            }

            var anchor = seat.Anchor;
            var forward = anchor.forward;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            var desired = anchor.position - forward.normalized * WaiterServiceOffset;
            if (NavMesh.SamplePosition(desired, out var hit, 0.75f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            if (NavMesh.SamplePosition(anchor.position, out hit, 0.75f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return anchor.position;
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

        private bool CanAttemptOrder(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (_menuPolicy != null && !_menuPolicy.IsAllowed(recipe))
            {
                return false;
            }

            if (_inventory != null && !_inventory.CanCraft(recipe))
            {
                return false;
            }

            return true;
        }

        private void CheckPatience(ref CustomerData data)
        {
            if (data.Patience <= 0f)
            {
                return;
            }

            if (data.WaitTimer < data.Patience)
            {
                return;
            }

            data.LeftAngry = true;
            data.Agent.StandUp();
            data.Agent.SetDestination(_exitPoint);
            data.State = CustomerState.Leave;
            data.StateTimer = 0f;
            data.WaitTimer = 0f;
            data.OrderBlocked = false;
            data.WaiterAssigned = false;
            var reason = !string.IsNullOrEmpty(data.BlockReason) ? data.BlockReason : "Demorou demais";
            var tableId = data.Table != null ? data.Table.Id : -1;
            if (data.Table != null && !_tablesNeedingClean.Contains(data.Table.Id))
            {
                _tablesNeedingClean.Add(data.Table.Id);
            }

            _customersNeedingOrder.Remove(data.Agent);
            data.PendingRecipe = null;
            data.CurrentRecipe = null;
            PublishCustomerAngry(data, reason, tableId);
            CustomerLeftAngry?.Invoke(data.Agent, reason);
            data.BlockReason = string.Empty;
            _reputationSystem?.Add(-2);

            if (data.Table != null && data.Seat != null)
            {
                _tableRegistry.ReleaseSeat(data.Table.Id, data.Seat.Id);
                data.Table = null;
                data.Seat = null;
            }
        }

        private void PublishOrderBlockedByMenu(CustomerData data, RecipeSO recipe)
        {
            if (_eventBus == null)
            {
                return;
            }

            var tableId = data.Table != null ? data.Table.Id : -1;
            var recipeName = recipe != null ? recipe.DisplayName : "Desconhecido";
            var eventData = new Dictionary<string, object>
            {
                ["tableId"] = tableId,
                ["recipeId"] = recipe != null ? recipe.Id : string.Empty
            };

            var message = $"Pedido da mesa {tableId} bloqueado pelo cardápio: {recipeName}";
            _eventBus.Publish(new GameEvent(message, GameEventSeverity.Warning, "OrderBlockedByMenu", eventData));
        }

        private void PublishNoIngredients(CustomerData data, RecipeSO recipe)
        {
            if (_eventBus == null)
            {
                return;
            }

            var tableId = data.Table != null ? data.Table.Id : -1;
            var recipeName = recipe != null ? recipe.DisplayName : "Desconhecido";
            var eventData = new Dictionary<string, object>
            {
                ["tableId"] = tableId,
                ["recipeId"] = recipe != null ? recipe.Id : string.Empty
            };

            var message = $"Sem ingredientes para {recipeName} na mesa {tableId}";
            _eventBus.Publish(new GameEvent(message, GameEventSeverity.Warning, "NoIngredients", eventData));
        }

        private void PublishCustomerAngry(CustomerData data, string reason, int tableId)
        {
            if (_eventBus == null)
            {
                return;
            }

            var eventData = new Dictionary<string, object>
            {
                ["tableId"] = tableId,
                ["customerName"] = data.Name ?? (data.Agent != null ? data.Agent.name : string.Empty),
                ["reason"] = reason
            };

            var displayName = data.Name ?? (data.Agent != null ? data.Agent.name : "Cliente");
            var message = $"{displayName} deixou a mesa {tableId} irritado: {reason}";
            _eventBus.Publish(new GameEvent(message, GameEventSeverity.Warning, "CustomerAngry", eventData));
        }

        private void PublishOrderDelivered(CustomerData data, RecipeSO recipe, PrepArea area)
        {
            if (_eventBus == null)
            {
                return;
            }

            var tableId = data.Table != null ? data.Table.Id : -1;
            var recipeName = recipe != null ? recipe.DisplayName : "Pedido";
            var eventData = new Dictionary<string, object>
            {
                ["tableId"] = tableId,
                ["recipeId"] = recipe != null ? recipe.Id : string.Empty,
                ["area"] = area.ToString()
            };

            var areaLabel = area.GetDisplayName();
            var message = $"Pedido entregue ({areaLabel}) - Mesa {tableId}: {recipeName}";
            _eventBus.Publish(new GameEvent(message, GameEventSeverity.Info, "OrderDelivered", eventData));
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
                case CustomerState.Eat:
                    return "Comendo";
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
            Eat,
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
            public float ConsumeTimer;
            public float SearchTimer;
            public float TotalWaitTime;
            public bool LeftAngry;
            public int DesiredCourses;
            public int CompletedCourses;
            public float BillRevenue;
            public float BillCost;
            public RecipeSO PendingRecipe;
            public RecipeSO CurrentRecipe;
            public int Gold;
            public float Patience;
            public string Name;
            public RecipeSO Favorite;
            public bool WaiterAssigned;
            public bool OrderBlocked;
            public string BlockReason;

            public CustomerData(Customer agent)
            {
                Agent = agent;
                DesiredCourses = UnityEngine.Random.Range(1, 3);
                CompletedCourses = 0;
                BillRevenue = 0f;
                BillCost = 0f;
                Gold = UnityEngine.Random.Range(8, 24);
                Patience = UnityEngine.Random.Range(10f, 25f);
                Name = $"Cliente {UnityEngine.Random.Range(1000, 9999)}";
                Favorite = null;
                WaiterAssigned = false;
                OrderBlocked = false;
                BlockReason = string.Empty;
            }

            public void Reset()
            {
                State = CustomerState.Enter;
                Table = null;
                Seat = null;
                StateTimer = 0f;
                WaitTimer = 0f;
                ConsumeTimer = 0f;
                SearchTimer = 0f;
                TotalWaitTime = 0f;
                LeftAngry = false;
                CompletedCourses = 0;
                BillRevenue = 0f;
                BillCost = 0f;
                PendingRecipe = null;
                CurrentRecipe = null;
                DesiredCourses = UnityEngine.Random.Range(1, 3);
                Gold = UnityEngine.Random.Range(8, 24);
                Patience = UnityEngine.Random.Range(10f, 25f);
                Name = $"Cliente {UnityEngine.Random.Range(1000, 9999)}";
                Favorite = null;
                WaiterAssigned = false;
                OrderBlocked = false;
                BlockReason = string.Empty;
                Agent?.ApplyProfile(Name, Gold, Patience);
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
            public PrepArea TargetArea;
            public PrepArea CarryingArea;
            public Vector3 ServicePosition;

            public WaiterData(Waiter agent)
            {
                Agent = agent;
                State = WaiterState.Idle;
                TargetArea = PrepArea.Kitchen;
                CarryingArea = PrepArea.Kitchen;
                ServicePosition = Vector3.zero;
            }
        }
    }
}
