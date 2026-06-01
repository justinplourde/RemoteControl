# MasterSplinter Capability Matrix

Last updated: June 1, 2026

## Intent

The modernization target is legacy Quasar admin-tool feature parity for legitimate remote
administration and support use. Parity does not always mean copying the legacy UX or runtime
behavior exactly. Features that can affect user privacy, system state, credentials,
persistence, or network routing must move behind explicit operator permissions, audit
records, and user consent/status where appropriate.

This matrix is a planning guardrail for parity work. It does not grant permission to port
state-changing or sensitive behavior before the required safety boundary exists.

Two kinds of parity are tracked:

- Runtime parity: modern client/server handlers intentionally implement the capability.
- Compatibility parity: protocol DTOs, fixtures, and legacy references are preserved, but no
  modern runtime handler is enabled until product, legal, consent, and safety requirements
  are explicitly satisfied.

## Status Legend

- `done`: Modern client/server path exists and is covered by tests.
- `keep`: Preserve the capability and port it behind the appropriate abstraction.
- `redesign`: Preserve the useful goal, but change UX, permission, consent, or implementation.
- `defer`: Do not implement until prerequisite safety/platform work is complete.
- `quarantine`: Preserve compatibility/reference only; do not enable a modern runtime handler unless a lawful, consentful, explicitly approved requirement exists.
- `remove`: Do not carry forward.

## Capability Matrix

| Capability | Legacy messages | Modern status | Platform scope | Safety class | Decision | Required boundary before broader port |
| --- | --- | --- | --- | --- | --- | --- |
| Client identification and handshake | `ClientIdentification`, `ClientIdentificationResult` | done | portable | connection lifecycle | keep | Existing validation, protocol compatibility tests, audit lifecycle events |
| TLS transport with pinned server certificate | legacy `SslStream` transport | done for loopback host | portable transport, cert storage platform-specific | connection security | keep | Certificate loading/storage plan before non-loopback use |
| System information | `GetSystemInfo`, `GetSystemInfoResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest manual pass returned 19 rows, but several fields are placeholders; fill or document degraded fields |
| Drive listing | `GetDrives`, `GetDrivesResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest manual pass returned `C:\ (OS) [Local Disk, NTFS] => C:\` |
| Directory listing | `GetDirectory`, `GetDirectoryResponse`, `SetStatusFileManager` | done; manually verified | portable with platform-specific permissions | read-only filesystem | keep | Latest manual pass returned 24 entries for `C:\`; path normalization and access-denial reporting before broader file manager work |
| Process listing | `GetProcesses`, `GetProcessesResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest manual pass returned 280 processes; capability reporting for process metadata differences |
| Startup item listing | `GetStartupItems`, `GetStartupItemsResponse`, `SetStatus` | done; manually verified | Windows-specific today, portable concept | read-only persistence inventory | keep | Latest manual pass returned 5 entries; platform-specific providers and clear capability labels |
| TCP connection listing | `GetConnections`, `GetConnectionsResponse` | done; manually verified | Windows-specific today, portable concept | read-only network inventory | keep | Latest manual pass returned 47 TCP connections; platform-specific providers and degraded behavior on unsupported OSes |
| File download from client / file upload to client | `FileTransferRequest`, `FileTransferChunk`, `FileTransferComplete`, `FileTransferCancel` | protocol only | portable with platform-specific filesystem constraints | sensitive data movement | keep | Operator permission, audit, size limits, path policy, cancellation and progress model |
| File delete and rename | `DoPathDelete`, `DoPathRename`, `SetStatusFileManager` | protocol only | portable with platform-specific permissions | state-changing filesystem | keep | Operator permission, audit, confirmation policy, protected path rules |
| Process start and process end | `DoProcessStart`, `DoProcessEnd`, `DoProcessResponse` | protocol only | platform-specific | state-changing execution | keep | Operator permission, audit, explicit command provenance, allow/deny policy |
| Remote shell | `DoShellExecute`, `DoShellExecuteResponse` | protocol only | platform-specific | sensitive execution | redesign | Strong operator permission, full audit transcript, optional client consent/status |
| Registry read/write | `DoLoadRegistryKey`, registry create/delete/rename/change messages and responses | protocol only | Windows-only unless rethought | state-changing configuration | keep for Windows | Operator permission, audit, registry provider abstraction, read/write split |
| Startup item add/remove | `DoStartupItemAdd`, `DoStartupItemRemove`, `SetStatus` | protocol only | Windows-specific today, portable concept | persistence-changing | redesign | Separate permission for persistence changes, audit, explicit UI language |
| TCP connection close | `DoCloseConnection` | protocol only | Windows-specific today | state-changing network | keep | Operator permission, audit, admin/elevation handling |
| Remote desktop screen capture | `GetDesktop`, `GetDesktopResponse`, `GetMonitors`, `GetMonitorsResponse` | protocol only | platform-specific | sensitive capture | redesign | Client-visible status, consent policy, session controls, audit |
| Remote input | `DoMouseEvent`, `DoKeyboardEvent` | protocol only | platform-specific | sensitive control | redesign | Consent/session model, visible active state, operator permission, audit |
| Webcam stop / capture-era controls | `DoWebcamStop` | protocol only | platform-specific | sensitive capture | defer | Product decision, consent model, visible active state |
| Message box | `DoShowMessageBox` | protocol only | portable concept | user interaction | keep | Operator permission, audit, clear source labeling |
| Website visit | `DoVisitWebsite` | protocol only | portable concept | user interaction / browser launch | redesign | Consent or notification policy, audit, URL validation |
| Shutdown/restart actions | `DoShutdownAction` | protocol only | platform-specific | disruptive system action | keep | Strong operator permission, confirmation policy, audit |
| Client disconnect/reconnect | `DoClientDisconnect`, `DoClientReconnect` | protocol only | portable | connection lifecycle | keep | Operator permission, audit, reconnect policy |
| Elevation request | `DoAskElevate` | protocol only | Windows-specific | privilege boundary | redesign | Transparent user prompt, audit, no silent elevation |
| Client uninstall | `DoClientUninstall` | protocol only | platform-specific | installation lifecycle | keep | Local/client consent or admin policy, audit, service-mode design |
| Service/install/update behavior | legacy client services/updater/build paths | not modernized | platform-specific | installation lifecycle / persistence | redesign | Transparent installer/service model, signed update plan, operator permissions |
| Reverse proxy | `ReverseProxyConnect`, `ReverseProxyData`, `ReverseProxyDisconnect`, `ReverseProxyConnectResponse` | protocol only | portable transport, platform networking varies | sensitive network routing | defer | Explicit product decision, operator permission, audit, allowlist and session controls |
| Password recovery | `GetPasswords`, `GetPasswordsResponse` | compatibility parity only | platform-specific | credential access | quarantine | Preserve protocol compatibility; do not enable runtime credential collection without explicit lawful, consentful requirement and dedicated review |
| Keylogger logs | `GetKeyloggerLogsDirectory`, `GetKeyloggerLogsDirectoryResponse` | compatibility parity only | platform-specific | keystroke surveillance | quarantine | Preserve protocol compatibility; do not enable runtime keystroke collection without explicit lawful, consentful requirement and dedicated review |

## Implementation Order Guidance

1. Prefer read-only inventory slices until permission, consent, and audit boundaries are implemented.
2. For state-changing behavior, add server-side operator permission and audit tests before client behavior.
3. For sensitive capture/control behavior, add client-visible status and consent models before streaming or input control.
4. For Windows-only behavior, keep provider interfaces in `MasterSplinter.Client.Core` and platform details behind implementations.
5. For quarantined behavior, preserve compatibility parity but do not build modern runtime handlers by default.

## Near-Term Candidates

The safest next implementation work is not another powerful command. Recommended next steps:

- Wire operator/consent authorization into future API/CLI command request creation.
- Add audit expectations per safety class.
- Add client capability reporting for completed slices.
- Start a read/write split for registry and file-manager behavior before any write operations.
- CLI `listen` mode verified all current read-only runtime slices on June 1, 2026; address
  system-info placeholder fields before treating read-only inventory parity as complete.
