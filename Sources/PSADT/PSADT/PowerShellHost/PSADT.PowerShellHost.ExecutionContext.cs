using System;
using System.Threading;
using PSADT.Impersonation;

namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Represents the context for executing PowerShell scripts.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// The options for configuring PowerShell execution.
        /// </summary>
        public PSExecutionOptions Options { get; set; }

        /// <summary>
        /// The impersonator object to be used for executing actions under a different security context.
        /// </summary>
        public Impersonator? Impersonator { get; set; }

        /// <summary>
        /// The cancellation token to signal cancellation of operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public ExecutionContext(PSExecutionOptions options, CancellationToken cancellationToken, Impersonator? impersonator = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Impersonator = impersonator;
            CancellationToken = cancellationToken;
        }
    }
}
