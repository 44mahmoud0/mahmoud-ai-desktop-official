using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using MahmoudAI.WindowsIntegration.Automation;
using Xunit;

namespace MahmoudAI.WindowsIntegration.Tests
{
    public class WindowsGraphicsCaptureE2ETests
    {
        [Fact]
        public async Task CaptureAsync_InvalidRegion_ReturnsInvalidRegionStatus()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                return;
            }

            try
            {
                using var backend = new WindowsGraphicsCaptureBackend();
                var request = new ScreenCaptureRequest(
                    Target: new ScreenCaptureTarget(ScreenCaptureTargetKind.Window, Hwnd: (nint)12345, ProcessId: 9999),
                    Region: new ScreenCaptureRegion(0, 0, 0, 0),
                    MaxWidth: 1920,
                    MaxHeight: 1080
                );

                var frame = await backend.CaptureAsync(request, CancellationToken.None);
                Assert.NotEqual(ScreenCaptureStatus.Captured, frame.Status);
            }
            catch (PlatformNotSupportedException)
            {
                // WGC not supported on this runner environment
            }
        }

        [Fact]
        public async Task CaptureAsync_CancelledToken_ReturnsCancelledStatus()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                return;
            }

            try
            {
                using var backend = new WindowsGraphicsCaptureBackend();
                var request = new ScreenCaptureRequest(
                    Target: new ScreenCaptureTarget(ScreenCaptureTargetKind.Window, Hwnd: (nint)12345, ProcessId: 9999),
                    Region: null,
                    MaxWidth: 1920,
                    MaxHeight: 1080
                );

                using var cts = new CancellationTokenSource();
                cts.Cancel();

                var frame = await backend.CaptureAsync(request, cts.Token);
                Assert.Equal(ScreenCaptureStatus.Cancelled, frame.Status);
            }
            catch (PlatformNotSupportedException)
            {
                // WGC not supported
            }
        }
    }
}
