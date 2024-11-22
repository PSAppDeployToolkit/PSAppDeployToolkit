using System;
using System.Threading;
using PSADT.AccessToken;
using PSADT.PowerShellHost;

namespace PSADT.Shared
{
    /// <summary>
    /// Represents the context for executing commands and PowerShell scripts.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// The options for configuring PowerShell execution.
        /// </summary>
        public PSExecutionOptions PSOptions { get; set; }

        /// <summary>
        /// The impersonator object to be used for executing actions under a different security context.
        /// </summary>
        public ImpersonationManager? Impersonator { get; set; }

        /// <summary>
        /// The cancellation token to signal cancellation of operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public ExecutionContext(PSExecutionOptions psOptions, CancellationToken cancellationToken, ImpersonationManager? impersonator = null)
        {
            PSOptions = psOptions ?? throw new ArgumentNullException(nameof(psOptions));
            Impersonator = impersonator;
            CancellationToken = cancellationToken;
        }
    }
}
