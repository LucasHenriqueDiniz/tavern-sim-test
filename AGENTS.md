# AGENTS.md

> Guia de arquitetura e contratos para os **agentes** do TavernSim (clientes, garçons, bartenders e cozinheiros) e seus sistemas de apoio.
>
> Público-alvo: quem implementa/faz review de IA de agentes, fluxo de pedidos e integração com HUD/Inventário/Bootstrap.
>
> OBS: .meta guardam o GUID de cada asset e são gerados pelo Unity; quando corrompidos/feitos à mão, o Editor pode ignorar o asset. A correção é remover os .meta ruins e deixar o Unity recriá-los na importação

---

## Objetivos

- Descrever **papéis, estados e contratos** dos agentes.
- Garantir **separação de responsabilidades** entre IA (FSM), sistemas determinísticos (`ISimSystem`) e UI.
- Padronizar **eventos de jogo** (toasts) e **pontos de integração** (cardápio, inventário, pickup).

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

### Eventos (HUD toasts)

- `IEventBus` / `GameEventBus` para publicar eventos do jogo (sem logar direto):
  - `MenuBlocked(recipe)`
  - `NoIngredients(recipe)`
  - `OrderReady(tableId, area)`
  - `Delivered(tableId, recipe)`
  - `CustomerAngry(customerId, reason)`
- `HudToastController` escuta e mostra toasts **temporários** (ex.: “Cliente saiu irritado”, “Sem ingredientes para Ale”).

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

- `EnqueueOrder(int tableId, RecipeSO recipe)`
- `TryConsumeReadyOrder(int tableId, out RecipeSO recipe, out PrepArea area)`
- `GetOrders()` para HUD.
- Eventos: `OrdersChanged(IReadOnlyList<Order>)`
- Configuração: `SetKitchenStations(int)`, `SetBarStations(int)`

### `AgentSystem`

- `Configure(entry, exit, kitchen, barPickup, kitchenPickup)`
- `SetMenuPolicy(IMenuPolicy)`
- `SetInventory(IInventoryService)`
- `RegisterWaiter(Waiter)`
- `SpawnCustomer(Customer)` (via `CustomerSpawner`)
- Eventos: `ActiveCustomerCountChanged(int)`, `CustomerReleased(Customer)`

### `TableRegistry`

- `RegisterTable(Table)`
- `TryReserveSeat(out Table, out Seat)`
- `ReleaseSeat(tableId, seatId)`
- `GetTable(tableId)`

### `EconomySystem`

- `AddRevenue(float)`, `TrySpend(float)`
- (Opcional) `CashChanged` para HUD

### `CleaningSystem`

- `RegisterTable(Table)`, `CleanTable(int)`

### UI / HUD

- `HUDController.Initialize(EconomySystem, OrderSystem)`
- `HUDController.SetCustomers(int)` (assinado em `ActiveCustomerCountChanged`)
- `MenuController.Initialize(Catalog)` e expõe `IMenuPolicy`
- `HudToastController.Initialize(IEventBus)`

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

## Testes

- **EditMode**:
  - `OrderSystem` (fila, slots paralelos, decremento de tempo, `TryConsumeReadyOrder`)
  - `EconomySystem` (gastos/receitas, eventos)
  - **Políticas**: `MenuController` (IsAllowed), `IInventoryService` stub
- **PlayMode**:
  - Fluxo cliente → garçom → pedido → entrega (em cena cinza)
  - Toasts: publicar `GameEvent` e validar render no HUD (pode ser snapshot/headless)

> CI: rodar `game-ci/unity-test-runner@v4` (EditMode + PlayMode). Manter pacotes estáveis no `manifest.json`.

---

## Boas práticas & armadilhas

- **NavMeshAgent**: use `Warp()` para encaixes imediatos (sentar/levantar), **não** setar `transform.position`.
- **Namespaces**: evitar colisões; agentes vivem em `TavernSim.Agents`, sistemas em `TavernSim.Simulation.Systems`.
- **Eventos**: publicar via `IEventBus`; HUD mostra toasts. Evitar `Debug.Log` para UX.
- **Determinismo**: simular por `SimulationRunner` com passo fixo; manter lógicas sem `Time.deltaTime` direto no `Update`.
- **Cardápio**: sempre checar `IMenuPolicy` antes de `EnqueueOrder`.
- **Inventário**: checar `CanCraft`/`TryConsume` (no Dev, retornar `true`).
- **HUD**: construir com UI Toolkit (`UIDocument`), sem dependências de cena frágil.

---

## Roadmap curto

- Implementar `InventorySystem` real (estoque por `ItemSO`, consumo por `RecipeSO`).
- **Reputação** → deriva `Gold` e `Patience` média dos clientes, afeta `tip` e taxa de “raiva”.
- Dar função aos **Bartender/Cook** (estações físicas + animações).
- Melhorar **seleção no HUD**: ficha do cliente (nome/gostos/dinheiro) quando selecionado.

---

## Mapa de arquivos (referência)

- Agentes: `Assets/Agents/Waiter.cs`, `Customer.cs`, (`Bartender.cs`, `Cook.cs` opcionais)
- Sistemas: `Assets/Simulation/Systems/*` (`AgentSystem`, `OrderSystem`, `TableRegistry`, `CleaningSystem`, `EconomySystem`)
- Modelos: `Assets/Simulation/Models/*` (`Table`, `Seat`)
- Domínio: `Assets/Domain/*` (`Catalog`, `ItemSO`, `RecipeSO`)
- UI: `Assets/UI/*` (`HUDController`, `TimeControls`, `MenuController`, `HudToastController`)
- Bootstrap: `Assets/Bootstrap/DevBootstrap.cs`

---
