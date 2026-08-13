using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MahmoudAI.Core
{
    public interface ISafeEffectBoundary
    {
        string SandboxRoot { get; }
        string ValidateAndNormalizePath(string relativePath);
        Task<bool> VerifyExpectationAsync(Func<Task<bool>> expectationCheck);
    }

    public class SafeEffectBoundary : ISafeEffectBoundary
    {
        public string SandboxRoot { get; }

        public SafeEffectBoundary(string sandboxRoot)
        {
            SandboxRoot = Path.GetFullPath(sandboxRoot ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sandbox"));
            Directory.CreateDirectory(SandboxRoot);
        }

        public string ValidateAndNormalizePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(relativePath));

            var fullPath = Path.GetFullPath(Path.Combine(SandboxRoot, relativePath));
            if (!fullPath.StartsWith(SandboxRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Access denied: Path '{relativePath}' escapes the safe effect boundary.");
            }
            return fullPath;
        }

        public async Task<bool> VerifyExpectationAsync(Func<Task<bool>> expectationCheck)
        {
            if (expectationCheck == null) return true;
            try
            {
                return await expectationCheck();
            }
            catch
            {
                return false;
            }
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
}
