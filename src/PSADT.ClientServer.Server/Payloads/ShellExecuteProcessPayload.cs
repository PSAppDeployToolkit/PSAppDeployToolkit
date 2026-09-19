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
                throw new SerializationException($"The deserialized {nameof(ShellExecuteProcessPayload)} has no options.");
            }
        }
    }
}
