# Mahmoud AI — Windows Executable Packaging Guide

This guide details how to compile and package **Mahmoud AI** into a standalone Windows executable (`MahmoudAI.exe`) and installer package.

---

## Prerequisites

- Windows 11 (or Windows 10 x64)
- **.NET 10 SDK** installed (`dotnet --version`)
- Visual Studio 2022 (with Workloads: *Desktop development with C++*, *Universal Windows Platform development*, and *Windows App SDK C# templates*) if compiling the WinUI 3 shell.

---

## Building the Standalone Executable

To publish the core runtime and teamwork engine as a self-contained Windows executable:

```powershell
cd ma hmoud-ai/src/MahmoudAI.Core
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The resulting executable will be located at:
`src/MahmoudAI.Core/bin/Release/net10.0/win-x64/publish/MahmoudAI.Core.exe`

---

## Building via Visual Studio Solution

1. Open `mahmoud-ai/MahmoudAI.sln` in Visual Studio 2022.
2. Select **Release** configuration and **x64** platform.
3. Build the solution (`Ctrl + Shift + B`).
4. Run unit tests via Test Explorer to verify correctness.
