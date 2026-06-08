# Legacy Quasar 1:1 Parity Audit

Last updated: June 7, 2026

## Goal

The parity target is full legacy Quasar accounting. Every legacy protocol message, client handler,
server handler, and operator form/action must be represented here with one of these statuses:

- `done`: modern runtime path exists, tests cover it, and at least one manual path is documented when practical.
- `partial`: modern runtime path exists, but legacy behavior is incomplete or manual verification is still pending.
- `not-started`: protocol/source is known, but no modern runtime behavior exists yet.
- `blocked-by-safety`: protocol compatibility may remain, but modern runtime implementation is blocked because the legacy behavior is credential theft, keylogging, covert collection, or similarly unsafe. A consentful administrative replacement can be designed separately.

This file is the parity ledger. Nothing should silently fall out of scope.

## Legacy Sources Audited

- Client message processors: `legacy/Quasar/Quasar.Client/Messages/*.cs`
- Server message processors: `legacy/Quasar/Quasar.Server/Messages/*.cs`
- Reverse proxy server/client glue: `legacy/Quasar/Quasar.Server/ReverseProxy/*.cs`
- Operator forms: `legacy/Quasar/Quasar.Server/Forms/Frm*.cs`
- Builder/install/update source: `legacy/Quasar/Quasar.Server/Build/*.cs`, `legacy/Quasar/Quasar.Client/Setup/*.cs`
- Credential/keylogging source: `legacy/Quasar/Quasar.Client/Recovery/**`, `legacy/Quasar/Quasar.Client/Logging/**`
- Protocol DTOs: `legacy/Quasar/Quasar.Common/Messages/**/*.cs`

## Parity Summary

| Legacy area | Primary legacy messages/forms | Modern status | Gap to 1:1 parity |
| --- | --- | --- | --- |
| Client handshake and identification | `ClientIdentification`, `ClientIdentificationResult` | done | Non-loopback certificate/config story still belongs to product hardening, not protocol parity. |
| Client status updates | `SetStatus`, `SetUserStatus`, `ClientStatusHandler`, `FrmMain` status rows | done | Modern listener tracks both status text and active/idle user status by client id; CLI `clients` surfaces both values. GUI parity pending. |
| System information | `GetSystemInfo`, `FrmSystemInformation` | done | GUI parity pending; CLI/manual parity complete. |
| File manager browse/drives | `GetDrives`, `GetDirectory`, `FrmFileManager` | done | GUI parity pending; broader access-denied/path edge manual checks remain. |
| File transfer download/upload/cancel | `FileTransferRequest`, `FileTransferChunk`, `FileTransferComplete`, `FileTransferCancel` | done | GUI transfer queue/progress parity pending. |
| File rename/delete | `DoPathRename`, `DoPathDelete`, `SetStatusFileManager` | done | Directory delete needs a broader harmless manual pass; GUI parity pending. |
| File manager execute-as-startup action | `DoStartupItemAdd` sent from file manager | partial | Startup add exists; file-manager-specific UX/action path not rebuilt. |
| Task/process manager list/start/end | `GetProcesses`, `DoProcessStart`, `DoProcessEnd`, `DoProcessResponse`, `FrmTaskManager`, `FrmRemoteExecution` | partial | Local process start/end done; legacy URL download/start and update modes are intentionally not implemented yet and need provenance/consent design. GUI parity pending. |
| TCP connections list/close | `GetConnections`, `DoCloseConnection`, `FrmConnections` | partial | List done and manually verified; close dispatch exists but actual row deletion requires elevated Windows client manual verification. GUI parity pending. |
| Registry editor | Registry load/create/delete/rename/change DTOs, `FrmRegistryEditor`, value edit forms | partial | Runtime paths done; harmless mutation manual pass and GUI/value-editor parity pending. |
| Startup manager | `GetStartupItems`, `DoStartupItemAdd`, `DoStartupItemRemove`, `FrmStartupManager`, `FrmStartupAdd` | partial | Runtime paths done; harmless add/remove manual pass and GUI parity pending. |
| Remote shell | `DoShellExecute`, `DoShellExecuteResponse`, `FrmRemoteShell` | partial | Persistent shell dispatch done; shell transcript/UI behavior and broader harmless manual verification pending. |
| Remote desktop monitor/capture/input | `GetMonitors`, `GetDesktop`, `GetDesktopResponse`, `DoMouseEvent`, `DoKeyboardEvent`, `FrmRemoteDesktop` | partial | Monitor count, single-frame capture, input, reusable request-response frame streaming, and first WinForms live viewer done; input coordinate scaling from rendered frame, monitor refresh UX, visible active state, and richer session controls pending. |
| Message box | `DoShowMessageBox`, `FrmShowMessagebox` | partial | Dispatch done; visible desktop manual verification and GUI parity pending. |
| Website visitor | `DoVisitWebsite`, `FrmVisitWebsite` | partial | Dispatch done; browser/hidden GET manual verification and GUI parity pending. |
| Shutdown/restart/standby | `DoShutdownAction` | partial | Dispatch done; real power-state manual verification pending on prepared client. |
| Client lifecycle | `DoClientDisconnect`, `DoClientReconnect` | partial | Dispatch closes current session; legacy reconnect scheduling/backoff behavior still needs host parity. |
| Elevation request | `DoAskElevate` | partial | UAC dispatch done; interactive accept/cancel manual verification pending. |
| Client uninstall | `DoClientUninstall` | partial | Provider/CLI done; published executable manual self-delete verification pending. |
| Reverse proxy | `ReverseProxyConnect`, `ReverseProxyData`, `ReverseProxyDisconnect`, `ReverseProxyConnectResponse`, `FrmReverseProxy` | not-started | Needs explicit modern network-control design, operator permission, audit, allowlist/session controls, and tests. |
| Builder/client packaging | `FrmBuilder`, `FrmCertificate`, `ClientBuilder`, `Renamer`, client setup/update classes | not-started | Need modern transparent installer/package builder plan, signing/certificate handling, config injection, service/install/startup/update behavior. |
| Settings/about/operator shell | `FrmSettings`, `FrmAbout`, main server listener settings | not-started | Operator GUI/settings parity pending after Web API/core boundaries. |
| Webcam stop / capture-era control | `DoWebcamStop` | not-started | Only protocol-compatible today; no modern webcam capture feature exists. Needs consentful capture/session design before any runtime path. |
| Password recovery | `GetPasswords`, `GetPasswordsResponse`, `FrmPasswordRecovery`, `Recovery/**` | blocked-by-safety | Legacy behavior extracts browser/FTP credentials. Runtime implementation is blocked. Keep protocol accounting/tests and consider only consentful user-export/admin inventory alternatives. |
| Keylogger logs | `GetKeyloggerLogsDirectory`, `GetKeyloggerLogsDirectoryResponse`, `FrmKeylogger`, `Logging/**` | blocked-by-safety | Legacy behavior enables keystroke surveillance/log retrieval. Runtime implementation is blocked. Keep protocol accounting/tests and consider only consentful diagnostic logging alternatives. |

## Protocol DTO Accounting

Modern `MasterSplinter.Common` mirrors the legacy protocol DTO surface and has reflection plus
wire-compatibility coverage. Remaining protocol DTOs without modern runtime enablement are tracked
above: reverse proxy, webcam stop, password recovery, keylogger logs, and parts of build/install
behavior that are not represented as simple command DTOs.

## Highest-Value Remaining 1:1 Gaps

1. Remote desktop input coordinate scaling and richer operator session controls.
2. Reverse proxy with explicit allowlist/session/audit boundaries.
3. Builder/install/update/service parity as a transparent administrative installer flow.
4. Manual verification matrix for all implemented sensitive commands.
5. GUI/operator workflow parity, likely after Web API/shared server core surfaces are ready.
6. Safety-blocked legacy credential/keylogging features: keep accounted for, but do not implement runtime credential theft or keylogging behavior.

## Acceptance Rule

Before starting Web API as a product feature, this audit must have no unknown rows. A row may remain
`partial`, `not-started`, or `blocked-by-safety`, but it must be deliberate, documented, and tied to
a next action or safety boundary.
