# RemoteControl / MasterSplinter Current State

Last updated: June 1, 2026

## Fresh Chat Handoff

The outer repository folder is now `RemoteControl`. The modern product/solution inside
the repository is still named `MasterSplinter`. If a Codex chat says its working directory
is missing, open a new chat from:

```powershell
C:\Users\Jplou\develop\RemoteControl
```

Then ask the new chat to read this file, `docs/roadmap-status.md`, and
`docs/repository-layout.md`.

## Repository

- Root: `C:\Users\Jplou\develop\RemoteControl`
- Current solution: `MasterSplinter.sln`
- Legacy imported source: `legacy/Quasar`
- Legacy solution: `legacy/Quasar/Quasar.sln`
- Latest committed roadmap checkpoint before this handoff: `Add CLI listen parity harness`

The modern work is intentionally in root-level `src` and `tests` folders. The legacy
Quasar code is preserved separately as reference material and parity source, and should
be removable once modern parity is proven.

## Verification

Primary acceptance check:

```powershell
dotnet test .\MasterSplinter.sln
```

Latest result from June 1, 2026:

- `MasterSplinter.Common.Tests`: 32 passed, 1 skipped
- `MasterSplinter.Client.Core.Tests`: 25 passed
- `MasterSplinter.Cli.Tests`: 8 passed
- `MasterSplinter.Server.Core.Tests`: 47 passed
- `MasterSplinter.Host.Tests`: 15 passed
- Total: 127 passed, 1 skipped, 0 failed

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

Current manual read-only parity check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47838
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47838 --handle-commands
```

At the CLI prompt, the latest manual check listed one connected client and successfully ran
all current read-only commands on the same persistent client connection:

- `dispatch first get-system-info`: `GetSystemInfoResponse`, 19 rows.
- `dispatch first get-drives`: `GetDrivesResponse`, 1 drive, `- C:\ (OS) [Local Disk, NTFS] => C:\`.
- `dispatch first get-directory --path C:\`: `GetDirectoryResponse`, 24 entries.
- `dispatch first get-processes`: `GetProcessesResponse`, 280 processes.
- `dispatch first get-startup-items`: `GetStartupItemsResponse`, 5 entries.
- `dispatch first get-connections`: `GetConnectionsResponse`, 47 TCP connections.

Both CLI and client exited cleanly. Note: several `GetSystemInfo` fields are still placeholder
`-` values and need provider parity follow-up even though the command path works.

Current CLI dispatch command names:

- `get-system-info`
- `get-drives`
- `get-directory --path <path>`
- `get-processes`
- `get-startup-items`
- `get-connections`

Current CLI listen commands:

- `clients`
- `dispatch <client-id|first> <command> [--path <path>]`
- `help`
- `exit`

## Modern Projects

- `src/MasterSplinter.Common`: protocol DTOs, shared models, crypto helpers, payload reader/writer.
- `src/MasterSplinter.Client.Core`: client dispatch contracts, response-handler adapters, client identification factory, system-info handling, drive-list handling, directory-list handling, process-list handling, startup-item listing, and TCP-connection listing.
- `src/MasterSplinter.Client.Host`: minimal runnable client host with smoke mode, loopback handshake, and one-command handling mode.
- `src/MasterSplinter.Cli`: minimal operator CLI for manual loopback command-dispatch testing across current read-only handlers.
- `src/MasterSplinter.Server.Core`: session registry, handshake coordination, lifecycle contracts, listener orchestration, audit and command dispatch contracts.
- `src/MasterSplinter.Server.Host`: minimal runnable loopback-only server host.
- `tests/*`: MSTest coverage for the modern projects.

All modern projects target `net10.0`.

## Decisions

- Modernization and parity come before new roadmap features.
- Keep the legacy Quasar source as reference under `legacy/Quasar`.
- Keep `MasterSplinter.sln` at the repo root as the main solution. This is a standard
  .NET layout when paired with root-level `src`, `tests`, `docs`, and temporary `legacy` folders.
- Add future Web API, CLI, service, and GUI projects as root-owned projects under `src`
  unless there is a strong reason to create a separate top-level folder.
- Treat `legacy/Quasar` as the old source-of-truth during parity work, not as the long-term
  product location.
- Preserve existing wire compatibility until tests define a versioned upgrade path.
- Protocol changes must be additive and covered by serialization/wire tests.
- Cross-platform work starts only after portable behavior is covered by tests.
- Web API, consentful clients, service mode, and GUI overhaul are post-parity roadmap work.
- Do not start the Web API until full legacy admin-tool parity for kept features is confirmed.

## Completed

- Fresh repository created; the outer workspace folder is now `RemoteControl`.
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
- Loopback TCP TLS 1.2 transport option added and tested with pinned server certificate validation.
- `GetDirectory` client handler added with deterministic tests.
- Loopback TCP `GetDirectory` command-response path added and tested.
- `GetProcesses` client handler added with deterministic tests.
- Loopback TCP `GetProcesses` command-response path added and tested.
- `GetStartupItems` client handler added with deterministic tests.
- Loopback TCP `GetStartupItems` command-response path added and tested.
- `GetConnections` client handler added with deterministic tests.
- Loopback TCP `GetConnections` command-response path added and tested.
- Capability matrix added at `docs/capability-matrix.md` with legacy admin-tool parity as the goal and with keep/redesign/defer/quarantine decisions.
- Server command safety classification added and attached to dispatch results and audit events.
- Server command policy enforcement added so commands requiring permission or consent are denied unless dispatch authorization grants them.
- Server authorization models and services added to derive dispatch authorization from operator permissions and client consent.
- Server host manual `--dispatch get-system-info` command added for loopback command-response smoke testing.
- CLI manual `dispatch --command get-system-info` command added for loopback command-response smoke testing.
- CLI dispatch expanded to `GetSystemInfo`, `GetDrives`, `GetDirectory`, `GetProcesses`,
  `GetStartupItems`, and `GetConnections`, with command safety metadata and authorization
  service plumbing applied before dispatch.
- CLI response formatting now prints payload rows for supported read-only responses and has
  formatter coverage in `MasterSplinter.Cli.Tests`.
- CLI `listen` mode added for repeated manual dispatch against connected clients, paired with
  client host `--handle-commands` persistent command handling.

## Current Limitations

- Legacy `legacy/Quasar/Quasar.sln` is preserved but is not the primary acceptance gate.
- The legacy WinForms surface has known build/security friction on the current machine.
- Modern hosts currently prove loopback handshake and selected read-only runtime parity, not full remote-management behavior.
- File-system, process, shell, registry, desktop, service, and UI behavior are not fully extracted yet.

## Recommended Next Tasks

1. Fill system-info provider placeholders or document intentional degraded fields.
2. Extract remaining read-only or permission-scoped client handlers behind explicit interfaces.
3. Add parity tests against legacy behavior before moving each behavior slice.
4. Confirm full legacy admin-tool parity for kept features before starting Web API work.
5. Once runtime parity is proven, resume roadmap features: permissioned operators, audit persistence, Web API, consentful client UI, service mode, cross-platform expansion, and GUI overhaul.
