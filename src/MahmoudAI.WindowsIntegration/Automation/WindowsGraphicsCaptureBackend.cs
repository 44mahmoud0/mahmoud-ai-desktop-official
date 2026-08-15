using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using Microsoft.Graphics.Canvas;
using WinRT;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace MahmoudAI.WindowsIntegration.Automation
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal sealed class WindowsGraphicsCaptureBackend : IScreenCaptureBackend, IDisposable
    {
        private const int FrameBufferCount = 2;
        private readonly CanvasDevice _device;
        private int _disposed;

        [ComImport]
        [Guid("36287B57-CB54-4B99-83B8-8143F75b49ef")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            void CreateForWindow(
                [In] IntPtr window,
                [In] ref Guid riid,
                [Out] out IntPtr result);
        }

        public WindowsGraphicsCaptureBackend()
        {
            if (!GraphicsCaptureSession.IsSupported())
            {
                throw new PlatformNotSupportedException("Windows.Graphics.Capture is not supported on this device.");
            }

            _device = CanvasDevice.GetSharedDevice();
        }

        public async Task<CapturedScreenFrame> CaptureAsync(
            ScreenCaptureRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ThrowIfDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Failure(ScreenCaptureStatus.Cancelled, "Screen capture was cancelled.");
            }

            if (request.Target.Kind != ScreenCaptureTargetKind.Window
                || request.Target.Hwnd is not nint hwnd
                || hwnd == nint.Zero)
            {
                return Failure(ScreenCaptureStatus.UnsupportedTarget, "Screen Capture supports only a non-zero window HWND target.");
            }

            if (request.Target.ProcessId is not int expectedProcessId || expectedProcessId <= 0)
            {
                return Failure(ScreenCaptureStatus.ProcessMismatch, "Screen Capture requires an explicit positive target process ID.");
            }

            if (!TryValidateTarget(hwnd, expectedProcessId, out var originX, out var originY, out _, out _, out var targetStatus, out var targetError))
            {
                return Failure(targetStatus, targetError);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Failure(ScreenCaptureStatus.Cancelled, "Screen capture was cancelled.");
            }

            GraphicsCaptureItem? item;
            try
            {
                var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
                var riid = typeof(GraphicsCaptureItem).GUID;
                interop.CreateForWindow(hwnd, ref riid, out var result);
                try
                {
                    item = Marshal.GetObjectForIUnknown(result) as GraphicsCaptureItem;
                }
                finally
                {
                    if (result != IntPtr.Zero)
                    {
                        Marshal.Release(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return Failure(ScreenCaptureStatus.ProviderError, $"GraphicsCaptureItem creation failed: {ex.Message}");
            }

            if (item is null)
            {
                return Failure(ScreenCaptureStatus.NotFound, "Windows.Graphics.Capture could not create an item for the target window.");
            }

            var itemSize = item.Size;
            if (itemSize.Width <= 0 || itemSize.Height <= 0)
            {
                return Failure(ScreenCaptureStatus.NotFound, "Target window has zero or negative capture dimensions.");
            }

            using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                FrameBufferCount,
                itemSize);

            using var session = framePool.CreateCaptureSession(item);
            
            // Guard IsCursorCaptureEnabled for Windows build >= 19041
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                session.IsCursorCaptureEnabled = request.IncludeCursor;
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            Direct3D11CaptureFrame frame;
            try
            {
                var frameTask = WaitForFrameAsync(framePool, linkedCts.Token);
                session.StartCapture();
                frame = await frameTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return Failure(ScreenCaptureStatus.Timeout, "Windows.Graphics.Capture frame acquisition timed out after 5 seconds.");
            }
            catch (OperationCanceledException)
            {
                return Failure(ScreenCaptureStatus.Cancelled, "Screen capture was cancelled.");
            }
            catch (Exception ex)
            {
                return Failure(ScreenCaptureStatus.ProviderError, $"Frame capture failed: {ex.Message}");
            }

            using (frame)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Failure(ScreenCaptureStatus.Cancelled, "Screen capture was cancelled.");
                }

                if (!TryValidateTarget(hwnd, expectedProcessId, out originX, out originY, out var dpiScaleX, out var dpiScaleY, out targetStatus, out targetError))
                {
                    return Failure(targetStatus, targetError);
                }

                var contentSize = frame.ContentSize;
                if (contentSize.Width <= 0 || contentSize.Height <= 0)
                {
                    return Failure(ScreenCaptureStatus.ProviderError, "Captured frame has invalid dimensions.");
                }

                using var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_device, frame.Surface);
                var pixelBytes = canvasBitmap.GetPixelBytes();

                int srcWidth = contentSize.Width;
                int srcHeight = contentSize.Height;
                int bytesPerPixel = 4;

                int cropX = 0;
                int cropY = 0;
                int cropWidth = srcWidth;
                int cropHeight = srcHeight;

                if (request.Region is ScreenCaptureRegion reqRegion)
                {
                    if (reqRegion.Width <= 0 || reqRegion.Height <= 0)
                    {
                        return Failure(ScreenCaptureStatus.UnsupportedTarget, "Requested capture region has non-positive width or height.");
                    }

                    // Check if region is completely outside source
                    if (reqRegion.X >= srcWidth || reqRegion.Y >= srcHeight || (reqRegion.X + reqRegion.Width) <= 0 || (reqRegion.Y + reqRegion.Height) <= 0)
                    {
                        return Failure(ScreenCaptureStatus.UnsupportedTarget, "Requested capture region is completely outside the source bounds.");
                    }

                    // Compute valid intersection
                    int intersectX = Math.Max(0, reqRegion.X);
                    int intersectY = Math.Max(0, reqRegion.Y);
                    int intersectRight = Math.Min(srcWidth, reqRegion.X + reqRegion.Width);
                    int intersectBottom = Math.Min(srcHeight, reqRegion.Y + reqRegion.Height);

                    cropX = intersectX;
                    cropY = intersectY;
                    cropWidth = intersectRight - intersectX;
                    cropHeight = intersectBottom - intersectY;

                    if (cropWidth <= 0 || cropHeight <= 0)
                    {
                        return Failure(ScreenCaptureStatus.UnsupportedTarget, "Computed region intersection has non-positive dimensions.");
                    }
                }

                // One-pass downscale computation
                double scaleX = 1.0;
                double scaleY = 1.0;

                if (request.MaxWidth is int mw && mw > 0 && cropWidth > mw)
                {
                    scaleX = (double)mw / cropWidth;
                }
                if (request.MaxHeight is int mh && mh > 0 && cropHeight > mh)
                {
                    scaleY = (double)mh / cropHeight;
                }
                double scale = Math.Min(scaleX, scaleY);

                int finalWidth = Math.Max(1, (int)Math.Round(cropWidth * scale));
                int finalHeight = Math.Max(1, (int)Math.Round(cropHeight * scale));

                // Buffer geometry safety check
                long expectedSourceBytes;
                try
                {
                    expectedSourceBytes = checked((long)srcWidth * srcHeight * bytesPerPixel);
                }
                catch (OverflowException)
                {
                    return Failure(ScreenCaptureStatus.ProviderError, "Source buffer geometry arithmetic overflow.");
                }

                if (pixelBytes.Length < expectedSourceBytes)
                {
                    return Failure(ScreenCaptureStatus.ProviderError, "Captured pixel buffer length is less than expected source geometry.");
                }

                byte[] processedBytes;
                try
                {
                    processedBytes = ExtractAndProcessRegion(
                        pixelBytes,
                        srcWidth,
                        srcHeight,
                        cropX,
                        cropY,
                        cropWidth,
                        cropHeight,
                        finalWidth,
                        finalHeight,
                        bytesPerPixel);
                }
                catch (Exception ex)
                {
                    return Failure(ScreenCaptureStatus.ProviderError, $"Region extraction and processing failed: {ex.Message}");
                }

                int stride = checked(finalWidth * bytesPerPixel);
                var frameId = Guid.NewGuid().ToString("N");
                var timestamp = DateTimeOffset.UtcNow;

                var metadata = new ScreenFrameMetadata(
                    frameId,
                    timestamp,
                    finalWidth,
                    finalHeight,
                    stride,
                    dpiScaleX,
                    dpiScaleY,
                    originX,
                    originY,
                    expectedProcessId,
                    hwnd);

                var actualRegionPx = new ScreenRect(cropX, cropY, cropWidth, cropHeight);
                var transform = WindowsGraphicsCaptureBackendTransformExtensions.CreateAuthoritativeTransform(
                    metadata,
                    srcWidth,
                    srcHeight,
                    actualRegionPx);

                return new CapturedScreenFrame(
                    ScreenCaptureStatus.Captured,
                    metadata,
                    processedBytes,
                    transform,
                    null);
            }
        }

        private static Task<Direct3D11CaptureFrame> WaitForFrameAsync(
            Direct3D11CaptureFramePool framePool,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration reg = default;

            void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
            {
                try
                {
                    var frame = sender.TryGetNextFrame();
                    if (frame != null)
                    {
                        if (tcs.TrySetResult(frame))
                        {
                            framePool.FrameArrived -= OnFrameArrived;
                            reg.Dispose();
                        }
                        else
                        {
                            frame.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (tcs.TrySetException(ex))
                    {
                        framePool.FrameArrived -= OnFrameArrived;
                        reg.Dispose();
                    }
                }
            }

            framePool.FrameArrived += OnFrameArrived;

            reg = cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled(cancellationToken))
                {
                    framePool.FrameArrived -= OnFrameArrived;
                    reg.Dispose();
                }
            });

            return tcs.Task;
        }

        private static bool TryValidateTarget(
            nint hwnd,
            int expectedProcessId,
            out int originX,
            out int originY,
            out float dpiScaleX,
            out float dpiScaleY,
            out ScreenCaptureStatus status,
            out string error)
        {
            originX = 0;
            originY = 0;
            dpiScaleX = 1.0f;
            dpiScaleY = 1.0f;
            status = ScreenCaptureStatus.NotFound;
            error = "Target window was not found.";

            if (hwnd == IntPtr.Zero || !CaptureNativeMethods.IsWindow(hwnd) || !CaptureNativeMethods.IsWindowVisible(hwnd))
            {
                return false;
            }

            uint actualProcessId = 0;
            uint pidResult = CaptureNativeMethods.GetWindowThreadProcessId(hwnd, out actualProcessId);
            if (pidResult == 0)
            {
                status = ScreenCaptureStatus.ProcessMismatch;
                error = "Failed to retrieve process ID for target window.";
                return false;
            }

            if (actualProcessId != (uint)expectedProcessId)
            {
                status = ScreenCaptureStatus.ProcessMismatch;
                error = $"Target process mismatch: expected {expectedProcessId}, actual {actualProcessId}.";
                return false;
            }

            if (!CaptureNativeMethods.GetWindowRect(hwnd, out var bounds))
            {
                status = ScreenCaptureStatus.NotFound;
                error = "Target window bounds could not be resolved.";
                return false;
            }

            if (bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top)
            {
                status = ScreenCaptureStatus.NotFound;
                error = "Target window has no visible bounds.";
                return false;
            }

            originX = bounds.Left;
            originY = bounds.Top;

            var dpi = CaptureNativeMethods.GetDpiForWindow(hwnd);
            if (dpi > 0)
            {
                dpiScaleX = dpi / 96.0f;
                dpiScaleY = dpi / 96.0f;
            }

            status = ScreenCaptureStatus.Captured;
            error = string.Empty;
            return true;
        }

        private static byte[] ExtractAndProcessRegion(
            byte[] source,
            int sourceWidth,
            int sourceHeight,
            int cropX,
            int cropY,
            int cropWidth,
            int cropHeight,
            int finalWidth,
            int finalHeight,
            int bytesPerPixel)
        {
            var sourceStride = checked(sourceWidth * bytesPerPixel);
            var croppedStride = checked(cropWidth * bytesPerPixel);
            var cropped = new byte[checked(croppedStride * cropHeight)];

            for (var row = 0; row < cropHeight; row++)
            {
                var srcRow = cropY + row;
                if (srcRow >= sourceHeight) break;
                var srcOffset = checked(srcRow * sourceStride + cropX * bytesPerPixel);
                var dstOffset = checked(row * croppedStride);
                Buffer.BlockCopy(source, srcOffset, cropped, dstOffset, Math.Min(croppedStride, source.Length - srcOffset));
            }

            if (cropWidth == finalWidth && cropHeight == finalHeight)
            {
                return cropped;
            }

            var finalStride = checked(finalWidth * bytesPerPixel);
            var final = new byte[checked(finalStride * finalHeight)];

            for (var y = 0; y < finalHeight; y++)
            {
                var srcY = (int)((double)y / finalHeight * cropHeight);
                srcY = Math.Clamp(srcY, 0, cropHeight - 1);
                var srcRowOffset = checked(srcY * croppedStride);
                var dstRowOffset = checked(y * finalStride);

                for (var x = 0; x < finalWidth; x++)
                {
                    var srcX = (int)((double)x / finalWidth * cropWidth);
                    srcX = Math.Clamp(srcX, 0, cropWidth - 1);
                    var srcPixelOffset = checked(srcRowOffset + srcX * bytesPerPixel);
                    var dstPixelOffset = checked(dstRowOffset + x * bytesPerPixel);

                    if (srcPixelOffset + bytesPerPixel <= cropped.Length && dstPixelOffset + bytesPerPixel <= final.Length)
                    {
                        final[dstPixelOffset] = cropped[srcPixelOffset];
                        final[dstPixelOffset + 1] = cropped[srcPixelOffset + 1];
                        final[dstPixelOffset + 2] = cropped[srcPixelOffset + 2];
                        final[dstPixelOffset + 3] = cropped[srcPixelOffset + 3];
                    }
                }
            }

            return final;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                // Device is shared CanvasDevice
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
        }

        private static CapturedScreenFrame Failure(ScreenCaptureStatus status, string error)
        {
            return new CapturedScreenFrame(status, null, null, null, error);
        }
    }

    internal static class CaptureNativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetDpiForWindow(IntPtr hWnd);
    }
}
