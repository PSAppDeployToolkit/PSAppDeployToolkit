using System;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using PSADT.Trust;
using PSADT.PInvoke;
using PSADT.PathEx;
using PSADT.Impersonation;
using PSADT.PowerShellHost;
using PSADT.ConsoleEx;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents a secure named pipe server that supports impersonation and privilege management.
    /// </summary>
    internal class NamedPipeServer : IDisposable
    {
        private readonly NamedPipeServerOptions _options;
        private NamedPipeServerStream? _pipeStream;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the SecureNamedPipeServer class.
        /// </summary>
        /// <param name="options">The options for configuring the server.</param>
        public NamedPipeServer(NamedPipeServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates and starts listening on the named pipe.
        /// </summary>
        /// <exception cref="SecureNamedPipeException">Thrown when the pipe creation fails.</exception>
        public void Start()
        {
            try
            {
                _pipeStream = new NamedPipeServerStream(
                    _options.PipeName,
                    PipeDirection.InOut,
                    _options.MaxServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    _options.InBufferSize,
                    _options.OutBufferSize);

                _pipeStream.SetAccessControl(GetPipeSecurity());
            }
            catch (Exception ex)
            {
                throw new SecureNamedPipeException("Failed to create named pipe.", ex);
            }
        }

        /// <summary>
        /// Defines the security settings for the named pipe.
        /// </summary>
        /// <returns>A PipeSecurity object with the defined access rules.</returns>
        private PipeSecurity GetPipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();

            pipeSecurity.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));

            if (_options.AllowedUserSid == null)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule("Authenticated Users", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }
            else
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(_options.AllowedUserSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            return pipeSecurity;
        }

        /// <summary>
        /// Waits for a client connection asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for a connection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when the connection fails.</exception>
        public async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_pipeStream == null)
            {
                throw new SecureNamedPipeException("Named pipe has not been created or is invalid.");
            }

            using var cancellationRegistration = cancellationToken.Register(() => _pipeStream.Dispose());

            try
            {
                await _pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new SecureNamedPipeException("Waiting for connection was canceled.");
            }
            catch (Exception ex)
            {
                throw new SecureNamedPipeException("Failed to wait for connection.", ex);
            }

            ValidateClientProcess();
        }

        /// <summary>
        /// Waits for a client connection and reads data from the pipe asynchronously.
        /// </summary>
        /// <returns>The data read from the pipe.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when the connection fails or reading fails.</exception>
        public async Task<string> WaitForConnectionAndReadDataAsync(CancellationToken cancellationToken = default)
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
            {
                throw new SecureNamedPipeException("Named pipe is not connected.");
            }

            await _pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            using (var reader = new StreamReader(_pipeStream))
            {
                var messageBuilder = new StringBuilder();
                char[] buffer = new char[1024]; // Buffer size can be adjusted based on expected message size.

                do
                {
                    int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    messageBuilder.Append(buffer, 0, bytesRead);
                }
                while (!_pipeStream.IsMessageComplete); // Continue reading until the entire message is received.

                return messageBuilder.ToString();
            }
        }

        /// <summary>
        /// Writes data to the pipe asynchronously.
        /// </summary>
        /// <param name="data">The data to write to the pipe.</param>
        /// <exception cref="SecureNamedPipeException">Thrown when writing fails.</exception>
        public async Task WriteDataAsync(string data)
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
            {
                throw new SecureNamedPipeException("Named pipe is not connected.");
            }

            using (var writer = new StreamWriter(_pipeStream, _options.Encoding))
            {
                int chunkSize = _options.OutBufferSize;

                if (_options.UseByteArrayConversion)
                {
                    // Convert the data to a byte array using the specified encoding
                    byte[] dataBytes = _options.Encoding.GetBytes(data);
                    int totalBytes = dataBytes.Length;
                    int offset = 0;

                    // Write the data in chunks
                    while (offset < totalBytes)
                    {
                        int currentChunkSize = Math.Min(chunkSize, totalBytes - offset);
                        await _pipeStream.WriteAsync(dataBytes, offset, currentChunkSize).ConfigureAwait(false);
                        offset += currentChunkSize;
                    }
                }
                else
                {
                    // Use string slicing based on chunk size
                    int totalLength = data.Length;
                    int offset = 0;

                    while (offset < totalLength)
                    {
                        int currentChunkSize = Math.Min(chunkSize, totalLength - offset);
                        string chunkData = data.Substring(offset, currentChunkSize);

                        await writer.WriteAsync(chunkData).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);

                        offset += currentChunkSize;
                    }
                }

                await writer.FlushAsync().ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Validates the client process based on the server options.
        /// </summary>
        /// <exception cref="SecureNamedPipeException">Thrown when the client process is not allowed to connect.</exception>
        private void ValidateClientProcess()
        {
            if (!_options.RestrictToPowerShellOnly)
            {
                return;
            }

            var clientProcessId = GetClientProcessId();
            var clientProcess = System.Diagnostics.Process.GetProcessById(clientProcessId);

            if (!clientProcess.ProcessName.Equals("powershell", StringComparison.OrdinalIgnoreCase) &&
                !clientProcess.ProcessName.Equals("pwsh", StringComparison.OrdinalIgnoreCase))
            {
                throw new SecureNamedPipeException("Only PowerShell processes are allowed to connect.");
            }

            // Use the SignatureVerifier class to verify the digital signature of the file.
            using (var verifier = new DigitalSignature())
            {
                string filePath = PathHelper.ResolveExecutableFullPath(clientProcess.StartInfo.FileName)
                    ?? throw new SecureNamedPipeException("Failed to resolve executable path so that we could validate the client process is a PowerShell host.");

                var result = verifier.Verify(filePath);
                ConsoleHelper.DebugWrite($"Client process with path [{filePath}] is {(result.isAuthenticodeSigned ? "" : "not")} [AuthenticodeSigned].", MessageType.Debug);
                ConsoleHelper.DebugWrite($"Client process with path [{filePath}] is {(result.isCatalogSigned ? "" : "not")} [CatalogSigned].", MessageType.Debug);

                if (!result.isAuthenticodeSigned && !result.isCatalogSigned)
                {
                    throw new SecureNamedPipeException("Only PowerShell processes with a verified authenticode or catalog signature are allowed to connect.");
                }
            }
        }

        /// <summary>
        /// Gets the process ID of the connected client.
        /// </summary>
        /// <returns>The process ID of the connected client.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when unable to get the client process ID.</exception>
        private int GetClientProcessId()
        {
            if (_pipeStream == null)
            {
                throw new SecureNamedPipeException("Pipe stream is not initialized.");
            }

            if (!NativeMethods.GetNamedPipeClientProcessId(_pipeStream.SafePipeHandle.DangerousGetHandle(), out var clientProcessId))
            {
                throw new SecureNamedPipeException("Failed to get client process id.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            return (int)clientProcessId;
        }

        /// <summary>
        /// Impersonates the connected client.
        /// </summary>
        /// <returns>An Impersonator object that can be used to revert the impersonation.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when impersonation fails.</exception>
        public Impersonator ImpersonateClient()
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
            {
                throw new SecureNamedPipeException("Named pipe is not connected.");
            }

            Impersonator? impersonator = null;
            PSADTShell.ExecuteWithMTA(() =>
            {
                impersonator = new Impersonator(_options.ImpersonationOptions);
                impersonator.ImpersonateNamedPipeClient(_pipeStream.SafePipeHandle);
            });

            if (impersonator == null)
            {
                throw new SecureNamedPipeException("Failed to create impersonator.");
            }

            return impersonator;
        }

        /// <summary>
        /// Gets a stream for reading from and writing to the pipe.
        /// </summary>
        /// <returns>A PipeStream object for the connected pipe.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when the pipe is not connected.</exception>
        public PipeStream GetStream()
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
            {
                throw new SecureNamedPipeException("Named pipe is not connected.");
            }

            return _pipeStream;
        }

        public void Disconnect()
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
            {
                throw new InvalidOperationException("No connected client to disconnect.");
            }

            try
            {
                _pipeStream.Disconnect();
            }
            catch (Exception ex)
            {
                throw new SecureNamedPipeException("Failed to disconnect the named pipe.", ex);
            }
        }

        /// <summary>
        /// Disposes the SecureNamedPipeServer instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the SecureNamedPipeServer instance.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _pipeStream?.Dispose();
            }

            _isDisposed = true;
        }
    }
}
