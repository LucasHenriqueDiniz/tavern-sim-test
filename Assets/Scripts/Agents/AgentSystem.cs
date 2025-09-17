// Scripts/Agents/AgentSystem.cs
public sealed class AgentSystem : ISimSystem {
    readonly List<Customer> _customers = new();
    readonly List<Waiter> _waiters = new();
    public void Tick(float dt) {
        foreach (var c in _customers) c.Tick(dt);
        foreach (var w in _waiters) w.Tick(dt);
    }
}

// Scripts/Agents/Customer.cs (conceito)
public enum CustomerState { Enter, FindTable, Sit, Order, Eat, Pay, Leave }
public sealed class Customer {
    CustomerState _state;
    public void Tick(float dt) {
        switch (_state) {
            case CustomerState.Enter: /* move to lobby */ break;
            case CustomerState.FindTable: /* reservar mesa livre */ break;
            case CustomerState.Sit: /* aguarda garçom */ break;
            case CustomerState.Order: /* cria pedido */ break;
            case CustomerState.Eat: /* countdown */ break;
            case CustomerState.Pay: /* pagar */ break;
            case CustomerState.Leave: /* sair/tidy */ break;
        }
    }
}
