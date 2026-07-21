# Security Policy

## Supported Versions

| Version | Supported | End-of-life date |
| ------- | --------- | ---------------- |
| 1.x     | ❌ |
| 2.x     | ✅ | 2026-12-31 (at least) |
| 3.x     | ✅ | not yet determined |

Each supported major version is only serviced for security issues at its tip.
For example 2.5 will receive updates but 2.4 will not.
3.0 will receive updates until 3.1 is stable, at which point 3.0 will no longer received security updates.

## Strong-name key

This repository intentionally includes `opensource.snk`, the strong-name key used to provide stable assembly identity for open-source builds. This key is public by design and is not a credential or package-authenticity secret.

Do not rely on strong names as a security boundary or as proof that a package was published by the project maintainers. Consume packages from trusted package sources and use the normal NuGet and release provenance checks for package authenticity.

## Reporting a Vulnerability

Please use [the Security tab](https://github.com/MessagePack-CSharp/MessagePack-CSharp/security) to responsibly report security vulnerabilities.

Alternatively, email the project's main contributors with any vulnerability you become aware of.

    Yoshifumi Kawai <ils@neue.cc>; Andrew Arnott <andrewarnott@live.com>
