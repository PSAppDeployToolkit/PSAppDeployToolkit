using System.Runtime.Serialization;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the SendKeys command.
    /// </summary>
    /// <param name="Options">The send keys options.</param>
    [DataContract]
    internal sealed record class SendKeysPayload(SendKeysOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The send keys options.
        /// </summary>
        [DataMember]
        internal readonly SendKeysOptions Options = Options;
    }
}
