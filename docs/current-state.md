# RemoteControl / MasterSplinter Current State

Last updated: June 7, 2026

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
- Latest committed roadmap checkpoint before this handoff: `Add shell execute CLI parity`

The modern work is intentionally in root-level `src` and `tests` folders. The legacy
Quasar code is preserved separately as reference material and parity source, and should
be removable once modern parity is proven.

## Verification

Primary acceptance check:

```powershell
dotnet test .\MasterSplinter.sln
```

Latest result from June 7, 2026:

- `MasterSplinter.Common.Tests`: 32 passed, 1 skipped
- `MasterSplinter.Client.Core.Tests`: 74 passed
- `MasterSplinter.Cli.Tests`: 8 passed
- `MasterSplinter.Server.Core.Tests`: 51 passed
- `MasterSplinter.Host.Tests`: 15 passed
- Total: 183 passed, 1 skipped, 0 failed

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
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47841
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47841 --handle-commands
```

At the CLI prompt, the latest manual check listed one connected client and successfully ran
all current read-only commands on the same persistent client connection:

- `dispatch first get-system-info`: `GetSystemInfoResponse`, 19 rows.
- `dispatch first get-drives`: `GetDrivesResponse`, 1 drive, `- C:\ (OS) [Local Disk, NTFS] => C:\`.
- `dispatch first get-directory --path C:\`: `GetDirectoryResponse`, 24 entries.
- `dispatch first get-processes`: `GetProcessesResponse`, 302 processes.
- `dispatch first get-startup-items`: `GetStartupItemsResponse`, 5 entries.
- `dispatch first get-connections`: `GetConnectionsResponse`, 50 TCP connections.

Both CLI and client exited cleanly. The latest focused `get-system-info` check populated CPU,
RAM, GPU, username, PC/host/domain, system paths, uptime, MAC, LAN IP, WAN IP, ASN, ISP,
antivirus, firewall, time zone, and country on this PC. The full read-only CLI parity pass was
re-run after this enrichment on June 2, 2026 and all current commands responded on one persistent
client connection.

Current CLI dispatch command names:

- `get-system-info`
- `get-drives`
- `get-directory --path <path>`
- `get-monitors` (requires `--grant-permission --grant-consent`; returns remote monitor count)
- `get-registry-key --path <hive\subkey>`
- `registry-create-key --path <hive\parent-subkey>` (requires `--grant-permission`; creates legacy-style `New Key #n`)
- `registry-delete-key --path <hive\parent-subkey> --name <child-key>` (requires `--grant-permission`)
- `registry-rename-key --path <hive\parent-subkey> --name <old-child-key> --new-name <new-child-key>` (requires `--grant-permission`)
- `registry-create-value --path <hive\key> --kind <string|expand-string|binary|dword|qword|multi-string>` (requires `--grant-permission`; creates legacy-style `New Value #n`)
- `registry-delete-value --path <hive\key> --name <value-name>` (requires `--grant-permission`)
- `registry-rename-value --path <hive\key> --name <old-value-name> --new-name <new-value-name>` (requires `--grant-permission`)
- `registry-change-value --path <hive\key> --name <value-name> --kind <string|expand-string|binary|dword|qword|multi-string> --data <value>` (requires `--grant-permission`; binary data is hex and multi-string data uses `|` separators)
- `shell-execute --shell-command <command>` (requires `--grant-permission --grant-consent`; executes in a persistent shell session and returns stdout/stderr; use `exit` to close the session)
- `download-file --path <remote-file> [--output <local-file>]` (requires `--grant-permission`)
- `upload-file --path <local-file> --remote-path <client-file>` (requires `--grant-permission`)
- `rename-path --path <client-old-path> --new-path <client-new-path> --type <file|directory>` (requires `--grant-permission`)
- `delete-path --path <client-path> --type <file|directory>` (requires `--grant-permission`; directory deletes are recursive)
- `start-process --path <client-file>` (requires `--grant-permission --grant-consent`; local file path only)
- `end-process --pid <pid>` (requires `--grant-permission --grant-consent`)
- `ask-elevate` (requires `--grant-permission --grant-consent`; triggers Windows UAC in the client session)
- `shutdown-action --action <shutdown|restart|standby>` (requires `--grant-permission --grant-consent`; real action affects the client machine)
- `disconnect-client` (requires `--grant-permission`; closes the client session)
- `reconnect-client` (requires `--grant-permission`; closes the current client session so reconnect policy can re-establish it)
- `uninstall-client` (requires `--grant-permission --grant-consent`; published Windows client executable only)
- `show-message --text <message> [--caption <title>] [--button <AbortRetryIgnore|OK|OKCancel|RetryCancel|YesNo|YesNoCancel>] [--icon <None|Error|Hand|Question|Exclamation|Warning|Information|Asterisk>]` (requires `--grant-permission --grant-consent`; displays a visible client desktop message box)
- `visit-website --url <http-url> [--hidden]` (requires `--grant-permission --grant-consent`; opens the client browser by default, or performs the legacy hidden GET path)
- `startup-add --name <name> --path <client-file> --startup-type <type>` (requires `--grant-permission --grant-consent`; persistence-changing)
- `startup-remove --name <name> --startup-type <type>` (requires `--grant-permission --grant-consent`; persistence-changing)
- `get-processes`
- `get-startup-items`
- `get-connections`
- `close-connection --local-address <ip> --local-port <port> --remote-address <ip> --remote-port <port>` (requires `--grant-permission`; actual close may require elevated client rights)

Current CLI listen commands:

- `clients`
- `dispatch <client-id|first> <command> [--path <path>] [--remote-path <client-path>] [--output <local-path>] [--pid <pid>]`
- `dispatch <client-id|first> ask-elevate`
- `help`
- `exit`

## Modern Projects

- `src/MasterSplinter.Common`: protocol DTOs, shared models, crypto helpers, payload reader/writer.
- `src/MasterSplinter.Client.Core`: client dispatch contracts, response-handler adapters, lifecycle-capable command contexts, client identification factory, system-info handling, drive-list handling, directory-list handling, process-list handling, startup-item listing/add/remove, registry key read/create/delete/rename, registry value create/delete/rename/change, TCP-connection listing/close, remote monitor counting, shell command execution, elevation request handling, shutdown/restart/standby request handling, client disconnect/reconnect/uninstall request handling, message-box handling, and website-visit handling.
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
- `GetSystemInfo` provider parity improved with Windows/local system, security product,
  LAN/MAC, uptime, and bounded public network enrichment providers.
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
- File download from client to operator added through `FileTransferRequest`, chunk/complete/cancel
  responses, file-read permission enforcement, CLI output writing, deterministic tests, and a
  manual loopback SHA-256 match check.
- File upload from operator to client added through `FileTransferChunk` and client
  complete/cancel responses, file-write permission enforcement, in-progress temp files,
  deterministic tests, and a manual loopback SHA-256 match check.
- File rename added through `DoPathRename` and `SetStatusFileManager`, file-write permission
  enforcement, deterministic tests, and a manual loopback hash-preserving temp-file rename check.
- File delete added for files through `DoPathDelete` and `SetStatusFileManager`, file-write
  permission enforcement, deterministic tests, and a manual loopback temp-file delete check.
- Process end added through `DoProcessEnd` and `DoProcessResponse`, execution permission and
  consent enforcement, guarded PID handling, deterministic tests, and a manual loopback check
  that terminated a harmless spawned sleep process.
- Local process start added through `DoProcessStart` and `DoProcessResponse`, execution
  permission and consent enforcement, deterministic tests, and a manual loopback check that ran
  a harmless temp command script and verified its marker output.
- Registry key read added through `DoLoadRegistryKey` and `GetRegistryKeysResponse`, deterministic
  tests, read-only safety classification, CLI formatting, and a manual loopback `HKCU\Software`
  check that returned 17 child-key matches without requiring permission or consent.
- Modern `src` and `tests` namespaces were renamed from `Quasar.Common.*` to
  `MasterSplinter.Common.*`; remaining `Quasar` references should be limited to legacy-reference
  documentation and the preserved `legacy/Quasar` source tree.
- TCP connection close added through `DoCloseConnection` with `NetworkControl` permission
  enforcement, deterministic tests, CLI four-tuple parsing, and a manual loopback dispatch check.
  Windows TCP row deletion requires an elevated client process, matching the legacy behavior.
  `MasterSplinter.Client.Host` now embeds a Windows manifest requesting administrator rights when
  run as a published executable.
- Client elevation/admin status is now detected through `ClientPrivilegeProvider`, carried in the
  existing `ClientIdentification.AccountType` field as `Admin` or `User`, and shown in CLI
  `clients` output as `AccountType=<value>`.
- Elevation request parity is now wired through `DoAskElevate`, a Windows UAC `runas` provider,
  a client handler returning legacy-style `SetStatus` messages, CLI `ask-elevate`, and
  `SystemControl` permission plus consent enforcement. Interactive UAC acceptance still needs a
  manual Windows desktop verification pass with a published client executable.
- Shutdown/restart/standby parity is now wired through `DoShutdownAction`, a Windows provider
  matching the legacy `shutdown /s`, `shutdown /r`, and suspend paths, CLI `shutdown-action
  --action <shutdown|restart|standby>`, and `SystemControl` permission plus consent enforcement.
  Automated tests cover provider result mapping; real power-state actions were not manually run
  because they would affect this workstation.
- Client disconnect/reconnect parity is now wired through `DoClientDisconnect` and
  `DoClientReconnect`, lifecycle-capable client contexts, CLI `disconnect-client` and
  `reconnect-client`, and `ConnectionLifecycle` permission enforcement. The current loopback
  host closes the active command session for both actions; a future long-running reconnect
  scheduler should reuse the same lifecycle abstraction.
- Message box parity is now wired through `DoShowMessageBox`, a Windows `user32!MessageBoxW`
  provider matching the legacy caption/text/button/icon fields, CLI `show-message`, and
  `UserInteraction` permission plus consent enforcement. Automated tests cover handler mapping,
  provider failure status, CLI parsing, and safety metadata; manual verification should be run
  from a visible Windows desktop session.
- Website visit parity is now wired through `DoVisitWebsite`, a provider that opens the URL with
  the client default browser or performs the legacy hidden GET path, CLI `visit-website --url
  <http-url> [--hidden]`, and `UserInteraction` permission plus consent enforcement. Modern URL
  validation accepts only HTTP/HTTPS URLs after legacy-style `http://` prefixing.
- Startup add/remove parity is now wired through `DoStartupItemAdd` and `DoStartupItemRemove`,
  the existing startup provider, CLI `startup-add`/`startup-remove`, and `Persistence` permission
  plus consent enforcement. The provider supports the legacy registry Run/RunOnce locations and
  Startup folder `.url` shortcut behavior; machine-wide locations may require an elevated client.
- Registry key create/delete/rename parity is now wired through `DoCreateRegistryKey`,
  `DoDeleteRegistryKey`, and `DoRenameRegistryKey`, the registry provider, CLI
  `registry-create-key`/`registry-delete-key`/`registry-rename-key`, and `Persistence`
  permission enforcement. Create preserves the legacy auto-generated `New Key #n` behavior.
- Registry value create/delete/rename/change parity is now wired through `DoCreateRegistryValue`,
  `DoDeleteRegistryValue`, `DoRenameRegistryValue`, and `DoChangeRegistryValue`, the registry
  provider, CLI `registry-create-value`/`registry-delete-value`/`registry-rename-value`/
  `registry-change-value`, and `Persistence` permission enforcement. Create preserves the legacy
  auto-generated `New Value #n` behavior, and change supports string, expand-string, binary,
  dword, qword, and multi-string data.
- Shell execute parity is now wired through `DoShellExecute` and `DoShellExecuteResponse`, a shell
  command provider, CLI `shell-execute --shell-command <command>`, and `Execution` permission plus
  consent enforcement. The modern provider keeps a shell process alive across dispatches so session
  state such as current directory can persist, returns stdout/stderr, marks stderr output as errors,
  and closes the shell session when `exit` is dispatched.
- Client uninstall parity is now wired through `DoClientUninstall`, a Windows self-delete batch
  provider, CLI `uninstall-client`, and `Persistence` permission plus consent enforcement. The
  provider intentionally refuses `dotnet run` because the process path is `dotnet.exe`; manual
  verification requires a published client executable.
- Monitor count parity is now wired through `GetMonitors`, a Windows monitor provider, CLI
  `get-monitors`, and `RemoteCapture` permission plus consent enforcement. It returns the legacy
  `GetMonitorsResponse.Number` count; actual remote desktop image streaming remains deferred.

## Current Limitations

- Legacy `legacy/Quasar/Quasar.sln` is preserved but is not the primary acceptance gate.
- The legacy WinForms surface has known build/security friction on the current machine.
- Modern hosts currently prove loopback handshake, read-only runtime parity, permissioned
  file download/upload slices, permissioned file rename, and permissioned file delete, not full
  remote-management behavior.
- Process-start URL download/update behavior, desktop, service, and UI behavior are not fully
  extracted yet. TCP connection close requires
  the Windows client host to run elevated. Elevation request dispatch is implemented, but the
  actual UAC acceptance path still needs manual desktop verification. Shutdown/restart/standby
  dispatch is implemented, but manual verification should only be run on a disposable or prepared
  Windows client because it will change the client machine power state. Reconnect currently
  disconnects the active loopback command session; automatic retry/reconnect scheduling is still
  future host behavior. Message box dispatch is implemented, but visible desktop display still
  needs manual verification. Website visit dispatch is implemented, but visible browser launch
  and hidden GET behavior still need manual verification from a prepared client session. Startup
  add/remove dispatch is implemented, but manual verification should use a harmless test entry and
  remove it afterward. Registry key and value mutations are implemented, but manual verification
  should use a harmless `HKCU\Software` test key/value and remove them afterward. Shell execute is
  implemented with a persistent session, but manual verification should use harmless commands only.

## Recommended Next Tasks

1. Continue confirming remaining legacy admin-tool parity gaps for kept features before Web API work.
2. Extract remaining read-only or permission-scoped client handlers behind explicit interfaces.
3. Add parity tests against legacy behavior before moving each behavior slice.
4. Once runtime parity is proven, resume roadmap features: permissioned operators, audit persistence, Web API, consentful client UI, service mode, cross-platform expansion, and GUI overhaul.
