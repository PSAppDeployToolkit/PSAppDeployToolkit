using System;
using System.IO;
using System.Linq;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Management.Automation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Trust;
using PSADT.PathEx;
using PSADT.PInvoke;
using PSADT.Shared;
using PSADT.Logging;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Abstract base class for secure named pipe communication.
    /// Provides common functionality for both server and client pipes, including asynchronous message handling.
    /// Implements IDisposable to ensure proper resource cleanup.
    /// </summary>
    public abstract class NamedPipeBase : IDisposable
    {
        /// <summary>
        /// Event triggered when a message is successfully received.
        /// </summary>
        public event EventHandler<PipeEventArgs>? MessageReceived;

        /// <summary>
        /// Event triggered when the pipe connection is closed.
        /// </summary>
        public event EventHandler? PipeClosed;

        /// <summary>
        /// The pipe stream used for communication.
        /// </summary>
        protected PipeStream? _pipeStream;

        /// <summary>
        /// The action to execute when a message is received.
        /// </summary>
        protected Action<NamedPipeBase> _asyncReader;

        /// <summary>
        /// The options used to configure the named pipe.
        /// </summary>
        protected NamedPipeOptions _options;

        /// <summary>
        /// Ensures thread safety when accessing or modifying shared resources.
        /// </summary>
        protected SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeBase"/> class.
        /// </summary>
        /// <param name="encoding">The encoding to use for string communication.</param>
        protected NamedPipeBase(NamedPipeOptions options, [PowerShellScriptBlock] Action<NamedPipeBase> asyncReader)
        {
            _options = options;

            // If the passed asyncReader is a ScriptBlock, wrap it in a delegate that invokes the ScriptBlock.
            if (asyncReader.Target is ScriptBlock scriptBlock)
            {
                asyncReader = pipeBase =>
                {
                    scriptBlock.Invoke(pipeBase);
                };
            }

            _asyncReader = asyncReader;
        }

        /// <summary>
        /// Starts asynchronously reading byte messages from the pipe.
        /// The first bytes read represent the size of the message.
        /// </summary>
        public void ReadBytesAsync(CancellationToken cancellationToken)
        {
            ReadBytesAsync((b) => MessageReceived?.Invoke(this, new PipeEventArgs(b, b.Length, _options.Encoding)), cancellationToken);
        }

        /// <summary>
        /// Starts asynchronously reading string messages from the pipe.
        /// The first bytes read represent the size of the message.
        /// </summary>
        public void ReadTextAsync(CancellationToken cancellationToken)
        {
            ReadBytesAsync((s) =>
            {
                string message = _options.Encoding.GetString(s).TrimEnd('\0');
                MessageReceived?.Invoke(this, new PipeEventArgs(message, _options.Encoding));
            }, cancellationToken);
        }

        /// <summary>
        /// Begins reading a message asynchronously, deciding whether to read it in chunks or in one go.
        /// </summary>
        /// <param name="processMessageMethod">The action to execute when the message is received.</param>
        /// <param name="cancellationToken">The cancellation token to observe during the operation.</param>
        public void ReadBytesAsync([PowerShellScriptBlock] Action<byte[]> processMessageMethod, CancellationToken cancellationToken)
        {
            int intSize = sizeof(int);
            byte[] lengthBuffer = new byte[intSize];

            _pipeStream!.ReadAsync(lengthBuffer, 0, intSize, cancellationToken).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;

                int totalMessageLength = BitConverter.ToInt32(lengthBuffer, 0);

                if (totalMessageLength == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Check if the provided method is a PowerShell ScriptBlock and wrap it accordingly
                    ScriptBlockWrapper? scriptBlockWrapper = null;
                    if (processMessageMethod.Target is ScriptBlock scriptBlock)
                    {
                        scriptBlockWrapper = new ScriptBlockWrapper(processMessageMethod, scriptBlock);
                        processMessageMethod = scriptBlockWrapper.GetWrappedActionDelegate();
                    }

                    // Decide whether to use chunked read or normal read based on message size
                    if (_options.UseChunkedRead == true && totalMessageLength > _options.PipeBufferSize)
                    {
                        // Use chunked reading
                        int remainingLength = totalMessageLength;
                        byte[] chunkBuffer = new byte[_options.PipeChunkSize];

                        while (remainingLength > 0)
                        {
                            int bytesToRead = Math.Min(_options.PipeChunkSize, remainingLength);
#if !CoreCLR
                            int bytesRead = await _pipeStream.ReadAsync(chunkBuffer, 0, bytesToRead, cancellationToken);
#else
                            int bytesRead = await _pipeStream.ReadAsync(chunkBuffer.AsMemory(0, bytesToRead), cancellationToken);
#endif

                            if (bytesRead == 0)
                            {
                                PipeClosed?.Invoke(this, EventArgs.Empty);
                                break;
                            }

                            // Invoke action with the received chunk
                            processMessageMethod(chunkBuffer.Take(bytesRead).ToArray());
                            remainingLength -= bytesRead;
                        }
                    }
                    else
                    {
                        // Read it in one go
                        byte[] fullBuffer = new byte[totalMessageLength];
#if !CoreCLR
                        await _pipeStream.ReadAsync(fullBuffer, 0, totalMessageLength, cancellationToken);
#else
                        await _pipeStream.ReadAsync(fullBuffer.AsMemory(0, totalMessageLength), cancellationToken);
#endif
                        processMessageMethod(fullBuffer);
                    }

                    // Continue reading for the next message
                    ReadBytesAsync(scriptBlockWrapper?.GetOriginalActionDelegate() ?? processMessageMethod, cancellationToken);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Appends the given message, encoded in the specified encoding, to the file, converting to UTF-8 if necessary.
        /// </summary>
        /// <param name="message">The message to append to the file, represented as a byte array.</param>
        /// <param name="filePath">The path of the file where the message will be appended.</param>
        /// <remarks>
        /// The message is converted to UTF-8 if the provided encoding differs from UTF-8.
        /// The file is opened in append mode, and UTF-8 encoding is used without a byte-order mark (BOM).
        /// If the file does not exist, it will be created.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="filePath"/> is null.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if access to the <paramref name="filePath"/> is denied.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs while writing to the file.</exception>
        public void ProcessMessageToFile(byte[] message, string filePath)
        {
            // Default to UTF-8 encoding if no encoding is provided
            System.Text.Encoding encodingToUse = _options.Encoding ?? System.Text.Encoding.UTF8;

            // Convert the message to a string using the provided or default encoding
            string messageAsString = encodingToUse.GetString(message);

            // If the message is not already UTF-8, convert it to UTF-8
            if (encodingToUse != System.Text.Encoding.UTF8)
            {
                byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(messageAsString);
                messageAsString = System.Text.Encoding.UTF8.GetString(utf8Bytes);
            }

            using var streamWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new System.Text.UTF8Encoding(false))
            {
                AutoFlush = true
            };

            streamWriter.Write(messageAsString);
        }

        /// <summary>
        /// Processes a pipe message by writing it to a memory stream.
        /// </summary>
        /// <param name="message">The message received from the pipe.</param>
        /// <param name="memoryStream">The memory stream where the message will be stored.</param>
        public void ProcessChunkToMemory(byte[] message, MemoryStream memoryStream)
        {
            // Append the message to the memory stream
            memoryStream.Write(message, 0, message.Length);
        }

        /// <summary>
        /// Writes a byte array message to the pipe stream asynchronously. 
        /// If the message size exceeds the configured pipe buffer size, it is written as a single message with a length prefix. 
        /// Otherwise, the message is written in chunks.
        /// </summary>
        /// <param name="bytes">The byte array to send.</param>
        /// <param name="cancellationToken">The cancellation token to observe for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method intelligently decides whether to write the message in chunks or in a single operation based on the size of the message:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>MessageText Exceeds Buffer Size:</b> If the message length exceeds the configured pipe buffer size, the message is prefixed with its length and written in a single asynchronous operation.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>MessageText Fits Within Buffer:</b> If the message length is smaller than or equal to the buffer size, the message is asynchronously written in smaller chunks to avoid overloading the buffer.
        /// </description>
        /// </item>
        /// </list>
        /// This behavior ensures that large messages do not overwhelm the pipe buffer, while smaller messages are written efficiently.
        /// </remarks>
        public Task WriteBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (bytes.Length > _options.PipeBufferSize)
            {
                byte[] lengthPrefix = BitConverter.GetBytes(bytes.Length);
                byte[] messageWithLength = lengthPrefix.Concat(bytes).ToArray();
                return _pipeStream!.WriteAsync(messageWithLength, 0, messageWithLength.Length, cancellationToken);
            }
            else
            {
                return WriteChunkedBytesAsync(bytes, cancellationToken);
            }
        }

        /// <summary>
        /// Writes a string message to the pipe stream asynchronously.
        /// </summary>
        /// <param name="message">The string message to send.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task WriteTextAsync(string message, CancellationToken cancellationToken)
        {
            byte[] messageBytes = _options.Encoding.GetBytes(message);
            return WriteBytesAsync(messageBytes, cancellationToken);
        }

        /// <summary>
        /// Writes a large byte array to the pipe stream asynchronously in smaller chunks.
        /// </summary>
        /// <param name="bytes">The byte array to send.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="cancellationToken">The cancellation token to observe for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method is useful for writing large messages in smaller chunks, rather than sending the entire message at once. 
        /// It first sends the total message length as a prefix and then writes the message in chunks of the specified size.
        /// 
        /// This approach reduces memory overhead when working with large data and ensures that the receiving end can process the data incrementally.
        /// 
        /// Example usage:
        /// <code>
        /// byte[] largeMessage = GetLargeMessage();
        /// await WriteChunkedBytesAsync(largeMessage, 1024, CancellationToken.None);
        /// </code>
        /// </remarks>
        public async Task WriteChunkedBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            byte[] lengthPrefix = BitConverter.GetBytes(bytes.Length);
#if !CoreCLR
            await _pipeStream!.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, cancellationToken);
#else
            await _pipeStream!.WriteAsync(lengthPrefix.AsMemory(0, lengthPrefix.Length), cancellationToken);
#endif

            int remainingBytes = bytes.Length;
            int currentOffset = 0;

            while (remainingBytes > 0)
            {
                int bytesToWrite = Math.Min(_options.PipeChunkSize, remainingBytes);
#if !CoreCLR
                await _pipeStream.WriteAsync(bytes, currentOffset, bytesToWrite, cancellationToken);
#else
                await _pipeStream.WriteAsync(bytes.AsMemory(currentOffset, bytesToWrite), cancellationToken);
#endif
                currentOffset += bytesToWrite;
                remainingBytes -= bytesToWrite;
            }

            await _pipeStream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Verifies if the connected pipe process is a PowerShell host.
        /// </summary>
        /// <param name="safePipeHandle">The safe pipe handle to get the process ID from.</param>
        /// <returns>True if the connected process is a PowerShell host, otherwise false.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when unable to verify the process.</exception>
        public static bool IsPipeConnectedProcessPowerShell(SafePipeHandle safePipeHandle)
        {
            int processId = GetPipeConnectedProcessId(safePipeHandle);
            return IsAuthenticPowerShellProcess(processId);
        }

        /// <summary>
        /// Gets the process ID of the connected pipe process.
        /// </summary>
        /// <param name="safePipeHandle">The safe pipe handle to get the process ID from.</param>
        /// <returns>The process ID of the connected pipe process.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when unable to get the client process ID.</exception>
        public static int GetPipeConnectedProcessId(SafePipeHandle safePipeHandle)
        {
            if (safePipeHandle.IsInvalid || safePipeHandle.IsClosed)
            {
                throw new SecureNamedPipeException("Pipe stream is not initialized.");
            }

            if (!NativeMethods.GetNamedPipeClientProcessId(safePipeHandle.DangerousGetHandle(), out var clientProcessId))
            {
                throw new SecureNamedPipeException("Failed to get client process ID.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            return (int)clientProcessId;
        }

        /// <summary>
        /// Verifies whether the specified process ID belongs to an authentic PowerShell host.
        /// </summary>
        /// <param name="processId">The process ID to verify.</param>
        /// <returns>True if the process is a PowerShell host, otherwise false.</returns>
        public static bool IsAuthenticPowerShellProcess(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                using (process)
                {
                    string processName = process.ProcessName.ToLowerInvariant();
                    if (processName != "powershell" && processName != "pwsh")
                    {
                        UnifiedLogger.Create().Message($"The pipe connected process is not [powershell] or [pwsh].").Severity(LogLevel.Debug);
                        return false;
                    }
                }

                // Use the SignatureVerifier class to verify the digital signature of the file.
                using DigitalSignature verifier = new DigitalSignature();
                string? filePath = PathHelper.ResolveExecutableFullPath(process.StartInfo.FileName);

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    UnifiedLogger.Create().Message($"Failed to resolve executable path so that we could validate the client process is a PowerShell host.").Severity(LogLevel.Debug);
                    return false;
                }

                SignatureVerification result = verifier.Verify(filePath!);
                UnifiedLogger.Create().Message($"Client process with path [{filePath}] is {(result.IsAuthenticodeSigned ? "" : "not")} [AuthenticodeSigned].").Severity(LogLevel.Debug);
                UnifiedLogger.Create().Message($"Client process with path [{filePath}] is {(result.IsCatalogSigned ? "" : "not")} [CatalogSigned].").Severity(LogLevel.Debug);

                if (!result.IsAuthenticodeSigned && !result.IsCatalogSigned)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to verify the connected process.").Error(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Asynchronously flushes the pipe stream, ensuring that any buffered data is immediately sent to the connected client or server.
        /// </summary>
        /// <remarks>
        /// In most asynchronous I/O scenarios, calling <see cref="FlushAsync"/> is not typically required because asynchronous write operations (such as <see cref="WriteAsync"/>) handle data transmission efficiently.
        /// However, there are certain situations where calling <see cref="FlushAsync"/> can be beneficial:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>After Writing Critical Data:</b> If you write data that must be processed immediately on the receiving end, calling <see cref="FlushAsync"/> ensures that the data is not buffered and is sent right away. This is useful in cases where the message is critical for synchronization or configuration.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>Low-Latency Communication:</b> In scenarios that require high-frequency message exchanges or where immediate feedback is necessary, calling <see cref="FlushAsync"/> ensures that data flows continuously without being delayed in a buffer.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>End of Transmission:</b> It's a good idea to call <see cref="FlushAsync"/> after all chunks have been written to ensure that any remaining buffered data is sent to the receiving end.
        /// </description>
        /// </item>
        /// </list>
        /// 
        /// <para>In fully asynchronous environments, it is often unnecessary to call <see cref="FlushAsync"/> after every write, as asynchronous write operations typically handle data transmission efficiently. Use it selectively when low-latency or critical data transmission is required.</para>
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the flush operation to complete.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_pipeStream != null)
            {
                await _pipeStream.FlushAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Closes and disposes of the pipe stream.
        /// </summary>
        public void Close()
        {
            _pipeStream?.WaitForPipeDrain();
            _pipeStream?.Dispose();
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="NamedPipeBase"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="NamedPipeBase"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphoreSlim?.Dispose();
                _pipeStream?.Dispose();
            }
        }
    }
}
