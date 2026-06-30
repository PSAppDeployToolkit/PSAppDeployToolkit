using System.Runtime.Serialization;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the GetProcessWindowInfo command.
    /// </summary>
    /// <param name="Options">The window info options.</param>
    [DataContract]
    internal sealed record class GetProcessWindowInfoPayload(WindowInfoOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The window info options.
        /// </summary>
        [DataMember]
        internal readonly WindowInfoOptions Options = Options;
    }
}
