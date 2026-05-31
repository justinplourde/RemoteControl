# LocationRemote Roadmap Status

Last updated: May 31, 2026

## Current Goal

Modernize the archived Quasar-derived codebase into a new `LocationRemote` repository
with a testable .NET 10 core first. New Web API, CLI, client, and service projects
should live at the repository root beside shared class libraries instead of being
buried inside the legacy `MasterSplinter` application folder.

The guiding order is:

1. Preserve and test current behavior.
2. Extract portable protocol and orchestration code.
3. Add explicit safety, permission, audit, and consent boundaries.
4. Build API/CLI/client surfaces on top of shared core libraries.
5. Expand platform support after behavior is covered by tests.

## Priority 0: Repository And Baseline

Status: Done

- Created a fresh git repository at `C:\Users\Jplou\develop\LocationRemote`.
- Preserved the imported archived code under `MasterSplinter`.
- Archived the nested fork history so `MasterSplinter/.git` is no longer the active repository.
- Added the legacy project as imported source in the new root repository.
- Kept `MasterSplinter/Quasar.sln` focused on legacy WinForms-era projects.
- Added root `.gitignore` coverage for modern build/test output.

## Priority 1: Modern .NET Test Baseline

Status: Done

- Moved modern work to root-level `src` and `tests` folders.
- Added `LocationRemote.sln` as the modern acceptance solution.
- Retargeted the modern class libraries and tests to `net10.0`.
- Kept MSTest as the test framework for the first modernization pass.
- Established `dotnet test .\LocationRemote.sln` as the primary acceptance check.
- Left legacy `MasterSplinter/Quasar.sln` in place for the original Windows desktop code.

Current root projects:

- `src/LocationRemote.Common`
- `src/LocationRemote.Client.Core`
- `src/LocationRemote.Server.Core`
- `tests/LocationRemote.Common.Tests`
- `tests/LocationRemote.Client.Core.Tests`
- `tests/LocationRemote.Server.Core.Tests`

Current verification:

- `LocationRemote.Common.Tests`: 32 passed, 1 skipped.
- `LocationRemote.Client.Core.Tests`: 6 passed.
- `LocationRemote.Server.Core.Tests`: 10 passed.

Known legacy limitation:

- `dotnet build .\MasterSplinter\Quasar.sln` still fails on the legacy WinForms surface because
  the installed .NET SDK reports resource-generation issues in `Quasar.Server`, and Windows
  Security blocks the legacy `Quasar.Common.dll` output as potentially unwanted software.
  This is one of the reasons the root modern libraries are being extracted and tested separately.

## Priority 2: Modern Shared Protocol Surface

Status: Done

- Added modern protocol serialization tests.
- Added additive protocol version and client capability metadata.
- Added payload length-prefix reader/writer coverage.
- Added file-transfer protocol contracts.
- Added file-system protocol contracts for drive listing, directory listing, path rename/delete,
  and file-manager status.
- Mirrored the full legacy protocol DTO surface in `LocationRemote.Common`.
- Added reflection coverage so every modern message contract round-trips through `IMessage`.
- Added pinned wire-compatibility fixtures, including full-surface representative fixtures for
  every `IMessage` DTO.
- Kept protobuf field numbers stable and additive.
- Confirmed older payloads that omit new metadata can still deserialize safely.

Protocol rules that remain active:

- Existing protobuf field numbers are frozen once added.
- Versioning and capability metadata must remain additive.
- Unknown future fields must be tolerated by readers.
- Any message-contract changes need compatibility tests before behavior changes.

## Priority 3: Modern Client Core

Status: Started

Done:

- Added `LocationRemote.Client.Core`.
- Added message dispatch contracts and typed routing infrastructure.
- Added tests for known-message dispatch, unknown-message handling, faulted handlers,
  duplicate registration, cancellation-token flow, and cancellation propagation.

Left to do:

- Implement client-side handlers behind explicit interfaces.
- Extract client identity, reconnect behavior, and command dispatch contracts from legacy code.
- Split portable client behavior from Windows-only behavior.
- Add consent/status-oriented client state models before building a new client UI.

## Priority 4: Modern Server Core

Status: Started

Done:

- Added `LocationRemote.Server.Core`.
- Added server-side client session abstractions.
- Added thread-safe client session registry.
- Added command dispatch result/status contracts.
- Added shared command dispatcher that future Web API and CLI projects can call.
- Added audit event and audit sink abstractions.
- Added tests for session registration, replacement, removal, snapshots, invalid IDs,
  command dispatch, missing clients, send failures, audit events, and cancellation.

Left to do:

- Extract server listener/orchestration behavior from `MasterSplinter/Quasar.Server`.
- Define connection lifecycle events for connected, identified, disconnected, and faulted clients.
- Define operator identity and authorization inputs for server commands.
- Add command correlation IDs so API/CLI calls can be traced through audit logs.
- Add integration tests for client/server handshake and command dispatch once networking is extracted.

## Priority 5: Permissioned Operators And Audit Logging

Status: Planned

- Add operator identity models.
- Add roles/permissions for administrative actions.
- Define which commands require explicit operator permission.
- Record audit events for operator login, client selection, command dispatch, command outcome,
  consent requests, and permission denials.
- Keep audit storage behind an interface so Web API, CLI, and desktop UI can share the same model.

## Priority 6: Web API

Status: Planned

- Add a root-level Web API project after `LocationRemote.Server.Core` owns orchestration contracts.
- Expose client inventory, session status, command dispatch, audit query, and capability discovery.
- Use the server core for all business logic.
- Do not duplicate server orchestration inside controllers.
- Add API tests around authorization, request validation, command routing, and audit recording.

## Priority 7: CLI Server

Status: Planned

- Add a root-level CLI project after `LocationRemote.Server.Core` has stable orchestration APIs.
- Support running a server, listing clients, inspecting capabilities, and dispatching safe commands.
- Use the same shared server core as the Web API.
- Keep CLI output scriptable and testable.
- Consider a proxy/forwarding mode from the original roadmap once the listener model is extracted.

## Priority 8: Basic Client GUI, Status, And Consent

Status: Planned

- Add a basic client UI or status surface.
- Show connection status and active feature status.
- Notify the user when the client is installed or connected.
- Support consent prompts for sensitive operations such as remote desktop.
- Keep consent/status models in shared client core so different UIs can reuse them.

## Priority 9: Windows Service Mode

Status: Planned

- Add Windows service installation mode only after client behavior has clear contracts and tests.
- Keep service install/start/stop behavior behind platform-specific interfaces.
- Preserve a non-service mode for interactive and consent-oriented scenarios.
- Add Windows-specific tests around service abstractions where practical.

## Priority 10: Cross-Platform Support

Status: Planned after modernization and functional tests

The answer to whether modern .NET enables more than Windows is yes, but only for the parts
that are actually portable or deliberately abstracted. The first cross-platform work should
be shared infrastructure and basic capabilities, not full feature parity.

Planned approach:

- Split portable logic from Windows-specific behavior.
- Keep protocol, networking, client identity, reconnect logic, command dispatch contracts,
  and server orchestration in shared core libraries.
- Create platform capability contracts for system info, file system access, shell execution,
  process inspection, remote desktop, and service/daemon installation.
- Implement Windows first, then Linux/macOS where capability and consent models make sense.
- Mark each remote capability as portable, platform-specific, or Windows-only.

Likely capability categories:

- Portable: protocol serialization, connection lifecycle, command routing, audit logging,
  capability reporting, file-system metadata, basic process metadata where supported.
- Platform-specific: shell execution, process management, service/daemon install,
  remote desktop capture, privilege/elevation, registry-like configuration stores.
- Windows-only unless rethought: Windows registry operations, UAC/elevation flows,
  WinForms desktop tooling, Windows service implementation.

## Priority 11: GUI Overhaul

Status: Long-term

- Rework the operator GUI after the API/core boundary exists.
- Consider WPF or a web-based interface.
- Drive GUI behavior through Web API or shared server core instead of directly coupling to
  legacy WinForms handlers.
- Improve remote desktop rendering only after streaming contracts and consent behavior are defined.

## Priority 12: Transparent Protocol Documentation

Status: Long-term, partially enabled by current tests

- Document message framing, protobuf contracts, version fields, capabilities, and compatibility rules.
- Publish a protocol compatibility matrix.
- Use the current pinned wire fixtures as the source of truth for examples.
- Keep protocol documentation updated whenever message contracts change.

## Priority 13: Remaining Legacy Behavior Extraction

Status: Planned

Areas still deferred from the legacy app:

- Remote desktop image compression and streaming.
- Registry operations.
- Process and shell execution behavior.
- File-system access behavior beyond DTO contracts.
- Client command handlers.
- Client installation, startup, updater, and uninstaller behavior.
- Reverse proxy behavior.
- Password recovery and keylogging-era legacy features need explicit product/security decisions
  before any modernization work continues on them.
- Windows-specific platform helpers and native methods.

## Immediate Next Steps

Recommended next sequence:

1. Add server connection lifecycle contracts to `LocationRemote.Server.Core`.
2. Add command correlation IDs and richer audit fields.
3. Extract handshake orchestration tests for client identification and capability negotiation.
4. Add a minimal listener abstraction that can be implemented by the legacy socket server first.
5. Add root-level Web API scaffold using only `LocationRemote.Server.Core` abstractions.
6. Add CLI scaffold after the same server-core API proves usable from Web API.
7. Start platform capability interfaces before moving Windows-specific behavior.

## Acceptance Checks

Current modern acceptance:

```powershell
dotnet test .\LocationRemote.sln
```

Legacy check, for awareness:

```powershell
dotnet test .\MasterSplinter\Quasar.sln
```

The modern root solution is the main quality gate until the legacy WinForms surface is either
retired, isolated further, or deliberately repaired.
