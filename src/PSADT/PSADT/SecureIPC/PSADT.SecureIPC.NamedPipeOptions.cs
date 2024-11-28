using System;
using System.Text;
using System.IO.Pipes;
using System.Security.Principal;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents configuration options for named pipe communication.
    /// Provides various configurable parameters for both client and server implementations.
    /// </summary>
    public class NamedPipeOptions
    {
        /// <summary>
        /// Gets the name of the pipe.
        /// </summary>
        public string PipeName { get; }

        /// <summary>
        /// Gets the server name to connect to.
        /// Defaults to "." (local machine) if not specified.
        /// </summary>
        public string ServerName { get; }

        /// <summary>
        /// Gets the security identifier (SID) of the allowed user.
        /// If null, only Authenticated Users can connect to the named pipe.
        /// </summary>
        public SecurityIdentifier? AllowedUserSid { get; }

        /// <summary>
        /// Gets the maximum number of server instances.
        /// Defaults to 1.
        /// </summary>
        public int MaxNumberOfServerInstances { get; }

        /// <summary>
        /// Gets the buffer size threshold for chunking.
        /// </summary>
        public int PipeBufferSize { get; }

        /// <summary>
        /// Specifies the size of the chunks, in bytes, used when reading or writing data to the pipe.
        /// </summary>
        /// <remarks>
        /// Chunk size plays an important role in balancing memory usage and I/O performance. The ideal chunk size depends on the specific
        /// characteristics of the system, memory constraints, and the size of the data being transmitted. 
        /// 
        /// <para>Here are some common chunk sizes and general recommendations for selecting an appropriate value:</para>
        /// 
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>4 KB:</b> Suitable for memory-constrained environments or when dealing with a large number of small, individual messages.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>8 KB to 16 KB:</b> A balanced choice for typical message-based systems, offering a good trade-off between memory usage and I/O performance.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>32 KB:</b> Useful for larger message transfers with moderate memory requirements. Provides improved performance over smaller sizes.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>64 KB:</b> Aligns with the default buffer size for named pipes, making it ideal for large data transfers or continuous data streams.
        /// It reduces the number of read/write operations while maintaining high performance.
        /// </description>
        /// </item>
        /// </list>
        /// 
        /// <para>In general, it is recommended to start with a chunk size of 64 KB, as it matches the default buffer size for named pipes. 
        /// You can adjust the chunk size based on factors such as memory usage, transfer speed, and system load. 
        /// Smaller chunk sizes (e.g., 8 KB to 16 KB) may be more appropriate for systems that handle smaller messages or where memory is a concern.</para>
        /// 
        /// <para>Stick to chunk sizes that are powers of two (e.g., 4 KB, 8 KB, 16 KB, 32 KB, 64 KB) for efficiency in memory allocation and to align with system buffer sizes.</para>
        /// </remarks>
        public int PipeChunkSize { get; }

        /// <summary>
        /// Gets a value indicating whether to use chunked read operations when reading data from the pipe.
        /// The default is false.
        /// </summary>
        public bool UseChunkedRead { get; }

        /// <summary>
        /// Gets the timeout in milliseconds for connecting to the pipe server.
        /// </summary>
        public int ConnectTimeout { get; }

        /// <summary>
        /// Gets the timeout in milliseconds for writing to the pipe.
        /// </summary>
        public int WriteTimeout { get; }

        /// <summary>
        /// Gets the security options for the pipe.
        /// </summary>
        public PipeSecurityOptions? SecurityOptions { get; }

        /// <summary>
        /// Gets the encoding used for string communication through the pipe.
        /// Defaults to UTF-8 encoding.
        /// </summary>
        public Encoding Encoding { get; }

        /// <summary>
        /// Gets a value indicating whether client impersonation is allowed.
        /// </summary>
        public bool AllowImpersonation { get; }

        /// <summary>
        /// Gets a value indicating whether to verify that the connected client is a PowerShell process.
        /// </summary>
        public bool VerifyPowerShellClient { get; }

        /// <summary>
        /// Gets the maximum number of retry attempts when connecting to the server.
        /// Defaults to 3 retries.
        /// </summary>
        public int MaxRetryCount { get; }

        /// <summary>
        /// Gets the delay in milliseconds between retry attempts when connecting to the server.
        /// Defaults to 2000 milliseconds (2 seconds).
        /// </summary>
        public int RetryDelayMilliseconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeOptions"/> class using the specified builder.
        /// </summary>
        /// <param name="builder">The builder that contains the pipe configuration options.</param>
        private NamedPipeOptions(Builder builder)
        {
            PipeName = builder.PipeName;
            ServerName = builder.ServerName ?? ".";
            AllowedUserSid = builder.AllowedUserSid;
            MaxNumberOfServerInstances = builder.MaxNumberOfServerInstances;
            PipeBufferSize = builder.PipeBufferSize;
            PipeChunkSize = builder.PipeChunkSize;
            UseChunkedRead = builder.UseChunkedRead;
            ConnectTimeout = builder.ConnectTimeout;
            WriteTimeout = builder.WriteTimeout;
            SecurityOptions = builder.SecurityOptions;
            Encoding = builder.Encoding ?? Encoding.UTF8;
            AllowImpersonation = builder.AllowImpersonation;
            VerifyPowerShellClient = builder.VerifyPowerShellClient;
            MaxRetryCount = builder.MaxRetryCount;
            RetryDelayMilliseconds = builder.RetryDelayMilliseconds;
        }

        /// <summary>
        /// Builder class for creating <see cref="NamedPipeOptions"/> instances.
        /// Provides a fluent interface for configuring the pipe options.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Gets the name of the pipe.
            /// </summary>
            public string PipeName { get; }

            /// <summary>
            /// Gets or sets the server name to connect to.
            /// Defaults to "." (local machine).
            /// </summary>
            public string? ServerName { get; set; }

            /// <summary>
            /// Gets or sets the security identifier (SID) of the allowed user.
            /// </summary>
            public SecurityIdentifier? AllowedUserSid { get; set; }

            /// <summary>
            /// Gets or sets the maximum number of server instances.
            /// Defaults to 1.
            /// </summary>
            public int MaxNumberOfServerInstances { get; set; } = 1;

            /// <summary>
            /// Get or sets the buffer size threshold for chunking.
            /// The default buffer size for named pipes is typically 64 KB in Windows. 
            /// </summary>
            public int PipeBufferSize { get; set; } = 64 * 1024; // 64 KB

            /// <summary>
            /// Gets or sets the size of the chunks, in bytes, used when reading or writing data to the pipe.
            /// </summary>
            public int PipeChunkSize { get; set; } = 64;

            /// <summary>
            /// Gets or sets a value indicating whether to use chunked read operations when reading data from the pipe.
            /// The default is false.
            /// </summary>
            public bool UseChunkedRead { get; set; } = false;

            /// <summary>
            /// Gets or sets the timeout in milliseconds for connecting to the pipe server.
            /// Defaults to 5000 milliseconds (5 seconds).
            /// </summary>
            public int ConnectTimeout { get; set; } = 5000;

            /// <summary>
            /// Gets or sets the timeout in milliseconds for writing to the pipe.
            /// Defaults to 5000 milliseconds (5 seconds).
            /// </summary>
            public int WriteTimeout { get; set; } = 5000;

            /// <summary>
            /// Gets or sets the security options for the pipe.
            /// </summary>
            public PipeSecurityOptions? SecurityOptions { get; set; }

            /// <summary>
            /// Gets or sets the encoding used for string communication.
            /// Defaults to UTF-8 encoding.
            /// </summary>
            public Encoding? Encoding { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether client impersonation is allowed.
            /// Defaults to false.
            /// </summary>
            public bool AllowImpersonation { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether to verify that the connected client is a PowerShell process.
            /// Defaults to true.
            /// </summary>
            public bool VerifyPowerShellClient { get; set; } = true;

            /// <summary>
            /// Gets or sets the maximum number of retry attempts when connecting to the server.
            /// Defaults to 3 retries.
            /// </summary>
            public int MaxRetryCount { get; set; } = 3;

            /// <summary>
            /// Gets or sets the delay in milliseconds between retry attempts.
            /// Defaults to 2000 milliseconds (2 seconds).
            /// </summary>
            public int RetryDelayMilliseconds { get; set; } = 2000;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            /// <param name="pipeName">The name of the pipe.</param>
            public Builder(string pipeName)
            {
                PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            }

            /// <summary>
            /// Builds a new <see cref="NamedPipeOptions"/> instance with the specified configuration.
            /// </summary>
            /// <returns>A configured <see cref="NamedPipeOptions"/> instance.</returns>
            public NamedPipeOptions Build()
            {
                return new NamedPipeOptions(this);
            }
        }
    }

    /// <summary>
    /// Represents security configuration options for a named pipe.
    /// Defines additional settings like write-through behavior and asynchronous I/O operations.
    /// </summary>
    public class PipeSecurityOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the pipe should be created with write-through behavior.
        /// When enabled, the pipe bypasses system buffers to improve consistency.
        /// </summary>
        public bool UseWriteThrough { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pipe should allow asynchronous I/O operations.
        /// When enabled, operations like reading and writing can be performed asynchronously.
        /// </summary>
        public bool UseAsync { get; set; }

        /// <summary>
        /// Converts the security options to <see cref="PipeOptions"/> flags.
        /// </summary>
        /// <returns>The combined <see cref="PipeOptions"/> flags representing the security options.</returns>
        public PipeOptions AsPipeOptions()
        {
            PipeOptions options = PipeOptions.None;
            if (UseWriteThrough) options |= PipeOptions.WriteThrough;
            if (UseAsync) options |= PipeOptions.Asynchronous;
            return options;
        }
    }
}
