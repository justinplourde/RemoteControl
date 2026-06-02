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
- Latest committed work before this handoff: `Add process start CLI parity`
- Latest known full test result: 154 passed, 1 skipped, 0 failed

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
`download-file --path <remote-file> [--output <local-file>]`,
`upload-file --path <local-file> --remote-path <client-file>`,
`rename-path --path <client-old-path> --new-path <client-new-path> --type <file|directory>`,
`delete-path --path <client-file> --type file`,
`start-process --path <client-file>`,
`end-process --pid <pid>`,
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

Do not start Web API work until full legacy admin-tool parity for kept features is confirmed.
