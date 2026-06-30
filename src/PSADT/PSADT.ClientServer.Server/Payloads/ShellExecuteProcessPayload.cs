using System.Runtime.Serialization;
using PSADT.ProcessManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShellExecuteProcess command.
    /// </summary>
    /// <param name="Options">The shell execute process options.</param>
    [DataContract]
    internal sealed record class ShellExecuteProcessPayload(UserShellExecuteOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The shell execute process options.
        /// </summary>
        [DataMember]
        internal readonly UserShellExecuteOptions Options = Options;
    }
}
