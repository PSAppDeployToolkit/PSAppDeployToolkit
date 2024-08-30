using PSADT.Impersonation;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace PSADT.SecureIPC
{
    // <summary>
    /// Represents the options for configuring a secure named pipe server.
    /// </summary>
    public class NamedPipeServerOptions
    {
        /// <summary>
        /// Gets or sets the name of the pipe.
        /// </summary>
        public string PipeName { get; set; } = "SecureNamedPipe";

        /// <summary>
        /// Gets or sets the maximum number of server instances.
        /// </summary>
        public int MaxServerInstances { get; set; } = 1;

        /// <summary>
        /// Gets or sets the in buffer size for the pipe.
        /// </summary>
        public int InBufferSize { get; set; } = 65536;

        /// <summary>
        /// Gets or sets the out buffer size for the pipe.
        /// </summary>
        public int OutBufferSize { get; set; } = 65536;

        /// <summary>
        /// Gets or sets the encoding to use for string conversion. Default is UTF8.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets a value indicating whether to use byte array conversion or string slicing for
        /// writing data to the pipe. Default is false.
        /// </summary>
        public bool UseByteArrayConversion { get; set; } = false;

        /// <summary>
        /// Gets or sets the options for impersonation.
        /// </summary>
        public ImpersonateOptions ImpersonationOptions { get; set; } = new ImpersonateOptions();

        /// <summary>
        /// Gets or sets the security identifier (SID) of the allowed user.
        /// If null, any user can connect.
        /// </summary>
        public SecurityIdentifier? AllowedUserSid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to restrict connections to PowerShell processes only.
        /// </summary>
        public bool RestrictToPowerShellOnly { get; set; } = true;
    }
}
