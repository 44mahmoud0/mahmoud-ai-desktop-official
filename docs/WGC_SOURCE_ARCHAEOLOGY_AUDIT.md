# WGC Source Archaeology Audit Report

## 1. Executive Summary
This audit compares the current stubbed/fake implementation of `WindowsGraphicsCaptureBackend` on canonical `main` (`8654a444...`) with the historical trusted reference implementation found in commit `f2126e9e60602ea04f65a9a991a2d74d6925b82b`. The current stub returns hardcoded $1920 \times 1080$ dimensions with empty pixel buffers (`Array.Empty<byte>()`), representing a **fake success** regression that must be replaced with the genuine WinRT/Win32 Windows Graphics Capture (WGC) pipeline.

---

## 2. Comparative Analysis

| Dimension | Current Implementation (`main`) | Historical Reference (`f2126e9e`) | Architectural Verdict |
|---|---|---|---|
| **Capture API** | Stubbed (No API calls) | `IGraphicsCaptureItemInterop.CreateForWindow`, `Direct3D11CaptureFramePool`, `GraphicsCaptureSession` | **Must Restore Genuine WGC** |
| **Target Validation** | Basic HWND/PID existence checks | `PInvoke.IsWindow`, `PInvoke.IsWindowVisible`, `PInvoke.GetWindowThreadProcessId`, `PInvoke.GetWindowRect`, pre- and post-capture verification | **Must Reinstate Robust Identity Brokerage** |
| **Resource Ownership** | Shared `CanvasDevice` only | Explicit lifecycle for `CanvasDevice`, `Direct3D11CaptureFramePool`, `GraphicsCaptureSession`, frame event subscriptions, and `IDisposable` cleanup | **Must Enforce Deterministic Cleanup** |
| **Pixel Extraction** | `Array.Empty<byte>()` | GPU surface (`CanvasBitmap.CreateFromDirect3D11Surface`) $\rightarrow$ CPU staging $\rightarrow$ BGRA8 byte array via `GetPixelBytes()` | **Must Restore Real Pixel Extraction** |
| **Crop & Downscale** | None (Null transform) | `ExtractAndProcessRegion` supporting region-relative cropping and aspect-preserving downscaling | **Must Restore Spatial Normalization** |
| **Coordinate Transform** | `null` | `FrameCoordinateTransform` mapping physical pixels end-to-end | **Must Restore Authoritative Provenance** |

---

## 3. Key Risks, Race Windows & Hardening Guidelines
1. **Target Identity Race Window**: Between initial validation and frame arrival, the target window might close or change HWND owner. We must validate PID/HWND both before starting capture and immediately upon frame arrival.
2. **Event Subscription Leaks**: Frame pool events (`FrameArrived`) must be subscribed to exactly once and unsubscribed in a `finally` block to prevent handler leaks and memory growth.
3. **Integer Arithmetic Overflows**: Stride and allocation calculations must use `checked` arithmetic to prevent buffer overflows.
4. **Cancellation vs. Timeout**: Caller cancellation (`CancellationToken`) and acquisition timeout must be handled independently so that `OperationCanceledException` does not get masked as a `ProviderError`.
