# Modernization Notes

## Current Direction

The modern .NET work starts with a small, safe shared core in `LocationRemote.Common`.
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

## Protocol Rules

- Existing protobuf field numbers are frozen once added.
- Versioning and capability metadata must be additive.
- Older payloads that omit protocol metadata must continue to deserialize safely.
- Unknown future fields must be tolerated by readers.

## Deferred

- Remote desktop image compression and streaming
- Registry operations
- Process and shell execution behavior
- Client installation, startup, and service behavior
- Windows-specific platform helpers and native methods

## Rule Of Thumb

Move protocol contracts and pure data models first. Move behavior only after tests describe
its current observable contract and after platform-specific boundaries are clear.
