# MasterSplinter Roadmap Status

Last updated: May 31, 2026

## Current Goal

Modernize the archived Quasar-derived codebase into a new `MasterSplinter` repository
with a testable .NET 10 implementation first. The next major gate is functional parity:
the modern root projects should be able to run equivalent client/server behavior, with
tests proving that the modern behavior mirrors the legacy behavior we intentionally keep.

Only after that parity gate should we start adding the new roadmap features: Web API,
CLI, permissioned operators, consentful client UI, Windows service mode, cross-platform
expansion, and the GUI overhaul.

The guiding order is:

1. Preserve and test current behavior.
2. Extract portable protocol, networking, and orchestration code.
3. Rebuild the existing tool on modern .NET and prove parity.
4. Add explicit safety, permission, audit, and consent boundaries.
5. Build API/CLI/client surfaces on top of shared core libraries.
6. Expand platform support after behavior is covered by tests.

## Priority 0: Repository And Baseline

Status: Done

- Created a fresh git repository at `C:\Users\Jplou\develop\MasterSplinter`.
- Preserved the imported archived code under `legacy/Quasar`.
- Archived the nested fork history so `legacy/Quasar/.git` is no longer the active repository.
- Added the legacy project as imported source in the new root repository.
- Kept `legacy/Quasar/Quasar.sln` focused on legacy WinForms-era projects.
- Added root `.gitignore` coverage for modern build/test output.

## Priority 1: Modern .NET Test Baseline

Status: Done

- Moved modern work to root-level `src` and `tests` folders.
- Added `MasterSplinter.sln` as the modern acceptance solution.
- Retargeted the modern class libraries and tests to `net10.0`.
- Kept MSTest as the test framework for the first modernization pass.
- Established `dotnet test .\MasterSplinter.sln` as the primary acceptance check.
- Left legacy `legacy/Quasar/Quasar.sln` in place for the original Windows desktop code.

Current root projects:

- `src/MasterSplinter.Common`
- `src/MasterSplinter.Client.Core`
- `src/MasterSplinter.Client.Host`
- `src/MasterSplinter.Server.Core`
- `src/MasterSplinter.Server.Host`
- `tests/MasterSplinter.Common.Tests`
- `tests/MasterSplinter.Client.Core.Tests`
- `tests/MasterSplinter.Host.Tests`
- `tests/MasterSplinter.Server.Core.Tests`

Current verification:

- `MasterSplinter.Common.Tests`: 32 passed, 1 skipped.
- `MasterSplinter.Client.Core.Tests`: 7 passed.
- `MasterSplinter.Host.Tests`: 6 passed.
- `MasterSplinter.Server.Core.Tests`: 35 passed.

Known legacy limitation:

- `dotnet build .\legacy\Quasar\Quasar.sln` still fails on the legacy WinForms surface because
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
- Mirrored the full legacy protocol DTO surface in `MasterSplinter.Common`.
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

- Added `MasterSplinter.Client.Core`.
- Added message dispatch contracts and typed routing infrastructure.
- Added client identification factory for building modern `ClientIdentification` messages.
- Added `MasterSplinter.Client.Host` as a minimal modern client executable for parity wiring.
- Added a placeholder client smoke-test mode that creates a modern identification payload
  without opening a transport yet.
- Added tests for known-message dispatch, unknown-message handling, faulted handlers,
  duplicate registration, cancellation-token flow, and cancellation propagation.
- Added tests for mapping client identity options to the protocol identification message.

Left to do:

- Implement client-side handlers behind explicit interfaces.
- Extract client identity, reconnect behavior, and command dispatch contracts from legacy code.
- Split portable client behavior from Windows-only behavior.
- Add consent/status-oriented client state models before building a new client UI.

## Priority 4: Modern Server Core

Status: Started

Done:

- Added `MasterSplinter.Server.Core`.
- Added server-side client session abstractions.
- Added thread-safe client session registry.
- Added connection lifecycle contracts for connected, identified, disconnected, and faulted clients.
- Added lifecycle coordinator that records identified sessions and removes disconnected/faulted sessions.
- Added command dispatch result/status contracts.
- Added command dispatch requests with correlation IDs, optional operator ID, and optional source.
- Added shared command dispatcher that future Web API and CLI projects can call.
- Added audit event and audit sink abstractions with correlation, operator, source, message,
  outcome, and error fields.
- Added handshake coordinator and legacy identification validator for client identification.
- Added handshake result surface that preserves protocol version and capability metadata.
- Added minimal remote-client listener and connection abstractions.
- Added listener orchestrator that starts/stops listeners, records connection lifecycle,
  runs client identification handshakes, disconnects invalid pre-handshake traffic,
  and forwards post-handshake messages to an injected sink.
- Added `MasterSplinter.Server.Host` as a minimal modern server executable for parity wiring.
- Replaced the idle listener with a loopback-only TCP listener in `MasterSplinter.Server.Host`.
- Added loopback-only TCP handshake support in `MasterSplinter.Client.Host`.
- Added host tests for command-line option parsing, loopback-only address guards,
  and real loopback TCP handshake behavior.
- Added tests for session registration, replacement, removal, snapshots, invalid IDs,
  command dispatch, missing clients, send failures, audit events, and cancellation.
- Added tests for caller-supplied correlation metadata, generated correlation IDs,
  and audit metadata flow.
- Added tests for lifecycle event emission, registry updates, pre-identification disconnects,
  invalid lifecycle inputs, and lifecycle cancellation flow.
- Added tests for accepted identification, rejected legacy IDs, injectable validation,
  legacy clients without protocol metadata, and capability negotiation metadata.
- Added tests for listener start/stop, connection lifecycle routing, handshake result sending,
  invalid pre-handshake message disconnects, rejected identification disconnects, and
  post-handshake message forwarding.
- Added in-memory parity tests proving the modern client identification factory can complete
  the modern server handshake path without sockets.
- Added automated loopback TCP parity tests proving the modern client and server host transport
  can complete the identification handshake.
- Verified `MasterSplinter.Server.Host --smoke-test` starts and stops cleanly.
- Verified `MasterSplinter.Client.Host --smoke-test` creates a modern identification payload.
- Verified a two-process loopback TCP handshake: server host `--once` plus client host returns
  `Handshake result: True`.

Left to do:

- Extract server listener/orchestration behavior from `legacy/Quasar/Quasar.Server`.
- Define operator identity and authorization inputs for server commands.
- Add integration tests for client/server handshake and command dispatch once networking is extracted.

## Priority 5: Modern Runtime Parity

Status: Next

This is the main gate before adding new product features from the original roadmap.
The goal is not just to have DTOs and core contracts compiling on .NET 10; the goal is
to have a modern runnable client/server path that mirrors the legacy functionality we
choose to carry forward.

Planned work:

- Extract the server listener and client connection lifecycle into `MasterSplinter.Server.Core`.
- Extract the client connection, identity, reconnect, and message-routing behavior into
  `MasterSplinter.Client.Core`.
- Add a modern runnable server host, initially minimal and local, that uses the shared server core.
- Add a modern runnable client host, initially minimal and local, that uses the shared client core.
- Prove handshake parity against the legacy protocol fixtures.
- Prove command dispatch parity for a small safe vertical slice before moving broader behavior.
- Build out behavior tests for file-system operations, system info, process metadata, shell behavior,
  remote desktop contracts, registry behavior, startup/service behavior, and reverse proxy behavior
  before or while each area is extracted.
- Classify each legacy feature as keep, redesign, defer, or remove before porting behavior.

Parity acceptance should include:

- Modern client/server handshake succeeds using the modern `net10.0` projects.
- Existing wire fixtures remain compatible.
- Modern command routing matches the tested legacy message behavior.
- A documented capability matrix exists for supported, deferred, removed, and Windows-only features.
- `dotnet test .\MasterSplinter.sln` remains green.

## Priority 6: Permissioned Operators And Audit Logging

Status: Planned after modern runtime parity

- Add operator identity models.
- Add roles/permissions for administrative actions.
- Define which commands require explicit operator permission.
- Record audit events for operator login, client selection, command dispatch, command outcome,
  consent requests, and permission denials.
- Keep audit storage behind an interface so Web API, CLI, and desktop UI can share the same model.

Note: lightweight audit contracts already exist in `MasterSplinter.Server.Core` because they help
shape command dispatch safely. Full permissioned operators and persistent audit logging remain
post-parity roadmap features.

## Priority 7: Web API

Status: Planned after modern runtime parity

- Add a root-level Web API project after `MasterSplinter.Server.Core` owns orchestration contracts.
- Expose client inventory, session status, command dispatch, audit query, and capability discovery.
- Use the server core for all business logic.
- Do not duplicate server orchestration inside controllers.
- Add API tests around authorization, request validation, command routing, and audit recording.

## Priority 8: CLI Server

Status: Planned after modern runtime parity

- Add a root-level CLI project after `MasterSplinter.Server.Core` has stable orchestration APIs.
- Support running a server, listing clients, inspecting capabilities, and dispatching safe commands.
- Use the same shared server core as the Web API.
- Keep CLI output scriptable and testable.
- Consider a proxy/forwarding mode from the original roadmap once the listener model is extracted.

## Priority 9: Basic Client GUI, Status, And Consent

Status: Planned after modern runtime parity

- Add a basic client UI or status surface.
- Show connection status and active feature status.
- Notify the user when the client is installed or connected.
- Support consent prompts for sensitive operations such as remote desktop.
- Keep consent/status models in shared client core so different UIs can reuse them.

## Priority 10: Windows Service Mode

Status: Planned after modern runtime parity

- Add Windows service installation mode only after client behavior has clear contracts and tests.
- Keep service install/start/stop behavior behind platform-specific interfaces.
- Preserve a non-service mode for interactive and consent-oriented scenarios.
- Add Windows-specific tests around service abstractions where practical.

## Priority 11: Cross-Platform Support

Status: Planned after modern runtime parity and functional tests

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

## Priority 12: GUI Overhaul

Status: Long-term, after modern runtime parity and API/core boundaries

- Rework the operator GUI after the API/core boundary exists.
- Consider WPF or a web-based interface.
- Drive GUI behavior through Web API or shared server core instead of directly coupling to
  legacy WinForms handlers.
- Improve remote desktop rendering only after streaming contracts and consent behavior are defined.

## Priority 13: Transparent Protocol Documentation

Status: Ongoing, required for parity and long-term compatibility

- Document message framing, protobuf contracts, version fields, capabilities, and compatibility rules.
- Publish a protocol compatibility matrix.
- Use the current pinned wire fixtures as the source of truth for examples.
- Keep protocol documentation updated whenever message contracts change.

## Priority 14: Remaining Legacy Behavior Extraction

Status: Planned as part of modern runtime parity

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

1. Add TLS/certificate validation parity for the transport handshake.
2. Continue extracting tested legacy behavior until the modern runtime parity gate is met.
3. Start original roadmap features: permissioned operators, Web API, CLI, consent UI,
   Windows service mode, cross-platform expansion, and GUI overhaul.

## Acceptance Checks

Current modern acceptance:

```powershell
dotnet test .\MasterSplinter.sln
```

Current server host smoke test:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Server.Host\MasterSplinter.Server.Host.csproj -- --smoke-test
```

Current client host smoke test:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --smoke-test
```

Current loopback handshake check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Server.Host\MasterSplinter.Server.Host.csproj -- --port 47830 --once
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47830
```

Legacy check, for awareness:

```powershell
dotnet test .\legacy\Quasar\Quasar.sln
```

The modern root solution is the main quality gate until the legacy WinForms surface is either
retired, isolated further, or deliberately repaired.
