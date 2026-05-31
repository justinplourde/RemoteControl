# Modernization Notes

## Current Direction

The modern .NET 10 work lives at the repository root in `src` and `tests`, with
`LocationRemote.sln` as the acceptance solution for portable code.

`MasterSplinter` remains the imported legacy application folder. Its `Quasar.sln`
now stays focused on the existing WinForms server, client, and legacy common
projects while the modern class libraries grow toward a runnable .NET 10 parity
implementation. Future root-level Web API, CLI, client UI, service, and cross-platform
projects should wait until that parity gate is met.

The modern .NET work starts with a small, safe shared core in `src/LocationRemote.Common`.
The legacy `Quasar.Common` project remains on `net452` while sensitive or Windows-specific
remote administration behavior is extracted deliberately.

## Modernized First

- Crypto helpers
- File and string helpers
- Basic protocol interfaces and handshake messages
- Additive protocol version and client capability metadata
- Payload length-prefix reader/writer
- Pure file-system models used by serialization tests
- Pure file-transfer message contracts
- Pure file-system protocol contracts for drive listing, directory listing, path rename/delete, and file-manager status
- Full legacy protocol DTO surface is present in `LocationRemote.Common`
- Reflection coverage verifies every modern message contract can payload round-trip through `IMessage`
- `LocationRemote.Client.Core` contains message dispatch contracts and typed routing infrastructure
- `LocationRemote.Client.Core` contains client identification factory support
- `LocationRemote.Client.Host` creates a modern identification payload through a runnable placeholder host
- `LocationRemote.Server.Core` contains session registry, command dispatch with correlation IDs, audit, and connection lifecycle contracts
- `LocationRemote.Server.Core` contains client identification handshake coordination with legacy ID validation and capability metadata preservation
- `LocationRemote.Server.Core` contains listener abstractions and a listener orchestrator for transport-independent parity work
- `LocationRemote.Server.Host` wires the modern server core into a runnable placeholder host with an idle listener for composition smoke testing

## Solutions

- `LocationRemote.sln`: modern root solution for portable class libraries and tests.
- `MasterSplinter/Quasar.sln`: legacy solution for the imported Windows desktop projects.

## Protocol Rules

- Existing protobuf field numbers are frozen once added.
- Versioning and capability metadata must be additive.
- Older payloads that omit protocol metadata must continue to deserialize safely.
- Unknown future fields must be tolerated by readers.
- Modernized contracts should gain pinned wire-compatibility fixtures before behavior is extracted.
- Full-surface fixtures pin one representative payload for every `IMessage` DTO.

## Deferred

- Remote desktop image compression and streaming
- Registry operations
- Process and shell execution behavior
- File-system access behavior; only DTO contracts are modernized so far
- Client command handlers; only dispatch routing infrastructure exists so far
- Client installation, startup, and service behavior
- Windows-specific platform helpers and native methods
- New roadmap features such as Web API, CLI, permissioned operators, consent UI,
  cross-platform expansion, and GUI overhaul until modern runtime parity is proven

## Rule Of Thumb

Move protocol contracts and pure data models first. Move behavior only after tests describe
its current observable contract and after platform-specific boundaries are clear. Build
new roadmap features only after the modern .NET runtime can mirror the selected legacy
client/server behavior with tests.
