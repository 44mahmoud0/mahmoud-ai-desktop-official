using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace MahmoudAI.WindowsIntegration.Automation
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal sealed class WindowsGraphicsCaptureBackend : IScreenCaptureBackend, IDisposable
    {
        private readonly CanvasDevice _device;
        private int _disposed;

        [ComImport]
        [Guid("36287B57-CB54-4B99-83B8-8143F75b49ef")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow(
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

            if (request.Target.Kind != ScreenCaptureTargetKind.Window
                || request.Target.Hwnd is not nint hwnd
                || hwnd == nint.Zero)
            {
                return Failure(ScreenCaptureStatus.UnsupportedTarget, "Screen Capture supports only non-zero window HWND target.");
            }

            if (request.Target.ProcessId is not int expectedProcessId || expectedProcessId <= 0)
            {
                return Failure(ScreenCaptureStatus.ProcessMismatch, "Screen Capture requires an explicit positive target process ID.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Gate 4: Pre-capture target identity validation
            if (!TryValidateTarget(hwnd, expectedProcessId, out var originX, out var originY, out var dpiScaleX, out var dpiScaleY, out var status, out var error, out var windowWidth, out var windowHeight))
            {
                return Failure(status, error);
            }

            GraphicsCaptureItem item;
            try
            {
                item = CreateCaptureItemForWindow(hwnd);
                if (item == null || item.Size.Width <= 0 || item.Size.Height <= 0)
                {
                    return Failure(ScreenCaptureStatus.NotFound, "Failed to create a valid GraphicsCaptureItem for the specified window.");
                }
            }
            catch (Exception ex)
            {
                return Failure(ScreenCaptureStatus.ProviderError, $"Failed to initialize capture item: {ex.Message}");
            }

            var sourceWidth = item.Size.Width;
            var sourceHeight = item.Size.Height;

            Direct3D11CaptureFramePool framePool = null;
            GraphicsCaptureSession session = null;
            Direct3D11CaptureFrame capturedFrame = null;

            try
            {
                framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    _device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    item.Size);

                session = framePool.CreateCaptureSession(item);

                var frameTcs = new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
                
                TypedEventHandler<Direct3D11CaptureFramePool, object> handler = (sender, args) =>
                {
                    try
                    {
                        var frame = sender.TryGetNextFrame();
                        if (frame != null)
                        {
                            frameTcs.TrySetResult(frame);
                        }
                    }
                    catch (Exception ex)
                    {
                        frameTcs.TrySetException(ex);
                    }
                };

                framePool.FrameArrived += handler;

                try
                {
                    session.StartCapture();

                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    using (linkedCts.Token.Register(() => frameTcs.TrySetCanceled(linkedCts.Token)))
                    {
                        capturedFrame = await frameTcs.Task.ConfigureAwait(false);
                    }
                }
                finally
                {
                    framePool.FrameArrived -= handler;
                    try { session.Dispose(); } catch { }
                    try { framePool.Dispose(); } catch { }
                }

                if (capturedFrame == null)
                {
                    return Failure(ScreenCaptureStatus.Timeout, "Capture frame acquisition timed out or returned null.");
                }

                // Gate 4: Post-capture target identity validation
                if (!TryValidateTarget(hwnd, expectedProcessId, out _, out _, out _, out _, out var postStatus, out var postError, out _, out _))
                {
                    capturedFrame.Dispose();
                    return Failure(postStatus, $"Post-capture validation failed: {postError}");
                }

                using (capturedFrame)
                {
                    var contentSize = capturedFrame.ContentSize;
                    if (contentSize.Width <= 0 || contentSize.Height <= 0)
                    {
                        return Failure(ScreenCaptureStatus.ProviderError, "Captured frame has invalid dimensions.");
                    }

                    using var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_device, capturedFrame.Surface);
                    var pixelBytes = canvasBitmap.GetPixelBytes();

                    int srcWidth = contentSize.Width;
                    int srcHeight = contentSize.Height;
                    int bytesPerPixel = 4;

                    // Gate 8: Crop handling relative to source window
                    int cropX = 0;
                    int cropY = 0;
                    int cropWidth = srcWidth;
                    int cropHeight = srcHeight;

                    if (request.Region is ScreenRect reqRegion)
                    {
                        cropX = Math.Clamp(reqRegion.X, 0, srcWidth - 1);
                        cropY = Math.Clamp(reqRegion.Y, 0, srcHeight - 1);
                        cropWidth = Math.Clamp(reqRegion.Width, 1, srcWidth - cropX);
                        cropHeight = Math.Clamp(reqRegion.Height, 1, srcHeight - cropY);
                    }

                    // Gate 9: Downscale handling
                    int finalWidth = cropWidth;
                    int finalHeight = cropHeight;

                    if (request.MaxWidth is int maxWidth && maxWidth > 0 && finalWidth > maxWidth)
                    {
                        double ratio = (double)maxWidth / finalWidth;
                        finalWidth = maxWidth;
                        finalHeight = Math.Max(1, (int)(finalHeight * ratio));
                    }

                    if (request.MaxHeight is int maxHeight && maxHeight > 0 && finalHeight > maxHeight)
                    {
                        double ratio = (double)maxHeight / finalHeight;
                        finalHeight = maxHeight;
                        finalWidth = Math.Max(1, (int)(finalWidth * ratio));
                    }

                    var processedBytes = ExtractAndProcessRegion(
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
                        windowWidth,
                        windowHeight,
                        actualRegionPx);

                    return new CapturedScreenFrame(
                        ScreenCaptureStatus.Captured,
                        metadata,
                        processedBytes,
                        transform,
                        null);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Failure(ScreenCaptureStatus.Cancelled, "Screen capture was cancelled by caller.");
            }
            catch (OperationCanceledException)
            {
                return Failure(ScreenCaptureStatus.Timeout, "Screen capture timed out.");
            }
            catch (Exception ex)
            {
                return Failure(ScreenCaptureStatus.ProviderError, ex.Message);
            }
        }

        private static GraphicsCaptureItem CreateCaptureItemForWindow(IntPtr hwnd)
        {
            var interop = (IGraphicsCaptureItemInterop)Marshal.GetComObjectForData(
                typeof(IGraphicsCaptureItemInterop).GUID,
                typeof(GraphicsCaptureItem)); // fallback via activation factory if needed

            // Using direct interop activation
            var factory = Windows.Foundation.ActivationFactory.GetActivationFactory<GraphicsCaptureItem>();
            var interopFactory = (IGraphicsCaptureItemInterop)factory;
            var riid = typeof(GraphicsCaptureItem).GUID;
            interopFactory.CreateForWindow(hwnd, ref riid, out var result);
            try
            {
                return Marshal.GetObjectForIUnknown(result) as GraphicsCaptureItem;
            }
            finally
            {
                if (result != IntPtr.Zero)
                {
                    Marshal.Release(result);
                }
            }
        }

        private static bool TryValidateTarget(
            nint hwnd,
            int expectedProcessId,
            out int originX,
            out int originY,
            out float dpiScaleX,
            out float dpiScaleY,
            out ScreenCaptureStatus status,
            out string error,
            out int windowWidth,
            out int windowHeight)
        {
            originX = 0;
            originY = 0;
            dpiScaleX = 1.0f;
            dpiScaleY = 1.0f;
            windowWidth = 0;
            windowHeight = 0;
            status = ScreenCaptureStatus.NotFound;
            error = "Target window was not found.";

            var window = (HWND)hwnd;
            if (!PInvoke.IsWindow(window) || !PInvoke.IsWindowVisible(window))
            {
                return false;
            }

            PInvoke.GetWindowThreadProcessId(window, out var actualProcessId);
            if (actualProcessId != (uint)expectedProcessId)
            {
                status = ScreenCaptureStatus.ProcessMismatch;
                error = $"Target process mismatch: expected {expectedProcessId}, actual {actualProcessId}.";
                return false;
            }

            if (!PInvoke.GetWindowRect(window, out var bounds))
            {
                status = ScreenCaptureStatus.NotFound;
                error = "Target window bounds could not be resolved.";
                return false;
            }

            windowWidth = bounds.right - bounds.left;
            windowHeight = bounds.bottom - bounds.top;

            if (windowWidth <= 0 || windowHeight <= 0)
            {
                status = ScreenCaptureStatus.NotFound;
                error = "Target window has no visible bounds.";
                return false;
            }

            originX = bounds.left;
            originY = bounds.top;

            var dpi = PInvoke.GetDpiForWindow(window);
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
                // Device is shared CanvasDevice, disposed elsewhere or managed by Win2D
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
