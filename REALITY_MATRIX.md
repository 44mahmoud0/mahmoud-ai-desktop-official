# Mahmoud AI — Reality Matrix & Architecture Roadmap

This document establishes the canonical reality audit and architectural inventory for the **Mahmoud AI** native Windows desktop AI platform, transitioning the repository from a web magazine foundation into a professional .NET 10 LTS WinUI 3 / Windows App SDK multi-agent workspace.

---

## 1. Executive Classification

| Feature Category | Current State | Target Architecture | Key Dependencies | Security / Safety |
| :--- | :--- | :--- | :--- | :--- |
| **Core Runtime** | **Infrastructure Ready** | .NET 10 LTS Generic Host, DI, CancellationToken propagation | Microsoft.Extensions.Hosting | Isolated dependency container, secure secret storage |
| **Mission System** | **Planned / Implemented** | State-driven Mission graphs, checkpoints, deterministic cancellation | MahmoudAI.Core, MahmoudAI.Runtime | Explicit user approval for destructive tasks |
| **Task Graph Engine** | **Planned / Implemented** | Directed Acyclic Graph (DAG) for concurrent & sequential subtasks | MahmoudAI.Core, System.Threading.Channels | Timeout and retry safety bounds |
| **Multi-Agent Teamwork** | **Infrastructure Ready** | Manager, Planner, Research, Coding, Vision, Tool, Memory, Verifier agents | MahmoudAI.Teamwork, MahmoudAI.Agents | Role isolation and policy guardrails |
| **Windows Desktop Shell** | **Infrastructure Ready** | WinUI 3 / Windows App SDK native application (`MahmoudAI.exe`) | Microsoft.WindowsAppSDK | Windows AppContainer isolation, DPAPI secrets |
| **Packaging & CI** | **Infrastructure Ready** | dotnet publish, MSIX installer, GitHub Actions CI workflow | .NET 10 SDK, MSBuild | Code-signing stub configuration |

---

## 2. Target Directory Structure

```
MahmoudAI/
├── src/
│   ├── MahmoudAI.App          # WinUI 3 Desktop App Shell
│   ├── MahmoudAI.Core         # Common abstractions, results, domain types
│   ├── MahmoudAI.Contracts    # Public interfaces & DTOs
│   ├── MahmoudAI.Runtime      # Host, DI, lifecycle, configuration
│   ├── MahmoudAI.Teamwork     # Multi-agent coordination & workflows
│   ├── MahmoudAI.Agents       # Specialized agent implementations
│   ├── MahmoudAI.Reasoning    # LLM integration & prompt orchestration
│   ├── MahmoudAI.Memory       # Local vector & episodic memory stores
│   ├── MahmoudAI.Models       # Local and cloud model connectors
│   ├── MahmoudAI.Tools        # Native tool registry & execution sandbox
│   ├── MahmoudAI.Plugins      # Dynamic plugin loader
│   ├── MahmoudAI.Mcp          # Model Context Protocol client support
│   ├── MahmoudAI.Vision       # OCR & screen understanding
│   ├── MahmoudAI.Voice        # Speech-to-text / text-to-speech stubs
│   ├── MahmoudAI.Automation   # Windows desktop automation & UI scripting
│   ├── MahmoudAI.Windows      # Win32 & Windows App SDK platform interop
│   ├── MahmoudAI.Security     # DPAPI, permissions, and policy guardrails
│   ├── MahmoudAI.Storage      # LiteDB / SQLite local state persistence
│   ├── MahmoudAI.Telemetry    # Structured logging & diagnostics
│   └── MahmoudAI.Workers      # Background task queues & schedulers
│
├── tests/
│   ├── MahmoudAI.Core.Tests
│   ├── MahmoudAI.Memory.Tests
│   ├── MahmoudAI.Teamwork.Tests
│   ├── MahmoudAI.Security.Tests
│   └── MahmoudAI.IntegrationTests
│
├── tools/
├── docs/
└── .github/workflows/
```
