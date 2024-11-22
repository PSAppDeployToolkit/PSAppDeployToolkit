using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Shared;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents a secure named pipe client.
    /// Inherits common functionality from <see cref="NamedPipeBase"/> and handles client-specific operations.
    /// </summary>
    public class NamedPipeClient : NamedPipeBase
    {
        /// <summary>
        /// Named pipe client stream for connecting to the named pipe server.
        /// </summary>
        private readonly NamedPipeClientStream _clientPipeStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeClient"/> class with the specified options.
        /// </summary>
        /// <param name="options">The named pipe options, including server name, pipe name, and encoding.</param>
        public NamedPipeClient(NamedPipeOptions options, [PowerShellScriptBlock] Action<NamedPipeBase> asyncReader)
            : base(options, asyncReader)
        {
            try
            {
                _clientPipeStream = new NamedPipeClientStream(
                    options.ServerName ?? ".",
                    options.PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | (options.SecurityOptions?.AsPipeOptions() ?? PipeOptions.None));
            }
            catch (Exception ex)
            {
                throw new SecureNamedPipeException("Failed to initialize named pipe client.", ex);
            }

            _pipeStream = _clientPipeStream;
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously, with retry logic.
        /// </summary>
        /// <returns>A task that represents the asynchronous connection to the server.</returns>
        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                int retryCount = 0;

                while (retryCount < _options.MaxRetryCount)
                {
                    try
                    {
                        await _clientPipeStream.ConnectAsync(_options.ConnectTimeout, cancellationToken);
                        _asyncReader(this);
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        retryCount++;
                        if (retryCount >= _options.MaxRetryCount)
                        {
                            throw new TimeoutException($"Failed to connect after {_options.MaxRetryCount} attempts.");
                        }

                        await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
                    }
                }
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        /// <summary>
        /// Closes and disposes of the client pipe stream.
        /// </summary>
        public void Disconnect()
        {
            Close();
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="NamedPipeClient"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _clientPipeStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
