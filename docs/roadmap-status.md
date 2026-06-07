# MasterSplinter Roadmap Status

Last updated: June 2, 2026

## Current Goal

Modernize the archived Quasar-derived codebase inside the `RemoteControl` repository,
with `MasterSplinter` as the modern product/solution name and a testable .NET 10
implementation first. The next major gate is legacy admin-tool feature parity:
the modern root projects should be able to run equivalent client/server behavior for
legitimate administration features, with tests proving that the modern behavior mirrors
the legacy behavior we intentionally keep. Sensitive legacy features may require
compatibility parity first and redesigned runtime behavior before they are enabled.

Only after that parity gate should we start adding the new roadmap features: Web API,
CLI, permissioned operators, consentful client UI, Windows service mode, cross-platform
expansion, and the GUI overhaul.

Current decision: do not start the Web API until full legacy admin-tool parity for kept
features is confirmed.

The guiding order is:

1. Preserve and test current behavior.
2. Extract portable protocol, networking, and orchestration code.
3. Rebuild the existing tool on modern .NET and prove parity.
4. Add explicit safety, permission, audit, and consent boundaries.
5. Build API/CLI/client surfaces on top of shared core libraries.
6. Expand platform support after behavior is covered by tests.

## Priority 0: Repository And Baseline

Status: Done

- Created a fresh git repository, now located at `C:\Users\Jplou\develop\RemoteControl`.
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
- `src/MasterSplinter.Cli`
- `src/MasterSplinter.Server.Core`
- `src/MasterSplinter.Server.Host`
- `tests/MasterSplinter.Common.Tests`
- `tests/MasterSplinter.Client.Core.Tests`
- `tests/MasterSplinter.Cli.Tests`
- `tests/MasterSplinter.Host.Tests`
- `tests/MasterSplinter.Server.Core.Tests`

Current verification:

- `MasterSplinter.Common.Tests`: 32 passed, 1 skipped.
- `MasterSplinter.Client.Core.Tests`: 49 passed.
- `MasterSplinter.Cli.Tests`: 8 passed.
- `MasterSplinter.Host.Tests`: 15 passed.
- `MasterSplinter.Server.Core.Tests`: 51 passed.

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
- Added response-handler adapters for client commands that emit protocol responses.
- Added `GetSystemInfo` handling behind `ISystemInfoProvider`.
- Added `GetDrives` handling behind `IDriveProvider`.
- Added `GetDirectory` handling behind `IDirectoryProvider`.
- Added `GetProcesses` handling behind `IProcessProvider`.
- Added `GetStartupItems` handling behind `IStartupItemProvider`.
- Added `GetConnections` handling behind `IConnectionProvider`.
- Added `--handle-one-command` mode to the client host for one command-response loopback slice.
- Added `--handle-commands` mode to keep the loopback client connected for repeated manual
  CLI dispatches.
- Added tests for known-message dispatch, unknown-message handling, faulted handlers,
  duplicate registration, cancellation-token flow, and cancellation propagation.
- Added tests for mapping client identity options to the protocol identification message.
- Added tests for `GetSystemInfo` response mapping and response-handler send behavior.
- Added deterministic tests for `GetSystemInfo` provider field ordering and fallback behavior.
- Added tests for `GetDrives` success mapping, legacy-style status errors, and response-handler send behavior.
- Added tests for `GetDirectory` success mapping, legacy-style status errors, and response-handler send behavior.
- Added tests for `GetProcesses` response mapping and response-handler send behavior.
- Added tests for `GetStartupItems` success mapping, legacy-style status errors, and response-handler send behavior.
- Added tests for `GetConnections` response mapping and response-handler send behavior.
- Added tests for `DoProcessEnd` success/failure response mapping and protected-PID rejection.
- Added tests for `DoProcessStart` success/failure response mapping, blank-path rejection,
  and explicit rejection of URL-download/update start requests.
- Added tests for `DoLoadRegistryKey` success/error response mapping and read-only command
  safety classification.
- Added tests for `DoCloseConnection` handler routing, CLI four-tuple parsing/message creation,
  and `NetworkControl` permission classification.

Left to do:

- Implement more client-side handlers behind explicit interfaces.
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
- Added command safety classification metadata for read-only, state-changing, consent-sensitive,
  credential-access, and keystroke-access command families.
- Added default command dispatch policy enforcement that denies permission- or consent-gated
  commands unless the dispatch request carries explicit authorization.
- Added operator identity, operator permission, client consent, and command authorization
  service contracts for deriving dispatch authorization before command policy enforcement.
- Added shared command dispatcher that future Web API and CLI projects can call.
- Added audit event and audit sink abstractions with correlation, operator, source, message,
  safety class, consent/permission flags, outcome, and error fields.
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
- Added host integration coverage proving server command dispatch can send a command
  over an identified loopback TCP session.
- Added tests for session registration, replacement, removal, snapshots, invalid IDs,
  command dispatch, missing clients, send failures, audit events, and cancellation.
- Added tests for caller-supplied correlation metadata, generated correlation IDs,
  and audit metadata flow.
- Added tests proving completed read-only commands classify as read-only and sensitive
  parity-target commands require permission, with consent where appropriate.
- Added tests proving policy-denied commands are audited and not sent, while sensitive commands
  with permission and consent are dispatched.
- Added tests proving authorization grants are derived from operator permission and client
  consent services.
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
- Added automated loopback TCP command dispatch coverage proving a server command can reach
  the connected client stream after identification.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetSystemInfo` and send `GetSystemInfoResponse` back to the server sink.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetDrives` and send `GetDrivesResponse` back to the server sink.
- Added optional loopback TCP TLS 1.2 support and host tests proving matching pinned
  server certificates complete handshakes while mismatched certificates are rejected.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetDirectory` and send `GetDirectoryResponse` back to the server sink.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetProcesses` and send `GetProcessesResponse` back to the server sink.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetStartupItems` and send `GetStartupItemsResponse` back to the server sink.
- Added automated loopback TCP command-response coverage proving the modern client can handle
  `GetConnections` and send `GetConnectionsResponse` back to the server sink.
- Verified `MasterSplinter.Server.Host --smoke-test` starts and stops cleanly.
- Verified `MasterSplinter.Client.Host --smoke-test` creates a modern identification payload.
- Verified a two-process loopback TCP handshake: server host `--once` plus client host returns
  `Handshake result: True`.
- Added `MasterSplinter.Cli` as a minimal operator CLI that can host one loopback listener,
  accept one client, dispatch `GetSystemInfo`, and print the response.
- Added CLI option parsing coverage and included `MasterSplinter.Cli.Tests` in the root solution.
- Verified a two-process CLI dispatch smoke: CLI `dispatch --command get-system-info` plus client
  host `--handle-one-command` returns `Dispatch result: Sent` and `GetSystemInfoResponse`.
- Expanded CLI dispatch to the current read-only parity slices: `GetSystemInfo`, `GetDrives`,
  `GetDirectory`, `GetProcesses`, `GetStartupItems`, and `GetConnections`.
- Routed CLI dispatch through command safety classification and authorization-service plumbing
  before sending commands through `ServerCommandDispatcher`.
- Verified a two-process CLI `get-drives` smoke returns `Dispatch result: Sent`,
  `Safety=FileRead`, and `GetDrivesResponse`.
- Added CLI response formatting rows for system info, drives, directory entries, processes,
  startup items, and TCP connections.
- Verified the latest two-process CLI `get-drives` smoke prints
  `- C:\ (OS) [Local Disk, NTFS] => C:\`.
- Added CLI `listen` mode with `clients`, `dispatch <client-id|first> <command>`, `help`,
  and `exit` commands for repeated manual dispatch against connected clients.
- Verified a persistent CLI/client session can list the connected client, run `get-drives`,
  then run `get-system-info` on the same client connection.
- Verified a pre-enrichment full read-only CLI parity pass on one persistent client connection:
  `get-system-info` returned 19 rows, `get-drives` returned 1 drive, `get-directory --path C:\` returned 24 entries,
  `get-processes` returned 280 processes, `get-startup-items` returned 5 entries, and
  `get-connections` returned 47 TCP connections.
- Improved `GetSystemInfo` provider parity with Windows/local WMI-backed CPU, RAM, GPU,
  security-product, LAN/MAC, uptime, and bounded public network enrichment.
- Verified a focused CLI `get-system-info` pass populated CPU, RAM, GPU, uptime, MAC, LAN IP,
  WAN IP, ASN, ISP, antivirus, firewall, time zone, and country on this PC.
- Re-ran the full read-only CLI parity pass after system-info enrichment on June 2, 2026:
  `get-system-info` returned 19 populated rows, `get-drives` returned 1 drive,
  `get-directory --path C:\` returned 24 entries, `get-processes` returned 302 processes,
  `get-startup-items` returned 5 entries, and `get-connections` returned 50 TCP connections.
- Added permissioned client-to-operator file download through the legacy file-transfer messages:
  client handler streams chunks and completion/cancel responses, CLI saves to a non-overwriting
  output path, and focused tests cover chunking, cancel behavior, CLI parsing/formatting, and
  `FileRead` safety metadata.
- Verified loopback `download-file` manually on June 2, 2026 with `--grant-permission`; a
  35-byte temp file was saved through the CLI and source/output SHA-256 hashes matched.
- Added permissioned operator-to-client file upload through the legacy file-transfer messages:
  CLI streams chunks through `FileWrite` policy enforcement, the client writes to an in-progress
  temp file and finalizes only on the declared byte count, and focused tests cover completion,
  offset-mismatch cleanup, cancel cleanup, CLI parsing, and safety metadata.
- Verified loopback `upload-file` manually on June 2, 2026 with `--grant-permission`; a
  34-byte temp file was written to a client destination and source/remote SHA-256 hashes matched.
- Added permissioned path rename through `DoPathRename` and `SetStatusFileManager`: the client
  handler supports file/directory renames, refuses target overwrite, returns legacy-style status,
  and focused tests cover success, existing-target rejection, CLI parsing/formatting, and
  `FileWrite` safety metadata.
- Verified loopback `rename-path` manually on June 2, 2026 with `--grant-permission`; a
  34-byte temp file was renamed, the old path disappeared, the new path existed, and SHA-256
  matched the pre-rename hash.
- Added permissioned file delete through `DoPathDelete` and `SetStatusFileManager`: the client
  handler deletes files, intentionally refuses directory delete until recursive policy is defined,
  and focused tests cover success, directory refusal, CLI parsing, and `FileWrite` safety metadata.
- Verified loopback `delete-path` manually on June 2, 2026 with `--grant-permission`; a temp file
  returned `Deleted file` and no longer existed after the command.
- Added consent-gated process end through `DoProcessEnd` and `DoProcessResponse`: the client
  handler terminates eligible PIDs, refuses protected/self PIDs, CLI parsing and response
  formatting are covered, and execution safety metadata requires both permission and consent.
- Verified loopback `end-process` manually on June 2, 2026 with `--grant-permission --grant-consent`;
  a harmless spawned sleep process returned `Process response: Action=End; Result=True.` and
  the PID was no longer alive afterward.
- Added consent-gated local process start through `DoProcessStart` and `DoProcessResponse`: the
  client handler starts explicit local file paths only, rejects URL-download/update requests for
  now, CLI parsing and response formatting are covered, and execution safety metadata requires
  both permission and consent.
- Verified loopback `start-process` manually on June 2, 2026 with `--grant-permission --grant-consent`;
  a harmless temp command script returned `Process response: Action=Start; Result=True.` and
  wrote its expected marker file afterward.
- Added read-only registry key loading through `DoLoadRegistryKey` and `GetRegistryKeysResponse`:
  the client handler loads child keys and value metadata behind a registry provider, CLI parsing
  and response formatting are covered, and registry load is classified as read-only inventory.
- Verified loopback `get-registry-key` manually on June 2, 2026 without permission or consent
  grants; `HKCU\Software` returned `Safety=ReadOnlyInventory`, `RequiresPermission=False`,
  `RequiresConsent=False`, and 17 child-key matches.
- Renamed the modern `src` and `tests` namespace surface from `Quasar.Common.*` to
  `MasterSplinter.Common.*` while keeping protocol DTO names, protobuf fields, and wire
  compatibility tests intact.
- Added permissioned TCP connection close through `DoCloseConnection`: the client provider looks
  up the exact local/remote address and port tuple, requests `Delete_TCB` via `SetTcpEntry`, and
  returns a refreshed `GetConnectionsResponse`; CLI parsing requires the full four-tuple.
- Verified loopback `close-connection` dispatch manually on June 6, 2026 with
  `--grant-permission`; dispatch metadata returned `Safety=NetworkControl`,
  `RequiresPermission=True`, and `RequiresConsent=False`, and the client returned a refreshed TCP
  list. Windows requires the client process to be elevated for `SetTcpEntry` deletion; the client
  host now embeds a `requireAdministrator` manifest for published Windows executables to match
  legacy parity expectations.

Left to do:

- Extract server listener/orchestration behavior from `legacy/Quasar/Quasar.Server`.
- Define operator identity and authorization inputs for server commands.
- Add integration tests for broader client/server behavior once client handlers are extracted.
- Continue confirming remaining kept-feature parity gaps before Web API work.

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
  remote desktop contracts, registry write behavior, startup/service behavior, and reverse proxy behavior
  before or while each area is extracted.
- Classify each legacy feature as keep, redesign, defer, or remove before porting behavior.

Parity acceptance should include:

- Modern client/server handshake succeeds using the modern `net10.0` projects.
- Existing wire fixtures remain compatible.
- Modern command routing matches the tested legacy message behavior.
- A documented capability matrix exists for supported, deferred, removed, and Windows-only features.
- Completed kept capabilities can be manually verified through CLI `listen` mode without
  restarting the client for each command.
- `dotnet test .\MasterSplinter.sln` remains green.

Current capability matrix:

- `docs/capability-matrix.md` documents the current keep/redesign/defer/quarantine decisions
  and should be checked before porting state-changing or sensitive legacy behavior. The
  matrix treats legacy admin-tool parity as the goal, with compatibility parity separated
  from runtime parity for features that require additional consent, legal, or safety review.

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
- Blocked until full legacy admin-tool parity for kept features is confirmed.
- Expose client inventory, session status, command dispatch, audit query, and capability discovery.
- Use the server core for all business logic.
- Do not duplicate server orchestration inside controllers.
- Add API tests around authorization, request validation, command routing, and audit recording.

## Priority 8: CLI Server

Status: Started

- Added a root-level CLI project for manual loopback smoke testing.
- Supports manual loopback dispatch for the current read-only command set.
- Supports listing connected clients and dispatching safe commands to selected clients in
  `listen` mode.
- Still needs inspecting capabilities and a less manual non-loopback operator experience.
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

1. Continue extracting tested read-only or permission-scoped legacy behavior until the modern runtime parity gate is met.
2. Confirm full legacy admin-tool parity for kept features before starting Web API work.
3. Start original roadmap features: permissioned operators, Web API, consent UI,
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

Current manual command dispatch check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47841
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47841 --handle-commands
```

Current manual registry-read check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47849
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47849 --handle-commands
dispatch first get-registry-key --path HKCU\Software
```

Current manual TCP connection-close check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- dispatch --command close-connection --port 47852 --grant-permission --local-address <ip> --local-port <port> --remote-address <ip> --remote-port <port>
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47852 --handle-one-command
```

Use a harmless loopback TCP test connection. Run the published client host executable elevated on
Windows for actual close behavior; `dotnet run` itself does not trigger the application manifest.

Current manual file-download check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47843 --grant-permission
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47843 --handle-commands
dispatch first download-file --path <remote-file> --output <local-file>
```

Current manual file-upload check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47844 --grant-permission
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47844 --handle-commands
dispatch first upload-file --path <local-file> --remote-path <client-file>
```

Current manual file-rename check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47845 --grant-permission
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47845 --handle-commands
dispatch first rename-path --path <client-old-path> --new-path <client-new-path> --type file
```

Current manual file-delete check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47846 --grant-permission
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47846 --handle-commands
dispatch first delete-path --path <client-file> --type file
```

Current manual process-end check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47847 --grant-permission --grant-consent
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47847 --handle-commands
dispatch first end-process --pid <pid>
```

Current manual process-start check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47848 --grant-permission --grant-consent
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47848 --handle-commands
dispatch first start-process --path <client-file>
```

Legacy check, for awareness:

```powershell
dotnet test .\legacy\Quasar\Quasar.sln
```

The modern root solution is the main quality gate until the legacy WinForms surface is either
retired, isolated further, or deliberately repaired.
