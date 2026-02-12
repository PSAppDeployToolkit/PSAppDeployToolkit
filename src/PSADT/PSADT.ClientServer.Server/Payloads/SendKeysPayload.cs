using System.Runtime.Serialization;
using PSADT.Types;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the SendKeys command.
    /// </summary>
    [DataContract]
    internal sealed record SendKeysPayload : IClientServerPayload
    {
        /// <summary>
        /// The send keys options.
        /// </summary>
        [DataMember]
        internal readonly SendKeysOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendKeysPayload"/> class.
        /// </summary>
        /// <param name="options">The send keys options.</param>
        internal SendKeysPayload(SendKeysOptions options)
        {
            Options = options;
        }
    }
}
