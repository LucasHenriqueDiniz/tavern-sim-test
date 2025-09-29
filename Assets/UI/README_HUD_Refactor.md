# HUD Refactor - Guia de Uso

## Visão Geral

O HUD foi refatorado para usar uma arquitetura de controllers separados, melhorando a organização e manutenibilidade do código.

## Estrutura dos Controllers

### 1. HUDController (Coordenador Principal)
- **Responsabilidade**: Coordena todos os outros controllers e gerencia a integração com sistemas
- **Localização**: `Assets/UI/HUDController.cs`
- **Funcionalidades**:
  - Inicialização e binding de sistemas
  - Gerenciamento de eventos entre controllers
  - Controle de input (F5/F9 para save/load, ESC para fechar painéis)
  - Gerenciamento de ponteiro para cursores

### 2. TopBarController
- **Responsabilidade**: Barra superior (recursos, clima, controles de tempo)
- **Localização**: `Assets/UI/TopBarController.cs`
- **Funcionalidades**:
  - Exibição de ouro, reputação, clientes
  - Clima atual (ícone + temperatura)
  - Relógio do jogo
  - Botão "Equipe" para abrir painel de staff

### 3. ToolbarController
- **Responsabilidade**: Barra inferior (construção, decoração, beleza)
- **Localização**: `Assets/UI/ToolbarController.cs`
- **Funcionalidades**:
  - Botões de construção e decoração
  - Menu de opções de build
  - Toggle de overlay de beleza
  - Integração com GridPlacer

### 4. SidePanelController
- **Responsabilidade**: Painel lateral (informações da taverna, log de eventos)
- **Localização**: `Assets/UI/SidePanelController.cs`
- **Funcionalidades**:
  - Estatísticas de reputação, pedidos, gorjetas
  - Log de eventos com filtros
  - Cardápio
  - Ações de contexto

### 5. SelectionPopupController
- **Responsabilidade**: Popup de seleção flutuante
- **Localização**: `Assets/UI/SelectionPopupController.cs`
- **Funcionalidades**:
  - Exibição de detalhes do objeto selecionado
  - Ações de contexto (demitir, mover, vender)
  - Posicionamento automático baseado na posição do objeto

### 6. StaffPanelController
- **Responsabilidade**: Painel de equipe (overlay central)
- **Localização**: `Assets/UI/StaffPanelController.cs`
- **Funcionalidades**:
  - Tabs para diferentes tipos de funcionários
  - Lista de funcionários atuais
  - Contratação de novos funcionários
  - Demissão de funcionários existentes

### 7. CursorManager
- **Responsabilidade**: Gerenciamento de cursores
- **Localização**: `Assets/UI/CursorManager.cs`
- **Funcionalidades**:
  - Troca de cursores baseada no modo atual
  - Cursores para construção, venda, pan, etc.
  - Respeita quando o ponteiro está sobre UI

## Integração com Sistemas

### Exemplo de Setup Básico

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private HUDController hudController;
    [SerializeField] private EconomySystem economySystem;
    [SerializeField] private OrderSystem orderSystem;
    // ... outros sistemas

    private void Start()
    {
        // Inicializar HUD
        hudController.Initialize(economySystem, orderSystem);
        hudController.BindSaveService(saveService);
        hudController.BindSelection(selectionService, gridPlacer);
        hudController.BindEventBus(eventBus);
        hudController.BindReputation(reputationSystem);
        hudController.BindBuildCatalog(buildCatalog);
        hudController.BindClock(clockSystem);
        hudController.BindWeather(weatherService);

        // Hook eventos de staff
        hudController.HireWaiterRequested += OnHireWaiter;
        hudController.HireCookRequested += OnHireCook;
        // ... outros eventos
    }
}
```

### Configuração de Clima

```csharp
// Criar um serviço de clima
var weatherService = gameObject.AddComponent<WeatherServiceStub>();
hudController.BindWeather(weatherService);

// O clima será atualizado automaticamente a cada 5 segundos
```

### Gerenciamento de Cursores

```csharp
// O CursorManager é configurado automaticamente quando você faz:
hudController.BindSelection(selectionService, gridPlacer);

// Cursores disponíveis:
// - Default: navegação normal
// - Build: modo construção com preview válido
// - BuildInvalid: preview inválido ou sem dinheiro
// - Sell: modo venda/remoção
// - Pan: segurando RMB para pan da câmera
```

## Assets Necessários

### Cursores
Crie os seguintes arquivos na pasta `Assets/UI/Cursors/`:
- `default.png` (hotspot 6,6)
- `hover.png` (hotspot 6,6)
- `build.png` (hotspot 6,6)
- `build_invalid.png` (hotspot 6,6)
- `sell.png` (hotspot 6,6)
- `pan.png` (hotspot 16,16)

### Ícones de Clima
Os ícones carregados pelo `IconManager` ficam em `Assets/UI/Icons/` e devem ser fornecidos como SVG/texto (evite binários `.png`).
- `weather-sun.svg`
- `weather-partly.svg`
- `weather-cloud.svg`
- `weather-rain.svg`
- `weather-storm.svg`
- `weather-snow.svg`

> Observação: mantenha estes nomes para que o `WeatherService` consiga resolver os ícones corretamente. Substitua os placeholders SVG por arte final quando os assets estiverem disponíveis.

### Staff Panel
- `Assets/UI/UXML/StaffPanel.uxml`
- `Assets/UI/USS/StaffPanel.uss`

## Eventos Disponíveis

### HUDController
- `HireWaiterRequested`
- `HireCookRequested`
- `HireBartenderRequested`
- `HireCleanerRequested`
- `FireWaiterRequested(Waiter)`
- `FireCookRequested(Cook)`
- `FireBartenderRequested(Bartender)`
- `FireCleanerRequested(Cleaner)`

### TopBarController
- `StaffButtonClicked`

### ToolbarController
- `BeautyToggleChanged`

### SidePanelController
- `PanelToggled`

## Melhorias Implementadas

1. **Separação de Responsabilidades**: Cada controller tem uma responsabilidade específica
2. **Código Limpo**: Removidas criações dinâmicas de elementos UI
3. **Cursores Polidos**: Sistema completo de gerenciamento de cursores
4. **Clima**: Sistema de clima integrado na topbar
5. **Staff Panel**: Painel dedicado para gerenciamento de equipe
6. **UX Melhorada**: Interface mais organizada e intuitiva
7. **ExecuteAlways**: Funciona no Editor para pré-visualização

## Migração do Código Antigo

O código antigo foi movido para `HUDControllerOld.cs` para referência. As principais mudanças:

1. **Removido**: Criação dinâmica de elementos UI
2. **Removido**: Lógica de staff do painel lateral
3. **Adicionado**: Controllers especializados
4. **Adicionado**: Sistema de clima
5. **Adicionado**: Gerenciamento de cursores
6. **Adicionado**: Staff panel dedicado

## Troubleshooting

### HUD não aparece
- Verifique se o `HUDVisualConfig` está configurado
- Confirme se o `UIDocument` está presente no GameObject

### Cursores não funcionam
- Verifique se as texturas de cursor estão na pasta correta
- Confirme se o `CursorManager` está presente

### Staff Panel não abre
- Verifique se o `StaffPanelController` está presente
- Confirme se o `StaffPanel.uxml` está configurado

### Clima não atualiza
- Verifique se o `WeatherServiceStub` está presente
- Confirme se `BindWeather()` foi chamado

