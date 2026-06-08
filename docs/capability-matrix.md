# MasterSplinter Capability Matrix

Last updated: June 7, 2026

## Intent

The modernization target is 100% legacy Quasar admin-tool parity accounting. Every legacy
admin feature must be tracked, even when a modern runtime implementation is incomplete,
redesigned, or blocked for safety. `docs/legacy-parity-audit.md` is the source of truth for
that 1:1 accounting.

Parity does not always mean copying the legacy UX or runtime behavior exactly. Features that can
affect user privacy, system state, credentials, persistence, or network routing must move behind
explicit operator permissions, audit records, and user consent/status where appropriate.

This matrix is a planning guardrail for parity work. It does not grant permission to port
state-changing or sensitive behavior before the required safety boundary exists.

Three kinds of parity are tracked:

- Runtime parity: modern client/server handlers intentionally implement the capability.
- Compatibility parity: protocol DTOs, fixtures, and legacy references are preserved, but no
  modern runtime handler is enabled until product, legal, consent, and safety requirements
  are explicitly satisfied.
- Blocked parity: the legacy surface is accounted for, but modern runtime behavior is blocked
  because it would implement credential theft, keylogging, covert collection, or similarly unsafe
  behavior.

## Status Legend

- `done`: Modern client/server path exists and is covered by tests.
- `partial`: Modern runtime path exists, but legacy behavior is incomplete or manual verification is pending.
- `not-started`: Legacy surface is known, but no modern runtime path exists yet.
- `blocked-by-safety`: Protocol/source is accounted for, but runtime implementation is blocked because it would implement credential theft, keylogging, covert collection, or similarly unsafe behavior.
- `redesign-required`: The legacy goal remains in the parity ledger, but the modern implementation needs explicit UX, permission, consent, audit, or allowlist boundaries.

## Capability Matrix

| Capability | Legacy messages | Modern status | Platform scope | Safety class | Parity handling | Required boundary before broader port |
| --- | --- | --- | --- | --- | --- | --- |
| Client identification and handshake | `ClientIdentification`, `ClientIdentificationResult` | done | portable | connection lifecycle | done | Existing validation, protocol compatibility tests, audit lifecycle events |
| TLS transport with pinned server certificate | legacy `SslStream` transport | done for loopback host | portable transport, cert storage platform-specific | connection security | partial | Certificate loading/storage plan before non-loopback use |
| System information | `GetSystemInfo`, `GetSystemInfoResponse` | done; manually verified | platform-specific | read-only inventory | done | Latest focused manual pass returned populated CPU, RAM, GPU, uptime, MAC, LAN IP, WAN IP, ASN, ISP, antivirus, firewall, time zone, and country on this PC |
| Drive listing | `GetDrives`, `GetDrivesResponse` | done; manually verified | platform-specific | read-only inventory | done | Latest manual pass returned `C:\ (OS) [Local Disk, NTFS] => C:\` |
| Directory listing | `GetDirectory`, `GetDirectoryResponse`, `SetStatusFileManager` | done; manually verified | portable with platform-specific permissions | read-only filesystem | done | Latest manual pass returned 24 entries for `C:\`; path normalization and access-denial reporting before broader file manager work |
| Process listing | `GetProcesses`, `GetProcessesResponse` | done; manually verified | platform-specific | read-only inventory | done | Latest manual pass returned 302 processes; capability reporting for process metadata differences |
| Startup item listing | `GetStartupItems`, `GetStartupItemsResponse`, `SetStatus` | done; manually verified | Windows-specific today, portable concept | read-only persistence inventory | done | Latest manual pass returned 5 entries; platform-specific providers and clear capability labels |
| TCP connection listing | `GetConnections`, `GetConnectionsResponse` | done; manually verified | Windows-specific today, portable concept | read-only network inventory | done | Latest manual pass returned 50 TCP connections; platform-specific providers and degraded behavior on unsupported OSes |
| File download from client / file upload to client | `FileTransferRequest`, `FileTransferChunk`, `FileTransferComplete`, `FileTransferCancel` | download/upload done and manually verified | portable with platform-specific filesystem constraints | sensitive data movement | done | Download requires file-read permission and refuses overwrite; upload requires file-write permission and writes through in-progress temp files; both were SHA-256 verified on loopback |
| File delete and rename | `DoPathDelete`, `DoPathRename`, `SetStatusFileManager` | file rename/delete done and manually verified; recursive directory delete done in provider/tests | portable with platform-specific permissions | state-changing filesystem | partial | Rename refuses target overwrite; delete supports files and recursive directories; both require file-write permission, with file paths verified on loopback and directory delete needing harmless-folder manual verification |
| Process start and process end | `DoProcessStart`, `DoProcessEnd`, `DoProcessResponse` | local process start and process end done and manually verified; URL/update start deferred | platform-specific | state-changing execution | partial | Both require execution permission plus client consent; process end guards protected/self PIDs and was verified against a harmless spawned sleep process; local process start was verified with a harmless temp command script; download/update start still needs provenance, allow/deny policy, and update consent design |
| Remote shell | `DoShellExecute`, `DoShellExecuteResponse` | persistent dispatch path done | platform-specific | sensitive execution | partial | Requires execution permission plus client consent; modern provider keeps shell state across dispatches, returns stdout/stderr, flags stderr as error output, and closes on `exit`; broader UX/audit transcript work remains |
| Registry read/write | `DoLoadRegistryKey`, registry create/delete/rename/change messages and responses | registry read done and manually verified; key create/delete/rename and value create/delete/rename/change dispatch paths done | Windows-only unless rethought | read-only configuration inventory for load; state-changing configuration for writes | partial | Registry read is behind a provider, classified read-only, supports hive aliases, and was manually verified against `HKCU\Software`; key/value mutations require permission and harmless HKCU manual verification |
| Startup item add/remove | `DoStartupItemAdd`, `DoStartupItemRemove`, `SetStatus` | dispatch path done; harmless-entry manual verification pending | Windows-specific today, portable concept | persistence-changing | partial | Requires operator permission plus client consent, supports legacy Run/RunOnce and Startup-folder paths, needs audit and explicit UI language before broader rollout |
| TCP connection close | `DoCloseConnection` | dispatch path done; requires elevated Windows client | Windows-specific today | state-changing network | partial | Requires operator permission and an elevated client process; `MasterSplinter.Client.Host` embeds a `requireAdministrator` manifest and reports `AccountType=Admin/User` in client identity, matching legacy's admin requirement |
| Remote desktop screen capture | `GetDesktop`, `GetDesktopResponse`, `GetMonitors`, `GetMonitorsResponse` | monitor count, single-frame capture, reusable request-loop streaming, first WinForms viewer surface, monitor refresh UX, and viewer input dispatch done | Windows-specific provider today | sensitive capture | partial | `GetMonitors` returns monitor count; `GetDesktop` returns a legacy first-frame JPEG payload; server core owns reusable request-response stream sequencing; CLI `get-desktop-stream` saves numbered JPEGs; WinForms viewer can listen, select a client, refresh displays, select a monitor, start/stop stream, render frames, show FPS, and send mouse/keyboard input with zoom-aware coordinate scaling. Visible active state, session controls, audit, and manual viewer-input verification remain |
| Remote input | `DoMouseEvent`, `DoKeyboardEvent` | dispatch path done; gentle local manual check passed | Windows-specific provider today | sensitive control | partial | Requires remote-input permission plus client consent, sends legacy-style mouse/keyboard events through `SendInput`/`SetCursorPos`, and still needs client-visible active state plus audit/session UX before broader rollout |
| Webcam stop / capture-era controls | `DoWebcamStop` | protocol only | platform-specific | sensitive capture | not-started | Product decision, consent model, visible active state |
| Message box | `DoShowMessageBox` | dispatch path done; visible desktop verification pending | Windows-specific provider today, portable concept | user interaction | partial | Requires operator permission plus client consent, supports legacy caption/text/button/icon fields, and should show clear source labeling in future consent UI |
| Website visit | `DoVisitWebsite` | dispatch path done; browser/hidden GET verification pending | portable concept, platform browser launch varies | user interaction / browser launch | partial | Requires operator permission plus client consent, validates HTTP/HTTPS URLs, supports legacy hidden GET path, and needs clearer consent/notification UX before broader rollout |
| Shutdown/restart/standby actions | `DoShutdownAction` | dispatch path done; real power-state verification pending | Windows-specific today | disruptive system action | partial | Requires operator permission plus client consent, returns status to CLI, and must only be manually verified on a disposable or prepared Windows client |
| Client disconnect/reconnect | `DoClientDisconnect`, `DoClientReconnect` | dispatch path done; automatic reconnect scheduling pending | portable | connection lifecycle | partial | Requires operator permission, returns status to CLI, and uses lifecycle-capable contexts so future hosts can plug in reconnect policy |
| Elevation request | `DoAskElevate` | dispatch path done; interactive UAC verification pending | Windows-specific | privilege boundary | partial | Requires operator permission plus client consent, triggers visible Windows UAC through `runas`, returns status to CLI, and still needs manual prompt acceptance/cancellation verification |
| Client uninstall | `DoClientUninstall` | dispatch path done; published-exe manual verification pending | Windows-specific today | installation lifecycle | partial | Requires persistence permission plus client consent; provider writes a legacy-style self-delete batch and refuses unsafe `dotnet run` self-delete paths |
| Service/install/update behavior | legacy client services/updater/build paths | not modernized | platform-specific | installation lifecycle / persistence | not-started | Transparent installer/service model, signed update plan, operator permissions |
| Reverse proxy | `ReverseProxyConnect`, `ReverseProxyData`, `ReverseProxyDisconnect`, `ReverseProxyConnectResponse` | protocol only | portable transport, platform networking varies | sensitive network routing | not-started | Explicit product decision, operator permission, audit, allowlist and session controls |
| Password recovery | `GetPasswords`, `GetPasswordsResponse` | compatibility parity only | platform-specific | credential access | blocked-by-safety | Preserve protocol compatibility; do not enable runtime credential collection. A consentful user-export/admin inventory alternative can be designed separately. |
| Keylogger logs | `GetKeyloggerLogsDirectory`, `GetKeyloggerLogsDirectoryResponse` | compatibility parity only | platform-specific | keystroke surveillance | blocked-by-safety | Preserve protocol compatibility; do not enable runtime keylogging or keystroke-log collection. A consentful diagnostic logging alternative can be designed separately. |

## Implementation Order Guidance

1. Prefer read-only inventory slices until permission, consent, and audit boundaries are implemented.
2. For state-changing behavior, add server-side operator permission and audit tests before client behavior.
3. For sensitive capture/control behavior, add client-visible status and consent models before streaming or input control.
4. For Windows-only behavior, keep provider interfaces in `MasterSplinter.Client.Core` and platform details behind implementations.
5. For blocked-by-safety behavior, preserve compatibility parity but do not build modern runtime handlers.

## Near-Term Candidates

The safest next implementation work is not another powerful command. Recommended next steps:

- Wire operator/consent authorization into future API/CLI command request creation.
- Add audit expectations per safety class.
- Add client capability reporting for completed slices.
- Start a read/write split for registry and file-manager behavior before any write operations.
- CLI `listen` mode verified all current read-only runtime slices again on June 2, 2026 after
  system-info enrichment; continue closing `docs/legacy-parity-audit.md` rows before Web API work.
