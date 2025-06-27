#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This class is used to enable the use of init-only properties in C# 9.0 and later.
    /// https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
    /// https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif

#if !NETCOREAPP3_0_OR_GREATER
namespace System.IO.Pipes
{
    /// <summary>
    /// Provides methods for creating and configuring instances of <see cref="NamedPipeServerStream">  with advanced
    /// security and customization options.
    /// </summary>
    /// <remarks>This class is designed to simplify the creation of named pipe server streams with
    /// configurable  parameters such as direction, transmission mode, buffer sizes, and security settings. It is 
    /// particularly useful for scenarios requiring fine-grained control over pipe behavior and access 
    /// permissions.</remarks>
    internal static class NamedPipeServerStreamAcl
    {
        /// <summary>
        /// Creates a new instance of <see cref="NamedPipeServerStream"/> with the specified configuration.
        /// </summary>
        /// <remarks>This method provides a convenient way to create a named pipe server stream with
        /// customizable parameters. Use this method to configure the pipe's direction, transmission mode, buffer sizes,
        /// and security settings.</remarks>
        /// <param name="pipeName">The name of the pipe. This cannot be null or empty.</param>
        /// <param name="direction">The direction of the pipe, specifying whether data flows in, out, or both. The default is <see
        /// cref="PipeDirection.InOut"/>.</param>
        /// <param name="numberOfServerInstances">The maximum number of server instances that can simultaneously connect to the pipe. The default is 1.</param>
        /// <param name="transmissionMode">The transmission mode of the pipe, specifying whether data is transmitted as bytes or messages. The default
        /// is <see cref="PipeTransmissionMode.Byte"/>.</param>
        /// <param name="options">The options for the pipe, such as asynchronous operation or write-through. The default is <see
        /// cref="PipeOptions.None"/>.</param>
        /// <param name="inBufferSize">The size, in bytes, of the input buffer. The default is 0, which uses the system default buffer size.</param>
        /// <param name="outBufferSize">The size, in bytes, of the output buffer. The default is 0, which uses the system default buffer size.</param>
        /// <param name="pipeSecurity">The security settings for the pipe. If null, the default security settings are applied.</param>
        /// <returns>A <see cref="NamedPipeServerStream"/> instance configured with the specified parameters.</returns>
        internal static NamedPipeServerStream Create(
            string pipeName,
            PipeDirection direction = PipeDirection.InOut,
            int numberOfServerInstances = 1,
            PipeTransmissionMode transmissionMode = PipeTransmissionMode.Byte,
            PipeOptions options = PipeOptions.None,
            int inBufferSize = 0,
            int outBufferSize = 0,
            PipeSecurity? pipeSecurity = null)
        {
            return new NamedPipeServerStream(pipeName, direction, numberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, pipeSecurity);
        }
    }
}
#endif
