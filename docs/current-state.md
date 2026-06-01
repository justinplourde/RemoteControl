# MasterSplinter Current State

Last updated: June 1, 2026

## Fresh Chat Handoff

This repository was renamed from `LocationRemote` to `MasterSplinter`. If a Codex
chat says its working directory is missing, open a new chat from:

```powershell
C:\Users\Jplou\develop\MasterSplinter
```

Then ask the new chat to read this file and `docs/roadmap-status.md`.

## Repository

- Root: `C:\Users\Jplou\develop\MasterSplinter`
- Current solution: `MasterSplinter.sln`
- Legacy imported source: `legacy/Quasar`
- Legacy solution: `legacy/Quasar/Quasar.sln`
- Latest committed rename checkpoint: `2e8fa30 Rename modern workspace to MasterSplinter`

The modern work is intentionally in root-level `src` and `tests` folders. The legacy
Quasar code is preserved separately as reference material and parity source.

## Verification

Primary acceptance check:

```powershell
dotnet test .\MasterSplinter.sln
```

Latest result from June 1, 2026:

- `MasterSplinter.Common.Tests`: 32 passed, 1 skipped
- `MasterSplinter.Client.Core.Tests`: 13 passed
- `MasterSplinter.Server.Core.Tests`: 35 passed
- `MasterSplinter.Host.Tests`: 9 passed
- Total: 89 passed, 1 skipped, 0 failed

Current smoke checks:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Server.Host\MasterSplinter.Server.Host.csproj -- --smoke-test
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --smoke-test
```

Current loopback handshake check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Server.Host\MasterSplinter.Server.Host.csproj -- --port 47830 --once
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47830
```

The latest manual loopback check returned `Handshake result: True`.

## Modern Projects

- `src/MasterSplinter.Common`: protocol DTOs, shared models, crypto helpers, payload reader/writer.
- `src/MasterSplinter.Client.Core`: client dispatch contracts, response-handler adapters, client identification factory, system-info handling, and drive-list handling.
- `src/MasterSplinter.Client.Host`: minimal runnable client host with smoke mode, loopback handshake, and one-command handling mode.
- `src/MasterSplinter.Server.Core`: session registry, handshake coordination, lifecycle contracts, listener orchestration, audit and command dispatch contracts.
- `src/MasterSplinter.Server.Host`: minimal runnable loopback-only server host.
- `tests/*`: MSTest coverage for the modern projects.

All modern projects target `net10.0`.

## Decisions

- Modernization and parity come before new roadmap features.
- Keep the legacy Quasar source as reference under `legacy/Quasar`.
- Preserve existing wire compatibility until tests define a versioned upgrade path.
- Protocol changes must be additive and covered by serialization/wire tests.
- Cross-platform work starts only after portable behavior is covered by tests.
- Web API, CLI, consentful clients, service mode, and GUI overhaul are post-parity roadmap work.

## Completed

- Fresh repository created and renamed to `MasterSplinter`.
- Legacy imported code moved to `legacy/Quasar`.
- Modern solution renamed to `MasterSplinter.sln`.
- Modern namespaces/projects renamed from `LocationRemote.*` to `MasterSplinter.*`.
- Modern shared protocol surface mirrored with tests.
- Client core dispatch and identification contracts added.
- Server core session, lifecycle, handshake, listener, audit, and dispatch contracts added.
- Minimal client/server hosts added.
- Loopback TCP handshake path added and tested.
- Loopback TCP server-to-client command dispatch path added and tested.
- `GetSystemInfo` client handler added with deterministic tests.
- Loopback TCP `GetSystemInfo` command-response path added and tested.
- `GetDrives` client handler added with deterministic tests.
- Loopback TCP `GetDrives` command-response path added and tested.

## Current Limitations

- Legacy `legacy/Quasar/Quasar.sln` is preserved but is not the primary acceptance gate.
- The legacy WinForms surface has known build/security friction on the current machine.
- Modern hosts currently prove loopback handshake parity, not full remote-management behavior.
- File-system, process, shell, registry, desktop, service, and UI behavior are not fully extracted yet.

## Recommended Next Tasks

1. Extract the next small read-only client handler behind explicit interfaces.
2. Add parity tests against legacy behavior before moving each behavior slice.
3. Build a capability matrix that marks each feature as portable, Windows-only, deferred, or removed.
4. Once runtime parity is proven, resume roadmap features: permissioned operators, audit persistence, Web API, CLI, consentful client UI, service mode, cross-platform expansion, and GUI overhaul.
