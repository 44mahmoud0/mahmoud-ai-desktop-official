using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using MahmoudAI.Core.Automation;
using MahmoudAI.Core.Integration;
using Microsoft.Extensions.Logging;

namespace MahmoudAI.WindowsIntegration.Automation
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal sealed class Win32AutomationBackend : IWindowsAutomationBackend
    {
        private readonly ILogger<Win32AutomationBackend> _logger;

        public Win32AutomationBackend(ILogger<Win32AutomationBackend> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<AutomationResult> ExecuteAsync(AutomationRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            return request.Operation switch
            {
                AutomationOperation.Keyboard => SendKeysAsync(request.Target, request.Payload ?? string.Empty, request.Context, cancellationToken),
                AutomationOperation.Pointer => ClickAsync(request.Target, request.Payload ?? string.Empty, request.Context, cancellationToken),
                _ => Task.FromResult(new AutomationResult(false, null, $"Unsupported Win32 operation: {request.Operation}"))
            };
        }

        private Task<AutomationResult> SendKeysAsync(string target, string keys, AutomationContext? context, CancellationToken cancellationToken)
        {
            var hwnd = ResolveWindow(target, context);
            if (hwnd == IntPtr.Zero)
            {
                return Task.FromResult(new AutomationResult(false, null, $"Target window '{target}' not found or out of context."));
            }

            try
            {
                NativeMethods.SetForegroundWindow(hwnd);
                Thread.Sleep(50);

                if (NativeMethods.GetForegroundWindow() != hwnd || !MatchesContext(hwnd, context))
                {
                    return Task.FromResult(new AutomationResult(false, null, "Foreground window changed before keyboard input."));
                }

                foreach (char c in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (NativeMethods.GetForegroundWindow() != hwnd)
                    {
                        return Task.FromResult(new AutomationResult(false, null, "Foreground window lost during keyboard input sequence."));
                    }

                    NativeMethods.keybd_event(0, (byte)c, 0, UIntPtr.Zero);
                    NativeMethods.keybd_event(0, (byte)c, 0x0002 /* KEYEVENTF_KEYUP */, UIntPtr.Zero);
                    Thread.Sleep(10);
                }

                return Task.FromResult(new AutomationResult(true, $"sent-chars:{keys.Length};hwnd:{hwnd.ToInt64()}"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AutomationResult(false, null, ex.Message));
            }
        }

        private Task<AutomationResult> ClickAsync(string target, string payload, AutomationContext? context, CancellationToken cancellationToken)
        {
            var parts = payload.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
            {
                return Task.FromResult(new AutomationResult(false, null, $"Invalid click payload: '{payload}'. Expected 'x,y'."));
            }

            var hwnd = ResolveWindow(target, context);
            if (hwnd == IntPtr.Zero)
            {
                return Task.FromResult(new AutomationResult(false, null, $"Target window '{target}' not found."));
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                NativeMethods.SetForegroundWindow(hwnd);
                Thread.Sleep(50);

                if (NativeMethods.GetForegroundWindow() != hwnd || !MatchesContext(hwnd, context))
                {
                    return Task.FromResult(new AutomationResult(false, null, "Foreground window changed before pointer input."));
                }

                NativeMethods.SetCursorPos(x, y);
                NativeMethods.mouse_event(0x0002 /* MOUSEEVENTF_LEFTDOWN */ | 0x0004 /* MOUSEEVENTF_LEFTUP */, (uint)x, (uint)y, 0, UIntPtr.Zero);

                return Task.FromResult(new AutomationResult(true, $"clicked;x:{x};y:{y};hwnd:{hwnd.ToInt64()}"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AutomationResult(false, null, ex.Message));
            }
        }

        private static nint ResolveWindow(string target, AutomationContext? context)
        {
            if (string.IsNullOrWhiteSpace(target) || target.Equals("foreground", StringComparison.OrdinalIgnoreCase))
            {
                var foreground = NativeMethods.GetForegroundWindow();
                return MatchesContext(foreground, context) ? foreground : IntPtr.Zero;
            }

            if (target.StartsWith("hwnd:", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(target[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var handle))
            {
                var window = (nint)handle;
                return NativeMethods.IsWindowVisible(window) && MatchesContext(window, context) ? window : IntPtr.Zero;
            }

            nint match = IntPtr.Zero;
            NativeMethods.EnumWindows((window, _) =>
            {
                if (!NativeMethods.IsWindowVisible(window) || !MatchesContext(window, context))
                {
                    return true;
                }

                var title = ReadWindowTitle(window);
                if (title.Equals(target, StringComparison.Ordinal))
                {
                    match = window;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return match;
        }

        private static bool MatchesContext(nint hwnd, AutomationContext? context)
        {
            if (context?.TargetProcessId is int pid && pid > 0)
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
                if ((int)processId != pid) return false;
            }
            return true;
        }

        private static string ReadWindowTitle(nint window)
        {
            int length = NativeMethods.GetWindowTextLength(window);
            if (length == 0) return string.Empty;

            var sb = new System.Text.StringBuilder(length + 1);
            NativeMethods.GetWindowText(window, sb, sb.Capacity);
            return sb.ToString();
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        internal static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }
}
