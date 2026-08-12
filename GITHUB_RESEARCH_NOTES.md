# GitHub research notes — verified sources

## Microsoft Agent Framework

Source: https://github.com/microsoft/agent-framework

Verified from the repository page: Microsoft Agent Framework is an open multi-language framework for production-grade AI agents and multi-agent workflows in .NET and Python. Its .NET workflow material includes sequential, concurrent, handoff, and group-collaboration patterns, plus checkpointing, streaming, human-in-the-loop, and time-travel concepts. It also exposes provider integrations, middleware, declarative YAML agent definitions, skills/knowledge sources, and OpenTelemetry-oriented observability. The repository is MIT licensed according to its displayed license link.

Recommended use for Mahmoud AI: use the project as an architecture and implementation reference first; consider integrating its .NET packages only after aligning the project with a stable .NET SDK and confirming package versions. The highest-value patterns are workflow checkpoints, human approval gates, streaming, middleware, and OpenTelemetry. It should not replace the local task engine until compatibility and dependency size are verified.

## Ollama

Source: https://github.com/ollama/ollama

Verified from the repository page: Ollama provides an HTTP REST API for running and managing local models. The page shows a chat endpoint at `http://localhost:11434/api/chat` and identifies support for multiple current model families. The repository page also shows active Windows-related work and packaging/build concerns, making it a practical candidate for a local model provider behind a Mahmoud AI adapter.

Recommended use for Mahmoud AI: implement an `ILocalModelProvider` adapter around Ollama's REST API, with model discovery, health checks, cancellation, streaming, timeouts, and explicit user configuration. Do not hardcode a model name or assume Ollama is installed; treat it as an optional provider.

## Official C# MCP SDK

Source: https://github.com/modelcontextprotocol/csharp-sdk

Verified from the repository page: this is the official C# SDK for MCP servers and clients, maintained in collaboration with Microsoft. It publishes separate packages for low-level Core, the primary hosting/DI package, ASP.NET Core HTTP servers, MCP Apps interactive UI extensions, and MCP Tasks for long-running tool calls with status polling and input requests. The repository page states an Apache-2.0 license.

Recommended use for Mahmoud AI: replace the current hand-written MCP placeholder connector with the official SDK once package versions and target frameworks are pinned. Start with `ModelContextProtocol` / Core for client and local tool discovery, then add Tasks for long-running operations. Do not expose filesystem or shell tools without a permission policy and audit trail.

## WinUI Gallery

Source: https://github.com/microsoft/WinUI-Gallery

Verified from the repository page: WinUI Gallery is a Microsoft MIT-licensed WinUI 3 / Windows App SDK sample application that demonstrates controls, Fluent Design, responsive/adaptive UI, accessibility guidance, and unit/UI testing patterns. The repository currently uses a .NET 10-era solution format and documents Visual Studio plus Windows requirements for building and running.

Recommended use for Mahmoud AI: use WinUI Gallery as the reference implementation for NavigationView, adaptive layout, focus behavior, accessible keyboard navigation, Fluent styling, and test structure. Do not copy unrelated sample code wholesale; reuse the patterns and keep the product UI focused on missions, agents, approvals, memory, and diagnostics.

## LLamaSharp

Source: https://github.com/SciSharp/LLamaSharp

Verified from the repository page: LLamaSharp is a cross-platform C#/.NET library for running local LLaMA-family and related models through llama.cpp. It provides CPU, CUDA 11/12, Vulkan, and Metal backend packages, supports higher-level APIs and RAG, and documents optional Semantic Kernel and Kernel Memory packages. It uses GGUF model files and recommends quantized models for lower memory use. The displayed repository page is actively maintained and exposes a NuGet-based integration path.

Recommended use for Mahmoud AI: compare LLamaSharp against Ollama as the embedded local-inference option. LLamaSharp is attractive when Mahmoud AI must run without a separate local service, but its native backend packaging, model downloads, GPU detection, and licensing need to be isolated behind an `ILocalModelProvider` interface and tested per Windows architecture.

## Whisper.net

Source: https://github.com/sandrohanea/whisper.net

Verified from the repository page: Whisper.net provides .NET bindings for OpenAI Whisper via whisper.cpp, supports Windows-oriented solution files, native runtimes, CUDA runtime packages, speech recognition, translation, and voice activity detection. The repository includes model loader abstractions, VAD support, examples, and tests, and displays an MIT license.

Recommended use for Mahmoud AI: use it for local speech-to-text and push-to-talk workflows, with a model manager, microphone permission boundary, cancellation, VAD, and explicit device selection. Keep audio processing optional so the base desktop app remains usable without downloaded models.
