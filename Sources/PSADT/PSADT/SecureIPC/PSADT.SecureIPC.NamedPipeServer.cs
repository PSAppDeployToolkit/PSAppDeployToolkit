using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Security.AccessControl;
using PSADT.Shared;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents a secure named pipe server.
    /// Inherits common functionality from <see cref="NamedPipeBase"/> and handles server-specific operations.
    /// </summary>
    public class NamedPipeServer : NamedPipeBase
    {
        /// <summary>
        /// Event triggered when a client connects to the named pipe server.
        /// </summary>
        public event EventHandler<EventArgs>? Connected;

        /// <summary>
        /// Named pipe server stream for handling client connections.
        /// </summary>
        private readonly NamedPipeServerStream _serverPipeStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeServer"/> class with the specified options.
        /// </summary>
        /// <param name="options">The named pipe options, including pipe name and encoding.</param>
        public NamedPipeServer(NamedPipeOptions options, [PowerShellScriptBlock] Action<NamedPipeBase> asyncReader)
            : base(options, asyncReader)
        {
            try
            {
                _serverPipeStream = new NamedPipeServerStream(
                    options.PipeName,
                    PipeDirection.InOut,
                    options.MaxNumberOfServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous | (options.SecurityOptions?.AsPipeOptions() ?? PipeOptions.None));

                if (options.WriteTimeout > 0)
                {
                    _serverPipeStream.WriteTimeout = options.WriteTimeout;
                }

                _serverPipeStream.SetAccessControl(GetPipeSecurity());

                _pipeStream = _serverPipeStream;
            }
            catch (Exception ex)
            {
                throw new SecureNamedPipeException("Failed to initialize named pipe server.", ex);
            }
        }

        /// <summary>
        /// Waits for a client to connect to the pipe asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous wait for a client connection.</returns>
        public async Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            await _serverPipeStream.WaitForConnectionAsync(cancellationToken);
            Connected?.Invoke(this, EventArgs.Empty);

            if (_options.VerifyPowerShellClient && !IsPipeConnectedProcessPowerShell(_serverPipeStream.SafePipeHandle))
            {
                throw new SecureNamedPipeException("Connected client is not a PowerShell process.");
            }
        }

        /// <summary>
        /// Start reading data from the client asynchronously with the configured reader action.
        /// </summary>
        public void StartAsyncReaderAction()
        {
            // Begin reading data from the client after connection
            _asyncReader(this);
        }

        /// <summary>
        /// Configures the security settings for the named pipe.
        /// </summary>
        /// <returns>The pipe security settings.</returns>
        private PipeSecurity GetPipeSecurity()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();

            // Grant FullControl to SYSTEM
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            // Grant ReadWrite access to Authenticated Users
            SecurityIdentifier allowedUserSid = _options.AllowedUserSid ?? new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                allowedUserSid,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));

            // Deny access to Everyone
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Deny));

            return pipeSecurity;
        }

        /// <summary>
        /// Closes and disposes of the server pipe stream.
        /// </summary>
        public void Stop()
        {
            Close();
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="NamedPipeServer"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _serverPipeStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
