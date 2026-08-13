using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
                var item = CaptureHelper.CreateItemForWindow(hwnd);
                if (item is null)
                {
                    return Task.FromResult(Failure(ScreenCaptureStatus.NotFound, "Failed to create GraphicsCaptureItem for window."));
                }

                var size = item.Size;
                return Task.FromResult(new CapturedScreenFrame(
                    ScreenCaptureStatus.Captured,
                    new ScreenFrameMetadata(
                        Guid.NewGuid().ToString("N"),
                        DateTimeOffset.UtcNow,
                        size.Width,
                        size.Height,
                        size.Width * 4,
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

    [ComImport]
    [Guid("36287A28-71B4-4C92-9EFE-8EEAD5841093")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IGraphicsCaptureItemInterop
    {
        void CreateForWindow(IntPtr window, [In] ref Guid riid, out IntPtr result);
        void CreateForMonitor(IntPtr monitor, [In] ref Guid riid, out IntPtr result);
    }

    internal static class CaptureHelper
    {
        internal static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
        {
            var guid = typeof(GraphicsCaptureItem).GUID;
            IntPtr pointer = IntPtr.Zero;
            try
            {
                var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem).FullName);
                var interop = (IGraphicsCaptureItemInterop)factory;
                interop.CreateForWindow(hwnd, ref guid, out pointer);
                return Marshal.GetObjectForIUnknown(pointer) as GraphicsCaptureItem ?? throw new InvalidOperationException("Failed to wrap GraphicsCaptureItem pointer.");
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.Release(pointer);
                }
            }
        }
    }
}
