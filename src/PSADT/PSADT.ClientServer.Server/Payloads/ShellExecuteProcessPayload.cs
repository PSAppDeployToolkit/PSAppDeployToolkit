using System.Runtime.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShellExecuteProcess command.
    /// </summary>
    [DataContract]
    internal sealed record ShellExecuteProcessPayload : IClientServerPayload
    {
        /// <summary>
        /// The shell execute process options.
        /// </summary>
        [DataMember]
        internal readonly UserShellExecuteOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellExecuteProcessPayload"/> class.
        /// </summary>
        /// <param name="options">The shell execute process options.</param>
        internal ShellExecuteProcessPayload(UserShellExecuteOptions options)
        {
            Options = options;
        }
    }
}
