
> Guia de arquitetura e contratos para os **agentes** do TavernSim (clientes, garçons, bartenders e cozinheiros) e seus sistemas de apoio.

---

## Visão geral

### Papéis

- **Customer (cliente)**: entra → senta → decide → pede → espera → consome → paga → (chance baixa) repete pedido → sai (ou sai irritado caso falhe na entrega ou encontrar mesa).
- **Waiter (garçom)**: atende mesas, **coleta pedido** do cliente, **aguarda preparo**, **retira** no pickup correto (Bar ou Kitchen) e **entrega**.
- **Bartender**: responsável por receitas do **Bar**. Neste MVP as “estações” podem ser **virtuais** no `OrderSystem`.
- **Cook (cozinheiro)**: responsável por receitas da **Kitchen**. Idem acima.

### Canais de preparo

- `OrderSystem` separa os pedidos por **área de preparo**:
  - `PrepArea.Kitchen`
  - `PrepArea.Bar`
- Cada área tem:
  - fila `_pending*`
  - em preparo `_inPrep*`
  - **estações paralelas** configuráveis (`SetKitchenStations`, `SetBarStations`)

### Pontos de retirada (pickup)

- Dois `Vector3` definidos pelo bootstrap:
  - `_barPickupPoint` (no balcão)
  - `_kitchenPickupPoint` (no guichê)
- O **Waiter** busca no pickup **condizente** com a área do pedido pronto.

### Cardápio (menu policy)

- `MenuController` (UI Toolkit) expõe uma `IMenuPolicy` consultada antes de enviar pedidos ao `OrderSystem`.
- O HUD permite **ativar/desativar** receitas do `Catalog`. Estados podem ser persistidos via `PlayerPrefs`.

### Inventário (gancho)

- `IInventoryService` define:
  - `CanCraft(RecipeSO recipe)`
  - `TryConsume(RecipeSO recipe)`
- No **Dev/MVP**, pode retornar **true** (estoque “infinito”) para validar o fluxo; evoluir depois.

### Eventos (Sistema de Eventos)

#### Arquitetura de Eventos

- **`IEventBus`** / **`GameEventBus`**: sistema centralizado de eventos
- **Eventos tipados**: usar `GameEvent<T>` para type safety
- **Eventos assíncronos**: suporte a `async/await` para operações longas
- **Event Batching**: agrupar eventos relacionados para performance

#### Categorias de Eventos

##### Eventos de Agentes (Agent Events)
```csharp
public class AgentEvent
{
    public class CustomerEntered : GameEvent<Customer> { }
    public class CustomerLeft : GameEvent<Customer, bool> { } // bool = angry
    public class CustomerSatisfied : GameEvent<Customer, float> { } // float = satisfaction
    public class WaiterAssigned : GameEvent<Waiter, int> { } // int = tableId
    public class StaffHired : GameEvent<StaffType, StaffMember> { }
    public class StaffFired : GameEvent<StaffType, int> { } // int = staffId
}
```

##### Eventos de Pedidos (Order Events)
```csharp
public class OrderEvent
{
    public class OrderPlaced : GameEvent<int, RecipeSO> { } // tableId, recipe
    public class OrderStarted : GameEvent<int, PrepArea> { } // tableId, area
    public class OrderReady : GameEvent<int, PrepArea> { } // tableId, area
    public class OrderDelivered : GameEvent<int, RecipeSO> { } // tableId, recipe
    public class OrderCancelled : GameEvent<int, string> { } // tableId, reason
    public class OrderFailed : GameEvent<int, string> { } // tableId, reason
}
```

##### Eventos de Economia (Economy Events)
```csharp
public class EconomyEvent
{
    public class RevenueAdded : GameEvent<float, string> { } // amount, source
    public class ExpenseMade : GameEvent<float, string> { } // amount, reason
    public class CashChanged : GameEvent<float, float> { } // old, new
    public class TipReceived : GameEvent<float, int> { } // amount, tableId
    public class SalaryPaid : GameEvent<float, StaffType> { } // amount, staffType
}
```

##### Eventos de UI (UI Events)
```csharp
public class UIEvent
{
    public class ToastRequested : GameEvent<string, ToastType> { } // message, type
    public class PanelOpened : GameEvent<PanelType> { }
    public class PanelClosed : GameEvent<PanelType> { }
    public class ObjectSelected : GameEvent<GameObject> { }
    public class ObjectDeselected : GameEvent { }
    public class CursorStateChanged : GameEvent<CursorState> { }
}
```

##### Eventos de Sistema (System Events)
```csharp
public class SystemEvent
{
    public class GamePaused : GameEvent { }
    public class GameResumed : GameEvent { }
    public class TimeSpeedChanged : GameEvent<float> { } // multiplier
    public class WeatherChanged : GameEvent<WeatherSnapshot> { }
    public class SaveRequested : GameEvent { }
    public class LoadRequested : GameEvent<string> { } // saveName
}
```

#### Eventos de Erro e Debug

##### Eventos de Erro (Error Events)
```csharp
public class ErrorEvent
{
    public class MenuBlocked : GameEvent<RecipeSO, string> { } // recipe, reason
    public class NoIngredients : GameEvent<RecipeSO> { }
    public class TableUnavailable : GameEvent<int> { } // tableId
    public class StaffOverloaded : GameEvent<StaffType, int> { } // type, count
    public class SystemError : GameEvent<string, Exception> { } // context, exception
}
```

##### Eventos de Debug (Debug Events)
```csharp
public class DebugEvent
{
    public class AgentStateChanged : GameEvent<GameObject, string, string> { } // agent, oldState, newState
    public class PerformanceWarning : GameEvent<string, float> { } // metric, value
    public class MemoryWarning : GameEvent<long> { } // bytes
    public class PathfindingFailed : GameEvent<GameObject, Vector3> { } // agent, target
}
```

#### Sistema de Toasts

##### HudToastController
- **Toast Types**: `Info`, `Warning`, `Error`, `Success`
- **Toast Duration**: configurável por tipo (ex: Error = 5s, Info = 3s)
- **Toast Queue**: fila para múltiplos toasts simultâneos
- **Toast Animation**: fade in/out com easing

##### Exemplos de Toasts
```csharp
// Eventos que geram toasts
EventBus.Publish(new ErrorEvent.MenuBlocked(aleRecipe, "Sem ingredientes"));
EventBus.Publish(new OrderEvent.OrderReady(tableId, PrepArea.Bar));
EventBus.Publish(new EconomyEvent.TipReceived(2.5f, tableId));
EventBus.Publish(new AgentEvent.CustomerAngry(customer, "Mesa ocupada"));
```

#### Event Bus Implementation

##### GameEventBus
```csharp
public class GameEventBus : IEventBus
{
    public void Publish<T>(T gameEvent) where T : GameEvent;
    public void Subscribe<T>(Action<T> handler) where T : GameEvent;
    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent;
    public void SubscribeAsync<T>(Func<T, Task> handler) where T : GameEvent;
    public void Clear(); // Para cleanup em cenas
}
```

##### Event Priorities
- **High**: eventos críticos (erros, falhas de sistema)
- **Normal**: eventos de gameplay (pedidos, economia)
- **Low**: eventos de UI (animações, toasts)

#### Boas Práticas de Eventos

##### Naming Conventions
- **PascalCase** para nomes de eventos
- **Sufixo Event** para classes de evento
- **Verbos no passado** para ações (Ordered, Delivered, Failed)
- **Adjetivos** para estados (Ready, Blocked, Overloaded)

##### Event Data
- **Imutabilidade**: eventos devem ser imutáveis
- **Serialização**: suporte a JSON para save/load
- **Validação**: validar dados antes de publicar
- **Documentação**: documentar todos os parâmetros

##### Performance
- **Event Pooling**: reutilizar objetos de evento
- **Async Events**: usar para operações longas
- **Event Filtering**: filtrar eventos irrelevantes
- **Memory Management**: limpar subscriptions em OnDestroy

---

## HUD (Interface do Usuário)

### Visão Geral

O HUD do TavernSim segue uma abordagem **UI-first** inspirada no The Sims 4, mas adaptada à temática medieval de taverna. Toda interface é construída com **UI Toolkit** (`UIDocument`) para máxima flexibilidade e performance.

### Estrutura do HUD

#### 1. Canto Superior Direito – Construção e Decoração

**Botão Build (Construção):**
- Ícone: `build.svg`
- Sub-ícones:
  - Estruturas (paredes/pisos): `bricks.svg`
  - Escadas/andares: `3d-stairs.svg`
  - Portas: `entry-door.svg`
  - Demolição: `wrecking-ball.svg`

**Botão Decoration (Decoração):**
- Ícone: `large-paint-brush.svg`
- Sub-ícones:
  - Arte/quadros: `mona-lisa.svg`
  - Móveis básicos: `house.svg`
  - Itens temáticos/festas: `carnival-mask.svg`
  - Natureza/plantas: `shiny-apple.svg`

*Ambos abrem barra inferior sobreposta, navegável entre modos Build e Decor.*

#### 2. Canto Inferior Direito – Gestão da Taverna

- 🍺 **Estoque**: `beer-stein.svg`
- 👨‍🍳 **Funcionários**: `cook.svg`
- 💰 **Finanças**: `money-stack.svg`
- 🎭 **Eventos Especiais**: `drama-masks.svg`
- 📜 **Objetivos/Quests**: `contract.svg`

*Alguns podem ficar bloqueados no início e desbloquearem ao longo do progresso.*

#### 3. Canto Inferior Esquerdo – Status Geral da Taverna

- Logo/brasão da taverna (arte fixa)
- Indicadores rápidos:
  - **Reputação**: `round-star.svg`
  - **Limpeza**: `trash-can.svg` (estado por cor)
  - **Satisfação dos clientes**: `thumb-up.svg` / `thumb-down.svg`

#### 4. Centro Inferior – Relógio, Clima e Controle de Tempo

- Relógio interno
- Clima (via `WeatherService`): ícone + texto
- Controles de tempo:
  - Pausa: `pause-button.svg`
  - Play: `play-button.svg`
  - Avançar rápido: `fast-forward-button.svg`

#### 5. Canto Superior Esquerdo – Notificações e Info

- Mensagens/eventos: `chat-bubble.svg`
- Estatísticas/dados: `histogram.svg`
- Info geral: `info.svg`

### Componentes Específicos

#### Top Bar (Barra Superior)

Mostra: ouro, reputação, clientes, tempo atual, clima atual.
- Adicionar botão **Staff** (abre painel dedicado)

#### Side Panel (Painel Lateral – Toggle)

- **FAB circular** para expandir/retrair
- Mostrar resumo: reputação, pedidos, gorjetas, log de eventos filtrável
- **Importante**: não incluir contratação neste painel

#### Selection HUD (HUD de Seleção)

Popup flutuante que segue o objeto selecionado:
- Mostrar: Nome, Tipo, Status, Velocidade
- Ações contextuais: "Demitir", "Mover", "Vender"

#### Bottom Bar (Barra Inferior – Estilo Sims)

- Botões grandes à esquerda: **Build**, **Decoration**
- Botão **Staff** (central) → abre painel de gestão de funcionários
- Botão à direita: **Beauty** (overlay visual com legenda)

### Sistema de Cursor

#### CursorManager

Gerenciador centralizado para todos os estados de cursor:

```csharp
public enum CursorState
{
    Default,
    HoverAction,
    BuildPlace,
    BuildInvalid,
    SellErase,
    Pan
}
```

**Regra**: se `_isPointerOverHud == true`, usar `Default`.

#### API para Cursores

```csharp
Cursor.SetCursor(Texture2D, hotspot, CursorMode.Auto)
```

**Assets esperados:**
- `default.png`
- `hover.png`
- `build.png`
- `build_invalid.png`
- `sell.png`
- `pan.png`

### Weather no Top Bar

#### IWeatherService

```csharp
public interface IWeatherService
{
    WeatherSnapshot GetSnapshot();
}

public struct WeatherSnapshot
{
    public Sprite icon;
    public string temperature;
    public string description;
}
```

#### Integração HUD

```csharp
HUDController.BindWeather(IWeatherService) // Atualiza weatherLabel
```

### Painel de Staff Dedicado

#### Arquivos
- `StaffPanel.uxml`
- `StaffPanel.uss`
- `StaffPanelController.cs`

#### Estrutura
- **Tabs** para: Cooks, Waiters, Bartenders, Cleaners
- Mostrar atuais + candidatos
- Expor eventos: `HireXRequested`, `FireXRequested`
- Overlay não-bloqueante

### Organização de Código (UI-first)

#### Princípios
- **Nada de criação dinâmica** (`CreateLabel`/`CreateButton`)
- **Tudo no UXML** → C# só registra handlers
- **Separação clara** entre lógica de apresentação e lógica de negócio

#### Estrutura de Arquivos

```
Assets/UI/
├── HUD/
│   ├── HUDController.cs
│   ├── HUD.uxml
│   └── HUD.uss
├── Components/
│   ├── TopBar/
│   ├── BottomBar/
│   ├── SidePanel/
│   └── SelectionHUD/
├── Staff/
│   ├── StaffPanel.uxml
│   ├── StaffPanel.uss
│   └── StaffPanelController.cs
├── Cursor/
│   ├── CursorManager.cs
│   └── CursorStates.cs
└── Weather/
    ├── WeatherService.cs
    └── WeatherSnapshot.cs
```

### Contratos HUD

#### HUDController

```csharp
public class HUDController : MonoBehaviour
{
    public void Initialize(EconomySystem economy, OrderSystem orders);
    public void SetCustomers(int count);
    public void BindWeather(IWeatherService weather);
    public void ShowStaffPanel();
    public void HideStaffPanel();
}
```

#### CursorManager

```csharp
public class CursorManager : MonoBehaviour
{
    public void SetCursor(CursorState state);
    public void SetPointerOverHUD(bool isOver);
    public CursorState CurrentState { get; }
}
```

#### StaffPanelController

```csharp
public class StaffPanelController : MonoBehaviour
{
    public event Action<StaffType> HireRequested;
    public event Action<StaffType, int> FireRequested;
    
    public void Show();
    public void Hide();
    public void UpdateStaffList(StaffType type, List<StaffMember> current, List<StaffMember> candidates);
}
```

### Eventos HUD

#### GameEventBus (extensão)

```csharp
// Eventos de UI
public class HUDEvent
{
    public class StaffPanelRequested : GameEvent { }
    public class BuildModeToggled : GameEvent<bool> { }
    public class DecorationModeToggled : GameEvent<bool> { }
    public class ObjectSelected : GameEvent<GameObject> { }
    public class ObjectDeselected : GameEvent { }
}

// Eventos de Cursor
public class CursorEvent
{
    public class StateChanged : GameEvent<CursorState> { }
    public class PointerOverHUDChanged : GameEvent<bool> { }
}
```

### Integração com Sistemas Existentes

#### EconomySystem → HUD
- `CashChanged` → atualiza display de ouro
- `RevenueAdded` → animação de ganho
- `ExpenseMade` → animação de gasto

#### OrderSystem → HUD
- `OrdersChanged` → atualiza lista de pedidos
- `OrderReady` → notificação visual
- `OrderCompleted` → feedback de entrega

#### AgentSystem → HUD
- `ActiveCustomerCountChanged` → contador de clientes
- `StaffHired` → atualiza painel de staff
- `StaffFired` → remove do painel

### Performance e Otimização

#### UI Toolkit Best Practices
- **USS** para estilos (não inline)
- **Query** para elementos (não `GetElement` repetitivo)
- **Event binding** uma vez na inicialização
- **Pooling** para elementos dinâmicos (toasts, notificações)

#### Responsividade
- **Anchors** para diferentes resoluções
- **USS Media Queries** para breakpoints
- **Flexbox** para layouts adaptativos

---

## Debugging e Logging

### Sistema de Logging

#### Log Levels
- **Trace**: informações detalhadas de execução (FSM transitions, pathfinding)
- **Debug**: informações de desenvolvimento (agent states, system updates)
- **Info**: informações gerais (orders placed, customers served)
- **Warning**: situações anômalas mas não críticas (pathfinding failed, low performance)
- **Error**: erros que não quebram o jogo (missing ingredients, table unavailable)
- **Critical**: erros críticos que podem quebrar o jogo (null reference, system failure)

#### Log Categories
```csharp
public static class LogCategories
{
    public const string AGENTS = "Agents";
    public const string ORDERS = "Orders";
    public const string ECONOMY = "Economy";
    public const string UI = "UI";
    public const string PERFORMANCE = "Performance";
    public const string PATHFINDING = "Pathfinding";
    public const string SAVE_LOAD = "SaveLoad";
}
```

#### Logger Implementation
```csharp
public interface ILogger
{
    void Log(LogLevel level, string category, string message, object context = null);
    void LogTrace(string category, string message, object context = null);
    void LogDebug(string category, string message, object context = null);
    void LogInfo(string category, string message, object context = null);
    void LogWarning(string category, string message, object context = null);
    void LogError(string category, string message, object context = null);
    void LogCritical(string category, string message, object context = null);
}
```

### Debug Tools

#### Agent Debugger
```csharp
public class AgentDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showStateLabels = true;
    public bool showPathLines = true;
    public bool showProximityRadius = false;
    public bool showWaitTimers = true;
    
    [Header("Colors")]
    public Color idleColor = Color.green;
    public Color workingColor = Color.yellow;
    public Color errorColor = Color.red;
}
```

#### Performance Monitor
```csharp
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Metrics")]
    public float targetFPS = 60f;
    public float memoryWarningThreshold = 100MB;
    public float frameTimeWarningThreshold = 16.67f; // 60 FPS
    
    [Header("Debug Display")]
    public bool showFPS = true;
    public bool showMemory = true;
    public bool showAgentCount = true;
    public bool showOrderQueue = true;
}
```

#### Order System Debugger
```csharp
public class OrderSystemDebugger : MonoBehaviour
{
    [Header("Debug Info")]
    public bool showOrderQueue = true;
    public bool showPrepStations = true;
    public bool showPickupPoints = true;
    public bool logOrderEvents = true;
}
```

### Debug Console

#### In-Game Console
- **Toggle**: `~` ou `F1` para abrir/fechar
- **Commands**: digite comandos para debug
- **History**: navegar com setas para cima/baixo
- **Auto-complete**: Tab para completar comandos

#### Console Commands
```csharp
public static class DebugCommands
{
    // Agent Commands
    [ConsoleCommand("spawn.customer")]
    public static void SpawnCustomer() { }
    
    [ConsoleCommand("spawn.waiter")]
    public static void SpawnWaiter() { }
    
    [ConsoleCommand("agent.state")]
    public static void SetAgentState(string agentName, string state) { }
    
    // Economy Commands
    [ConsoleCommand("economy.add_money")]
    public static void AddMoney(float amount) { }
    
    [ConsoleCommand("economy.set_money")]
    public static void SetMoney(float amount) { }
    
    // Order Commands
    [ConsoleCommand("order.place")]
    public static void PlaceOrder(int tableId, string recipeName) { }
    
    [ConsoleCommand("order.clear")]
    public static void ClearOrders() { }
    
    // System Commands
    [ConsoleCommand("time.pause")]
    public static void PauseTime() { }
    
    [ConsoleCommand("time.speed")]
    public static void SetTimeSpeed(float multiplier) { }
    
    [ConsoleCommand("debug.performance")]
    public static void TogglePerformanceDisplay() { }
    
    [ConsoleCommand("debug.agents")]
    public static void ToggleAgentDebug() { }
}
```

### Visual Debugging

#### Gizmos e Handles
```csharp
public class DebugGizmos : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // Draw pickup points
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(barPickupPoint, 0.5f);
        Gizmos.DrawWireSphere(kitchenPickupPoint, 0.5f);
        
        // Draw table areas
        Gizmos.color = Color.green;
        foreach (var table in tables)
        {
            Gizmos.DrawWireCube(table.transform.position, table.bounds.size);
        }
        
        // Draw agent paths
        if (showAgentPaths)
        {
            Gizmos.color = Color.yellow;
            foreach (var agent in agents)
            {
                if (agent.hasPath)
                {
                    var path = agent.path;
                    for (int i = 0; i < path.corners.Length - 1; i++)
                    {
                        Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                    }
                }
            }
        }
    }
}
```

#### Debug UI Overlay
- **FPS Counter**: canto superior direito
- **Memory Usage**: abaixo do FPS
- **Agent Count**: contador por tipo
- **Order Queue**: lista de pedidos pendentes
- **System Status**: status de cada sistema

### Logging Best Practices

#### Structured Logging
```csharp
// ✅ Good
logger.LogInfo(LogCategories.AGENTS, "Customer {customerId} entered tavern", customer.Id);

// ❌ Bad
Debug.Log($"Customer {customer.Id} entered tavern");
```

#### Context Information
```csharp
public class LogContext
{
    public string SceneName { get; set; }
    public float GameTime { get; set; }
    public int FrameCount { get; set; }
    public string SystemName { get; set; }
}
```

#### Performance Logging
```csharp
public class PerformanceLogger
{
    public void LogFrameTime(float frameTime)
    {
        if (frameTime > frameTimeWarningThreshold)
        {
            logger.LogWarning(LogCategories.PERFORMANCE, 
                "Frame time exceeded threshold: {frameTime}ms", frameTime);
        }
    }
    
    public void LogMemoryUsage(long memoryBytes)
    {
        if (memoryBytes > memoryWarningThreshold)
        {
            logger.LogWarning(LogCategories.PERFORMANCE, 
                "Memory usage high: {memoryMB}MB", memoryBytes / 1024 / 1024);
        }
    }
}
```

### Debug Builds

#### Conditional Compilation
```csharp
#if DEBUG
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool showDebugUI = true;
    public bool enablePerformanceMonitoring = true;
#endif
```

#### Development vs Production
- **Development**: logs detalhados, debug UI ativa, console habilitado
- **Production**: apenas logs de erro, debug UI desabilitada, console desabilitado
- **Profile**: logs de performance, debug UI seletiva, console limitado

### Error Handling

#### Exception Handling
```csharp
public class SafeAgent : MonoBehaviour
{
    private void Update()
    {
        try
        {
            UpdateAgent();
        }
        catch (Exception ex)
        {
            logger.LogError(LogCategories.AGENTS, 
                "Agent {agentName} failed in Update: {error}", 
                gameObject.name, ex.Message);
            
            // Graceful degradation
            SetState(AgentState.Error);
        }
    }
}
```

#### Recovery Strategies
- **Agent Recovery**: resetar agente para estado seguro
- **System Recovery**: reinicializar sistema com configurações padrão
- **Data Recovery**: restaurar dados de backup se disponível
- **User Notification**: informar usuário sobre problemas e soluções

---

## Performance e Otimização

### Agentes (AI Performance)

#### Pooling de Objetos
- **Customer Pool**: reutilizar instâncias de clientes para evitar GC
- **Waiter Pool**: manter pool de garçons ativos/inativos
- **Prefab Instantiation**: usar `ObjectPool<T>` para spawns frequentes

#### NavMeshAgent Otimização
- **Agent Density**: limitar agentes simultâneos na mesma área
- **Path Caching**: cache de rotas comuns (mesa → cozinha → mesa)
- **Obstacle Avoidance**: configurar `avoidancePriority` por tipo de agente
- **Agent Radius**: otimizar `radius` e `height` para colisões eficientes

#### FSM Performance
- **State Caching**: evitar recálculos desnecessários de transições
- **Event Queuing**: processar eventos em batch ao invés de individualmente
- **Timer Optimization**: usar `Time.fixedTime` para timers determinísticos

### Sistemas (System Performance)

#### OrderSystem
- **Queue Management**: usar `Queue<T>` para O(1) enqueue/dequeue
- **Parallel Processing**: processar múltiplas estações simultaneamente
- **Memory Pooling**: reutilizar objetos `Order` e `RecipeSO`

#### EconomySystem
- **Transaction Batching**: agrupar transações para reduzir overhead
- **Event Throttling**: limitar frequência de eventos `CashChanged`
- **Decimal Precision**: usar `decimal` para cálculos monetários precisos

#### AgentSystem
- **Spatial Partitioning**: dividir taverna em zonas para busca eficiente
- **Proximity Queries**: usar `Physics.OverlapSphere` para detecção de proximidade
- **Update Scheduling**: distribuir updates de agentes ao longo dos frames

### UI Performance

#### UI Toolkit Otimização
- **Element Pooling**: reutilizar elementos de toast e notificações
- **USS Caching**: cache de estilos para elementos dinâmicos
- **Event Binding**: usar `RegisterCallback` ao invés de `AddEventListener`
- **Query Optimization**: cache de `Query` results para elementos frequentes

#### HUD Responsiveness
- **Async Updates**: atualizar UI em corrotinas para não bloquear main thread
- **Dirty Flagging**: só atualizar elementos que mudaram
- **Frame Budget**: limitar updates de UI por frame (ex: 5 elementos/frame)

### Memory Management

#### Garbage Collection
- **String Interning**: usar `string.Intern()` para strings repetitivas
- **Struct Over Classes**: preferir structs para dados pequenos e frequentes
- **Object Pooling**: pool para todos os objetos temporários
- **Event Cleanup**: sempre desregistrar eventos em `OnDestroy`

#### Asset Management
- **ScriptableObject Caching**: cache de `RecipeSO` e `ItemSO` em memória
- **Texture Streaming**: carregar texturas sob demanda
- **Audio Pooling**: reutilizar `AudioSource` components

### Profiling e Debug

#### Unity Profiler
- **CPU Usage**: monitorar `Update()`, `FixedUpdate()`, e `LateUpdate()`
- **Memory Usage**: acompanhar heap allocations e GC spikes
- **Rendering**: verificar draw calls e batching efficiency

#### Custom Metrics
- **Agent Count**: número de agentes ativos por tipo
- **Order Throughput**: pedidos processados por segundo
- **Memory Usage**: tracking de pools e caches
- **Frame Time**: distribuição de tempo entre sistemas

---

## FSMs (resumo)

### Customer

Estados principais:

1. **Enter** → vai ao `_entryPoint`.
2. **FindTable** → tenta `TableRegistry.TryReserveSeat`.
3. **Sit** → animação/warp para o assento (`Seat.Anchor`), inicia **Order**.
4. **Order** → entra na fila de atendimento de garçom.
5. **WaitDrink** → aguarda preparo/entrega (acumula `WaitTimer`).
6. **Drink** → consome (contagem `DrinkTimer`).
7. **Pay** → paga (Economy), sinaliza limpeza da mesa.
8. **Leave** → vai ao `_exitPoint`; libera assento.

Regras de paciência/dinheiro:

- Campos por cliente: `Gold`, `Patience` (segundos aceitáveis de espera).
- Tip calculado por `waitTime` (linear entre 2 e 0).
- Se **tempo estourar**, publicar `CustomerAngry` e sair.

### Waiter

Estados principais:

1. **Idle** → procura próximo **cliente aguardando pedido**; senão, pega **mesa para limpar**; fallback: fica na cozinha.
2. **TakeOrder** → vai até a mesa, escolhe receita via `ChooseRecipeFor(CustomerData)` respeitando `IMenuPolicy` e `IInventoryService`.
3. **WaitPrep** → aguarda pedido ficar pronto no `OrderSystem` (área resolve pickup).
4. **Deliver** → leva ao assento do cliente; muda estado do cliente para **Drink**.
5. **Clean** → aciona `CleaningSystem.CleanTable`.

---

## Contratos & dependências

### `OrderSystem`

#### Interface Principal
```csharp
public interface IOrderSystem : ISimSystem
{
    // Order Management
    bool EnqueueOrder(int tableId, RecipeSO recipe);
    bool TryConsumeReadyOrder(int tableId, out RecipeSO recipe, out PrepArea area);
    bool CancelOrder(int tableId, string reason = null);
    
    // Query Methods
    IReadOnlyList<Order> GetOrders();
    IReadOnlyList<Order> GetOrdersByArea(PrepArea area);
    Order GetOrder(int tableId);
    bool HasOrder(int tableId);
    
    // Configuration
    void SetKitchenStations(int count);
    void SetBarStations(int count);
    void SetPrepTime(RecipeSO recipe, float time);
    
    // Events
    event Action<Order> OrderPlaced;
    event Action<Order> OrderStarted;
    event Action<Order> OrderReady;
    event Action<Order> OrderCompleted;
    event Action<Order> OrderCancelled;
}
```

#### Order Model
```csharp
public class Order
{
    public int TableId { get; }
    public RecipeSO Recipe { get; }
    public PrepArea Area { get; }
    public OrderStatus Status { get; }
    public float PrepTime { get; }
    public float ElapsedTime { get; }
    public int StationId { get; }
    public DateTime CreatedAt { get; }
    public DateTime? StartedAt { get; }
    public DateTime? CompletedAt { get; }
}

public enum OrderStatus
{
    Pending,
    InProgress,
    Ready,
    Completed,
    Cancelled
}
```

### `AgentSystem`

#### Interface Principal
```csharp
public interface IAgentSystem : ISimSystem
{
    // Configuration
    void Configure(Vector3 entryPoint, Vector3 exitPoint, Vector3 kitchenPoint, 
                   Vector3 barPickup, Vector3 kitchenPickup);
    void SetMenuPolicy(IMenuPolicy menuPolicy);
    void SetInventory(IInventoryService inventory);
    
    // Agent Management
    void RegisterWaiter(Waiter waiter);
    void RegisterCustomer(Customer customer);
    void UnregisterAgent(GameObject agent);
    
    // Spawning
    Customer SpawnCustomer();
    Waiter SpawnWaiter();
    void DespawnCustomer(Customer customer);
    
    // Query Methods
    IReadOnlyList<Customer> GetActiveCustomers();
    IReadOnlyList<Waiter> GetActiveWaiters();
    int GetCustomerCount();
    int GetWaiterCount();
    
    // Events
    event Action<int> ActiveCustomerCountChanged;
    event Action<Customer> CustomerReleased;
    event Action<Waiter> WaiterReleased;
}
```

#### Agent Models
```csharp
public class CustomerData
{
    public int Id { get; set; }
    public float Gold { get; set; }
    public float Patience { get; set; }
    public float Satisfaction { get; set; }
    public CustomerPreferences Preferences { get; set; }
    public Table AssignedTable { get; set; }
    public Seat AssignedSeat { get; set; }
}

public class WaiterData
{
    public int Id { get; set; }
    public float Speed { get; set; }
    public float Efficiency { get; set; }
    public int MaxOrders { get; set; }
    public List<int> AssignedTables { get; set; }
    public WaiterState CurrentState { get; set; }
}
```

### `TableRegistry`

#### Interface Principal
```csharp
public interface ITableRegistry
{
    // Table Management
    void RegisterTable(Table table);
    void UnregisterTable(int tableId);
    Table GetTable(int tableId);
    IReadOnlyList<Table> GetAllTables();
    
    // Seat Management
    bool TryReserveSeat(out Table table, out Seat seat);
    bool TryReserveSeat(int tableId, out Seat seat);
    void ReleaseSeat(int tableId, int seatId);
    void ReleaseSeat(Table table, Seat seat);
    
    // Query Methods
    bool IsTableAvailable(int tableId);
    bool HasAvailableSeats();
    int GetAvailableSeatCount();
    IReadOnlyList<Seat> GetAvailableSeats();
    
    // Events
    event Action<Table, Seat> SeatReserved;
    event Action<Table, Seat> SeatReleased;
    event Action<Table> TableRegistered;
    event Action<Table> TableUnregistered;
}
```

#### Table Models
```csharp
public class Table
{
    public int Id { get; }
    public Vector3 Position { get; }
    public Bounds Bounds { get; }
    public List<Seat> Seats { get; }
    public TableStatus Status { get; }
    public bool IsClean { get; }
    public float Cleanliness { get; }
}

public class Seat
{
    public int Id { get; }
    public Vector3 Anchor { get; }
    public bool IsOccupied { get; }
    public Customer Occupant { get; }
    public Table ParentTable { get; }
}
```

### `EconomySystem`

#### Interface Principal
```csharp
public interface IEconomySystem : ISimSystem
{
    // Money Management
    float CurrentCash { get; }
    bool TrySpend(float amount, string reason = null);
    void AddRevenue(float amount, string source = null);
    void SetCash(float amount);
    
    // Financial Tracking
    float GetTotalRevenue();
    float GetTotalExpenses();
    float GetNetProfit();
    FinancialReport GetReport(TimeSpan period);
    
    // Events
    event Action<float, float> CashChanged; // old, new
    event Action<float, string> RevenueAdded;
    event Action<float, string> ExpenseMade;
}
```

#### Financial Models
```csharp
public class FinancialReport
{
    public float TotalRevenue { get; set; }
    public float TotalExpenses { get; set; }
    public float NetProfit { get; set; }
    public float ProfitMargin { get; set; }
    public Dictionary<string, float> RevenueBySource { get; set; }
    public Dictionary<string, float> ExpensesByCategory { get; set; }
    public TimeSpan Period { get; set; }
}
```

### `CleaningSystem`

#### Interface Principal
```csharp
public interface ICleaningSystem : ISimSystem
{
    // Table Management
    void RegisterTable(Table table);
    void UnregisterTable(int tableId);
    
    // Cleaning Operations
    void CleanTable(int tableId);
    void CleanTable(Table table);
    void MarkTableDirty(int tableId, float dirtiness = 1.0f);
    
    // Query Methods
    bool IsTableClean(int tableId);
    float GetTableCleanliness(int tableId);
    IReadOnlyList<Table> GetDirtyTables();
    IReadOnlyList<Table> GetCleanTables();
    
    // Events
    event Action<Table> TableCleaned;
    event Action<Table> TableDirtied;
}
```

### `MenuController` & `IMenuPolicy`

#### Interface Principal
```csharp
public interface IMenuPolicy
{
    bool IsRecipeAllowed(RecipeSO recipe);
    bool IsRecipeAvailable(RecipeSO recipe);
    void SetRecipeEnabled(RecipeSO recipe, bool enabled);
    IReadOnlyList<RecipeSO> GetAvailableRecipes();
    IReadOnlyList<RecipeSO> GetAllowedRecipes();
}

public interface IMenuController
{
    void Initialize(Catalog catalog);
    void SetMenuPolicy(IMenuPolicy policy);
    void RefreshMenu();
    void SaveMenuState();
    void LoadMenuState();
}
```

### `InventoryService`

#### Interface Principal
```csharp
public interface IInventoryService
{
    // Item Management
    bool HasItem(ItemSO item, int quantity = 1);
    bool TryConsume(ItemSO item, int quantity = 1);
    bool TryAdd(ItemSO item, int quantity = 1);
    int GetItemCount(ItemSO item);
    
    // Recipe Support
    bool CanCraft(RecipeSO recipe);
    bool TryConsumeRecipe(RecipeSO recipe);
    IReadOnlyList<ItemSO> GetMissingIngredients(RecipeSO recipe);
    
    // Events
    event Action<ItemSO, int> ItemAdded; // item, quantity
    event Action<ItemSO, int> ItemConsumed; // item, quantity
    event Action<RecipeSO> RecipeCrafted;
}
```

### UI / HUD Contracts

#### HUDController
```csharp
public interface IHUDController
{
    void Initialize(IEconomySystem economy, IOrderSystem orders);
    void SetCustomers(int count);
    void BindWeather(IWeatherService weather);
    void ShowStaffPanel();
    void HideStaffPanel();
    void ShowToast(string message, ToastType type = ToastType.Info);
}
```

#### HudToastController
```csharp
public interface IHudToastController
{
    void Initialize(IEventBus eventBus);
    void ShowToast(string message, ToastType type, float duration = 3f);
    void HideAllToasts();
    void SetToastQueueLimit(int limit);
}
```

### Service Locator Pattern

#### IServiceLocator
```csharp
public interface IServiceLocator
{
    T GetService<T>() where T : class;
    void RegisterService<T>(T service) where T : class;
    void UnregisterService<T>() where T : class;
    bool HasService<T>() where T : class;
}
```

#### Service Registration
```csharp
public class ServiceLocator : IServiceLocator
{
    private readonly Dictionary<Type, object> _services = new();
    
    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }
    
    public T GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }
}
```

---

## Configuração e Settings

### Sistema de Configuração

#### GameSettings (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "TavernSim/Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Gameplay")]
    public float gameSpeed = 1.0f;
    public int maxCustomers = 20;
    public float customerSpawnInterval = 5.0f;
    public float customerPatience = 30.0f;
    
    [Header("Economy")]
    public float startingCash = 100.0f;
    public float baseTipAmount = 2.0f;
    public float salaryMultiplier = 1.0f;
    
    [Header("Orders")]
    public int maxKitchenStations = 2;
    public int maxBarStations = 1;
    public float basePrepTime = 10.0f;
    
    [Header("UI")]
    public bool showDebugUI = false;
    public bool enableConsole = true;
    public float toastDuration = 3.0f;
    
    [Header("Performance")]
    public int maxAgentsPerFrame = 5;
    public float performanceWarningThreshold = 16.67f;
    public bool enableObjectPooling = true;
}
```

#### AgentSettings
```csharp
[CreateAssetMenu(fileName = "AgentSettings", menuName = "TavernSim/Settings/Agent Settings")]
public class AgentSettings : ScriptableObject
{
    [Header("Customer Settings")]
    public float minGold = 5.0f;
    public float maxGold = 50.0f;
    public float minPatience = 20.0f;
    public float maxPatience = 60.0f;
    public float satisfactionDecayRate = 0.1f;
    
    [Header("Waiter Settings")]
    public float baseSpeed = 3.5f;
    public float baseEfficiency = 1.0f;
    public int maxOrdersPerWaiter = 3;
    public float orderTakingTime = 2.0f;
    
    [Header("Pathfinding")]
    public float agentRadius = 0.5f;
    public float agentHeight = 2.0f;
    public float avoidancePriority = 50.0f;
    public float pathfindingTimeout = 5.0f;
}
```

#### UISettings
```csharp
[CreateAssetMenu(fileName = "UISettings", menuName = "TavernSim/Settings/UI Settings")]
public class UISettings : ScriptableObject
{
    [Header("HUD")]
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.gray;
    public Color successColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    
    [Header("Fonts")]
    public Font primaryFont;
    public Font secondaryFont;
    public int baseFontSize = 14;
    
    [Header("Animations")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Layout")]
    public Vector2 screenPadding = new Vector2(20, 20);
    public float elementSpacing = 10.0f;
    public float panelBorderRadius = 5.0f;
}
```

### Configuração de Cena

#### SceneConfiguration
```csharp
[System.Serializable]
public class SceneConfiguration
{
    [Header("Spawn Points")]
    public Vector3 entryPoint = Vector3.zero;
    public Vector3 exitPoint = Vector3.zero;
    public Vector3 kitchenPoint = Vector3.zero;
    public Vector3 barPickupPoint = Vector3.zero;
    public Vector3 kitchenPickupPoint = Vector3.zero;
    
    [Header("Tables")]
    public List<TableConfig> tables = new List<TableConfig>();
    
    [Header("Staff")]
    public int initialWaiters = 1;
    public int initialCooks = 1;
    public int initialBartenders = 1;
    
    [Header("Environment")]
    public WeatherType initialWeather = WeatherType.Sunny;
    public float dayLength = 300.0f; // 5 minutes
    public bool enableDayNightCycle = true;
}

[System.Serializable]
public class TableConfig
{
    public int id;
    public Vector3 position;
    public Vector3 rotation;
    public int seatCount;
    public Vector3[] seatPositions;
}
```

### PlayerPrefs Integration

#### SettingsManager
```csharp
public class SettingsManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings gameSettings;
    public AgentSettings agentSettings;
    public UISettings uiSettings;
    
    private const string SETTINGS_KEY = "TavernSim_Settings";
    
    public void SaveSettings()
    {
        var settingsData = new SettingsData
        {
            gameSpeed = gameSettings.gameSpeed,
            maxCustomers = gameSettings.maxCustomers,
            showDebugUI = gameSettings.showDebugUI,
            enableConsole = gameSettings.enableConsole,
            // ... outros campos
        };
        
        string json = JsonUtility.ToJson(settingsData, true);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();
    }
    
    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY);
            var settingsData = JsonUtility.FromJson<SettingsData>(json);
            
            // Aplicar configurações
            gameSettings.gameSpeed = settingsData.gameSpeed;
            gameSettings.maxCustomers = settingsData.maxCustomers;
            // ... outros campos
        }
    }
}

[System.Serializable]
public class SettingsData
{
    public float gameSpeed;
    public int maxCustomers;
    public bool showDebugUI;
    public bool enableConsole;
    public float toastDuration;
    public Color primaryColor;
    // ... outros campos
}
```

### Configuração de Dificuldade

#### DifficultySettings
```csharp
[CreateAssetMenu(fileName = "DifficultySettings", menuName = "TavernSim/Settings/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Easy")]
    public DifficultyConfig easy = new DifficultyConfig
    {
        customerPatience = 60.0f,
        customerSpawnRate = 0.5f,
        prepTimeMultiplier = 0.8f,
        tipMultiplier = 1.5f,
        maxCustomers = 15
    };
    
    [Header("Normal")]
    public DifficultyConfig normal = new DifficultyConfig
    {
        customerPatience = 30.0f,
        customerSpawnRate = 1.0f,
        prepTimeMultiplier = 1.0f,
        tipMultiplier = 1.0f,
        maxCustomers = 20
    };
    
    [Header("Hard")]
    public DifficultyConfig hard = new DifficultyConfig
    {
        customerPatience = 20.0f,
        customerSpawnRate = 1.5f,
        prepTimeMultiplier = 1.2f,
        tipMultiplier = 0.8f,
        maxCustomers = 25
    };
}

[System.Serializable]
public class DifficultyConfig
{
    public float customerPatience;
    public float customerSpawnRate;
    public float prepTimeMultiplier;
    public float tipMultiplier;
    public int maxCustomers;
    public float staffEfficiencyMultiplier = 1.0f;
    public float ingredientCostMultiplier = 1.0f;
}
```

### Configuração de Localização

#### LocalizationSettings
```csharp
[CreateAssetMenu(fileName = "LocalizationSettings", menuName = "TavernSim/Settings/Localization Settings")]
public class LocalizationSettings : ScriptableObject
{
    [Header("Supported Languages")]
    public List<LanguageConfig> supportedLanguages = new List<LanguageConfig>
    {
        new LanguageConfig { code = "en", name = "English", nativeName = "English" },
        new LanguageConfig { code = "pt", name = "Portuguese", nativeName = "Português" },
        new LanguageConfig { code = "es", name = "Spanish", nativeName = "Español" }
    };
    
    [Header("Default Settings")]
    public string defaultLanguage = "en";
    public bool autoDetectLanguage = true;
    public bool fallbackToEnglish = true;
    
    [Header("Text Assets")]
    public TextAsset[] localizationFiles;
}

[System.Serializable]
public class LanguageConfig
{
    public string code;
    public string name;
    public string nativeName;
    public Sprite flag;
}
```

### Configuração de Audio

#### AudioSettings
```csharp
[CreateAssetMenu(fileName = "AudioSettings", menuName = "TavernSim/Settings/Audio Settings")]
public class AudioSettings : ScriptableObject
{
    [Header("Master Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1.0f;
    
    [Header("Music")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    public bool musicEnabled = true;
    
    [Header("SFX")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    public bool sfxEnabled = true;
    
    [Header("UI Sounds")]
    [Range(0f, 1f)]
    public float uiVolume = 0.6f;
    public bool uiSoundsEnabled = true;
    
    [Header("Ambient")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.5f;
    public bool ambientEnabled = true;
}
```

### Configuração de Performance

#### PerformanceSettings
```csharp
[CreateAssetMenu(fileName = "PerformanceSettings", menuName = "TavernSim/Settings/Performance Settings")]
public class PerformanceSettings : ScriptableObject
{
    [Header("Quality Levels")]
    public QualityLevel currentQuality = QualityLevel.Medium;
    
    [Header("Rendering")]
    public int targetFPS = 60;
    public bool vsyncEnabled = true;
    public int maxLODLevel = 2;
    
    [Header("AI Performance")]
    public int maxAgentsPerFrame = 5;
    public float agentUpdateInterval = 0.1f;
    public bool enableAgentPooling = true;
    
    [Header("UI Performance")]
    public int maxUIUpdatesPerFrame = 10;
    public bool enableUIPooling = true;
    public float uiUpdateInterval = 0.05f;
    
    [Header("Memory")]
    public int maxObjectPoolSize = 100;
    public bool enableGarbageCollection = true;
    public float gcInterval = 30.0f;
}

public enum QualityLevel
{
    Low,
    Medium,
    High,
    Ultra
}
```

### Configuração de Debug

#### DebugSettings
```csharp
[CreateAssetMenu(fileName = "DebugSettings", menuName = "TavernSim/Settings/Debug Settings")]
public class DebugSettings : ScriptableObject
{
    [Header("Debug UI")]
    public bool showDebugUI = false;
    public bool showFPS = true;
    public bool showMemory = true;
    public bool showAgentCount = true;
    public bool showOrderQueue = true;
    
    [Header("Logging")]
    public LogLevel minLogLevel = LogLevel.Info;
    public bool enableFileLogging = false;
    public string logFilePath = "Logs/";
    public int maxLogFiles = 10;
    
    [Header("Console")]
    public bool enableConsole = true;
    public KeyCode consoleToggleKey = KeyCode.F1;
    public int maxConsoleLines = 100;
    
    [Header("Gizmos")]
    public bool showAgentPaths = false;
    public bool showPickupPoints = true;
    public bool showTableBounds = true;
    public bool showProximityRadius = false;
}
```

### Configuração de Save/Load

#### SaveSettings
```csharp
[CreateAssetMenu(fileName = "SaveSettings", menuName = "TavernSim/Settings/Save Settings")]
public class SaveSettings : ScriptableObject
{
    [Header("Save System")]
    public string saveDirectory = "Saves/";
    public string saveFileExtension = ".tavern";
    public int maxSaveSlots = 10;
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300.0f; // 5 minutes
    
    [Header("Compression")]
    public bool compressSaves = true;
    public CompressionLevel compressionLevel = CompressionLevel.Medium;
    
    [Header("Encryption")]
    public bool encryptSaves = false;
    public string encryptionKey = "TavernSim2024";
}

public enum CompressionLevel
{
    None,
    Low,
    Medium,
    High
}
```

### Configuração de Input

#### InputSettings
```csharp
[CreateAssetMenu(fileName = "InputSettings", menuName = "TavernSim/Settings/Input Settings")]
public class InputSettings : ScriptableObject
{
    [Header("Mouse")]
    public float mouseSensitivity = 1.0f;
    public bool invertMouseY = false;
    public bool enableMouseWheelZoom = true;
    
    [Header("Keyboard")]
    public KeyCode pauseKey = KeyCode.Space;
    public KeyCode speedUpKey = KeyCode.RightBracket;
    public KeyCode speedDownKey = KeyCode.LeftBracket;
    public KeyCode debugKey = KeyCode.F1;
    
    [Header("UI")]
    public KeyCode staffPanelKey = KeyCode.S;
    public KeyCode buildModeKey = KeyCode.B;
    public KeyCode decorationModeKey = KeyCode.D;
}
```

### Configuração de Rede (Multiplayer)

#### NetworkSettings
```csharp
[CreateAssetMenu(fileName = "NetworkSettings", menuName = "TavernSim/Settings/Network Settings")]
public class NetworkSettings : ScriptableObject
{
    [Header("Connection")]
    public string serverAddress = "localhost";
    public int serverPort = 7777;
    public int maxConnections = 8;
    
    [Header("Synchronization")]
    public float syncInterval = 0.1f;
    public bool syncEconomy = true;
    public bool syncOrders = true;
    public bool syncAgents = true;
    
    [Header("Latency")]
    public float maxLatency = 200.0f; // ms
    public bool enableLagCompensation = true;
    public float lagCompensationTime = 0.1f;
}
```

---

## Bootstrap (DevBootstrap)

Sequência recomendada:

1. **Cenário**: cria chão/bar/mesa/assentos, marca obstáculos (`NavMeshObstacle`), **bake** NavMesh (AI Navigation).
2. **Pontos**: define `_entryPoint`, `_exitPoint`, `_kitchenPoint`, `_barPickupPoint`, `_kitchenPickupPoint`.
3. **Sistemas**: instancia `SimulationRunner`, registra `EconomySystem`, `OrderSystem`, `CleaningSystem`, `TableRegistry`, `AgentSystem`, `CustomerSpawner`.
4. **Agentes**: cria **Waiter** (capsule + `NavMeshAgent`) e registra no `AgentSystem`.
5. **Mesas**: constrói `Table/Seat` a partir dos anchors e registra em `TableRegistry` e `CleaningSystem`.
6. **UI**: adiciona `HUDController` (liga a Economy/Orders), `TimeControls`, `MenuController` (passa `Catalog`), `HudToastController`.
7. **Integrações**:
   - `AgentSystem.SetMenuPolicy(menu)`
   - `AgentSystem.SetInventory(devInventoryStub)`
   - `_orderSystem.SetKitchenStations(2); _orderSystem.SetBarStations(1);`
8. **Spawner**: configura `CustomerSpawner` com prefab (capsule + `NavMeshAgent`), pré-aquece pool.

---


## Boas práticas & armadilhas

- **NavMeshAgent**: use `Warp()` para encaixes imediatos (sentar/levantar), **não** setar `transform.position`.
- **Namespaces**: evitar colisões; agentes vivem em `TavernSim.Agents`, sistemas em `TavernSim.Simulation.Systems`.
- **Eventos**: publicar via `IEventBus`; HUD mostra toasts. Evitar `Debug.Log` para UX.
- **Determinismo**: simular por `SimulationRunner` com passo fixo; manter lógicas sem `Time.deltaTime` direto no `Update`.
- **Cardápio**: sempre checar `IMenuPolicy` antes de `EnqueueOrder`.
- **Inventário**: checar `CanCraft`/`TryConsume` (no Dev, retornar `true`).
- **HUD**: construir com UI Toolkit (`UIDocument`), sem dependências de cena frágil.
- **UI-first**: sempre definir layout no UXML, C# apenas para handlers e lógica.
- **Cursor**: usar `CursorManager` centralizado, nunca `Cursor.SetCursor` direto.
- **Responsividade**: usar anchors e USS media queries para diferentes resoluções.
- **Performance UI**: evitar criação dinâmica de elementos, preferir pooling para toasts.

---

## Mapa de arquivos (referência)

- Agentes: `Assets/Agents/Waiter.cs`, `Customer.cs`, (`Bartender.cs`, `Cook.cs` opcionais)
- Sistemas: `Assets/Simulation/Systems/*` (`AgentSystem`, `OrderSystem`, `TableRegistry`, `CleaningSystem`, `EconomySystem`)
- Modelos: `Assets/Simulation/Models/*` (`Table`, `Seat`)
- Domínio: `Assets/Domain/*` (`Catalog`, `ItemSO`, `RecipeSO`)
- UI: `Assets/UI/*` (`HUDController`, `TimeControls`, `MenuController`, `HudToastController`)
- HUD: `Assets/UI/HUD/*` (`HUDController`, `HUD.uxml`, `HUD.uss`)
- HUD Components: `Assets/UI/Components/*` (`TopBar`, `BottomBar`, `SidePanel`, `SelectionHUD`)
- Staff UI: `Assets/UI/Staff/*` (`StaffPanel.uxml`, `StaffPanel.uss`, `StaffPanelController.cs`)
- Cursor: `Assets/UI/Cursor/*` (`CursorManager.cs`, `CursorStates.cs`)
- Weather: `Assets/UI/Weather/*` (`WeatherService.cs`, `WeatherSnapshot.cs`)
- Bootstrap: `Assets/Bootstrap/DevBootstrap.cs`

---
