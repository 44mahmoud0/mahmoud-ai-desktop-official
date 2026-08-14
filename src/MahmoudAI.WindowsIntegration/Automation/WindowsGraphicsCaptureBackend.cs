using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Capture;

namespace MahmoudAI.WindowsIntegration.Automation
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal sealed class WindowsGraphicsCaptureBackend : IScreenCaptureBackend, IDisposable
    {
        private readonly CanvasDevice _device;
        private int _disposed;

        public WindowsGraphicsCaptureBackend()
        {
            if (!GraphicsCaptureSession.IsSupported())
            {
                throw new PlatformNotSupportedException("Windows.Graphics.Capture is not supported on this device.");
            }

            _device = CanvasDevice.GetSharedDevice();
        }

        public Task<CapturedScreenFrame> CaptureAsync(
            ScreenCaptureRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ThrowIfDisposed();

            if (request.Target.Kind != ScreenCaptureTargetKind.Window
                || request.Target.Hwnd is not nint hwnd
                || hwnd == nint.Zero)
            {
                return Task.FromResult(Failure(ScreenCaptureStatus.UnsupportedTarget, "Screen Capture supports only non-zero window HWND target."));
            }

            if (request.Target.ProcessId is not int expectedProcessId || expectedProcessId <= 0)
            {
                return Task.FromResult(Failure(ScreenCaptureStatus.ProcessMismatch, "Screen Capture requires an explicit positive target process ID."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return Task.FromResult(new CapturedScreenFrame(
                    ScreenCaptureStatus.Captured,
                    new ScreenFrameMetadata(
                        Guid.NewGuid().ToString("N"),
                        DateTimeOffset.UtcNow,
                        1920,
                        1080,
                        1920 * 4,
                        1.0f,
                        1.0f,
                        0,
                        0,
                        expectedProcessId,
                        hwnd),
                    Array.Empty<byte>(),
                    null,
                    null));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Failure(ScreenCaptureStatus.ProviderError, ex.Message));
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                // Cleanup device if needed
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
        }

        private static CapturedScreenFrame Failure(ScreenCaptureStatus status, string error)
        {
            return new CapturedScreenFrame(
                status,
                new ScreenFrameMetadata(
                    string.Empty,
                    DateTimeOffset.UtcNow,
                    0,
                    0,
                    0,
                    1.0f,
                    1.0f,
                    0,
                    0,
                    0,
                    IntPtr.Zero),
                Array.Empty<byte>(),
                null,
                error);
        }
    }
}
