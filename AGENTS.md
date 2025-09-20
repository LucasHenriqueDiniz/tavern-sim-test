# Unity contributor handbook

This repository contains a Unity 2022.3.62f1 project. The notes below document the expectations for tooling, coding, and testing so contributions remain reproducible across machines.

## Toolchain & packages
- Author or modify assets using **Unity 2022.3.62f1**. When upgrading, update this document and verify the project opens cleanly.
- Required packages include:
  - AI Navigation (`com.unity.ai.navigation`) for `NavMeshSurface`, `NavMeshAgent`, and related components.
  - Input System (`com.unity.inputsystem`) with both Player Input and the generated input actions committed.
  - Any other dependencies present in `Packages/manifest.json`—avoid removing or downgrading them without coordination.
- Never hand-craft `.meta` files or binary assets. Always create ScriptableObjects, prefabs, and scenes through the Unity editor so GUIDs remain stable.
- Commit new assets together with their `.meta` files. If an asset is deleted, delete its `.meta` as well.

## Project & source layout
- Runtime C# scripts live under `Assets/` and use the `TavernSim.*` namespace tree. Keep editor utilities inside `Assets/Editor/` and gate them with `#if UNITY_EDITOR` when necessary.
- Keep serialized fields private with `[SerializeField]` unless public access is required. Avoid adding `try/catch` around imports or other boilerplate.
- Prefer explicit null/`TryGet` checks with informative `Debug.LogWarning` messages when optional references may be missing—this eases diagnosis in editor and CI runs.
- Use assembly definition files (`.asmdef`) when adding new code folders so edit mode tests can target smaller assemblies and to speed up player builds.

## Testing expectations
- Follow the Unity Test Framework conventions outlined in Codex’s Unity testing overview: organise tests with the Arrange/Act/Assert pattern, use `[SetUp]` to prepare fixtures, and keep tests deterministic (no reliance on `Time.deltaTime`, randomness, or actual scene state).
- Author **Edit Mode** tests for pure logic, domain models, and utility classes—they execute quickly in headless CI.
- Use **Play Mode** tests when behaviour depends on scene objects, coroutines, or `MonoBehaviour` lifecycles. Limit scene dependencies by instantiating prefabs in setup methods and cleaning them up in `[TearDown]`.
- When touching gameplay systems, add or update tests under `Assets/Tests/` and ensure they run locally via the Unity Test Runner (`Test > Run All Tests`) or the command line (`-runTests`) before committing.
- If a change cannot be validated in-editor (e.g., hardware-specific), clearly document the limitation in your summary.

## Workflow & pull requests
- Run the project in Play Mode after modifying gameplay code or assets that impact runtime behaviour.
- Keep the working tree clean (`git status` empty) before finishing a task. Each change set should be committed with a descriptive message.
- Ensure the project opens without missing packages or compile errors. When adding packages, update both `manifest.json` and `packages-lock.json`.
- Update documentation (including this file) whenever workflow expectations change so future contributors have accurate guidance.
