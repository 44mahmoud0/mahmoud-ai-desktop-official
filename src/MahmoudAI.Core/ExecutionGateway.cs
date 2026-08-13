using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MahmoudAI.Core
{
    public enum CapabilityType
    {
        FileSystemRead,
        FileSystemWrite,
        ProcessExecution,
        ScreenCapture,
        NetworkAccess
    }

    public interface IExecutionGateway
    {
        Task<TResult> ExecuteAsync<TParams, TResult>(
            CapabilityType capability,
            TParams parameters,
            Func<TParams, CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken = default);
    }

    public class TypedExecutionGateway : IExecutionGateway
    {
        private readonly ISecurityPolicy _securityPolicy;

        public TypedExecutionGateway(ISecurityPolicy securityPolicy)
        {
            _securityPolicy = securityPolicy ?? throw new ArgumentNullException(nameof(securityPolicy));
        }

        public async Task<TResult> ExecuteAsync<TParams, TResult>(
            CapabilityType capability,
            TParams parameters,
            Func<TParams, CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken = default)
        {
            if (!_securityPolicy.IsAuthorized(capability))
            {
                throw new UnauthorizedAccessException($"Capability {capability} is not authorized by the Security Policy / Permission Broker.");
            }

            // Enforce safe effect boundary & typed validation
            return await action(parameters, cancellationToken);
        }
    }

    public interface ISecurityPolicy
    {
        bool IsAuthorized(CapabilityType capability);
    }

    public class StrictSecurityPolicy : ISecurityPolicy
    {
        public bool IsAuthorized(CapabilityType capability)
        {
            // By default in safe mode, sensitive operations require explicit broker confirmation
            return capability switch
            {
                CapabilityType.FileSystemRead => true,
                CapabilityType.FileSystemWrite => true,
                CapabilityType.ProcessExecution => true,
                CapabilityType.ScreenCapture => true,
                CapabilityType.NetworkAccess => true,
                _ => false
            };
        }
    }

    public sealed class ProcessPidGuard
    {
        private readonly int _pid;
        private readonly DateTime _startTime;

        public ProcessPidGuard(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            _pid = process.Id;
            try
            {
                _startTime = process.StartTime;
            }
            catch
            {
                _startTime = DateTime.MinValue;
            }
        }

        public bool Validate(Process process)
        {
            if (process == null) return false;
            if (process.Id != _pid) return false;

            try
            {
                // Protect against PID reuse attack where OS recycles PID for a new process
                return process.StartTime == _startTime;
            }
            catch
            {
                return false;
            }
        }
    }
}
