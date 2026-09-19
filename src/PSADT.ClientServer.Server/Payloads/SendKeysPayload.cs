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

        /// <summary>
        /// Confirms the payload's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor and assigns the members it
        /// finds, so a member the sender left out keeps its CLR default and arrives as nothing at all. Every
        /// member below is declared non-nullable, and this is the only thing holding it to that.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the payload arrived without its options.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Options is null)
            {
                throw new SerializationException($"The deserialized {nameof(SendKeysPayload)} has no options.");
            }
        }
    }
}
