# Unity contributor handbook

This repository targets **Unity 2022.3.62f1**. The guidance below codifies our workflow expectations so local development, CI,
and Codex assistance stay aligned.

## Toolchain & packages
- Open and author the project with Unity 2022.3 LTS (62f1). If you intentionally upgrade, update this document, bump CI, and
  confirm the editor launches without compile errors.
- Keep required packages installed and locked:
  - AI Navigation (`com.unity.ai.navigation`) for `NavMeshSurface`, `NavMeshAgent`, and NavMesh baking.
  - Input System (`com.unity.inputsystem`) with the generated input actions checked in.
  - Any dependency already listed in `Packages/manifest.json`. Coordinate before removing or downgrading items.
- Never hand-craft `.meta` files—create and delete assets through the editor so GUIDs remain stable. Commit assets together with
  their matching `.meta` files.

## Project & source layout
- Runtime scripts belong under `Assets/` and should use the `TavernSim.*` namespace hierarchy. Editor utilities live in
  `Assets/Editor/` and must be wrapped in `#if UNITY_EDITOR` guards when they reference editor-only APIs.
- Prefer private `[SerializeField]` members unless public exposure is required. Avoid defensive `try/catch` blocks around imports
  or other boilerplate.
- Use assembly definition files to isolate systems and tests; hook up references (including package assemblies such as
  `Unity.AI.Navigation`) explicitly when creating new modules.

### HUD menu & toast system
- The runtime HUD now instantiates a `MenuController` (UI Toolkit) that exposes a foldout with toggles for every `RecipeSO` in the
  `Catalog`. The component implements `IMenuPolicy` and persists toggle state via `PlayerPrefs` using the recipe id as the key.
  `AgentSystem.SetMenuPolicy` must be called during bootstrap (see `DevBootstrap`) so waiters can respect menu restrictions
  before submitting orders.
- `HudToastController` subscribes to the shared `GameEventBus` and renders temporary toast notifications at the bottom-left of the
  HUD. Gameplay systems publish events through the `IEventBus` (either by injecting `GameEventBus` or by calling
  `HUDController.PublishEvent`). Core events include menu blocks, missing ingredients, customer anger, and order readiness/delivery.
- When creating new systems that need to notify the player, prefer publishing `GameEvent` instances rather than logging directly.
  The bootstrap scene wires everything together; other scenes must inject the event bus into `OrderSystem`, `AgentSystem`, and the
  HUD components to enable the experience.

### Build & staffing controls
- The build menu in the HUD now exposes toggles for kitchen stations, bar counters, and pickup point markers alongside the table
  and decoration props. The `GridPlacer` handles these new `PlaceableKind` entries by spawning simple graybox meshes and marking
  the appropriate NavMesh obstacles so pathing remains valid.
- Runtime HUD controls include "Contratar garçom" and "Contratar cozinheiro" buttons. Each click spawns a new NavMesh-agent
  backed capsule; waiters are automatically registered with `AgentSystem`, which keeps customer assignments unique across
  multiple staffers.

## Continuous integration
- Pull requests must keep the GitHub Actions workflow green. The pipeline contains two jobs:
  - **Tests** – `game-ci/unity-test-runner@v4` runs EditMode + PlayMode tests against Unity 2022.3.62f1.
  - **Validation** – executes `TavernSim.Editor.CI.ProjectValidation.Run` via `-executeMethod` to verify packages, required
    MonoBehaviours, duplicate types, and other guardrails.
- Expectation: a PR is only mergeable once both jobs pass. Update this document and the workflow together if CI requirements
  change.

## Local automation
- Unity automation:
  - Validation menu: `Tools → TavernSim → Validate Project`.
  - CLI validation:
    ```bash
    <UnityEditorPath>/Unity -quit -batchmode -nographics \
      -projectPath "<repo>" \
      -executeMethod TavernSim.Editor.CI.ProjectValidation.Run
    ```
  - CLI tests:
    ```bash
    <UnityEditorPath>/Unity -quit -batchmode -nographics \
      -projectPath "<repo>" \
      -runTests -testResults results.xml
    ```
- Offline .NET harness (for environments without Unity):
  - Use `Tools/OfflineTests/OfflineTests.csproj`, which stubs the minimal Unity APIs required by our logic tests and links the
    real game code plus EditMode test files.
  - Run it with `dotnet test Tools/OfflineTests/OfflineTests.csproj`. This is not a substitute for Unity Test Runner but mirrors
    the critical EditMode coverage so Codex and CI-like environments can execute quick checks.

## Testing expectations
- Follow the Arrange/Act/Assert pattern. Use `[SetUp]` for fixture preparation and keep tests deterministic (avoid reliance on
  real time, randomness, or scene state).
- Write **EditMode** tests for pure logic, domain models, and utilities—they run fastest in CI.
- Use **PlayMode** tests when lifecycle behaviour or scenes are involved. Instantiate prefabs in setup and dispose of them in
  `[TearDown]` to keep tests isolated.
- Run the Unity Test Runner (or the CLI above) before committing gameplay changes. Document any scenario you could not verify in
  this environment.

## Workflow & pull requests
- Launch Play Mode after modifying gameplay scripts or assets that impact runtime behaviour.
- Keep `git status` clean before concluding work. Commit logical chunks with descriptive messages.
- Ensure the project opens without missing packages or compile errors. Update both `manifest.json` and `packages-lock.json`
  whenever dependencies change.
- Sync this handbook whenever workflow expectations evolve so future contributors share the same context.

## Working with AI assistance

### When you are the assistant
- Read the entire prompt and restate the requester’s goal (bugfix, feature, tooling, workflow) before providing code.
- Confirm referenced files or symbols exist in this repo. Ask for missing snippets or clarify ambiguities instead of guessing.
- Prefer targeted diffs pointing to exact Unity asset paths. Highlight when scripting defines (`ENABLE_INPUT_SYSTEM`,
  `ENABLE_LEGACY_INPUT_MANAGER`, etc.) affect behaviour.
- Explain *why* each change helps, tying guidance to Unity patterns (reflection avoidance, PlayerInput usage, coroutine
  lifecycles, etc.) so maintainers learn alongside the fix.
- Call out environment limits (no Unity Editor/Play Mode here) and give concrete local follow-up actions—run Play Mode, refresh
  packages, regenerate NavMesh, and so on.
- When multiple solutions exist (Input System vs. legacy input, UI Toolkit vs. IMGUI), compare trade-offs and recommend the most
  maintainable option for this project.
- Capture assumptions about Unity version, packages, or scripting defines so future assistants inherit the right context.

### When you are requesting help
- Include relevant file paths and snippets so the assistant can reason about namespaces, symbols, and conditional compilation.
- State the gameplay or tooling objective to keep suggestions aligned with Unity best practices.
- Provide full compiler or console messages (file, line, condition) to pinpoint issues quickly.
- Mention project-wide context (Unity version, active packages, scripting defines) to avoid incorrect assumptions.
- Summarise previous attempts and partial fixes to prevent repetitive advice and focus on the next viable option.
- Remember: this environment cannot open the Unity Editor. Plan to validate fixes locally and note any unverified behaviour in
  your summary.
