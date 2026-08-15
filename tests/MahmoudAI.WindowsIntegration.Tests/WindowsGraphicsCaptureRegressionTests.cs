using System;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using MahmoudAI.WindowsIntegration.Automation;
using Xunit;

namespace MahmoudAI.WindowsIntegration.Tests
{
    public class WindowsGraphicsCaptureRegressionTests
    {
        [Fact]
        public async Task CaptureAsync_StubBackend_ViolatesCaptureInvariants()
        {
            // Arrange
            using var backend = new WindowsGraphicsCaptureBackend();
            var request = new ScreenCaptureRequest(
                Target: new ScreenCaptureTarget(ScreenCaptureTargetKind.Window, Hwnd: (nint)12345, ProcessId: 9999),
                Region: null,
                MaxWidth: 1920,
                MaxHeight: 1080
            );

            // Act
            var frame = await backend.CaptureAsync(request, CancellationToken.None);

            // Assertions that prove the current stub implementation fails capture invariants
            if (frame.Status == ScreenCaptureStatus.Captured)
            {
                // The current stub violates these invariants:
                // 1. PixelBuffer must not be empty
                // 2. Transform must not be null
                Assert.NotNull(frame.PixelBuffer);
                Assert.True(frame.PixelBuffer.Length > 0, "PixelBuffer must contain captured pixel data.");
                Assert.NotNull(frame.Transform);
                Assert.NotNull(frame.Metadata);
                Assert.True(frame.Metadata.PixelWidth > 0);
                Assert.True(frame.Metadata.PixelHeight > 0);
                Assert.True(frame.Metadata.Stride >= frame.Metadata.PixelWidth * 4);
                Assert.True(frame.PixelBuffer.Length >= frame.Metadata.Stride * frame.Metadata.PixelHeight);
            }
            else
            {
                // If it returns non-captured status, that's also expected for a fake/invalid HWND in a real environment,
                // but the stub returns 'Captured' with empty pixels, which is the fake success bug.
                Assert.NotEqual(ScreenCaptureStatus.Captured, frame.Status);
            }
        }
    }
}
