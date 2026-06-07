# MasterSplinter Capability Matrix

Last updated: June 7, 2026

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
| System information | `GetSystemInfo`, `GetSystemInfoResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest focused manual pass returned populated CPU, RAM, GPU, uptime, MAC, LAN IP, WAN IP, ASN, ISP, antivirus, firewall, time zone, and country on this PC |
| Drive listing | `GetDrives`, `GetDrivesResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest manual pass returned `C:\ (OS) [Local Disk, NTFS] => C:\` |
| Directory listing | `GetDirectory`, `GetDirectoryResponse`, `SetStatusFileManager` | done; manually verified | portable with platform-specific permissions | read-only filesystem | keep | Latest manual pass returned 24 entries for `C:\`; path normalization and access-denial reporting before broader file manager work |
| Process listing | `GetProcesses`, `GetProcessesResponse` | done; manually verified | platform-specific | read-only inventory | keep | Latest manual pass returned 302 processes; capability reporting for process metadata differences |
| Startup item listing | `GetStartupItems`, `GetStartupItemsResponse`, `SetStatus` | done; manually verified | Windows-specific today, portable concept | read-only persistence inventory | keep | Latest manual pass returned 5 entries; platform-specific providers and clear capability labels |
| TCP connection listing | `GetConnections`, `GetConnectionsResponse` | done; manually verified | Windows-specific today, portable concept | read-only network inventory | keep | Latest manual pass returned 50 TCP connections; platform-specific providers and degraded behavior on unsupported OSes |
| File download from client / file upload to client | `FileTransferRequest`, `FileTransferChunk`, `FileTransferComplete`, `FileTransferCancel` | download/upload done and manually verified | portable with platform-specific filesystem constraints | sensitive data movement | keep | Download requires file-read permission and refuses overwrite; upload requires file-write permission and writes through in-progress temp files; both were SHA-256 verified on loopback |
| File delete and rename | `DoPathDelete`, `DoPathRename`, `SetStatusFileManager` | file rename/delete done and manually verified; recursive directory delete deferred | portable with platform-specific permissions | state-changing filesystem | keep | Rename refuses target overwrite and delete currently supports files only; both require file-write permission and were verified on loopback |
| Process start and process end | `DoProcessStart`, `DoProcessEnd`, `DoProcessResponse` | local process start and process end done and manually verified; URL/update start deferred | platform-specific | state-changing execution | keep | Both require execution permission plus client consent; process end guards protected/self PIDs and was verified against a harmless spawned sleep process; local process start was verified with a harmless temp command script; download/update start still needs provenance, allow/deny policy, and update consent design |
| Remote shell | `DoShellExecute`, `DoShellExecuteResponse` | protocol only | platform-specific | sensitive execution | redesign | Strong operator permission, full audit transcript, optional client consent/status |
| Registry read/write | `DoLoadRegistryKey`, registry create/delete/rename/change messages and responses | registry read done and manually verified; writes protocol only | Windows-only unless rethought | read-only configuration inventory for load; state-changing configuration for writes | keep for Windows | Registry read is behind a provider, classified read-only, supports hive aliases, and was manually verified against `HKCU\Software`; writes still require operator permission, audit, and explicit read/write split |
| Startup item add/remove | `DoStartupItemAdd`, `DoStartupItemRemove`, `SetStatus` | dispatch path done; harmless-entry manual verification pending | Windows-specific today, portable concept | persistence-changing | redesign | Requires operator permission plus client consent, supports legacy Run/RunOnce and Startup-folder paths, needs audit and explicit UI language before broader rollout |
| TCP connection close | `DoCloseConnection` | dispatch path done; requires elevated Windows client | Windows-specific today | state-changing network | keep | Requires operator permission and an elevated client process; `MasterSplinter.Client.Host` embeds a `requireAdministrator` manifest and reports `AccountType=Admin/User` in client identity, matching legacy's admin requirement |
| Remote desktop screen capture | `GetDesktop`, `GetDesktopResponse`, `GetMonitors`, `GetMonitorsResponse` | protocol only | platform-specific | sensitive capture | redesign | Client-visible status, consent policy, session controls, audit |
| Remote input | `DoMouseEvent`, `DoKeyboardEvent` | protocol only | platform-specific | sensitive control | redesign | Consent/session model, visible active state, operator permission, audit |
| Webcam stop / capture-era controls | `DoWebcamStop` | protocol only | platform-specific | sensitive capture | defer | Product decision, consent model, visible active state |
| Message box | `DoShowMessageBox` | dispatch path done; visible desktop verification pending | Windows-specific provider today, portable concept | user interaction | keep | Requires operator permission plus client consent, supports legacy caption/text/button/icon fields, and should show clear source labeling in future consent UI |
| Website visit | `DoVisitWebsite` | dispatch path done; browser/hidden GET verification pending | portable concept, platform browser launch varies | user interaction / browser launch | redesign | Requires operator permission plus client consent, validates HTTP/HTTPS URLs, supports legacy hidden GET path, and needs clearer consent/notification UX before broader rollout |
| Shutdown/restart/standby actions | `DoShutdownAction` | dispatch path done; real power-state verification pending | Windows-specific today | disruptive system action | keep | Requires operator permission plus client consent, returns status to CLI, and must only be manually verified on a disposable or prepared Windows client |
| Client disconnect/reconnect | `DoClientDisconnect`, `DoClientReconnect` | dispatch path done; automatic reconnect scheduling pending | portable | connection lifecycle | keep | Requires operator permission, returns status to CLI, and uses lifecycle-capable contexts so future hosts can plug in reconnect policy |
| Elevation request | `DoAskElevate` | dispatch path done; interactive UAC verification pending | Windows-specific | privilege boundary | redesign | Requires operator permission plus client consent, triggers visible Windows UAC through `runas`, returns status to CLI, and still needs manual prompt acceptance/cancellation verification |
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
- CLI `listen` mode verified all current read-only runtime slices again on June 2, 2026 after
  system-info enrichment; continue confirming remaining kept-feature parity before Web API work.
