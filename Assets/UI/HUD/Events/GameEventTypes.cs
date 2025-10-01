using System;
using UnityEngine;
using TavernSim.Core.Events;

namespace TavernSim.UI.Events
{
    /// <summary>
    /// Tipos de toast para o sistema de notificações
    /// </summary>
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    // ISelectable já existe em TavernSim.Core, não precisamos redefinir

    /// <summary>
    /// Eventos de agentes (clientes, garçons, etc.)
    /// </summary>
    public static class AgentEvent
    {
        public class CustomerEntered : GameEvent<Customer> 
        { 
            public CustomerEntered(Customer customer) : base(customer) { }
        }
        public class CustomerLeft : GameEvent<Customer, bool> 
        { 
            public CustomerLeft(Customer customer, bool angry) : base(customer, angry) { }
        }
        public class CustomerSatisfied : GameEvent<Customer, float> 
        { 
            public CustomerSatisfied(Customer customer, float satisfaction) : base(customer, satisfaction) { }
        }
        public class WaiterAssigned : GameEvent<Waiter, int> 
        { 
            public WaiterAssigned(Waiter waiter, int tableId) : base(waiter, tableId) { }
        }
        public class StaffHired : GameEvent<StaffType, StaffMember> 
        { 
            public StaffHired(StaffType staffType, StaffMember staffMember) : base(staffType, staffMember) { }
        }
        public class StaffFired : GameEvent<StaffType, int> 
        { 
            public StaffFired(StaffType staffType, int staffId) : base(staffType, staffId) { }
        }
    }

    /// <summary>
    /// Eventos de pedidos
    /// </summary>
    public static class OrderEvent
    {
        public class OrderPlaced : GameEvent<int, RecipeSO> 
        { 
            public OrderPlaced(int tableId, RecipeSO recipe) : base(tableId, recipe) { }
        }
        public class OrderStarted : GameEvent<int, PrepArea> 
        { 
            public OrderStarted(int tableId, PrepArea area) : base(tableId, area) { }
        }
        public class OrderReady : GameEvent<int, PrepArea> 
        { 
            public OrderReady(int tableId, PrepArea area) : base(tableId, area) { }
        }
        public class OrderDelivered : GameEvent<int, RecipeSO> 
        { 
            public OrderDelivered(int tableId, RecipeSO recipe) : base(tableId, recipe) { }
        }
        public class OrderCancelled : GameEvent<int, string> 
        { 
            public OrderCancelled(int tableId, string reason) : base(tableId, reason) { }
        }
        public class OrderFailed : GameEvent<int, string> 
        { 
            public OrderFailed(int tableId, string reason) : base(tableId, reason) { }
        }
    }

    /// <summary>
    /// Eventos de economia
    /// </summary>
    public static class EconomyEvent
    {
        public class RevenueAdded : GameEvent<float, string> 
        { 
            public RevenueAdded(float amount, string source) : base(amount, source) { }
        }
        public class ExpenseMade : GameEvent<float, string> 
        { 
            public ExpenseMade(float amount, string reason) : base(amount, reason) { }
        }
        public class CashChanged : GameEvent<float, float> 
        { 
            public CashChanged(float oldAmount, float newAmount) : base(oldAmount, newAmount) { }
        }
        public class TipReceived : GameEvent<float, int> 
        { 
            public TipReceived(float amount, int tableId) : base(amount, tableId) { }
        }
        public class SalaryPaid : GameEvent<float, StaffType> 
        { 
            public SalaryPaid(float amount, StaffType staffType) : base(amount, staffType) { }
        }
    }

    /// <summary>
    /// Eventos de UI
    /// </summary>
    public static class UIEvent
    {
        public class ToastRequested : GameEvent<string, ToastType> 
        { 
            public ToastRequested(string message, ToastType type) : base(message, type) { }
        }
        public class PanelOpened : GameEvent<PanelType> 
        { 
            public PanelOpened(PanelType panelType) : base(panelType) { }
        }
        public class PanelClosed : GameEvent<PanelType> 
        { 
            public PanelClosed(PanelType panelType) : base(panelType) { }
        }
        public class ObjectSelected : GameEvent<GameObject> 
        { 
            public ObjectSelected(GameObject obj) : base(obj) { }
        }
        public class ObjectDeselected { } // Evento simples sem dados
        public class CursorStateChanged : GameEvent<CursorState> 
        { 
            public CursorStateChanged(CursorState state) : base(state) { }
        }
    }

    /// <summary>
    /// Eventos de sistema
    /// </summary>
    public static class SystemEvent
    {
        public class GamePaused { } // Evento simples sem dados
        public class GameResumed { } // Evento simples sem dados
        public class TimeSpeedChanged : GameEvent<float> 
        { 
            public TimeSpeedChanged(float multiplier) : base(multiplier) { }
        }
        public class WeatherChanged : GameEvent<WeatherSnapshot> 
        { 
            public WeatherChanged(WeatherSnapshot snapshot) : base(snapshot) { }
        }
        public class SaveRequested { } // Evento simples sem dados
        public class LoadRequested : GameEvent<string> 
        { 
            public LoadRequested(string saveName) : base(saveName) { }
        }
    }

    /// <summary>
    /// Tipos de painel
    /// </summary>
    public enum PanelType
    {
        SidePanel,
        StaffPanel,
        BuildMenu,
        SelectionPopup
    }

    /// <summary>
    /// Estados do cursor
    /// </summary>
    public enum CursorState
    {
        Default,
        HoverAction,
        BuildPlace,
        BuildInvalid,
        SellErase,
        Pan
    }

    /// <summary>
    /// Tipos de staff
    /// </summary>
    public enum StaffType
    {
        Waiter,
        Cook,
        Bartender,
        Cleaner
    }

    /// <summary>
    /// Áreas de preparo
    /// </summary>
    public enum PrepArea
    {
        Kitchen,
        Bar
    }

    /// <summary>
    /// Tipos de clima
    /// </summary>
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rainy,
        Stormy,
        Snowy,
        PartlyCloudy
    }

    /// <summary>
    /// Snapshot do clima
    /// </summary>
    public struct WeatherSnapshot
    {
        public Sprite icon;
        public string temperature;
        public string description;
        public WeatherType type;
        
        // Propriedades para compatibilidade
        public string weatherType => type.ToString();
    }

    /// <summary>
    /// Classe base para eventos tipados
    /// </summary>
    public abstract class GameEvent<T>
    {
        public T Data { get; }
        
        protected GameEvent(T data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Classe base para eventos tipados com dois parâmetros
    /// </summary>
    public abstract class GameEvent<T1, T2>
    {
        public T1 Data1 { get; }
        public T2 Data2 { get; }
        
        protected GameEvent(T1 data1, T2 data2)
        {
            Data1 = data1;
            Data2 = data2;
        }
    }

    // Placeholder classes para evitar erros de compilação
    public class Customer { }
    public class Waiter { }
    public class StaffMember { }
    public class RecipeSO { }
}
