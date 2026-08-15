using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MahmoudAI.WindowsIntegration.Tests;

internal sealed class WindowsTestHostFixture : IAsyncDisposable
{
    public Process Process { get; private set; } = null!;

    public nint Hwnd => Process.MainWindowHandle;

    public int ProcessId => Process.Id;

    public static async Task<WindowsTestHostFixture> StartAsync(
        CancellationToken cancellationToken = default)
    {
        var fixture = new WindowsTestHostFixture();

        string executable = ResolveTestHostExecutable();

        if (!File.Exists(executable))
        {
            throw new FileNotFoundException(
                "Windows TestHost executable was not built.",
                executable);
        }

        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executable)!
            });

        if (process is null)
        {
            throw new InvalidOperationException(
                "Failed to launch WindowsIntegration.TestHost.");
        }

        fixture.Process = process;

        var deadline = DateTimeOffset.UtcNow.AddSeconds(20);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            process.Refresh();

            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"TestHost exited before window initialization. ExitCode={process.ExitCode}");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return fixture;
            }

            await Task.Delay(100, cancellationToken);
        }

        await fixture.DisposeAsync();

        throw new TimeoutException(
            "TestHost did not expose a real HWND within 20 seconds.");
    }

    private static string ResolveTestHostExecutable()
    {
        var repoRoot = FindRepositoryRoot();

        return Path.Combine(
            repoRoot,
            "tests",
            "MahmoudAI.WindowsIntegration.TestHost",
            "bin",
            "Release",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "MahmoudAI.WindowsIntegration.TestHost.exe");
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                Path.Combine(directory.FullName, "MahmoudAI.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not resolve repository root.");
    }

    public async ValueTask DisposeAsync()
    {
        if (Process is null)
            return;

        try
        {
            if (!Process.HasExited)
            {
                Process.CloseMainWindow();

                using var timeout =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(3));

                try
                {
                    await Process.WaitForExitAsync(
                        timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    Process.Kill(entireProcessTree: true);
                    await Process.WaitForExitAsync();
                }
            }
        }
        finally
        {
            Process.Dispose();
        }
    }
}
