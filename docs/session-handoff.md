# Session Handoff

Open a new Codex session in:

```powershell
C:\Users\Jplou\develop\RemoteControl
```

Then ask:

```text
Read docs/current-state.md, docs/roadmap-status.md, and docs/repository-layout.md, then continue the modernization plan.
```

Current checkpoint:

- Repo path: `C:\Users\Jplou\develop\RemoteControl`
- Main solution: `MasterSplinter.sln`
- Legacy reference: `legacy/Quasar`
- Latest committed work before this handoff: `Add shell execute CLI parity`
- Latest known full test result: 183 passed, 1 skipped, 0 failed

Primary verification command:

```powershell
dotnet test .\MasterSplinter.sln
```

Manual command-dispatch smoke check:

```powershell
dotnet run --no-launch-profile --project .\src\MasterSplinter.Cli\MasterSplinter.Cli.csproj -- listen --port 47841
dotnet run --no-launch-profile --project .\src\MasterSplinter.Client.Host\MasterSplinter.Client.Host.csproj -- --port 47841 --handle-commands
```

Supported CLI dispatch commands are `get-system-info`, `get-drives`, `get-directory --path <path>`,
`get-monitors`,
`get-registry-key --path <hive\subkey>`,
`registry-create-key --path <hive\parent-subkey>`,
`registry-delete-key --path <hive\parent-subkey> --name <child-key>`,
`registry-rename-key --path <hive\parent-subkey> --name <old-child-key> --new-name <new-child-key>`,
`registry-create-value --path <hive\key> --kind <kind>`,
`registry-delete-value --path <hive\key> --name <value-name>`,
`registry-rename-value --path <hive\key> --name <old-value-name> --new-name <new-value-name>`,
`registry-change-value --path <hive\key> --name <value-name> --kind <kind> --data <value>`,
`shell-execute --shell-command <command>`,
`download-file --path <remote-file> [--output <local-file>]`,
`upload-file --path <local-file> --remote-path <client-file>`,
`rename-path --path <client-old-path> --new-path <client-new-path> --type <file|directory>`,
`delete-path --path <client-path> --type <file|directory>`,
`start-process --path <client-file>`,
`end-process --pid <pid>`,
`ask-elevate`,
`shutdown-action --action <shutdown|restart|standby>`,
`disconnect-client`,
`reconnect-client`,
`uninstall-client`,
`show-message --text <message> [--caption <title>] [--button <button>] [--icon <icon>]`,
`visit-website --url <http-url> [--hidden]`,
`startup-add --name <name> --path <client-file> --startup-type <type>`,
`startup-remove --name <name> --startup-type <type>`,
`close-connection --local-address <ip> --local-port <port> --remote-address <ip> --remote-port <port>`,
`get-processes`, `get-startup-items`, and `get-connections`.

Monitor count parity is wired through `get-monitors`. It requires `--grant-permission
--grant-consent`, maps to `RemoteCapture`, returns `GetMonitorsResponse.Number`, and does not
start desktop image capture.

In the CLI listen prompt, the latest manual pass ran `clients`, `get-system-info`, `get-drives`,
`get-directory --path C:\`, `get-processes`, `get-startup-items`, and `get-connections` on one
persistent client connection. Results: 19 system-info rows, 1 drive, 24 directory entries,
302 processes, 5 startup items, and 50 TCP connections.

After system-info enrichment, the full read-only CLI parity pass was re-run on June 2, 2026.
`get-system-info` populated CPU, RAM, GPU, uptime, MAC, LAN IP, WAN IP, ASN, ISP, antivirus,
firewall, time zone, and country on this PC.

File download parity is now implemented for client-to-operator transfers. Manual loopback check:
`download-file` required `--grant-permission`, streamed a 35-byte temp file, wrote the explicit
local output path, and source/output SHA-256 hashes matched.

File upload parity is now implemented for operator-to-client transfers. Manual loopback check:
`upload-file` required `--grant-permission`, streamed a 34-byte temp file, wrote the requested
client path, and source/remote SHA-256 hashes matched.

File rename parity is now implemented. Manual loopback check: `rename-path` required
`--grant-permission`, renamed a 34-byte temp file, removed the old path, created the new path,
and preserved the SHA-256 hash.

File delete parity is now implemented for files and recursive directories. Manual loopback check:
`delete-path` required `--grant-permission`, deleted a temp file, and the path no longer existed.
Directory delete is covered by provider tests and should be manually verified with a harmless temp
folder.

Process end parity is now implemented. Manual loopback check: `end-process` required
`--grant-permission --grant-consent`, terminated a harmless spawned sleep process, returned
`Process response: Action=End; Result=True.`, and the PID was no longer alive afterward.

Local process start parity is now implemented. Manual loopback check: `start-process` required
`--grant-permission --grant-consent`, ran a harmless temp command script, returned
`Process response: Action=Start; Result=True.`, and the expected marker file existed afterward.
URL-download and update-style process start remain deferred until provenance and update policy
are defined.

Registry read parity is now implemented. Manual loopback check: `get-registry-key --path
HKCU\Software` required no permission or consent, returned `Safety=ReadOnlyInventory`, and loaded
17 child-key matches. Registry writes remain deferred until the permission/audit boundary is
defined.

Modern `src` and `tests` namespaces now use `MasterSplinter.Common.*` instead of
`Quasar.Common.*`. Any remaining `Quasar` references should be documentation about the preserved
legacy source under `legacy/Quasar`, not active modern code.

TCP connection close parity is now implemented as a permissioned dispatch path. Manual loopback
check: `close-connection` required `--grant-permission`, returned `Safety=NetworkControl`,
`RequiresPermission=True`, `RequiresConsent=False`, and produced a refreshed connection list.
Windows requires the client process to be elevated for actual TCP row deletion, matching legacy
behavior; `MasterSplinter.Client.Host` now has a `requireAdministrator` manifest for published
Windows executables.

Client elevation/admin status is now first-class in the modern identity path. The client host
sets `ClientIdentification.AccountType` to `Admin` or `User` using the current Windows principal,
and CLI `clients` output includes `AccountType=<value>`.

Elevation request parity is wired through `ask-elevate`. It requires `--grant-permission
--grant-consent`, maps `DoAskElevate` to `SystemControl`, calls Windows UAC `runas` in the client
host, and returns `SetStatus` messages for already elevated, requested, refused, or failed paths.
Automated tests cover the handler/status behavior; accepting or canceling the UAC prompt still
needs manual verification in an interactive Windows desktop session.

Shutdown/restart/standby parity is wired through `shutdown-action --action
<shutdown|restart|standby>`. It requires `--grant-permission --grant-consent`, maps
`DoShutdownAction` to `SystemControl`, and calls the Windows `shutdown`/suspend paths in the
client host. Automated tests cover status mapping only; real power-state verification should be
run only on a disposable or prepared Windows client.

Client lifecycle parity is wired through `disconnect-client` and `reconnect-client`. Both require
`--grant-permission`, map to `ConnectionLifecycle`, send a `SetStatus` acknowledgement, and close
the active loopback command session. Automatic reconnect scheduling remains future host behavior.

Client uninstall parity is wired through `uninstall-client`. It requires `--grant-permission
--grant-consent`, maps to `Persistence`, sends the legacy `Uninstalling... good bye :-(` status,
and launches a Windows self-delete batch before disconnecting. The provider intentionally refuses
`dotnet run` because that would target `dotnet.exe`; manual verification requires a disposable
published client executable.

Message box parity is wired through `show-message`. It requires `--grant-permission
--grant-consent`, maps to `UserInteraction`, supports legacy caption/text/button/icon fields, and
returns `Successfully displayed MessageBox`. Visible desktop display still needs manual
verification from an interactive Windows session.

Website visit parity is wired through `visit-website`. It requires `--grant-permission
--grant-consent`, maps to `UserInteraction`, supports visible browser launch and the legacy
`--hidden` GET path, and accepts only HTTP/HTTPS URLs after legacy-style `http://` prefixing.
Manual browser/hidden GET verification is still pending.

Startup add/remove parity is wired through `startup-add` and `startup-remove`. Both require
`--grant-permission --grant-consent`, map to `Persistence`, support legacy Run/RunOnce and
Startup-folder types, and should be manually verified with a harmless test entry that is removed
afterward.

Registry key create/delete/rename parity is wired through `registry-create-key`,
`registry-delete-key`, and `registry-rename-key`. They require `--grant-permission`, map to
`Persistence`, preserve legacy response DTOs and generated `New Key #n` naming, and should be
manually verified with a harmless `HKCU\Software` test key that is removed afterward.

Registry value create/delete/rename/change parity is wired through `registry-create-value`,
`registry-delete-value`, `registry-rename-value`, and `registry-change-value`. They require
`--grant-permission`, map to `Persistence`, preserve legacy response DTOs and generated
`New Value #n` naming, and should be manually verified with a harmless `HKCU\Software` test
key/value that is removed afterward. Supported CLI kinds are `string`, `expand-string`, `binary`,
`dword`, `qword`, and `multi-string`; binary data is hex and multi-string data uses `|`.

Shell execute parity is wired through `shell-execute --shell-command <command>`. It requires
`--grant-permission --grant-consent`, maps to `Execution`, returns `DoShellExecuteResponse`, and
should be manually verified with harmless commands only. The shell provider keeps a live shell
process across dispatches, so current directory/session state persists; send `exit` to close it.

Do not start Web API work until full legacy admin-tool parity for kept features is confirmed.
