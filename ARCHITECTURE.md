# Mahmoud AI — System Architecture Specification (.NET 10 LTS WinUI 3)

## 1. Overview

**Mahmoud AI** is designed as a native Windows 11 AI operating workspace built on **.NET 10 LTS** and **WinUI 3 / Windows App SDK**. It provides a robust, modular multi-agent architecture capable of autonomous mission planning, task graph execution, secure credential management via Windows DPAPI, and local-first memory persistence.

---

## 2. Core Architectural Pillars

1. **Native Windows Experience**: Built with WinUI 3 for hardware-accelerated fluid UI, native window chrome, and seamless Windows 11 Mica/Acrylic styling.
2. **Modular .NET 10 Solution**: Strict separation of concerns across core runtimes, agents, teamwork coordination, security boundaries, and tool integrations.
3. **Deterministic Mission & Task Graph Engine**: Missions transition through explicit lifecycle states (`Created`, `Planning`, `WaitingForApproval`, `Running`, `Completed`, `Failed`) powered by asynchronous task graphs with dependency resolution, retry policies, and `CancellationToken` support.
4. **Multi-Agent Teamwork**: Coordinated specialist roles (Manager, Planner, Research, Coding, Vision, Tool, Memory, Verifier, Safety) supporting sequential, parallel, handoff, and reviewer workflows.
5. **Secure Local-First Storage**: Local state and memory backed by SQLite/LiteDB with encrypted credentials protected via Windows Data Protection API (DPAPI).

---

## 3. Technology Stack

- **Platform**: Windows 11 (x64 / ARM64)
- **Runtime**: .NET 10 LTS
- **UI Framework**: WinUI 3 (Windows App SDK 1.6+)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog / Microsoft.Extensions.Logging
- **Testing**: xUnit, FluentAssertions, Moq
- **Packaging**: .NET CLI publishing (`dotnet publish -c Release -r win-x64 --self-contained true`) and MSIX packaging path.
