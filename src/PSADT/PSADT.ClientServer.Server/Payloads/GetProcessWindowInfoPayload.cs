using System.Runtime.Serialization;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the GetProcessWindowInfo command.
    /// </summary>
    [DataContract]
    internal sealed record GetProcessWindowInfoPayload : IClientServerPayload
    {
        /// <summary>
        /// The window info options.
        /// </summary>
        [DataMember]
        internal readonly WindowInfoOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetProcessWindowInfoPayload"/> class.
        /// </summary>
        /// <param name="options">The window info options.</param>
        internal GetProcessWindowInfoPayload(WindowInfoOptions options)
        {
            Options = options;
        }
    }
}
