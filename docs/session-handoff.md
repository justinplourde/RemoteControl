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
- Latest committed work before this handoff: `Add client lifecycle CLI parity`
- Latest known full test result: 166 passed, 1 skipped, 0 failed

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
`get-registry-key --path <hive\subkey>`,
`download-file --path <remote-file> [--output <local-file>]`,
`upload-file --path <local-file> --remote-path <client-file>`,
`rename-path --path <client-old-path> --new-path <client-new-path> --type <file|directory>`,
`delete-path --path <client-file> --type file`,
`start-process --path <client-file>`,
`end-process --pid <pid>`,
`ask-elevate`,
`shutdown-action --action <shutdown|restart|standby>`,
`disconnect-client`,
`reconnect-client`,
`close-connection --local-address <ip> --local-port <port> --remote-address <ip> --remote-port <port>`,
`get-processes`, `get-startup-items`, and `get-connections`.

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

File delete parity is now implemented for files only. Manual loopback check: `delete-path`
required `--grant-permission`, deleted a temp file, and the path no longer existed. Recursive
directory delete remains deferred until explicit policy is defined.

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

Do not start Web API work until full legacy admin-tool parity for kept features is confirmed.
