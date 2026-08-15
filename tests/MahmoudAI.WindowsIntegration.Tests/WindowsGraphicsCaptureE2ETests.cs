using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using MahmoudAI.WindowsIntegration.Automation;
using Xunit;

namespace MahmoudAI.WindowsIntegration.Tests;

public class WindowsGraphicsCaptureE2ETests
{
    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RealTestHost_ReturnsRealPixels()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        Assert.NotEqual(IntPtr.Zero, host.Hwnd);
        Assert.True(host.ProcessId > 0);

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    Target:
                        new ScreenCaptureTarget(
                            ScreenCaptureTargetKind.Window,
                            host.Hwnd,
                            host.ProcessId),
                    Region: null,
                    MaxWidth: null,
                    MaxHeight: null,
                    IncludeCursor: false),
                CancellationToken.None);

        Assert.Equal(
            ScreenCaptureStatus.Captured,
            frame.Status);

        Assert.NotNull(frame.Metadata);
        Assert.NotNull(frame.PixelBuffer);
        Assert.NotNull(frame.Transform);

        var metadata = frame.Metadata!;
        var pixels = frame.PixelBuffer!;

        Assert.True(metadata.PixelWidth > 0);
        Assert.True(metadata.PixelHeight > 0);

        int minimumStride =
            checked(metadata.PixelWidth * 4);

        Assert.True(
            metadata.Stride >= minimumStride);

        long expectedMinimumLength =
            checked(
                (long)metadata.Stride *
                metadata.PixelHeight);

        Assert.True(
            pixels.LongLength >= expectedMinimumLength);

        Assert.Equal(
            host.ProcessId,
            metadata.SourceProcessId);

        Assert.Equal(
            host.Hwnd,
            metadata.SourceHwnd);

        Assert.False(
            pixels.All(b => b == 0),
            "Captured image is entirely zero.");

        Assert.True(
            CountDistinctSampledPixels(
                pixels,
                metadata.Stride,
                metadata.PixelWidth,
                metadata.PixelHeight) >= 8,
            "Captured buffer does not show enough pixel variation to prove real rendered content.");
    }

    [Theory]
    [Trait("Category", "WindowsE2E")]
    [InlineData(0, 0, 0, 100)]
    [InlineData(0, 0, 100, 0)]
    [InlineData(0, 0, -1, 100)]
    [InlineData(0, 0, 100, -1)]
    public async Task CaptureAsync_ValidTarget_InvalidRegion_IsRejected(
        int x,
        int y,
        int width,
        int height)
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        host.Hwnd,
                        host.ProcessId),
                    new ScreenCaptureRegion(
                        x,
                        y,
                        width,
                        height)),
                CancellationToken.None);

        Assert.Equal(
            ScreenCaptureStatus.UnsupportedTarget,
            frame.Status);

        Assert.Null(frame.PixelBuffer);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RealHwndWrongPid_ReturnsProcessMismatch()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        host.Hwnd,
                        host.ProcessId + 1)),
                CancellationToken.None);

        Assert.Equal(
            ScreenCaptureStatus.ProcessMismatch,
            frame.Status);

        Assert.Null(frame.PixelBuffer);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_ClosedTarget_DoesNotCapture()
    {
        var host =
            await WindowsTestHostFixture.StartAsync();

        var hwnd = host.Hwnd;
        var pid = host.ProcessId;

        await host.DisposeAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        hwnd,
                        pid)),
                CancellationToken.None);

        Assert.NotEqual(
            ScreenCaptureStatus.Captured,
            frame.Status);

        Assert.Null(frame.PixelBuffer);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RealTestHost_DownscalesWithoutUpscaling()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        host.Hwnd,
                        host.ProcessId),
                    MaxWidth: 320,
                    MaxHeight: 240),
                CancellationToken.None);

        Assert.Equal(
            ScreenCaptureStatus.Captured,
            frame.Status);

        Assert.NotNull(frame.Metadata);
        Assert.NotNull(frame.Transform);
        Assert.NotNull(frame.PixelBuffer);

        Assert.InRange(
            frame.Metadata!.PixelWidth,
            1,
            320);

        Assert.InRange(
            frame.Metadata.PixelHeight,
            1,
            240);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RealTargetCrop_ProducesAuthoritativeTransform()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        var region =
            new ScreenCaptureRegion(
                X: 20,
                Y: 20,
                Width: 200,
                Height: 120);

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        host.Hwnd,
                        host.ProcessId),
                    Region: region),
                CancellationToken.None);

        Assert.Equal(
            ScreenCaptureStatus.Captured,
            frame.Status);

        Assert.NotNull(frame.Transform);

        Assert.Equal(
            200,
            frame.Metadata!.PixelWidth);

        Assert.Equal(
            120,
            frame.Metadata.PixelHeight);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RepeatedRealCaptures_AllComplete()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        var frameIds =
            new HashSet<string>();

        for (int i = 0; i < 25; i++)
        {
            using var frame =
                await backend.CaptureAsync(
                    new ScreenCaptureRequest(
                        new ScreenCaptureTarget(
                            ScreenCaptureTargetKind.Window,
                            host.Hwnd,
                            host.ProcessId),
                        MaxWidth: 320,
                        MaxHeight: 240),
                    CancellationToken.None);

            Assert.Equal(
                ScreenCaptureStatus.Captured,
                frame.Status);

            Assert.NotNull(frame.Metadata);
            Assert.NotNull(frame.PixelBuffer);

            Assert.True(
                frameIds.Add(
                    frame.Metadata!.FrameId),
                "FrameId was unexpectedly reused.");
        }

        Assert.Equal(
            25,
            frameIds.Count);
    }

    [Fact]
    [Trait("Category", "WindowsE2E")]
    public async Task CaptureAsync_RealTarget_PreCancelled_ReturnsCancelled()
    {
        await using var host =
            await WindowsTestHostFixture.StartAsync();

        using var backend =
            new WindowsGraphicsCaptureBackend();

        using var cts =
            new CancellationTokenSource();

        cts.Cancel();

        using var frame =
            await backend.CaptureAsync(
                new ScreenCaptureRequest(
                    new ScreenCaptureTarget(
                        ScreenCaptureTargetKind.Window,
                        host.Hwnd,
                        host.ProcessId)),
                cts.Token);

        Assert.Equal(
            ScreenCaptureStatus.Cancelled,
            frame.Status);

        Assert.Null(frame.PixelBuffer);
    }

    private static int CountDistinctSampledPixels(
        byte[] pixels,
        int stride,
        int width,
        int height)
    {
        var distinct =
            new HashSet<uint>();

        int stepX = Math.Max(1, width / 16);
        int stepY = Math.Max(1, height / 16);

        for (int y = 0; y < height; y += stepY)
        {
            for (int x = 0; x < width; x += stepX)
            {
                int offset =
                    checked(y * stride + x * 4);

                if (offset + 3 >= pixels.Length)
                    continue;

                uint bgra =
                    pixels[offset]
                    | ((uint)pixels[offset + 1] << 8)
                    | ((uint)pixels[offset + 2] << 16)
                    | ((uint)pixels[offset + 3] << 24);

                distinct.Add(bgra);

                if (distinct.Count >= 8)
                    return distinct.Count;
            }
        }

        return distinct.Count;
    }
}
